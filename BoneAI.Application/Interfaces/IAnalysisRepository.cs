using BoneAI.Application.DTOs;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;

namespace BoneAI.Application.Interfaces;

public interface IAnalysisRepository
{
    Task<Analysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Analysis?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AnalysisHistoryItemDto> Items, int TotalCount)> GetPagedHistoryAsync(
        int page,
        int pageSize,
        AnalysisStatus? status,
        Guid? modelId,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default);
    Task<Analysis?> GetByIdForDeletionAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Analysis analysis, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
