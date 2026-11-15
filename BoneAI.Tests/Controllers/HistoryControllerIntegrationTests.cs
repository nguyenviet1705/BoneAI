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

public class HistoryControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testStorageDir;

    private readonly Guid _testImageId = Guid.NewGuid();
    private readonly Guid _model1Id = Guid.NewGuid();
    private readonly Guid _model2Id = Guid.NewGuid();

    private readonly Guid _analysis1Id = Guid.NewGuid();
    private readonly Guid _analysis2Id = Guid.NewGuid();
    private readonly Guid _analysis3Id = Guid.NewGuid();

    public HistoryControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _testStorageDir = Path.Combine(Path.GetTempPath(), "BoneAI_History_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDir);

        var dbName = "HistoryIntegrationDb_" + Guid.NewGuid().ToString("N");

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

        // Seed data
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

        var imagePath = Path.Combine(_testStorageDir, "orig_wrist.png");
        File.WriteAllBytes(imagePath, TestImageGenerator.CreateValidPng(100, 100));

        var resultPath1 = Path.Combine(_testStorageDir, "result_1.png");
        File.WriteAllBytes(resultPath1, TestImageGenerator.CreateValidPng(100, 100));

        db.XRayImages.Add(new XRayImage
        {
            Id = _testImageId,
            FileName = "orig_wrist.png",
            FilePath = "orig_wrist.png",
            ContentType = "image/png",
            FileSize = 100,
            CreatedAt = DateTime.UtcNow
        });

        var m1 = new AIModel { Id = _model1Id, Name = "Model 1", ModelKey = "m1", Version = "1.0", IsActive = true };
        var m2 = new AIModel { Id = _model2Id, Name = "Model 2", ModelKey = "m2", Version = "2.0", IsActive = true };
        db.AIModels.AddRange(m1, m2);

        // Analysis 1: Completed, CreatedAt = Now - 2h
        var a1 = new Analysis
        {
            Id = _analysis1Id,
            XRayImageId = _testImageId,
            Status = AnalysisStatus.Completed,
            Mode = AnalysisMode.SingleModel,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            AnalysisRuns = new List<AnalysisRun>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AIModelId = _model1Id,
                    Status = AnalysisRunStatus.Completed,
                    ProcessingTimeMs = 500,
                    ResultImagePath = "result_1.png",
                    Detections = new List<Detection>
                    {
                        new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.95m, X = 10, Y = 10, Width = 20, Height = 20 }
                    }
                }
            }
        };

        // Analysis 2: Failed, CreatedAt = Now - 1h
        var a2 = new Analysis
        {
            Id = _analysis2Id,
            XRayImageId = _testImageId,
            Status = AnalysisStatus.Failed,
            Mode = AnalysisMode.SingleModel,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            AnalysisRuns = new List<AnalysisRun>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AIModelId = _model2Id,
                    Status = AnalysisRunStatus.Failed,
                    ErrorMessage = "AI service timeout"
                }
            }
        };

        // Analysis 3: PartiallyCompleted, CreatedAt = Now
        var a3 = new Analysis
        {
            Id = _analysis3Id,
            XRayImageId = _testImageId,
            Status = AnalysisStatus.PartiallyCompleted,
            Mode = AnalysisMode.MultiModel,
            CreatedAt = DateTime.UtcNow,
            AnalysisRuns = new List<AnalysisRun>
            {
                new() { Id = Guid.NewGuid(), AIModelId = _model1Id, Status = AnalysisRunStatus.Completed, ProcessingTimeMs = 400 },
                new() { Id = Guid.NewGuid(), AIModelId = _model2Id, Status = AnalysisRunStatus.Failed, ErrorMessage = "Error" }
            }
        };

        db.Analyses.AddRange(a1, a2, a3);
        db.SaveChanges();
    }

    [Fact]
    public async Task GetList_DefaultPagination_ReturnsFirstPageOrderedByCreatedAtDesc()
    {
        var response = await _client.GetAsync("/api/history?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisHistoryResponse>();

        Assert.NotNull(body);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal(1, body.Pagination.Page);
        Assert.Equal(2, body.Pagination.PageSize);
        Assert.Equal(3, body.Pagination.TotalItems);
        Assert.Equal(2, body.Pagination.TotalPages);
        Assert.True(body.Pagination.HasNextPage);
        Assert.False(body.Pagination.HasPreviousPage);

        // First item should be newest (_analysis3Id)
        Assert.Equal(_analysis3Id, body.Items[0].AnalysisId);
        Assert.Equal(_analysis2Id, body.Items[1].AnalysisId);
    }

    [Fact]
    public async Task GetList_SecondPage_HasPreviousPageIsTrueAndHasNextPageIsFalse()
    {
        var response = await _client.GetAsync("/api/history?page=2&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisHistoryResponse>();

        Assert.NotNull(body);
        Assert.Single(body.Items);
        Assert.Equal(_analysis1Id, body.Items[0].AnalysisId);
        Assert.False(body.Pagination.HasNextPage);
        Assert.True(body.Pagination.HasPreviousPage);
    }

    [Fact]
    public async Task GetList_InvalidPageOrPageSize_Returns400BadRequest()
    {
        var r1 = await _client.GetAsync("/api/history?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);

        var r2 = await _client.GetAsync("/api/history?pageSize=150");
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
    }

    [Fact]
    public async Task GetList_FilterStatus_ReturnsMatchingItemsOnly()
    {
        var response = await _client.GetAsync("/api/history?status=Completed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisHistoryResponse>();

        Assert.NotNull(body);
        Assert.Single(body.Items);
        Assert.Equal(_analysis1Id, body.Items[0].AnalysisId);
        Assert.Equal("Completed", body.Items[0].Status);
    }

    [Fact]
    public async Task GetList_InvalidStatus_Returns400BadRequest()
    {
        var response = await _client.GetAsync("/api/history?status=InvalidStatus");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetList_FilterModelId_ReturnsAnalysesContainingModel()
    {
        // Model 2 is present in Analysis 2 and Analysis 3
        var response = await _client.GetAsync($"/api/history?modelId={_model2Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisHistoryResponse>();

        Assert.NotNull(body);
        Assert.Equal(2, body.Items.Count);
        Assert.Contains(body.Items, i => i.AnalysisId == _analysis2Id);
        Assert.Contains(body.Items, i => i.AnalysisId == _analysis3Id);
    }

    [Fact]
    public async Task GetList_SortOrderAsc_ReturnsOldestFirst()
    {
        var response = await _client.GetAsync("/api/history?sortBy=createdAt&sortOrder=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisHistoryResponse>();

        Assert.NotNull(body);
        Assert.Equal(3, body.Items.Count);
        Assert.Equal(_analysis1Id, body.Items[0].AnalysisId);
        Assert.Equal(_analysis2Id, body.Items[1].AnalysisId);
        Assert.Equal(_analysis3Id, body.Items[2].AnalysisId);
    }

    [Fact]
    public async Task GetDetail_ExistingId_Returns200OKWithFullAnalysisResult()
    {
        var response = await _client.GetAsync($"/api/history/{_analysis1Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisResultDto>();

        Assert.NotNull(body);
        Assert.Equal(_analysis1Id, body.AnalysisId);
        Assert.Equal("Completed", body.OverallStatus);
        Assert.Single(body.Runs);
        Assert.Equal(0.5, body.Runs[0].ProcessingTime);
        Assert.Single(body.Runs[0].Detections);
        Assert.Equal("fracture", body.Runs[0].Detections[0].ClassLabel);
    }

    [Fact]
    public async Task GetDetail_NonExistentId_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/history/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingAnalysis_SoftDeletesHidesFromListAndDetail()
    {
        // Create an analysis specifically to delete
        var deleteAnalysisId = Guid.NewGuid();
        var resultImgPath = Path.Combine(_testStorageDir, "to_delete.png");
        File.WriteAllBytes(resultImgPath, TestImageGenerator.CreateValidPng(10, 10));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();
            var analysis = new Analysis
            {
                Id = deleteAnalysisId,
                XRayImageId = _testImageId,
                Status = AnalysisStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                AnalysisRuns = new List<AnalysisRun>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AIModelId = _model1Id,
                        Status = AnalysisRunStatus.Completed,
                        ResultImagePath = "to_delete.png"
                    }
                }
            };
            db.Analyses.Add(analysis);
            db.SaveChanges();
        }

        // Delete via API
        var deleteResponse = await _client.DeleteAsync($"/api/history/{deleteAnalysisId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verification 1: Detail endpoint returns 404
        var detailResponse = await _client.GetAsync($"/api/history/{deleteAnalysisId}");
        Assert.Equal(HttpStatusCode.NotFound, detailResponse.StatusCode);

        // Verification 2: History list does not contain the deleted ID
        var listResponse = await _client.GetAsync("/api/history");
        var listBody = await listResponse.Content.ReadFromJsonAsync<AnalysisHistoryResponse>();
        Assert.NotNull(listBody);
        Assert.DoesNotContain(listBody.Items, i => i.AnalysisId == deleteAnalysisId);

        // Verification 3: Original XRayImage still exists
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();
            var origImg = await db.XRayImages.FindAsync(_testImageId);
            Assert.NotNull(origImg);

            // AIModel still exists
            var model = await db.AIModels.FindAsync(_model1Id);
            Assert.NotNull(model);
        }
    }

    [Fact]
    public async Task Delete_NonExistentAnalysis_Returns404NotFound()
    {
        var response = await _client.DeleteAsync($"/api/history/{Guid.NewGuid()}");
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
