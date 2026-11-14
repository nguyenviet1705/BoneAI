using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Infrastructure.BackgroundServices;
using BoneAI.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BoneAI.Tests.Services;

public class AnalysisBackgroundWorkerTests
{
    private readonly Mock<IAnalysisRunRepository> _mockRunRepo;
    private readonly Mock<IAnalysisRepository> _mockAnalysisRepo;
    private readonly Mock<IImageStorageService> _mockStorage;
    private readonly Mock<IAIServiceClient> _mockAiClient;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly AnalysisBackgroundWorker _worker;

    public AnalysisBackgroundWorkerTests()
    {
        _mockRunRepo = new Mock<IAnalysisRunRepository>();
        _mockAnalysisRepo = new Mock<IAnalysisRepository>();
        _mockStorage = new Mock<IImageStorageService>();
        _mockAiClient = new Mock<IAIServiceClient>();

        var mockScope = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        serviceProviderMock.Setup(sp => sp.GetService(typeof(IAnalysisRunRepository))).Returns(_mockRunRepo.Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IAnalysisRepository))).Returns(_mockAnalysisRepo.Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IImageStorageService))).Returns(_mockStorage.Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IAIServiceClient))).Returns(_mockAiClient.Object);

        mockScope.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        var options = Options.Create(new AIServiceOptions
        {
            MaxRetryAttempts = 2,
            InitialRetryDelayMs = 100
        });

        _worker = new AnalysisBackgroundWorker(
            _mockScopeFactory.Object,
            options,
            NullLogger<AnalysisBackgroundWorker>.Instance);
    }

    [Fact]
    public async Task ProcessNextJobAsync_SuccessfulInference_UpdatesStatusToCompletedAndPersistsDetections()
    {
        var runId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();

        var image = new XRayImage { Id = Guid.NewGuid(), FilePath = "2026/11/wrist.jpg", ContentType = "image/jpeg" };
        var analysis = new Analysis { Id = analysisId, Status = AnalysisStatus.Pending, XRayImage = image };
        var model = new AIModel { Id = Guid.NewGuid(), Name = "mock-bone-fracture-v1", Version = "1.0.0" };

        var run = new AnalysisRun
        {
            Id = runId,
            Analysis = analysis,
            AIModel = model,
            Status = AnalysisRunStatus.Pending,
            RetryCount = 0
        };

        _mockRunRepo.Setup(r => r.ClaimNextPendingRunAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _mockStorage.Setup(s => s.GetAsync("2026/11/wrist.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        var inferenceResponse = new InferenceClientResponse
        {
            AnalysisRunId = runId,
            ModelKey = "mock-bone-fracture-v1",
            ImageWidth = 800,
            ImageHeight = 600,
            ProcessingTimeMs = 50,
            Detections = new List<InferenceDetectionItem>
            {
                new() { ClassName = "fracture", Confidence = 0.95m, X = 100, Y = 150, Width = 80, Height = 60 }
            }
        };

        _mockAiClient.Setup(c => c.ExecuteInferenceAsync(It.IsAny<InferenceClientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inferenceResponse);

        var processed = await _worker.ProcessNextJobAsync(CancellationToken.None);

        Assert.True(processed);
        Assert.Equal(AnalysisRunStatus.Completed, run.Status);
        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        Assert.NotNull(run.CompletedAt);
        Assert.Equal(50, run.ProcessingTimeMs);

        _mockRunRepo.Verify(r => r.SaveDetectionsAsync(runId, It.Is<IEnumerable<Detection>>(d => d.Count() == 1), It.IsAny<CancellationToken>()), Times.Once);
        _mockRunRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessNextJobAsync_PhysicalImageMissing_FailsRunAndAnalysis()
    {
        var runId = Guid.NewGuid();
        var image = new XRayImage { Id = Guid.NewGuid(), FilePath = "missing.jpg" };
        var analysis = new Analysis { Id = Guid.NewGuid(), Status = AnalysisStatus.Pending, XRayImage = image };

        var run = new AnalysisRun
        {
            Id = runId,
            Analysis = analysis,
            AIModel = new AIModel { Name = "mock" },
            Status = AnalysisRunStatus.Pending
        };

        _mockRunRepo.Setup(r => r.ClaimNextPendingRunAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        _mockStorage.Setup(s => s.GetAsync("missing.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        var processed = await _worker.ProcessNextJobAsync(CancellationToken.None);

        Assert.True(processed);
        Assert.Equal(AnalysisRunStatus.Failed, run.Status);
        Assert.Equal(AnalysisStatus.Failed, analysis.Status);
        Assert.Contains("was not found in storage", run.ErrorMessage);
    }

    [Fact]
    public async Task ProcessNextJobAsync_TransientNetworkError_IncrementsRetryAndSchedulesBackoff()
    {
        var runId = Guid.NewGuid();
        var image = new XRayImage { Id = Guid.NewGuid(), FilePath = "wrist.jpg" };
        var analysis = new Analysis { Id = Guid.NewGuid(), Status = AnalysisStatus.Pending, XRayImage = image };

        var run = new AnalysisRun
        {
            Id = runId,
            Analysis = analysis,
            AIModel = new AIModel { Name = "mock" },
            Status = AnalysisRunStatus.Processing,
            RetryCount = 0
        };

        _mockRunRepo.Setup(r => r.ClaimNextPendingRunAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        _mockStorage.Setup(s => s.GetAsync("wrist.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

        _mockAiClient.Setup(c => c.ExecuteInferenceAsync(It.IsAny<InferenceClientRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var processed = await _worker.ProcessNextJobAsync(CancellationToken.None);

        Assert.True(processed);
        Assert.Equal(1, run.RetryCount);
        Assert.Equal(AnalysisRunStatus.Pending, run.Status);
        Assert.NotNull(run.LeaseUntil);
        Assert.True(run.LeaseUntil > DateTime.UtcNow);
    }

    [Fact]
    public async Task ProcessNextJobAsync_TransientErrorExhaustedRetries_FailsRun()
    {
        var runId = Guid.NewGuid();
        var image = new XRayImage { Id = Guid.NewGuid(), FilePath = "wrist.jpg" };
        var analysis = new Analysis { Id = Guid.NewGuid(), Status = AnalysisStatus.Pending, XRayImage = image };

        var run = new AnalysisRun
        {
            Id = runId,
            Analysis = analysis,
            AIModel = new AIModel { Name = "mock" },
            Status = AnalysisRunStatus.Processing,
            RetryCount = 2 // Already reached MaxRetryAttempts (2)
        };

        _mockRunRepo.Setup(r => r.ClaimNextPendingRunAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        _mockStorage.Setup(s => s.GetAsync("wrist.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

        _mockAiClient.Setup(c => c.ExecuteInferenceAsync(It.IsAny<InferenceClientRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Inference timeout"));

        var processed = await _worker.ProcessNextJobAsync(CancellationToken.None);

        Assert.True(processed);
        Assert.Equal(AnalysisRunStatus.Failed, run.Status);
        Assert.Equal(AnalysisStatus.Failed, analysis.Status);
        Assert.Contains("after 2 retry attempts", run.ErrorMessage);
    }
}
