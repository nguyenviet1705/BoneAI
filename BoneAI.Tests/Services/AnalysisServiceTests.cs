using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Application.Services;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BoneAI.Tests.Services;

public class AnalysisServiceTests
{
    private readonly Mock<IAnalysisRepository> _mockAnalysisRepo;
    private readonly Mock<IXRayImageRepository> _mockImageRepo;
    private readonly Mock<IAIModelRepository> _mockModelRepo;
    private readonly Mock<IImageStorageService> _mockStorage;
    private readonly AnalysisService _service;

    public AnalysisServiceTests()
    {
        _mockAnalysisRepo = new Mock<IAnalysisRepository>();
        _mockImageRepo = new Mock<IXRayImageRepository>();
        _mockModelRepo = new Mock<IAIModelRepository>();
        _mockStorage = new Mock<IImageStorageService>();

        _service = new AnalysisService(
            _mockAnalysisRepo.Object,
            _mockImageRepo.Object,
            _mockModelRepo.Object,
            _mockStorage.Object,
            NullLogger<AnalysisService>.Instance);
    }

    [Fact]
    public async Task SubmitAsync_ValidRequest_CreatesAndReturnsPendingAnalysis()
    {
        var imageId = Guid.NewGuid();
        var modelId = Guid.NewGuid();

        var image = new XRayImage { Id = imageId, FilePath = "2026/11/xray.jpg" };
        var model = new AIModel { Id = modelId, Name = "mock-bone-fracture-v1", Version = "1.0.0", IsActive = true };

        _mockImageRepo.Setup(r => r.GetByIdAsync(imageId, It.IsAny<CancellationToken>())).ReturnsAsync(image);
        _mockStorage.Setup(s => s.ExistsAsync(image.FilePath, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockModelRepo.Setup(r => r.GetByIdAsync(modelId, It.IsAny<CancellationToken>())).ReturnsAsync(model);

        var request = new SubmitAnalysisRequest
        {
            XRayImageId = imageId,
            AIModelId = modelId
        };

        var response = await _service.SubmitAsync(request);

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.AnalysisId);
        Assert.NotEqual(Guid.Empty, response.AnalysisRunId);
        Assert.Equal("Pending", response.Status);
        Assert.Equal($"/api/analysis/{response.AnalysisId}", response.StatusUrl);

        _mockAnalysisRepo.Verify(r => r.AddAsync(It.IsAny<Analysis>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAnalysisRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_NonexistentImage_ThrowsNotFoundException()
    {
        var imageId = Guid.NewGuid();
        _mockImageRepo.Setup(r => r.GetByIdAsync(imageId, It.IsAny<CancellationToken>())).ReturnsAsync((XRayImage?)null);

        var request = new SubmitAnalysisRequest { XRayImageId = imageId, AIModelId = Guid.NewGuid() };

        await Assert.ThrowsAsync<NotFoundException>(() => _service.SubmitAsync(request));
    }

    [Fact]
    public async Task SubmitAsync_MissingPhysicalImage_ThrowsNotFoundException()
    {
        var imageId = Guid.NewGuid();
        var image = new XRayImage { Id = imageId, FilePath = "missing.jpg" };

        _mockImageRepo.Setup(r => r.GetByIdAsync(imageId, It.IsAny<CancellationToken>())).ReturnsAsync(image);
        _mockStorage.Setup(s => s.ExistsAsync(image.FilePath, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new SubmitAnalysisRequest { XRayImageId = imageId, AIModelId = Guid.NewGuid() };

        await Assert.ThrowsAsync<NotFoundException>(() => _service.SubmitAsync(request));
    }

    [Fact]
    public async Task SubmitAsync_NonexistentModel_ThrowsNotFoundException()
    {
        var imageId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var image = new XRayImage { Id = imageId, FilePath = "xray.jpg" };

        _mockImageRepo.Setup(r => r.GetByIdAsync(imageId, It.IsAny<CancellationToken>())).ReturnsAsync(image);
        _mockStorage.Setup(s => s.ExistsAsync(image.FilePath, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockModelRepo.Setup(r => r.GetByIdAsync(modelId, It.IsAny<CancellationToken>())).ReturnsAsync((AIModel?)null);

        var request = new SubmitAnalysisRequest { XRayImageId = imageId, AIModelId = modelId };

        await Assert.ThrowsAsync<NotFoundException>(() => _service.SubmitAsync(request));
    }

    [Fact]
    public async Task SubmitAsync_InactiveModel_ThrowsValidationException()
    {
        var imageId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var image = new XRayImage { Id = imageId, FilePath = "xray.jpg" };
        var model = new AIModel { Id = modelId, Name = "old-model", Version = "0.9", IsActive = false };

        _mockImageRepo.Setup(r => r.GetByIdAsync(imageId, It.IsAny<CancellationToken>())).ReturnsAsync(image);
        _mockStorage.Setup(s => s.ExistsAsync(image.FilePath, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockModelRepo.Setup(r => r.GetByIdAsync(modelId, It.IsAny<CancellationToken>())).ReturnsAsync(model);

        var request = new SubmitAnalysisRequest { XRayImageId = imageId, AIModelId = modelId };

        await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitAsync(request));
    }

    [Fact]
    public async Task SubmitAsync_IdempotentSubmission_ReturnsExistingAnalysisWithoutDuplicate()
    {
        var imageId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var existingAnalysisId = Guid.NewGuid();
        var existingRunId = Guid.NewGuid();

        var image = new XRayImage { Id = imageId, FilePath = "xray.jpg" };
        var model = new AIModel { Id = modelId, Name = "model", Version = "1.0", IsActive = true };

        var existingAnalysis = new Analysis
        {
            Id = existingAnalysisId,
            XRayImageId = imageId,
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            IdempotencyKey = "key-12345"
        };
        existingAnalysis.AnalysisRuns.Add(new AnalysisRun { Id = existingRunId, Status = AnalysisRunStatus.Completed });

        _mockImageRepo.Setup(r => r.GetByIdAsync(imageId, It.IsAny<CancellationToken>())).ReturnsAsync(image);
        _mockStorage.Setup(s => s.ExistsAsync(image.FilePath, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockModelRepo.Setup(r => r.GetByIdAsync(modelId, It.IsAny<CancellationToken>())).ReturnsAsync(model);
        _mockAnalysisRepo.Setup(r => r.GetByIdempotencyKeyAsync("key-12345", It.IsAny<CancellationToken>())).ReturnsAsync(existingAnalysis);

        var request = new SubmitAnalysisRequest
        {
            XRayImageId = imageId,
            AIModelId = modelId,
            IdempotencyKey = "key-12345"
        };

        var response = await _service.SubmitAsync(request);

        Assert.Equal(existingAnalysisId, response.AnalysisId);
        Assert.Equal(existingRunId, response.AnalysisRunId);
        Assert.Equal("Completed", response.Status);

        // Verify no new record was added
        _mockAnalysisRepo.Verify(r => r.AddAsync(It.IsAny<Analysis>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStatusAsync_ExistingAnalysis_ReturnsDetailsWithRuns()
    {
        var analysisId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var model = new AIModel { Id = Guid.NewGuid(), Name = "mock-bone-fracture-v1", Version = "1.0.0" };

        var analysis = new Analysis
        {
            Id = analysisId,
            XRayImageId = Guid.NewGuid(),
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        var run = new AnalysisRun
        {
            Id = runId,
            AnalysisId = analysisId,
            AIModel = model,
            AIModelId = model.Id,
            Status = AnalysisRunStatus.Completed,
            StartedAt = DateTime.UtcNow.AddSeconds(-2),
            CompletedAt = DateTime.UtcNow,
            ProcessingTimeMs = 45
        };

        run.Detections.Add(new Detection
        {
            Id = Guid.NewGuid(),
            AnalysisRunId = runId,
            ClassName = "fracture",
            Confidence = 0.95m,
            X = 10,
            Y = 20,
            Width = 30,
            Height = 40
        });

        analysis.AnalysisRuns.Add(run);

        _mockAnalysisRepo.Setup(r => r.GetByIdAsync(analysisId, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);

        var result = await _service.GetStatusAsync(analysisId);

        Assert.Equal(analysisId, result.AnalysisId);
        Assert.Equal("Completed", result.Status);
        Assert.Single(result.AnalysisRuns);

        var runResult = result.AnalysisRuns[0];
        Assert.Equal("mock-bone-fracture-v1", runResult.ModelName);
        Assert.Single(runResult.Detections);
        Assert.Equal("fracture", runResult.Detections[0].ClassName);
    }

    [Fact]
    public async Task GetStatusAsync_Nonexistent_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _mockAnalysisRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Analysis?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetStatusAsync(id));
    }
}
