using System.Net;
using System.Net.Http.Json;
using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Infrastructure.Configuration;
using BoneAI.Infrastructure.Persistence;
using BoneAI.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BoneAI.Tests.Controllers;

public class MultiModelScenarioIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testStorageDir;

    private readonly Guid _testImageId = Guid.NewGuid();
    private readonly Guid _modelAId = Guid.NewGuid();
    private readonly Guid _modelBId = Guid.NewGuid();
    private readonly Guid _modelCId = Guid.NewGuid();

    public MultiModelScenarioIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _testStorageDir = Path.Combine(Path.GetTempPath(), "BoneAI_MM_Scenario_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDir);

        var dbName = "MultiModelScenarioDb_" + Guid.NewGuid().ToString("N");

        var mockAiClient = new Mock<IAIServiceClient>();
        mockAiClient.Setup(c => c.CheckHealthAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockAiClient.Setup(c => c.CheckReadinessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BoneAIDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<BoneAIDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });

                services.Configure<ImageStorageOptions>(options =>
                {
                    options.RootPath = _testStorageDir;
                });

                var aiDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAIServiceClient));
                if (aiDescriptor != null)
                {
                    services.Remove(aiDescriptor);
                }
                services.AddSingleton(mockAiClient.Object);
            });
        });

        _client = _factory.CreateClient();

        // Seed initial models and test image
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

        var imagePath = Path.Combine(_testStorageDir, "wrist_multi.png");
        File.WriteAllBytes(imagePath, TestImageGenerator.CreateValidPng(200, 200));

        db.XRayImages.Add(new XRayImage
        {
            Id = _testImageId,
            FileName = "wrist_multi.png",
            FilePath = "wrist_multi.png",
            ContentType = "image/png",
            FileSize = 500,
            CreatedAt = DateTime.UtcNow
        });

        var mA = new AIModel { Id = _modelAId, Name = "Model A (YOLO)", ModelKey = "model-a", Version = "1.0.0", Framework = "PyTorch", IsActive = true, IsAvailable = true };
        var mB = new AIModel { Id = _modelBId, Name = "Model B (FasterRCNN)", ModelKey = "model-b", Version = "2.0.0", Framework = "PyTorch", IsActive = true, IsAvailable = true };
        var mC = new AIModel { Id = _modelCId, Name = "Model C (ResNet)", ModelKey = "model-c", Version = "1.5.0", Framework = "ONNX", IsActive = true, IsAvailable = true };

        db.AIModels.AddRange(mA, mB, mC);
        db.SaveChanges();
    }

    [Fact]
    public async Task Scenario1_AllThreeModelsSucceed_IsolatedDetectionsAndAccurateComparison()
    {
        var analysisId = Guid.NewGuid();
        var runAId = Guid.NewGuid();
        var runBId = Guid.NewGuid();
        var runCId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

            var analysis = new Analysis
            {
                Id = analysisId,
                XRayImageId = _testImageId,
                Mode = AnalysisMode.CompareAll,
                Status = AnalysisStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            var runA = new AnalysisRun
            {
                Id = runAId,
                AnalysisId = analysisId,
                AIModelId = _modelAId,
                Status = AnalysisRunStatus.Completed,
                ProcessingTimeMs = 350,
                ResultImagePath = "wrist_multi.png",
                Detections = new List<Detection>
                {
                    new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.95m, X = 10, Y = 10, Width = 30, Height = 30 },
                    new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.91m, X = 50, Y = 50, Width = 40, Height = 40 }
                }
            };

            var runB = new AnalysisRun
            {
                Id = runBId,
                AnalysisId = analysisId,
                AIModelId = _modelBId,
                Status = AnalysisRunStatus.Completed,
                ProcessingTimeMs = 780,
                ResultImagePath = "wrist_multi.png",
                Detections = new List<Detection>
                {
                    new() { Id = Guid.NewGuid(), ClassName = "callus", Confidence = 0.82m, X = 20, Y = 20, Width = 35, Height = 35 }
                }
            };

            var runC = new AnalysisRun
            {
                Id = runCId,
                AnalysisId = analysisId,
                AIModelId = _modelCId,
                Status = AnalysisRunStatus.Completed,
                ProcessingTimeMs = 210,
                ResultImagePath = null,
                Detections = new List<Detection>() // 0 detections
            };

            analysis.AnalysisRuns.Add(runA);
            analysis.AnalysisRuns.Add(runB);
            analysis.AnalysisRuns.Add(runC);

            db.Analyses.Add(analysis);
            db.SaveChanges();
        }

        // Test GET /api/results/{analysisId}
        var res1 = await _client.GetAsync($"/api/results/{analysisId}");
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
        var body1 = await res1.Content.ReadFromJsonAsync<AnalysisResultDto>();
        Assert.NotNull(body1);
        Assert.Equal("Completed", body1.OverallStatus);
        Assert.Equal(3, body1.CompletedRunsCount);
        Assert.Equal(0, body1.FailedRunsCount);

        // Test GET /api/results/{analysisId}/comparison
        var res2 = await _client.GetAsync($"/api/results/{analysisId}/comparison");
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var comp = await res2.Content.ReadFromJsonAsync<AnalysisComparisonDto>();
        Assert.NotNull(comp);
        Assert.Equal(3, comp.Models.Count);

        var mA = comp.Models.First(m => m.RunId == runAId);
        Assert.Equal(2, mA.DetectionCount);
        Assert.Equal(0.35, mA.ProcessingTime);
        Assert.Equal(0.93m, mA.AverageConfidence); // (0.95 + 0.91) / 2

        var mB = comp.Models.First(m => m.RunId == runBId);
        Assert.Equal(1, mB.DetectionCount);
        Assert.Equal(0.78, mB.ProcessingTime);
        Assert.Equal(0.82m, mB.AverageConfidence);

        var mC = comp.Models.First(m => m.RunId == runCId);
        Assert.Equal(0, mC.DetectionCount);
        Assert.Equal(0.21, mC.ProcessingTime);
        Assert.Null(mC.AverageConfidence);
    }

    [Fact]
    public async Task Scenario2_OneModelSucceeds_TwoModelsFail_PreservesCompletedResultsInComparison()
    {
        var analysisId = Guid.NewGuid();
        var runSuccessId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

            var analysis = new Analysis
            {
                Id = analysisId,
                XRayImageId = _testImageId,
                Mode = AnalysisMode.CompareAll,
                Status = AnalysisStatus.PartiallyCompleted,
                CreatedAt = DateTime.UtcNow
            };

            var run1 = new AnalysisRun
            {
                Id = runSuccessId,
                AnalysisId = analysisId,
                AIModelId = _modelAId,
                Status = AnalysisRunStatus.Completed,
                ProcessingTimeMs = 400,
                Detections = new List<Detection>
                {
                    new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.96m, X = 15, Y = 15, Width = 30, Height = 30 }
                }
            };

            var run2 = new AnalysisRun
            {
                Id = Guid.NewGuid(),
                AnalysisId = analysisId,
                AIModelId = _modelBId,
                Status = AnalysisRunStatus.Failed,
                ErrorMessage = "AI service timeout on port 8000"
            };

            var run3 = new AnalysisRun
            {
                Id = Guid.NewGuid(),
                AnalysisId = analysisId,
                AIModelId = _modelCId,
                Status = AnalysisRunStatus.Failed,
                ErrorMessage = "CUDA out of memory error"
            };

            analysis.AnalysisRuns.Add(run1);
            analysis.AnalysisRuns.Add(run2);
            analysis.AnalysisRuns.Add(run3);

            db.Analyses.Add(analysis);
            db.SaveChanges();
        }

        var response = await _client.GetAsync($"/api/results/{analysisId}/comparison");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var comp = await response.Content.ReadFromJsonAsync<AnalysisComparisonDto>();
        Assert.NotNull(comp);
        Assert.Equal("PartiallyCompleted", comp.OverallStatus);
        Assert.Equal(3, comp.Models.Count);

        var successModel = comp.Models.First(m => m.RunId == runSuccessId);
        Assert.Equal("Completed", successModel.Status);
        Assert.Equal(1, successModel.DetectionCount);
        Assert.Equal(0.96m, successModel.AverageConfidence);
        Assert.Null(successModel.ErrorMessage);

        var failedModels = comp.Models.Where(m => m.Status == "Failed").ToList();
        Assert.Equal(2, failedModels.Count);
        foreach (var fm in failedModels)
        {
            Assert.Equal(0, fm.DetectionCount);
            Assert.Null(fm.AverageConfidence);
            Assert.Null(fm.ProcessingTime);
            Assert.NotNull(fm.ErrorMessage);
        }
    }

    [Fact]
    public async Task Scenario3_AllModelsFail_OverallStatusFailedAndNoGhostDetections()
    {
        var analysisId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

            var analysis = new Analysis
            {
                Id = analysisId,
                XRayImageId = _testImageId,
                Mode = AnalysisMode.MultiModel,
                Status = AnalysisStatus.Failed,
                CreatedAt = DateTime.UtcNow
            };

            var run1 = new AnalysisRun
            {
                Id = Guid.NewGuid(),
                AnalysisId = analysisId,
                AIModelId = _modelAId,
                Status = AnalysisRunStatus.Failed,
                ErrorMessage = "Connection refused"
            };

            var run2 = new AnalysisRun
            {
                Id = Guid.NewGuid(),
                AnalysisId = analysisId,
                AIModelId = _modelBId,
                Status = AnalysisRunStatus.Failed,
                ErrorMessage = "Connection reset by peer"
            };

            analysis.AnalysisRuns.Add(run1);
            analysis.AnalysisRuns.Add(run2);

            db.Analyses.Add(analysis);
            db.SaveChanges();
        }

        var response = await _client.GetAsync($"/api/results/{analysisId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnalysisResultDto>();
        Assert.NotNull(result);
        Assert.Equal("Failed", result.OverallStatus);
        Assert.Equal(0, result.CompletedRunsCount);
        Assert.Equal(2, result.FailedRunsCount);

        foreach (var run in result.Runs)
        {
            Assert.Equal("Failed", run.Status);
            Assert.Empty(run.Detections);
            Assert.Equal(0, run.DetectionCount);
        }
    }

    [Fact]
    public async Task Scenario4_RunsInProcessingAndPending_OverallStatusIsProcessing()
    {
        var analysisId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

            var analysis = new Analysis
            {
                Id = analysisId,
                XRayImageId = _testImageId,
                Mode = AnalysisMode.CompareAll,
                Status = AnalysisStatus.Processing,
                CreatedAt = DateTime.UtcNow
            };

            var runCompleted = new AnalysisRun
            {
                Id = Guid.NewGuid(),
                AnalysisId = analysisId,
                AIModelId = _modelAId,
                Status = AnalysisRunStatus.Completed,
                ProcessingTimeMs = 300,
                Detections = new List<Detection>
                {
                    new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.90m, X = 10, Y = 10, Width = 20, Height = 20 }
                }
            };

            var runProcessing = new AnalysisRun
            {
                Id = Guid.NewGuid(),
                AnalysisId = analysisId,
                AIModelId = _modelBId,
                Status = AnalysisRunStatus.Processing,
                StartedAt = DateTime.UtcNow
            };

            var runPending = new AnalysisRun
            {
                Id = Guid.NewGuid(),
                AnalysisId = analysisId,
                AIModelId = _modelCId,
                Status = AnalysisRunStatus.Pending
            };

            analysis.AnalysisRuns.Add(runCompleted);
            analysis.AnalysisRuns.Add(runProcessing);
            analysis.AnalysisRuns.Add(runPending);

            db.Analyses.Add(analysis);
            db.SaveChanges();
        }

        var response = await _client.GetAsync($"/api/results/{analysisId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnalysisResultDto>();
        Assert.NotNull(result);
        Assert.Equal("Processing", result.OverallStatus);
        Assert.Equal(1, result.CompletedRunsCount);
        Assert.Equal(0, result.FailedRunsCount);
        Assert.Equal(3, result.TotalRunsCount);
    }

    [Fact]
    public async Task Scenario5_DynamicTwoModelsSelection_DoesNotHardcodeThreeModels()
    {
        var request = new SubmitAnalysisRequest
        {
            XRayImageId = _testImageId,
            Mode = AnalysisMode.MultiModel,
            SelectedModelIds = new List<Guid> { _modelAId, _modelBId }
        };

        var response = await _client.PostAsJsonAsync("/api/analysis", request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var submission = await response.Content.ReadFromJsonAsync<AnalysisSubmissionResponse>();
        Assert.NotNull(submission);
        Assert.Equal(2, submission.AnalysisRunIds.Count);

        var detailRes = await _client.GetAsync($"/api/results/{submission.AnalysisId}");
        Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);
        var detail = await detailRes.Content.ReadFromJsonAsync<AnalysisResultDto>();
        Assert.NotNull(detail);
        Assert.Equal(2, detail.Runs.Count);
        Assert.Contains(detail.Runs, r => r.Model.ModelId == _modelAId);
        Assert.Contains(detail.Runs, r => r.Model.ModelId == _modelBId);
        Assert.DoesNotContain(detail.Runs, r => r.Model.ModelId == _modelCId);
    }

    [Fact]
    public async Task Scenario6_StrictDetectionIsolation_NoCrossModelContamination()
    {
        var runAId = Guid.NewGuid();
        var runBId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

            var analysis = new Analysis
            {
                Id = Guid.NewGuid(),
                XRayImageId = _testImageId,
                Mode = AnalysisMode.MultiModel,
                Status = AnalysisStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            var detA = new Detection { Id = Guid.NewGuid(), ClassName = "radius_fracture", Confidence = 0.99m, X = 1, Y = 2, Width = 3, Height = 4 };
            var detB = new Detection { Id = Guid.NewGuid(), ClassName = "ulna_fracture", Confidence = 0.88m, X = 5, Y = 6, Width = 7, Height = 8 };

            var runA = new AnalysisRun { Id = runAId, AnalysisId = analysis.Id, AIModelId = _modelAId, Status = AnalysisRunStatus.Completed, Detections = new List<Detection> { detA } };
            var runB = new AnalysisRun { Id = runBId, AnalysisId = analysis.Id, AIModelId = _modelBId, Status = AnalysisRunStatus.Completed, Detections = new List<Detection> { detB } };

            analysis.AnalysisRuns.Add(runA);
            analysis.AnalysisRuns.Add(runB);

            db.Analyses.Add(analysis);
            db.SaveChanges();
        }

        // Check Run A
        var resA = await _client.GetAsync($"/api/results/runs/{runAId}");
        var runAResult = await resA.Content.ReadFromJsonAsync<AnalysisRunResultDto>();
        Assert.NotNull(runAResult);
        Assert.Single(runAResult.Detections);
        Assert.Equal("radius_fracture", runAResult.Detections[0].ClassLabel);

        // Check Run B
        var resB = await _client.GetAsync($"/api/results/runs/{runBId}");
        var runBResult = await resB.Content.ReadFromJsonAsync<AnalysisRunResultDto>();
        Assert.NotNull(runBResult);
        Assert.Single(runBResult.Detections);
        Assert.Equal("ulna_fracture", runBResult.Detections[0].ClassLabel);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testStorageDir))
            {
                Directory.Delete(_testStorageDir, true);
            }
        }
        catch { }
    }
}
