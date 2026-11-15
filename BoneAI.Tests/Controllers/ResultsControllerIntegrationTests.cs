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

public class ResultsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testStorageDir;

    private readonly Guid _testImageId = Guid.NewGuid();
    private readonly Guid _testAnalysisId = Guid.NewGuid();
    private readonly Guid _testRun1Id = Guid.NewGuid();
    private readonly Guid _testRun2Id = Guid.NewGuid();
    private readonly Guid _testRun3Id = Guid.NewGuid();
    private readonly Guid _model1Id = Guid.NewGuid();
    private readonly Guid _model2Id = Guid.NewGuid();
    private readonly Guid _model3Id = Guid.NewGuid();

    public ResultsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _testStorageDir = Path.Combine(Path.GetTempPath(), "BoneAI_Results_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDir);

        var dbName = "ResultsIntegrationDb_" + Guid.NewGuid().ToString("N");

        var mockAiClient = new Mock<IAIServiceClient>();
        mockAiClient.Setup(c => c.CheckHealthAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

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

        // Seed test data
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

        // Create test image files in storage
        var origFilePath = Path.Combine(_testStorageDir, "orig_wrist.png");
        File.WriteAllBytes(origFilePath, TestImageGenerator.CreateValidPng(200, 200));

        var resultFilePath = Path.Combine(_testStorageDir, "result_run1.png");
        File.WriteAllBytes(resultFilePath, TestImageGenerator.CreateValidPng(200, 200));

        db.XRayImages.Add(new XRayImage
        {
            Id = _testImageId,
            FileName = "orig_wrist.png",
            FilePath = "orig_wrist.png",
            ContentType = "image/png",
            FileSize = 1024,
            CreatedAt = DateTime.UtcNow
        });

        var model1 = new AIModel { Id = _model1Id, Name = "Model One", ModelKey = "model-one", Version = "1.0.0", Framework = "PyTorch", IsActive = true };
        var model2 = new AIModel { Id = _model2Id, Name = "Model Two", ModelKey = "model-two", Version = "2.0.0", Framework = "ONNX", IsActive = true };
        var model3 = new AIModel { Id = _model3Id, Name = "Model Three", ModelKey = "model-three", Version = "1.5.0", Framework = "TensorFlow", IsActive = true };

        db.AIModels.AddRange(model1, model2, model3);

        var analysis = new Analysis
        {
            Id = _testAnalysisId,
            XRayImageId = _testImageId,
            Status = AnalysisStatus.PartiallyCompleted,
            Mode = AnalysisMode.CompareAll,
            CreatedAt = DateTime.UtcNow
        };

        var run1 = new AnalysisRun
        {
            Id = _testRun1Id,
            AnalysisId = _testAnalysisId,
            AIModelId = _model1Id,
            Status = AnalysisRunStatus.Completed,
            ProcessingTimeMs = 450,
            ResultImagePath = "result_run1.png",
            Detections = new List<Detection>
            {
                new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.94m, X = 20, Y = 30, Width = 50, Height = 60 },
                new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.86m, X = 80, Y = 90, Width = 40, Height = 40 }
            }
        };

        var run2 = new AnalysisRun
        {
            Id = _testRun2Id,
            AnalysisId = _testAnalysisId,
            AIModelId = _model2Id,
            Status = AnalysisRunStatus.Completed,
            ProcessingTimeMs = 720,
            ResultImagePath = null,
            Detections = new List<Detection>() // 0 detections
        };

        var run3 = new AnalysisRun
        {
            Id = _testRun3Id,
            AnalysisId = _testAnalysisId,
            AIModelId = _model3Id,
            Status = AnalysisRunStatus.Failed,
            ErrorMessage = "Inference endpoint error",
            ResultImagePath = null
        };

        analysis.AnalysisRuns.Add(run1);
        analysis.AnalysisRuns.Add(run2);
        analysis.AnalysisRuns.Add(run3);

        db.Analyses.Add(analysis);
        db.SaveChanges();
    }

    [Fact]
    public async Task GetAnalysisResult_ExistingAnalysis_Returns200OKWithAllRuns()
    {
        var response = await _client.GetAsync($"/api/results/{_testAnalysisId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisResultDto>();

        Assert.NotNull(body);
        Assert.Equal(_testAnalysisId, body.AnalysisId);
        Assert.Equal(_testImageId, body.ImageId);
        Assert.Equal("PartiallyCompleted", body.OverallStatus);
        Assert.Equal("orig_wrist.png", body.OriginalImage.FileName);
        Assert.Equal(3, body.Runs.Count);
        Assert.Equal(2, body.CompletedRunsCount);
        Assert.Equal(1, body.FailedRunsCount);

        // Verify run 1
        var r1 = body.Runs.First(r => r.RunId == _testRun1Id);
        Assert.Equal("Completed", r1.Status);
        Assert.Equal(0.45, r1.ProcessingTime);
        Assert.Equal(2, r1.DetectionCount);
        Assert.Equal($"/api/results/runs/{_testRun1Id}/image", r1.ResultImageUrl);

        // Verify run 3 (failed)
        var r3 = body.Runs.First(r => r.RunId == _testRun3Id);
        Assert.Equal("Failed", r3.Status);
        Assert.Equal("Inference endpoint error", r3.ErrorMessage);
        Assert.Empty(r3.Detections);
    }

    [Fact]
    public async Task GetAnalysisResult_NonExistentAnalysis_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/results/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAnalysisRunResult_ExistingRun_Returns200OKWithDetections()
    {
        var response = await _client.GetAsync($"/api/results/runs/{_testRun1Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisRunResultDto>();

        Assert.NotNull(body);
        Assert.Equal(_testRun1Id, body.RunId);
        Assert.Equal("Model One", body.Model.ModelName);
        Assert.Equal("1.0.0", body.Model.Version);
        Assert.Equal(2, body.DetectionCount);
        Assert.Equal(0.45, body.ProcessingTime);

        var firstDet = body.Detections[0];
        Assert.Equal("fracture", firstDet.ClassLabel);
        Assert.Equal(0.94m, firstDet.ConfidenceScore);
        Assert.Equal(20, firstDet.BoundingBox.X);
    }

    [Fact]
    public async Task GetAnalysisRunResult_NonExistentRun_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/results/runs/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAnalysisComparison_ExistingAnalysis_Returns200OKWithComparisonData()
    {
        var response = await _client.GetAsync($"/api/results/{_testAnalysisId}/comparison");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisComparisonDto>();

        Assert.NotNull(body);
        Assert.Equal(_testAnalysisId, body.AnalysisId);
        Assert.Equal(_testImageId, body.ImageId);
        Assert.Equal($"/api/images/{_testImageId}/file", body.OriginalImageUrl);
        Assert.Equal(3, body.Models.Count);

        // Model 1: avg confidence = (0.94 + 0.86) / 2 = 0.90
        var m1 = body.Models.First(m => m.RunId == _testRun1Id);
        Assert.Equal("Completed", m1.Status);
        Assert.Equal(2, m1.DetectionCount);
        Assert.Equal(0.90m, m1.AverageConfidence);
        Assert.Equal(0.45, m1.ProcessingTime);
        Assert.Equal($"/api/results/runs/{_testRun1Id}/image", m1.ResultImageUrl);

        // Model 2: 0 detections -> Avg null
        var m2 = body.Models.First(m => m.RunId == _testRun2Id);
        Assert.Equal("Completed", m2.Status);
        Assert.Equal(0, m2.DetectionCount);
        Assert.Null(m2.AverageConfidence);
        Assert.Equal(0.72, m2.ProcessingTime);

        // Model 3: Failed -> Avg null, Processing null, DetectionCount 0
        var m3 = body.Models.First(m => m.RunId == _testRun3Id);
        Assert.Equal("Failed", m3.Status);
        Assert.Equal(0, m3.DetectionCount);
        Assert.Null(m3.AverageConfidence);
        Assert.Null(m3.ProcessingTime);
        Assert.Equal("Inference endpoint error", m3.ErrorMessage);
    }

    [Fact]
    public async Task GetAnalysisComparison_NonExistentAnalysis_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/results/{Guid.NewGuid()}/comparison");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRunResultImage_ExistingImage_Returns200OKWithImageBytes()
    {
        var response = await _client.GetAsync($"/api/results/runs/{_testRun1Id}/image");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task GetRunResultImage_WhenNoResultImage_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/results/runs/{_testRun2Id}/image");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
        catch
        {
            // Ignore test cleanup exceptions
        }
    }
}
