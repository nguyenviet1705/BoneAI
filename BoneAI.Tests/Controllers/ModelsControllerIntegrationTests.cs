using System.Net;
using System.Net.Http.Json;
using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BoneAI.Tests.Controllers;

public class ModelsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Guid _testModelId = Guid.NewGuid();

    public ModelsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "ModelsIntegrationDb_" + Guid.NewGuid().ToString("N");

        var mockAiClient = new Mock<IAIServiceClient>();
        mockAiClient.Setup(c => c.CheckHealthAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockAiClient.Setup(c => c.CheckReadinessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockAiClient.Setup(c => c.TriggerModelDiscoveryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIServiceModelSyncResultDto());
        mockAiClient.Setup(c => c.GetModelCatalogAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AIServiceModelDto>
            {
                new()
                {
                    ModelKey = "test-sync-model",
                    ModelName = "Test Sync Model",
                    Version = "1.0.0",
                    Framework = "MockProvider",
                    Status = "Loaded",
                    IsActive = true
                }
            });
        mockAiClient.Setup(c => c.GetModelDetailsAsync("test-model-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIServiceModelDto
            {
                ModelKey = "test-model-key",
                ModelName = "Test Model",
                Version = "1.0.0",
                Framework = "MockProvider",
                Status = "Loaded"
            });

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

                var aiClientDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAIServiceClient));
                if (aiClientDescriptor != null)
                {
                    services.Remove(aiClientDescriptor);
                }
                services.AddSingleton(mockAiClient.Object);
            });
        });

        _client = _factory.CreateClient();

        // Seed test model
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();
        db.Database.EnsureCreated();
        db.AIModels.Add(new AIModel
        {
            Id = _testModelId,
            ModelKey = "test-model-key",
            Name = "Test Integration Model",
            Version = "1.0.0",
            Framework = "MockProvider",
            IsActive = true,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GetModels_ReturnsList()
    {
        var response = await _client.GetAsync("/api/models");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<AIModelResponse>>();
        Assert.NotNull(list);
        Assert.Contains(list, m => m.Id == _testModelId);
    }

    [Fact]
    public async Task GetModelById_ExistingId_ReturnsModel()
    {
        var response = await _client.GetAsync($"/api/models/{_testModelId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var model = await response.Content.ReadFromJsonAsync<AIModelResponse>();
        Assert.NotNull(model);
        Assert.Equal("test-model-key", model.ModelKey);
    }

    [Fact]
    public async Task GetModelById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/models/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetModelStatus_ExistingId_ReturnsLiveStatus()
    {
        var response = await _client.GetAsync($"/api/models/{_testModelId}/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<AIModelStatusResponse>();
        Assert.NotNull(status);
        Assert.Equal("Loaded", status.AiServiceStatus);
    }

    [Fact]
    public async Task SyncModels_DiscoversAndSyncsModels()
    {
        var response = await _client.PostAsync("/api/models/sync", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var syncResult = await response.Content.ReadFromJsonAsync<SyncModelsResponse>();
        Assert.NotNull(syncResult);
        Assert.True(syncResult.DiscoveredInAiService >= 1);
    }

    [Fact]
    public async Task UpdateModelStatus_TogglesActiveState()
    {
        var updateReq = new UpdateModelStatusRequest { IsActive = false };
        var response = await _client.PatchAsJsonAsync($"/api/models/{_testModelId}/status", updateReq);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var model = await response.Content.ReadFromJsonAsync<AIModelResponse>();
        Assert.NotNull(model);
        Assert.False(model.IsActive);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
