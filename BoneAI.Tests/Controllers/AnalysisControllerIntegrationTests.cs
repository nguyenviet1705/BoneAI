using System.Net;
using System.Net.Http.Json;
using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Infrastructure.Configuration;
using BoneAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BoneAI.Tests.Controllers;

public class AnalysisControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testStorageDir;
    private readonly Guid _testImageId = Guid.NewGuid();
    private readonly Guid _testModelId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public AnalysisControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _testStorageDir = Path.Combine(Path.GetTempPath(), "BoneAI_Analysis_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDir);

        var dbName = "AnalysisIntegrationDb_" + Guid.NewGuid().ToString("N");

        var mockAiClient = new Mock<IAIServiceClient>();
        mockAiClient.Setup(c => c.CheckHealthAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockAiClient.Setup(c => c.CheckReadinessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockAiClient.Setup(c => c.ExecuteInferenceAsync(It.IsAny<InferenceClientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InferenceClientResponse
            {
                ModelKey = "mock-bone-fracture-v1",
                ImageWidth = 800,
                ImageHeight = 600,
                ProcessingTimeMs = 30,
                Detections = new List<InferenceDetectionItem>
                {
                    new() { ClassName = "fracture", Confidence = 0.9m, X = 10, Y = 10, Width = 50, Height = 50 }
                }
            });

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                // Replace DbContext with InMemory
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BoneAIDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<BoneAIDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });

                // Configure test storage path
                services.Configure<ImageStorageOptions>(options =>
                {
                    options.RootPath = _testStorageDir;
                });

                // Replace IAIServiceClient with mock
                var aiDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAIServiceClient));
                if (aiDescriptor != null)
                {
                    services.Remove(aiDescriptor);
                }
                services.AddSingleton(mockAiClient.Object);
            });
        });

        _client = _factory.CreateClient();

        // Seed initial data
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

        var imageFile = Path.Combine(_testStorageDir, "sample.jpg");
        File.WriteAllBytes(imageFile, new byte[] { 1, 2, 3 });

        db.XRayImages.Add(new XRayImage
        {
            Id = _testImageId,
            FileName = "sample.jpg",
            FilePath = "sample.jpg",
            ContentType = "image/jpeg",
            FileSize = 3,
            CreatedAt = DateTime.UtcNow
        });

        db.AIModels.Add(new AIModel
        {
            Id = _testModelId,
            Name = "mock-bone-fracture-v1",
            Version = "1.0.0",
            IsActive = true
        });

        db.SaveChanges();
    }

    [Fact]
    public async Task Submit_ValidRequest_Returns202Accepted()
    {
        var request = new SubmitAnalysisRequest
        {
            XRayImageId = _testImageId,
            AIModelId = _testModelId,
            IdempotencyKey = "key-" + Guid.NewGuid()
        };

        var response = await _client.PostAsJsonAsync("/api/analysis", request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisSubmissionResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.AnalysisId);
        Assert.NotEqual(Guid.Empty, body.AnalysisRunId);
        Assert.Equal("Pending", body.Status);
    }

    [Fact]
    public async Task GetStatus_ExistingAnalysis_Returns200OK()
    {
        // Submit first
        var request = new SubmitAnalysisRequest
        {
            XRayImageId = _testImageId,
            AIModelId = _testModelId
        };
        var submitRes = await _client.PostAsJsonAsync("/api/analysis", request);
        var subBody = await submitRes.Content.ReadFromJsonAsync<AnalysisSubmissionResponse>();

        var statusRes = await _client.GetAsync($"/api/analysis/{subBody!.AnalysisId}");

        Assert.Equal(HttpStatusCode.OK, statusRes.StatusCode);
        var statusBody = await statusRes.Content.ReadFromJsonAsync<AnalysisDetailResponse>();
        Assert.NotNull(statusBody);
        Assert.Equal(subBody.AnalysisId, statusBody.AnalysisId);
    }

    [Fact]
    public async Task GetModels_ReturnsActiveModels()
    {
        var response = await _client.GetAsync("/api/analysis/models");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CheckAIServiceHealth_ReturnsLivenessAndReadiness()
    {
        var response = await _client.GetAsync("/api/analysis/ai-service-health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testStorageDir))
            {
                Directory.Delete(_testStorageDir, recursive: true);
            }
        }
        catch
        {
            // Ignore
        }
    }
}
