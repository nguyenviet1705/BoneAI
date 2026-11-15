using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Application.Services;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BoneAI.Tests.Services;

public class ResultsServiceTests
{
    private readonly Mock<IAnalysisRepository> _analysisRepoMock;
    private readonly Mock<IAnalysisRunRepository> _runRepoMock;
    private readonly Mock<IXRayImageRepository> _imageRepoMock;
    private readonly Mock<IImageStorageService> _storageServiceMock;
    private readonly Mock<ILogger<ResultsService>> _loggerMock;
    private readonly ResultsService _service;

    public ResultsServiceTests()
    {
        _analysisRepoMock = new Mock<IAnalysisRepository>();
        _runRepoMock = new Mock<IAnalysisRunRepository>();
        _imageRepoMock = new Mock<IXRayImageRepository>();
        _storageServiceMock = new Mock<IImageStorageService>();
        _loggerMock = new Mock<ILogger<ResultsService>>();

        _service = new ResultsService(
            _analysisRepoMock.Object,
            _runRepoMock.Object,
            _imageRepoMock.Object,
            _storageServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAnalysisResult_WhenAnalysisNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _analysisRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Analysis?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAnalysisResultAsync(id));
    }

    [Fact]
    public async Task GetAnalysisResult_WithThreeCompletedRuns_ReturnsAllRunsWithIsolatedDetections()
    {
        var imageId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();

        var modelA = new AIModel { Id = Guid.NewGuid(), Name = "Model A", ModelKey = "model-a", Version = "1.0.0", Framework = "PyTorch" };
        var modelB = new AIModel { Id = Guid.NewGuid(), Name = "Model B", ModelKey = "model-b", Version = "2.0.0", Framework = "ONNX" };
        var modelC = new AIModel { Id = Guid.NewGuid(), Name = "Model C", ModelKey = "model-c", Version = "1.5.0", Framework = "TensorFlow" };

        var runA = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            AIModelId = modelA.Id,
            AIModel = modelA,
            Status = AnalysisRunStatus.Completed,
            ProcessingTimeMs = 850,
            ResultImagePath = "results/runA.png",
            Detections = new List<Detection>
            {
                new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.95m, X = 10, Y = 20, Width = 100, Height = 80 },
                new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.88m, X = 30, Y = 40, Width = 50, Height = 50 }
            }
        };

        var runB = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            AIModelId = modelB.Id,
            AIModel = modelB,
            Status = AnalysisRunStatus.Completed,
            ProcessingTimeMs = 1200,
            ResultImagePath = "results/runB.png",
            Detections = new List<Detection>
            {
                new() { Id = Guid.NewGuid(), ClassName = "callus", Confidence = 0.76m, X = 15, Y = 25, Width = 80, Height = 60 }
            }
        };

        var runC = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            AIModelId = modelC.Id,
            AIModel = modelC,
            Status = AnalysisRunStatus.Completed,
            ProcessingTimeMs = 500,
            ResultImagePath = null,
            Detections = new List<Detection>()
        };

        var analysis = new Analysis
        {
            Id = analysisId,
            XRayImageId = imageId,
            Status = AnalysisStatus.Completed,
            Mode = AnalysisMode.CompareAll,
            CreatedAt = DateTime.UtcNow,
            XRayImage = new XRayImage
            {
                Id = imageId,
                FileName = "wrist.png",
                ContentType = "image/png",
                FileSize = 1024,
                FilePath = "images/wrist.png"
            },
            AnalysisRuns = new List<AnalysisRun> { runA, runB, runC }
        };

        _analysisRepoMock.Setup(r => r.GetByIdAsync(analysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        var result = await _service.GetAnalysisResultAsync(analysisId);

        Assert.NotNull(result);
        Assert.Equal(analysisId, result.AnalysisId);
        Assert.Equal(imageId, result.ImageId);
        Assert.Equal("Completed", result.OverallStatus);
        Assert.Equal("wrist.png", result.OriginalImage.FileName);
        Assert.Equal($"/api/images/{imageId}/file", result.OriginalImage.OriginalImageUrl);
        Assert.Equal(3, result.TotalRunsCount);
        Assert.Equal(3, result.CompletedRunsCount);
        Assert.Equal(0, result.FailedRunsCount);
        Assert.Equal(3, result.Runs.Count);

        // Verify Run A isolation and formatting
        var resRunA = result.Runs.First(r => r.RunId == runA.Id);
        Assert.Equal("Model A", resRunA.Model.ModelName);
        Assert.Equal("1.0.0", resRunA.Model.Version);
        Assert.Equal(0.85, resRunA.ProcessingTime);
        Assert.Equal(2, resRunA.DetectionCount);
        Assert.Equal($"/api/results/runs/{runA.Id}/image", resRunA.ResultImageUrl);
        Assert.Equal(2, resRunA.Detections.Count);
        Assert.Contains(resRunA.Detections, d => d.ClassLabel == "fracture" && d.ConfidenceScore == 0.95m);

        // Verify Run B isolation
        var resRunB = result.Runs.First(r => r.RunId == runB.Id);
        Assert.Equal("Model B", resRunB.Model.ModelName);
        Assert.Equal(1.2, resRunB.ProcessingTime);
        Assert.Single(resRunB.Detections);
        Assert.Equal("callus", resRunB.Detections[0].ClassLabel);

        // Verify Run C with 0 detections
        var resRunC = result.Runs.First(r => r.RunId == runC.Id);
        Assert.Equal("Model C", resRunC.Model.ModelName);
        Assert.Equal(0.5, resRunC.ProcessingTime);
        Assert.Equal(0, resRunC.DetectionCount);
        Assert.Empty(resRunC.Detections);
        Assert.Null(resRunC.ResultImageUrl);
    }

    [Fact]
    public async Task GetAnalysisResult_OneCompletedTwoFailed_KeepsCompletedDetectionsAndReportsErrors()
    {
        var analysisId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        var modelA = new AIModel { Id = Guid.NewGuid(), Name = "Model A", Version = "1.0.0" };
        var modelB = new AIModel { Id = Guid.NewGuid(), Name = "Model B", Version = "1.0.0" };
        var modelC = new AIModel { Id = Guid.NewGuid(), Name = "Model C", Version = "1.0.0" };

        var runA = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            AIModelId = modelA.Id,
            AIModel = modelA,
            Status = AnalysisRunStatus.Completed,
            ProcessingTimeMs = 700,
            Detections = new List<Detection>
            {
                new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.91m, X = 10, Y = 10, Width = 50, Height = 50 }
            }
        };

        var runB = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            AIModelId = modelB.Id,
            AIModel = modelB,
            Status = AnalysisRunStatus.Failed,
            ErrorMessage = "Inference service timed out"
        };

        var runC = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            AIModelId = modelC.Id,
            AIModel = modelC,
            Status = AnalysisRunStatus.Failed,
            ErrorMessage = "CUDA out of memory"
        };

        var analysis = new Analysis
        {
            Id = analysisId,
            XRayImageId = imageId,
            Status = AnalysisStatus.PartiallyCompleted,
            CreatedAt = DateTime.UtcNow,
            AnalysisRuns = new List<AnalysisRun> { runA, runB, runC }
        };

        _analysisRepoMock.Setup(r => r.GetByIdAsync(analysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        var result = await _service.GetAnalysisResultAsync(analysisId);

        Assert.Equal("PartiallyCompleted", result.OverallStatus);
        Assert.Equal(1, result.CompletedRunsCount);
        Assert.Equal(2, result.FailedRunsCount);

        var resRunA = result.Runs.First(r => r.RunId == runA.Id);
        Assert.Equal("Completed", resRunA.Status);
        Assert.Single(resRunA.Detections);
        Assert.Null(resRunA.ErrorMessage);

        var resRunB = result.Runs.First(r => r.RunId == runB.Id);
        Assert.Equal("Failed", resRunB.Status);
        Assert.Empty(resRunB.Detections);
        Assert.Equal("Inference service timed out", resRunB.ErrorMessage);
        Assert.Null(resRunB.ProcessingTime);

        var resRunC = result.Runs.First(r => r.RunId == runC.Id);
        Assert.Equal("Failed", resRunC.Status);
        Assert.Empty(resRunC.Detections);
        Assert.Equal("CUDA out of memory", resRunC.ErrorMessage);
    }

    [Fact]
    public async Task GetAnalysisResult_WhenProcessing_ReturnsCurrentProcessingState()
    {
        var analysisId = Guid.NewGuid();
        var model = new AIModel { Id = Guid.NewGuid(), Name = "Model A", Version = "1.0.0" };

        var run = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            AIModelId = model.Id,
            AIModel = model,
            Status = AnalysisRunStatus.Processing,
            StartedAt = DateTime.UtcNow
        };

        var analysis = new Analysis
        {
            Id = analysisId,
            Status = AnalysisStatus.Processing,
            AnalysisRuns = new List<AnalysisRun> { run }
        };

        _analysisRepoMock.Setup(r => r.GetByIdAsync(analysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        var result = await _service.GetAnalysisResultAsync(analysisId);

        Assert.Equal("Processing", result.OverallStatus);
        Assert.Equal(0, result.CompletedRunsCount);
        Assert.Equal("Processing", result.Runs[0].Status);
        Assert.Null(result.Runs[0].ProcessingTime);
    }

    [Fact]
    public async Task GetAnalysisRunResult_ExistingRun_ReturnsRunDetailsAndBoundingBox()
    {
        var runId = Guid.NewGuid();
        var model = new AIModel { Id = Guid.NewGuid(), Name = "YOLO Wrist", ModelKey = "yolo-wrist", Version = "8.0", Framework = "PyTorch" };

        var run = new AnalysisRun
        {
            Id = runId,
            AnalysisId = Guid.NewGuid(),
            AIModelId = model.Id,
            AIModel = model,
            Status = AnalysisRunStatus.Completed,
            ProcessingTimeMs = 345,
            ResultImagePath = "results/run.png",
            Detections = new List<Detection>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ClassName = "fracture",
                    Confidence = 0.9234m,
                    X = 120.5m,
                    Y = 85.0m,
                    Width = 150.0m,
                    Height = 90.0m
                }
            }
        };

        _runRepoMock.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var result = await _service.GetAnalysisRunResultAsync(runId);

        Assert.NotNull(result);
        Assert.Equal(runId, result.RunId);
        Assert.Equal("YOLO Wrist", result.Model.ModelName);
        Assert.Equal("8.0", result.Model.Version);
        Assert.Equal(0.345, result.ProcessingTime);
        Assert.Equal(1, result.DetectionCount);

        var det = result.Detections[0];
        Assert.Equal("fracture", det.ClassLabel);
        Assert.Equal(0.9234m, det.ConfidenceScore);
        Assert.Equal(120.5m, det.BoundingBox.X);
        Assert.Equal(85.0m, det.BoundingBox.Y);
        Assert.Equal(150.0m, det.BoundingBox.Width);
        Assert.Equal(90.0m, det.BoundingBox.Height);
    }

    [Fact]
    public async Task GetAnalysisRunResult_FailedRun_ReturnsFailedStateWithEmptyDetections()
    {
        var runId = Guid.NewGuid();
        var model = new AIModel { Id = Guid.NewGuid(), Name = "Model X", Version = "1.0.0" };

        var run = new AnalysisRun
        {
            Id = runId,
            AIModelId = model.Id,
            AIModel = model,
            Status = AnalysisRunStatus.Failed,
            ErrorMessage = "AI service timeout"
        };

        _runRepoMock.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var result = await _service.GetAnalysisRunResultAsync(runId);

        Assert.Equal("Failed", result.Status);
        Assert.Equal("AI service timeout", result.ErrorMessage);
        Assert.Empty(result.Detections);
        Assert.Equal(0, result.DetectionCount);
        Assert.Null(result.ProcessingTime);
    }

    [Fact]
    public async Task GetAnalysisRunResult_NotFound_ThrowsNotFoundException()
    {
        var runId = Guid.NewGuid();
        _runRepoMock.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisRun?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAnalysisRunResultAsync(runId));
    }

    [Fact]
    public async Task GetAnalysisComparison_ThreeModels_CalculatesMetricsAccurately()
    {
        var analysisId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        var modelA = new AIModel { Id = Guid.NewGuid(), Name = "Model A", ModelKey = "model-a", Version = "1.0.0" };
        var modelB = new AIModel { Id = Guid.NewGuid(), Name = "Model B", ModelKey = "model-b", Version = "1.1.0" };
        var modelC = new AIModel { Id = Guid.NewGuid(), Name = "Model C", ModelKey = "model-c", Version = "2.0.0" };

        // Model A: 2 detections (0.90, 0.92) -> Avg 0.91, 850ms -> 0.85s
        var runA = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            AIModelId = modelA.Id,
            AIModel = modelA,
            Status = AnalysisRunStatus.Completed,
            ProcessingTimeMs = 850,
            ResultImagePath = "results/a.png",
            Detections = new List<Detection>
            {
                new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.90m, X = 10, Y = 10, Width = 20, Height = 20 },
                new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.92m, X = 30, Y = 30, Width = 40, Height = 40 }
            }
        };

        // Model B: Failed -> 0 detections, Avg null, processing null
        var runB = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            AIModelId = modelB.Id,
            AIModel = modelB,
            Status = AnalysisRunStatus.Failed,
            ErrorMessage = "AI Service unavailable"
        };

        // Model C: 0 detections -> Avg null, 600ms -> 0.6s
        var runC = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            AIModelId = modelC.Id,
            AIModel = modelC,
            Status = AnalysisRunStatus.Completed,
            ProcessingTimeMs = 600,
            Detections = new List<Detection>()
        };

        var analysis = new Analysis
        {
            Id = analysisId,
            XRayImageId = imageId,
            Status = AnalysisStatus.PartiallyCompleted,
            AnalysisRuns = new List<AnalysisRun> { runA, runB, runC }
        };

        _analysisRepoMock.Setup(r => r.GetByIdAsync(analysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        var comparison = await _service.GetAnalysisComparisonAsync(analysisId);

        Assert.NotNull(comparison);
        Assert.Equal(analysisId, comparison.AnalysisId);
        Assert.Equal(imageId, comparison.ImageId);
        Assert.Equal($"/api/images/{imageId}/file", comparison.OriginalImageUrl);
        Assert.Equal(3, comparison.Models.Count);

        // Check Model A
        var compA = comparison.Models.First(m => m.ModelId == modelA.Id);
        Assert.Equal("Completed", compA.Status);
        Assert.Equal(0.85, compA.ProcessingTime);
        Assert.Equal(2, compA.DetectionCount);
        Assert.Equal(0.91m, compA.AverageConfidence);
        Assert.Equal($"/api/results/runs/{runA.Id}/image", compA.ResultImageUrl);
        Assert.Null(compA.ErrorMessage);

        // Check Model B (Failed)
        var compB = comparison.Models.First(m => m.ModelId == modelB.Id);
        Assert.Equal("Failed", compB.Status);
        Assert.Null(compB.ProcessingTime);
        Assert.Equal(0, compB.DetectionCount);
        Assert.Null(compB.AverageConfidence);
        Assert.Null(compB.ResultImageUrl);
        Assert.Equal("AI Service unavailable", compB.ErrorMessage);

        // Check Model C (Completed with 0 detections)
        var compC = comparison.Models.First(m => m.ModelId == modelC.Id);
        Assert.Equal("Completed", compC.Status);
        Assert.Equal(0.6, compC.ProcessingTime);
        Assert.Equal(0, compC.DetectionCount);
        Assert.Null(compC.AverageConfidence);
    }

    [Fact]
    public async Task GetAnalysisComparison_DynamicModelCount_SupportsArbitraryCount()
    {
        var analysisId = Guid.NewGuid();
        var runs = new List<AnalysisRun>();

        // Create 4 runs
        for (int i = 0; i < 4; i++)
        {
            var model = new AIModel { Id = Guid.NewGuid(), Name = $"Model {i}", Version = "1.0.0" };
            runs.Add(new AnalysisRun
            {
                Id = Guid.NewGuid(),
                AIModelId = model.Id,
                AIModel = model,
                Status = AnalysisRunStatus.Completed,
                ProcessingTimeMs = 100 * (i + 1),
                Detections = new List<Detection>()
            });
        }

        var analysis = new Analysis
        {
            Id = analysisId,
            XRayImageId = Guid.NewGuid(),
            AnalysisRuns = runs
        };

        _analysisRepoMock.Setup(r => r.GetByIdAsync(analysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        var comparison = await _service.GetAnalysisComparisonAsync(analysisId);

        Assert.Equal(4, comparison.Models.Count);
    }

    [Fact]
    public async Task GetRunResultImage_WhenRunHasNoResultImage_ThrowsNotFoundException()
    {
        var runId = Guid.NewGuid();
        var run = new AnalysisRun
        {
            Id = runId,
            Status = AnalysisRunStatus.Completed,
            ResultImagePath = null
        };

        _runRepoMock.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetRunResultImageAsync(runId));
    }

    [Fact]
    public async Task GetRunResultImage_WhenStorageFileNotFound_ThrowsNotFoundException()
    {
        var runId = Guid.NewGuid();
        var run = new AnalysisRun
        {
            Id = runId,
            Status = AnalysisRunStatus.Completed,
            ResultImagePath = "results/missing.png"
        };

        _runRepoMock.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        _storageServiceMock.Setup(s => s.GetAsync("results/missing.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetRunResultImageAsync(runId));
    }

    [Fact]
    public async Task GetRunResultImage_WhenImageExists_ReturnsImageFileResult()
    {
        var runId = Guid.NewGuid();
        var run = new AnalysisRun
        {
            Id = runId,
            Status = AnalysisRunStatus.Completed,
            ResultImagePath = "results/run.png"
        };

        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });

        _runRepoMock.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        _storageServiceMock.Setup(s => s.GetAsync("results/run.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ms);

        var result = await _service.GetRunResultImageAsync(runId);

        Assert.NotNull(result);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal("run.png", result.FileName);
    }
}
