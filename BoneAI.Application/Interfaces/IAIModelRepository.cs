using BoneAI.Domain.Entities;

namespace BoneAI.Application.Interfaces;

public interface IAIModelRepository
{
    Task<AIModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AIModel?> GetByModelKeyAndVersionAsync(string modelKey, string version, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AIModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AIModel>> GetActiveModelsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AIModel>> GetActiveAndAvailableModelsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AIModel model, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
