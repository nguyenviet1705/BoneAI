using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Application.Services;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoneAI.Tests.Services;

public class MultiModelAnalysisServiceTests
{
    private readonly Mock<IAnalysisRepository> _mockAnalysisRepo = new();
    private readonly Mock<IXRayImageRepository> _mockImageRepo = new();
    private readonly Mock<IAIModelRepository> _mockModelRepo = new();
    private readonly Mock<IImageStorageService> _mockStorage = new();
    private readonly Mock<ILogger<AnalysisService>> _mockLogger = new();
    private readonly AnalysisService _service;

    public MultiModelAnalysisServiceTests()
    {
        _service = new AnalysisService(
            _mockAnalysisRepo.Object,
            _mockImageRepo.Object,
            _mockModelRepo.Object,
            _mockStorage.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SubmitAsync_MultiModelMode_CreatesMultipleRuns()
    {
        var imageId = Guid.NewGuid();
        var model1Id = Guid.NewGuid();
        var model2Id = Guid.NewGuid();

        _mockImageRepo.Setup(r => r.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new XRayImage { Id = imageId, FilePath = "images/wrist.jpg" });

        _mockStorage.Setup(s => s.ExistsAsync("images/wrist.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockModelRepo.Setup(r => r.GetByIdAsync(model1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIModel { Id = model1Id, Name = "Model 1", Version = "1.0", IsActive = true, IsAvailable = true });

        _mockModelRepo.Setup(r => r.GetByIdAsync(model2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIModel { Id = model2Id, Name = "Model 2", Version = "2.0", IsActive = true, IsAvailable = true });

        var request = new SubmitAnalysisRequest
        {
            XRayImageId = imageId,
            Mode = AnalysisMode.MultiModel,
            SelectedModelIds = new List<Guid> { model1Id, model2Id }
        };

        var response = await _service.SubmitAsync(request);

        Assert.NotNull(response);
        Assert.Equal(AnalysisStatus.Pending.ToString(), response.Status);
        Assert.Equal(AnalysisMode.MultiModel.ToString(), response.Mode);
        Assert.Equal(2, response.AnalysisRunIds.Count);

        _mockAnalysisRepo.Verify(r => r.AddAsync(It.Is<Analysis>(a =>
            a.AnalysisRuns.Count == 2 &&
            a.Mode == AnalysisMode.MultiModel &&
            a.Status == AnalysisStatus.Pending), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_MultiModelMode_DuplicateModelIds_ThrowsValidationException()
    {
        var imageId = Guid.NewGuid();
        var model1Id = Guid.NewGuid();

        _mockImageRepo.Setup(r => r.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new XRayImage { Id = imageId, FilePath = "images/wrist.jpg" });
        _mockStorage.Setup(s => s.ExistsAsync("images/wrist.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new SubmitAnalysisRequest
        {
            XRayImageId = imageId,
            Mode = AnalysisMode.MultiModel,
            SelectedModelIds = new List<Guid> { model1Id, model1Id } // duplicate
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitAsync(request));
        Assert.Contains("Duplicate model IDs", ex.Message);
    }

    [Fact]
    public async Task SubmitAsync_MultiModelMode_FewerThanTwoModels_ThrowsValidationException()
    {
        var imageId = Guid.NewGuid();
        var model1Id = Guid.NewGuid();

        _mockImageRepo.Setup(r => r.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new XRayImage { Id = imageId, FilePath = "images/wrist.jpg" });
        _mockStorage.Setup(s => s.ExistsAsync("images/wrist.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new SubmitAnalysisRequest
        {
            XRayImageId = imageId,
            Mode = AnalysisMode.MultiModel,
            SelectedModelIds = new List<Guid> { model1Id }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitAsync(request));
        Assert.Contains("requires at least two selected model IDs", ex.Message);
    }

    [Fact]
    public async Task SubmitAsync_CompareAllMode_DynamicallyQueuesAllEligibleModels()
    {
        var imageId = Guid.NewGuid();
        var model1 = new AIModel { Id = Guid.NewGuid(), Name = "M1", Version = "1.0", IsActive = true, IsAvailable = true };
        var model2 = new AIModel { Id = Guid.NewGuid(), Name = "M2", Version = "1.0", IsActive = true, IsAvailable = true };
        var model3 = new AIModel { Id = Guid.NewGuid(), Name = "M3", Version = "1.0", IsActive = true, IsAvailable = true };

        _mockImageRepo.Setup(r => r.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new XRayImage { Id = imageId, FilePath = "images/wrist.jpg" });
        _mockStorage.Setup(s => s.ExistsAsync("images/wrist.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockModelRepo.Setup(r => r.GetActiveAndAvailableModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AIModel> { model1, model2, model3 });

        var request = new SubmitAnalysisRequest
        {
            XRayImageId = imageId,
            Mode = AnalysisMode.CompareAll
        };

        var response = await _service.SubmitAsync(request);

        Assert.NotNull(response);
        Assert.Equal(3, response.AnalysisRunIds.Count);
        Assert.Equal(AnalysisMode.CompareAll.ToString(), response.Mode);

        _mockAnalysisRepo.Verify(r => r.AddAsync(It.Is<Analysis>(a =>
            a.AnalysisRuns.Count == 3 &&
            a.Mode == AnalysisMode.CompareAll), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_ModelUnavailable_ThrowsValidationException()
    {
        var imageId = Guid.NewGuid();
        var modelId = Guid.NewGuid();

        _mockImageRepo.Setup(r => r.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new XRayImage { Id = imageId, FilePath = "images/wrist.jpg" });
        _mockStorage.Setup(s => s.ExistsAsync("images/wrist.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockModelRepo.Setup(r => r.GetByIdAsync(modelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIModel { Id = modelId, Name = "Unloaded Model", Version = "1.0", IsActive = true, IsAvailable = false });

        var request = new SubmitAnalysisRequest
        {
            XRayImageId = imageId,
            Mode = AnalysisMode.SingleModel,
            AIModelId = modelId
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitAsync(request));
        Assert.Contains("unavailable", ex.Message);
    }
}
