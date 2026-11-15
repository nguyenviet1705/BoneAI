using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Enums;
using BoneAI.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace BoneAI.Application.Services;

public class HistoryService : IHistoryService
{
    private readonly IAnalysisRepository _analysisRepo;
    private readonly IAIModelRepository _modelRepo;
    private readonly IResultsService _resultsService;
    private readonly IImageStorageService _storageService;
    private readonly ILogger<HistoryService> _logger;

    public HistoryService(
        IAnalysisRepository analysisRepo,
        IAIModelRepository modelRepo,
        IResultsService resultsService,
        IImageStorageService storageService,
        ILogger<HistoryService> logger)
    {
        _analysisRepo = analysisRepo;
        _modelRepo = modelRepo;
        _resultsService = resultsService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<AnalysisHistoryResponse> GetHistoryAsync(GetHistoryQuery query, CancellationToken cancellationToken = default)
    {
        query ??= new GetHistoryQuery();

        if (query.Page < 1)
        {
            throw new ValidationException("Page number must be greater than or equal to 1.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            throw new ValidationException("PageSize must be between 1 and 100.");
        }

        AnalysisStatus? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<AnalysisStatus>(query.Status.Trim(), true, out var parsedStatus))
            {
                throw new ValidationException($"Invalid status filter: '{query.Status}'. Valid statuses are: Pending, Processing, Completed, PartiallyCompleted, Failed.");
            }
            statusEnum = parsedStatus;
        }

        if (query.ModelId.HasValue && query.ModelId.Value != Guid.Empty)
        {
            var model = await _modelRepo.GetByIdAsync(query.ModelId.Value, cancellationToken);
            if (model == null)
            {
                throw new ValidationException($"AI Model with ID '{query.ModelId.Value}' does not exist.");
            }
        }

        var sortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "createdAt" : query.SortBy.Trim();
        var allowedSortFields = new[] { "createdat", "status" };
        if (!allowedSortFields.Contains(sortBy.ToLowerInvariant()))
        {
            throw new ValidationException($"Invalid sortBy field: '{query.SortBy}'. Supported fields are: createdAt, status.");
        }

        var sortOrder = string.IsNullOrWhiteSpace(query.SortOrder) ? "desc" : query.SortOrder.Trim();
        var allowedSortOrders = new[] { "asc", "desc" };
        if (!allowedSortOrders.Contains(sortOrder.ToLowerInvariant()))
        {
            throw new ValidationException($"Invalid sortOrder: '{query.SortOrder}'. Supported values are: asc, desc.");
        }

        var (items, totalCount) = await _analysisRepo.GetPagedHistoryAsync(
            query.Page,
            query.PageSize,
            statusEnum,
            query.ModelId,
            sortBy,
            sortOrder,
            cancellationToken);

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / query.PageSize) : 0;
        var hasNextPage = query.Page < totalPages;
        var hasPreviousPage = query.Page > 1 && totalPages > 0;

        return new AnalysisHistoryResponse
        {
            Items = items,
            Pagination = new PaginationMetadataDto
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = totalCount,
                TotalPages = totalPages,
                HasNextPage = hasNextPage,
                HasPreviousPage = hasPreviousPage
            }
        };
    }

    public async Task<AnalysisResultDto> GetHistoryDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _resultsService.GetAnalysisResultAsync(id, cancellationToken);
    }

    public async Task DeleteHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var analysis = await _analysisRepo.GetByIdForDeletionAsync(id, cancellationToken);
        if (analysis == null)
        {
            throw new NotFoundException($"Analysis with ID '{id}' was not found.");
        }

        analysis.IsDeleted = true;
        analysis.DeletedAt = DateTime.UtcNow;

        var resultImagesToClean = analysis.AnalysisRuns
            .Where(r => !string.IsNullOrEmpty(r.ResultImagePath))
            .Select(r => r.ResultImagePath!)
            .ToList();

        // Commit database soft-delete first
        await _analysisRepo.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Soft-deleted Analysis {AnalysisId} and its associated runs/detections in database.", id);

        // Then safely clean up result image storage files without risking database inconsistency
        foreach (var imagePath in resultImagesToClean)
        {
            try
            {
                await _storageService.DeleteAsync(imagePath, cancellationToken);
                _logger.LogInformation("Cleaned up result image {Path} for soft-deleted Analysis {AnalysisId}.", imagePath, id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete result image {Path} from storage for Analysis {AnalysisId}. Non-fatal.", imagePath, id);
            }
        }
    }
}
