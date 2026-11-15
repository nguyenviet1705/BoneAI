using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Application.Services;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BoneAI.Tests.Services;

public class HistoryServiceTests
{
    private readonly Mock<IAnalysisRepository> _analysisRepoMock;
    private readonly Mock<IAIModelRepository> _modelRepoMock;
    private readonly Mock<IResultsService> _resultsServiceMock;
    private readonly Mock<IImageStorageService> _storageServiceMock;
    private readonly Mock<ILogger<HistoryService>> _loggerMock;
    private readonly HistoryService _service;

    public HistoryServiceTests()
    {
        _analysisRepoMock = new Mock<IAnalysisRepository>();
        _modelRepoMock = new Mock<IAIModelRepository>();
        _resultsServiceMock = new Mock<IResultsService>();
        _storageServiceMock = new Mock<IImageStorageService>();
        _loggerMock = new Mock<ILogger<HistoryService>>();

        _service = new HistoryService(
            _analysisRepoMock.Object,
            _modelRepoMock.Object,
            _resultsServiceMock.Object,
            _storageServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetHistory_ValidPagination_ReturnsPagedResponse()
    {
        var items = new List<AnalysisHistoryItemDto>
        {
            new() { AnalysisId = Guid.NewGuid(), Status = "Completed", ModelCount = 2, CompletedModelCount = 2, CreatedAt = DateTime.UtcNow }
        };

        _analysisRepoMock.Setup(r => r.GetPagedHistoryAsync(1, 10, null, null, "createdAt", "desc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 25));

        var query = new GetHistoryQuery { Page = 1, PageSize = 10 };
        var response = await _service.GetHistoryAsync(query);

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal(1, response.Pagination.Page);
        Assert.Equal(10, response.Pagination.PageSize);
        Assert.Equal(25, response.Pagination.TotalItems);
        Assert.Equal(3, response.Pagination.TotalPages);
        Assert.True(response.Pagination.HasNextPage);
        Assert.False(response.Pagination.HasPreviousPage);
    }

    [Fact]
    public async Task GetHistory_LastPage_HasPreviousPageIsTrueAndHasNextPageIsFalse()
    {
        _analysisRepoMock.Setup(r => r.GetPagedHistoryAsync(3, 10, null, null, "createdAt", "desc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AnalysisHistoryItemDto>(), 25));

        var query = new GetHistoryQuery { Page = 3, PageSize = 10 };
        var response = await _service.GetHistoryAsync(query);

        Assert.Equal(3, response.Pagination.Page);
        Assert.False(response.Pagination.HasNextPage);
        Assert.True(response.Pagination.HasPreviousPage);
    }

    [Fact]
    public async Task GetHistory_PageExceedsTotalPages_ReturnsEmptyItems()
    {
        _analysisRepoMock.Setup(r => r.GetPagedHistoryAsync(5, 10, null, null, "createdAt", "desc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AnalysisHistoryItemDto>(), 20));

        var query = new GetHistoryQuery { Page = 5, PageSize = 10 };
        var response = await _service.GetHistoryAsync(query);

        Assert.Empty(response.Items);
        Assert.Equal(5, response.Pagination.Page);
        Assert.Equal(2, response.Pagination.TotalPages);
        Assert.False(response.Pagination.HasNextPage);
        Assert.True(response.Pagination.HasPreviousPage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetHistory_InvalidPage_ThrowsValidationException(int page)
    {
        var query = new GetHistoryQuery { Page = page, PageSize = 10 };
        await Assert.ThrowsAsync<ValidationException>(() => _service.GetHistoryAsync(query));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task GetHistory_InvalidPageSize_ThrowsValidationException(int pageSize)
    {
        var query = new GetHistoryQuery { Page = 1, PageSize = pageSize };
        await Assert.ThrowsAsync<ValidationException>(() => _service.GetHistoryAsync(query));
    }

    [Theory]
    [InlineData("Pending", AnalysisStatus.Pending)]
    [InlineData("Processing", AnalysisStatus.Processing)]
    [InlineData("Completed", AnalysisStatus.Completed)]
    [InlineData("PartiallyCompleted", AnalysisStatus.PartiallyCompleted)]
    [InlineData("Failed", AnalysisStatus.Failed)]
    public async Task GetHistory_FilterStatus_ParsesStatusAndCallsRepository(string statusStr, AnalysisStatus expectedEnum)
    {
        _analysisRepoMock.Setup(r => r.GetPagedHistoryAsync(1, 10, expectedEnum, null, "createdAt", "desc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AnalysisHistoryItemDto>(), 0));

        var query = new GetHistoryQuery { Page = 1, PageSize = 10, Status = statusStr };
        var response = await _service.GetHistoryAsync(query);

        Assert.NotNull(response);
        _analysisRepoMock.Verify(r => r.GetPagedHistoryAsync(1, 10, expectedEnum, null, "createdAt", "desc", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistory_InvalidStatus_ThrowsValidationException()
    {
        var query = new GetHistoryQuery { Page = 1, PageSize = 10, Status = "UnknownStatus" };
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.GetHistoryAsync(query));
        Assert.Contains("Invalid status filter", ex.Message);
    }

    [Fact]
    public async Task GetHistory_FilterModelId_ModelExists_PassesToRepo()
    {
        var modelId = Guid.NewGuid();
        _modelRepoMock.Setup(m => m.GetByIdAsync(modelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIModel { Id = modelId, Name = "Model 1" });

        _analysisRepoMock.Setup(r => r.GetPagedHistoryAsync(1, 10, null, modelId, "createdAt", "desc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AnalysisHistoryItemDto>(), 0));

        var query = new GetHistoryQuery { Page = 1, PageSize = 10, ModelId = modelId };
        var response = await _service.GetHistoryAsync(query);

        Assert.NotNull(response);
        _analysisRepoMock.Verify(r => r.GetPagedHistoryAsync(1, 10, null, modelId, "createdAt", "desc", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistory_FilterModelId_ModelDoesNotExist_ThrowsValidationException()
    {
        var modelId = Guid.NewGuid();
        _modelRepoMock.Setup(m => m.GetByIdAsync(modelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIModel?)null);

        var query = new GetHistoryQuery { Page = 1, PageSize = 10, ModelId = modelId };
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.GetHistoryAsync(query));
        Assert.Contains("does not exist", ex.Message);
    }

    [Theory]
    [InlineData("createdAt", "asc")]
    [InlineData("status", "desc")]
    public async Task GetHistory_ValidSortByAndOrder_PassesToRepo(string sortBy, string sortOrder)
    {
        _analysisRepoMock.Setup(r => r.GetPagedHistoryAsync(1, 10, null, null, sortBy, sortOrder, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AnalysisHistoryItemDto>(), 0));

        var query = new GetHistoryQuery { Page = 1, PageSize = 10, SortBy = sortBy, SortOrder = sortOrder };
        var response = await _service.GetHistoryAsync(query);

        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetHistory_InvalidSortBy_ThrowsValidationException()
    {
        var query = new GetHistoryQuery { Page = 1, PageSize = 10, SortBy = "invalid_field" };
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.GetHistoryAsync(query));
        Assert.Contains("Invalid sortBy field", ex.Message);
    }

    [Fact]
    public async Task GetHistory_InvalidSortOrder_ThrowsValidationException()
    {
        var query = new GetHistoryQuery { Page = 1, PageSize = 10, SortOrder = "sideways" };
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.GetHistoryAsync(query));
        Assert.Contains("Invalid sortOrder", ex.Message);
    }

    [Fact]
    public async Task GetHistoryDetail_CallsResultsService()
    {
        var analysisId = Guid.NewGuid();
        var expectedDto = new AnalysisResultDto
        {
            AnalysisId = analysisId,
            OverallStatus = "Completed",
            Runs = new List<AnalysisRunResultDto>()
        };

        _resultsServiceMock.Setup(s => s.GetAnalysisResultAsync(analysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var result = await _service.GetHistoryDetailAsync(analysisId);

        Assert.NotNull(result);
        Assert.Equal(analysisId, result.AnalysisId);
        _resultsServiceMock.Verify(s => s.GetAnalysisResultAsync(analysisId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistoryDetail_WhenNotFound_PropagatesNotFoundException()
    {
        var analysisId = Guid.NewGuid();
        _resultsServiceMock.Setup(s => s.GetAnalysisResultAsync(analysisId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException($"Analysis with ID '{analysisId}' was not found."));

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetHistoryDetailAsync(analysisId));
    }

    [Fact]
    public async Task DeleteHistory_ExistingAnalysis_SoftDeletesAndCleansStorage()
    {
        var analysisId = Guid.NewGuid();
        var analysis = new Analysis
        {
            Id = analysisId,
            IsDeleted = false,
            AnalysisRuns = new List<AnalysisRun>
            {
                new() { Id = Guid.NewGuid(), ResultImagePath = "results/run1.png" },
                new() { Id = Guid.NewGuid(), ResultImagePath = null }
            }
        };

        _analysisRepoMock.Setup(r => r.GetByIdForDeletionAsync(analysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        await _service.DeleteHistoryAsync(analysisId);

        Assert.True(analysis.IsDeleted);
        Assert.NotNull(analysis.DeletedAt);
        _analysisRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _storageServiceMock.Verify(s => s.DeleteAsync("results/run1.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteHistory_StorageCleanupFailure_DoesNotThrowAndCompletesSoftDelete()
    {
        var analysisId = Guid.NewGuid();
        var analysis = new Analysis
        {
            Id = analysisId,
            AnalysisRuns = new List<AnalysisRun>
            {
                new() { Id = Guid.NewGuid(), ResultImagePath = "results/run1.png" }
            }
        };

        _analysisRepoMock.Setup(r => r.GetByIdForDeletionAsync(analysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        _storageServiceMock.Setup(s => s.DeleteAsync("results/run1.png", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk read error"));

        // Should NOT throw exception
        await _service.DeleteHistoryAsync(analysisId);

        Assert.True(analysis.IsDeleted);
        _analysisRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteHistory_NotFound_ThrowsNotFoundException()
    {
        var analysisId = Guid.NewGuid();
        _analysisRepoMock.Setup(r => r.GetByIdForDeletionAsync(analysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Analysis?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteHistoryAsync(analysisId));
    }
}
