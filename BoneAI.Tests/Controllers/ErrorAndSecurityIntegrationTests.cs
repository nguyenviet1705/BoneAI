using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BoneAI.API.Models;
using BoneAI.Application.DTOs;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Infrastructure.Configuration;
using BoneAI.Infrastructure.Persistence;
using BoneAI.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BoneAI.Tests.Controllers;

public class ErrorAndSecurityIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testStorageDir;

    private readonly Guid _testImageId = Guid.NewGuid();
    private readonly Guid _testModelId = Guid.NewGuid();
    private readonly Guid _testAnalysisId = Guid.NewGuid();

    public ErrorAndSecurityIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _testStorageDir = Path.Combine(Path.GetTempPath(), "BoneAI_Error_Sec_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDir);

        var dbName = "ErrorAndSecDb_" + Guid.NewGuid().ToString("N");

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
                    options.MaxFileSizeInBytes = 10 * 1024 * 1024; // 10MB
                });
            });
        });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

        var imagePath = Path.Combine(_testStorageDir, "sec_wrist.png");
        File.WriteAllBytes(imagePath, TestImageGenerator.CreateValidPng(100, 100));

        db.XRayImages.Add(new XRayImage
        {
            Id = _testImageId,
            FileName = "sec_wrist.png",
            FilePath = "sec_wrist.png",
            ContentType = "image/png",
            FileSize = 100,
            CreatedAt = DateTime.UtcNow
        });

        var model = new AIModel
        {
            Id = _testModelId,
            Name = "Model Sec",
            ModelKey = "model-sec",
            Version = "1.0.0",
            IsActive = true,
            IsAvailable = true
        };
        db.AIModels.Add(model);

        var analysis = new Analysis
        {
            Id = _testAnalysisId,
            XRayImageId = _testImageId,
            Mode = AnalysisMode.SingleModel,
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            AnalysisRuns = new List<AnalysisRun>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AIModelId = _testModelId,
                    Status = AnalysisRunStatus.Completed
                }
            }
        };
        db.Analyses.Add(analysis);
        db.SaveChanges();
    }

    [Fact]
    public async Task Upload_EmptyFile_Returns400BadRequestWithErrorEnvelope()
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "File", "empty.jpg");

        var response = await _client.PostAsync("/api/images/upload", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(400, error.StatusCode);
        Assert.NotEmpty(error.TraceId);
    }

    [Fact]
    public async Task Upload_FileExceedsMaxLimit_Returns413PayloadTooLarge()
    {
        // 11MB file (limit is 10MB)
        var largeBytes = new byte[11 * 1024 * 1024];
        // Put JPEG magic bytes at header so it passes format check and hits size check
        largeBytes[0] = 0xFF;
        largeBytes[1] = 0xD8;
        largeBytes[2] = 0xFF;

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(largeBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "File", "too_large.jpg");

        var response = await _client.PostAsync("/api/images/upload", content);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Upload_FakeExtensionWithNonImageMagicBytes_Returns415UnsupportedMediaType()
    {
        // Text bytes named as .png
        var fakeBytes = System.Text.Encoding.UTF8.GetBytes("MZ_NOT_AN_IMAGE_FILE_PAYLOAD");

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(fakeBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "File", "fake_malicious.png");

        var response = await _client.PostAsync("/api/images/upload", content);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task SubmitAnalysis_NonExistentImage_Returns404NotFound()
    {
        var request = new SubmitAnalysisRequest
        {
            XRayImageId = Guid.NewGuid(),
            AIModelId = _testModelId
        };

        var response = await _client.PostAsJsonAsync("/api/analysis", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(404, error.StatusCode);
        Assert.Contains("was not found", error.Message);
    }

    [Fact]
    public async Task DeleteImage_WhenReferencedByAnalysis_Returns409ConflictAndPreservesFile()
    {
        // Image _testImageId is referenced by _testAnalysisId
        var response = await _client.DeleteAsync($"/api/images/{_testImageId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(409, error.StatusCode);
        Assert.Contains("referenced by existing analysis records", error.Message);

        // Verify image was not deleted
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();
        var image = await db.XRayImages.FindAsync(_testImageId);
        Assert.NotNull(image);
        Assert.True(File.Exists(Path.Combine(_testStorageDir, "sec_wrist.png")));
    }

    [Fact]
    public async Task SoftDeleteAnalysis_HidesFromHistoryAndPreservesXRayImageAndAIModel()
    {
        var response = await _client.DeleteAsync($"/api/history/{_testAnalysisId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verification 1: Cannot query via /api/history/{id}
        var getRes = await _client.GetAsync($"/api/history/{_testAnalysisId}");
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);

        // Verification 2: Original image was NOT deleted
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();
            var origImg = await db.XRayImages.FindAsync(_testImageId);
            Assert.NotNull(origImg);

            // Model was NOT deleted
            var model = await db.AIModels.FindAsync(_testModelId);
            Assert.NotNull(model);
        }
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
