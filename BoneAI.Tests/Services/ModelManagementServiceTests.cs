using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Application.Services;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoneAI.Tests.Services;

public class ModelManagementServiceTests
{
    private readonly Mock<IAIModelRepository> _mockModelRepo = new();
    private readonly Mock<IAIServiceClient> _mockAiClient = new();
    private readonly Mock<ILogger<ModelManagementService>> _mockLogger = new();
    private readonly ModelManagementService _service;

    public ModelManagementServiceTests()
    {
        _service = new ModelManagementService(
            _mockModelRepo.Object,
            _mockAiClient.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SyncModelsWithAIServiceAsync_DiscoversNewModels_AddsToDatabase()
    {
        var catalog = new List<AIServiceModelDto>
        {
            new()
            {
                ModelKey = "mock-wrist-yolo-v8",
                ModelName = "YOLOv8 Wrist",
                Version = "1.2.0",
                Framework = "MockProvider",
                Status = "Loaded",
                IsActive = true
            }
        };

        _mockAiClient.Setup(c => c.TriggerModelDiscoveryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIServiceModelSyncResultDto());
        _mockAiClient.Setup(c => c.GetModelCatalogAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);

        _mockModelRepo.Setup(r => r.GetByModelKeyAndVersionAsync("mock-wrist-yolo-v8", "1.2.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIModel?)null);

        _mockModelRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AIModel>
            {
                new() { ModelKey = "mock-wrist-yolo-v8", Name = "YOLOv8 Wrist", Version = "1.2.0", Framework = "MockProvider", IsActive = true, IsAvailable = true }
            });

        var response = await _service.SyncModelsWithAIServiceAsync();

        Assert.NotNull(response);
        Assert.Equal(1, response.DiscoveredInAiService);
        Assert.Equal(1, response.CreatedInDatabase);
        Assert.Equal(0, response.UpdatedInDatabase);

        _mockModelRepo.Verify(r => r.AddAsync(It.Is<AIModel>(m =>
            m.ModelKey == "mock-wrist-yolo-v8" &&
            m.Version == "1.2.0" &&
            m.IsAvailable == true), It.IsAny<CancellationToken>()), Times.Once);
        _mockModelRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncModelsWithAIServiceAsync_ExistingModel_PreservesIdAndUpdatesAvailability()
    {
        var existingModelId = Guid.NewGuid();
        var existingModel = new AIModel
        {
            Id = existingModelId,
            ModelKey = "unloaded-faster-rcnn",
            Name = "Faster RCNN",
            Version = "0.9.0",
            Framework = "PyTorch",
            IsActive = true,
            IsAvailable = true // currently marked available in DB
        };

        var catalog = new List<AIServiceModelDto>
        {
            new()
            {
                ModelKey = "unloaded-faster-rcnn",
                ModelName = "Faster RCNN",
                Version = "0.9.0",
                Framework = "PyTorch",
                Status = "Unavailable", // changed to unavailable in AI service
                IsActive = true
            }
        };

        _mockAiClient.Setup(c => c.TriggerModelDiscoveryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIServiceModelSyncResultDto());
        _mockAiClient.Setup(c => c.GetModelCatalogAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);

        _mockModelRepo.Setup(r => r.GetByModelKeyAndVersionAsync("unloaded-faster-rcnn", "0.9.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingModel);

        _mockModelRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AIModel> { existingModel });

        var response = await _service.SyncModelsWithAIServiceAsync();

        Assert.Equal(1, response.UpdatedInDatabase);
        Assert.False(existingModel.IsAvailable);
        Assert.Equal(existingModelId, existingModel.Id); // ID preserved!
    }

    [Fact]
    public async Task SetModelActiveStatusAsync_UpdatesAndReturnsModel()
    {
        var modelId = Guid.NewGuid();
        var model = new AIModel { Id = modelId, Name = "Test", Version = "1.0", IsActive = true };

        _mockModelRepo.Setup(r => r.GetByIdAsync(modelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _service.SetModelActiveStatusAsync(modelId, false);

        Assert.False(result.IsActive);
        Assert.False(model.IsActive);
        _mockModelRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
