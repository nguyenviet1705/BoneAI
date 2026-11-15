using BoneAI.Domain.Enums;

namespace BoneAI.Application.DTOs;

public class GetHistoryQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Status { get; set; }
    public Guid? ModelId { get; set; }
    public string? SortBy { get; set; } = "createdAt";
    public string? SortOrder { get; set; } = "desc";
}

public class AnalysisHistoryItemDto
{
    public Guid AnalysisId { get; set; }
    public Guid ImageId { get; set; }
    public string OriginalImageUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ModelCount { get; set; }
    public int CompletedModelCount { get; set; }
    public int FailedModelCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class PaginationMetadataDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class AnalysisHistoryResponse
{
    public IReadOnlyList<AnalysisHistoryItemDto> Items { get; set; } = Array.Empty<AnalysisHistoryItemDto>();
    public PaginationMetadataDto Pagination { get; set; } = new();
}
