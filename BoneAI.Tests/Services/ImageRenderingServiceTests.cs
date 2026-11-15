using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Infrastructure.Services;
using BoneAI.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BoneAI.Tests.Services;

public class ImageRenderingServiceTests
{
    private readonly Mock<IAnalysisRunRepository> _runRepoMock;
    private readonly Mock<IImageStorageService> _storageServiceMock;
    private readonly Mock<ILogger<ImageRenderingService>> _loggerMock;
    private readonly ImageRenderingService _service;

    public ImageRenderingServiceTests()
    {
        _runRepoMock = new Mock<IAnalysisRunRepository>();
        _storageServiceMock = new Mock<IImageStorageService>();
        _loggerMock = new Mock<ILogger<ImageRenderingService>>();

        _service = new ImageRenderingService(_runRepoMock.Object, _storageServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task RenderAndSaveRunImage_ValidRunAndDetections_RendersAndSavesImage()
    {
        var runId = Guid.NewGuid();
        var origBytes = TestImageGenerator.CreateValidPng(200, 200);

        var run = new AnalysisRun
        {
            Id = runId,
            Status = AnalysisRunStatus.Completed,
            Analysis = new Analysis
            {
                XRayImage = new XRayImage
                {
                    Id = Guid.NewGuid(),
                    FilePath = "images/orig.png",
                    FileName = "orig.png"
                }
            },
            Detections = new List<Detection>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ClassName = "fracture",
                    Confidence = 0.95m,
                    X = 20,
                    Y = 30,
                    Width = 80,
                    Height = 60
                }
            }
        };

        _runRepoMock.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        _storageServiceMock.Setup(s => s.GetAsync("images/orig.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(origBytes));

        string? capturedSavedFileName = null;
        byte[]? capturedSavedBytes = null;

        _storageServiceMock.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/png", It.IsAny<CancellationToken>()))
            .Callback<Stream, string, string, CancellationToken>((stream, fileName, ct, token) =>
            {
                capturedSavedFileName = fileName;
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                capturedSavedBytes = ms.ToArray();
            })
            .ReturnsAsync("results/result_saved.png");

        var resultPath = await _service.RenderAndSaveRunImageAsync(runId);

        Assert.Equal("results/result_saved.png", resultPath);
        Assert.Equal("results/result_saved.png", run.ResultImagePath);
        Assert.NotNull(capturedSavedFileName);
        Assert.Contains(runId.ToString("N"), capturedSavedFileName);
        Assert.NotNull(capturedSavedBytes);
        Assert.True(capturedSavedBytes.Length > 0);

        // Verify result image is a valid PNG and can be opened
        using var loaded = Image.Load<Rgba32>(capturedSavedBytes);
        Assert.Equal(200, loaded.Width);
        Assert.Equal(200, loaded.Height);
    }

    [Fact]
    public async Task RenderAndSaveRunImage_OutOfBoundsCoordinates_ClampsSafelyWithoutException()
    {
        var runId = Guid.NewGuid();
        var origBytes = TestImageGenerator.CreateValidPng(100, 100);

        var run = new AnalysisRun
        {
            Id = runId,
            Analysis = new Analysis
            {
                XRayImage = new XRayImage { FilePath = "images/orig.png" }
            },
            Detections = new List<Detection>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ClassName = "fracture",
                    Confidence = 0.88m,
                    X = -50,
                    Y = -20,
                    Width = 500,
                    Height = 300
                }
            }
        };

        _runRepoMock.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        _storageServiceMock.Setup(s => s.GetAsync("images/orig.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(origBytes));
        _storageServiceMock.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("results/clamped.png");

        var resultPath = await _service.RenderAndSaveRunImageAsync(runId);

        Assert.Equal("results/clamped.png", resultPath);
    }

    [Fact]
    public async Task RenderAndSaveRunImage_WhenRunNotFound_ReturnsNull()
    {
        var runId = Guid.NewGuid();
        _runRepoMock.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisRun?)null);

        var result = await _service.RenderAndSaveRunImageAsync(runId);
        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAndSaveRunImage_WhenOriginalFileMissing_ReturnsNull()
    {
        var runId = Guid.NewGuid();
        var run = new AnalysisRun
        {
            Id = runId,
            Analysis = new Analysis { XRayImage = new XRayImage { FilePath = "missing.png" } }
        };

        _runRepoMock.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        _storageServiceMock.Setup(s => s.GetAsync("missing.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        var result = await _service.RenderAndSaveRunImageAsync(runId);
        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAndSaveRunImage_DegenerateAndInvalidDetections_SkipsWithoutException()
    {
        var runId = Guid.NewGuid();
        var origBytes = TestImageGenerator.CreateValidPng(100, 100);

        var run = new AnalysisRun
        {
            Id = runId,
            Analysis = new Analysis
            {
                XRayImage = new XRayImage { FilePath = "images/orig.png" }
            },
            Detections = new List<Detection>
            {
                new() { Id = Guid.NewGuid(), ClassName = "zero-w", Confidence = 0.9m, X = 10, Y = 10, Width = 0, Height = 20 },
                new() { Id = Guid.NewGuid(), ClassName = "neg-h", Confidence = 0.9m, X = 10, Y = 10, Width = 20, Height = -5 },
                new() { Id = Guid.NewGuid(), ClassName = "out-bounds", Confidence = 0.9m, X = 150, Y = 150, Width = 20, Height = 20 }
            }
        };

        _runRepoMock.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        _storageServiceMock.Setup(s => s.GetAsync("images/orig.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(origBytes));
        _storageServiceMock.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("results/no_valid_boxes.png");

        var resultPath = await _service.RenderAndSaveRunImageAsync(runId);

        Assert.Equal("results/no_valid_boxes.png", resultPath);
    }
}
