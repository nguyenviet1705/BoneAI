using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BoneAI.API.Models;
using BoneAI.Application.DTOs;
using BoneAI.Infrastructure.Configuration;
using BoneAI.Infrastructure.Persistence;
using BoneAI.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BoneAI.Tests.Controllers;

public class ImagesControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testStorageDir;

    public ImagesControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _testStorageDir = Path.Combine(Path.GetTempPath(), "BoneAI_Integration_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDir);

        var dbName = "IntegrationTestDb_" + Guid.NewGuid().ToString("N");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BoneAIDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Use InMemory database for testing
                services.AddDbContext<BoneAIDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });

                // Configure test storage path
                services.Configure<ImageStorageOptions>(options =>
                {
                    options.RootPath = _testStorageDir;
                    options.MaxFileSizeInBytes = 10 * 1024 * 1024;
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Upload_ValidJpeg_Returns201Created()
    {
        var jpegBytes = TestImageGenerator.CreateValidJpeg(100, 100);
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "wrist_test.jpg");

        var response = await _client.PostAsync("/api/images/upload", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var image = await response.Content.ReadFromJsonAsync<ImageResponse>();
        Assert.NotNull(image);
        Assert.NotEqual(Guid.Empty, image.Id);
        Assert.Equal("wrist_test.jpg", image.FileName);
        Assert.Equal("image/jpeg", image.ContentType);
        Assert.Equal($"/api/images/{image.Id}/file", image.FileUrl);
    }

    [Fact]
    public async Task Upload_UnsupportedFormat_Returns415()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "document.pdf");

        var response = await _client.PostAsync("/api/images/upload", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(415, error.StatusCode);
    }

    [Fact]
    public async Task Upload_NoFile_Returns400()
    {
        using var content = new MultipartFormDataContent();
        var response = await _client.PostAsync("/api/images/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(400, error.StatusCode);
    }

    [Fact]
    public async Task GetList_Returns200WithPagination()
    {
        // Upload an image first
        var jpegBytes = TestImageGenerator.CreateValidJpeg(60, 60);
        using (var content = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(jpegBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "file", "list_test.jpg");
            await _client.PostAsync("/api/images/upload", content);
        }

        var response = await _client.GetAsync("/api/images?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<ImagePaginationResponse>();
        Assert.NotNull(paged);
        Assert.True(paged.TotalCount >= 1);
        Assert.NotEmpty(paged.Items);
    }

    [Fact]
    public async Task GetById_ExistingAndNonexistent_BehavesCorrectly()
    {
        // Upload
        var pngBytes = TestImageGenerator.CreateValidPng(50, 50);
        Guid imageId;
        using (var content = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(pngBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "file", "get_by_id.png");
            var uploadRes = await _client.PostAsync("/api/images/upload", content);
            var uploaded = await uploadRes.Content.ReadFromJsonAsync<ImageResponse>();
            imageId = uploaded!.Id;
        }

        // Existing
        var response = await _client.GetAsync($"/api/images/{imageId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var image = await response.Content.ReadFromJsonAsync<ImageResponse>();
        Assert.Equal(imageId, image!.Id);

        // Nonexistent
        var notFoundResponse = await _client.GetAsync($"/api/images/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
    }

    [Fact]
    public async Task GetFile_ExistingImage_ReturnsImageStream()
    {
        var jpegBytes = TestImageGenerator.CreateValidJpeg(80, 80);
        Guid imageId;
        using (var content = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(jpegBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "file", "download_test.jpg");
            var uploadRes = await _client.PostAsync("/api/images/upload", content);
            var uploaded = await uploadRes.Content.ReadFromJsonAsync<ImageResponse>();
            imageId = uploaded!.Id;
        }

        var fileResponse = await _client.GetAsync($"/api/images/{imageId}/file");

        Assert.Equal(HttpStatusCode.OK, fileResponse.StatusCode);
        Assert.Equal("image/jpeg", fileResponse.Content.Headers.ContentType?.MediaType);
        var bytes = await fileResponse.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task Delete_ExistingImage_DeletesAndReturns204()
    {
        var jpegBytes = TestImageGenerator.CreateValidJpeg(50, 50);
        Guid imageId;
        using (var content = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(jpegBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "file", "to_delete.jpg");
            var uploadRes = await _client.PostAsync("/api/images/upload", content);
            var uploaded = await uploadRes.Content.ReadFromJsonAsync<ImageResponse>();
            imageId = uploaded!.Id;
        }

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/images/{imageId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verification after deletion: 404
        var getResponse = await _client.GetAsync($"/api/images/{imageId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonexistentImage_Returns404()
    {
        var deleteResponse = await _client.DeleteAsync($"/api/images/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
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
