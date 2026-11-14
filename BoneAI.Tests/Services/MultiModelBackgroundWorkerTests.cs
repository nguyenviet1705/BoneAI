using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Infrastructure.BackgroundServices;
using BoneAI.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BoneAI.Tests.Services;

public class MultiModelBackgroundWorkerTests
{
    private readonly Mock<IAnalysisRunRepository> _mockRunRepo = new();
    private readonly Mock<IAnalysisRepository> _mockAnalysisRepo = new();
    private readonly Mock<IImageStorageService> _mockStorage = new();
    private readonly Mock<IAIServiceClient> _mockAiClient = new();
    private readonly Mock<ILogger<AnalysisBackgroundWorker>> _mockLogger = new();
    private readonly AnalysisBackgroundWorker _worker;

    public MultiModelBackgroundWorkerTests()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _mockRunRepo.Object);
        services.AddScoped(_ => _mockAnalysisRepo.Object);
        services.AddScoped(_ => _mockStorage.Object);
        services.AddScoped(_ => _mockAiClient.Object);
        var provider = services.BuildServiceProvider();

        var scopeFactory = new Mock<IServiceScopeFactory>();
        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(provider);
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var options = Options.Create(new AIServiceOptions
        {
            MaxDegreeOfParallelism = 2,
            MaxRetryAttempts = 1,
            InitialRetryDelayMs = 50
        });

        _worker = new AnalysisBackgroundWorker(scopeFactory.Object, options, _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessNextJobAsync_PartialCompletion_Run1Succeeds_Run2Fails_YieldsPartiallyCompletedAnalysis()
    {
        var analysisId = Guid.NewGuid();
        var analysis = new Analysis
        {
            Id = analysisId,
            Mode = AnalysisMode.MultiModel,
            Status = AnalysisStatus.Processing
        };

        var run1Id = Guid.NewGuid();
        var run1 = new AnalysisRun
        {
            Id = run1Id,
            AnalysisId = analysisId,
            Analysis = analysis,
            Status = AnalysisRunStatus.Pending,
            AIModel = new AIModel { Name = "Model 1", Version = "1.0", ModelKey = "m1" }
        };

        var run2Id = Guid.NewGuid();
        var run2 = new AnalysisRun
        {
            Id = run2Id,
            AnalysisId = analysisId,
            Analysis = analysis,
            Status = AnalysisRunStatus.Pending,
            AIModel = new AIModel { Name = "Model 2", Version = "1.0", ModelKey = "m2" }
        };

        var image = new XRayImage { Id = Guid.NewGuid(), FilePath = "test.png", FileName = "test.png", ContentType = "image/png" };
        analysis.XRayImage = image;

        // Step 1: Claim and execute Run 1 successfully
        _mockRunRepo.Setup(r => r.ClaimNextPendingRunAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(run1);

        _mockStorage.Setup(s => s.GetAsync("test.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

        _mockAiClient.Setup(c => c.ExecuteInferenceAsync(It.Is<InferenceClientRequest>(req => req.AnalysisRunId == run1Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InferenceClientResponse
            {
                AnalysisRunId = run1Id,
                ModelKey = "m1",
                ImageWidth = 100,
                ImageHeight = 100,
                ProcessingTimeMs = 15,
                Detections = new List<InferenceDetectionItem>
                {
                    new() { ClassName = "fracture", Confidence = 0.95m, X = 10, Y = 10, Width = 20, Height = 20 }
                }
            });

        _mockRunRepo.Setup(r => r.GetRunsByAnalysisIdAsync(analysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AnalysisRun> { run1, run2 });

        await _worker.ProcessNextJobAsync(CancellationToken.None);

        Assert.Equal(AnalysisRunStatus.Completed, run1.Status);
        // Run 2 is still pending, so aggregate status is Processing
        Assert.Equal(AnalysisStatus.Processing, analysis.Status);

        // Step 2: Claim and execute Run 2, which fails permanently
        _mockRunRepo.Setup(r => r.ClaimNextPendingRunAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(run2);

        _mockAiClient.Setup(c => c.ExecuteInferenceAsync(It.Is<InferenceClientRequest>(req => req.AnalysisRunId == run2Id), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Model 2 crashed"));

        await _worker.ProcessNextJobAsync(CancellationToken.None);

        Assert.Equal(AnalysisRunStatus.Failed, run2.Status);
        // Now Run 1 is Completed, Run 2 is Failed, no pending runs left -> PartiallyCompleted!
        Assert.Equal(AnalysisStatus.PartiallyCompleted, analysis.Status);

        // Verify detection save was only called for Run 1, isolating results
        _mockRunRepo.Verify(r => r.SaveDetectionsAsync(run1Id, It.IsAny<IEnumerable<Detection>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRunRepo.Verify(r => r.SaveDetectionsAsync(run2Id, It.IsAny<IEnumerable<Detection>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
