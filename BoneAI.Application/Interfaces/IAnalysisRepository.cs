using BoneAI.Domain.Entities;

namespace BoneAI.Application.Interfaces;

public interface IAnalysisRepository
{
    Task<Analysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Analysis?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(Analysis analysis, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
