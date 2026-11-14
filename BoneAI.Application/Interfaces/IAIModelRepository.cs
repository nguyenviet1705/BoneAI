using BoneAI.Domain.Entities;

namespace BoneAI.Application.Interfaces;

public interface IAIModelRepository
{
    Task<AIModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AIModel>> GetActiveModelsAsync(CancellationToken cancellationToken = default);
}
