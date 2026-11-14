using BoneAI.Application.DTOs;

namespace BoneAI.Application.Interfaces;

public interface IModelManagementService
{
    Task<IReadOnlyList<AIModelResponse>> GetModelsAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<AIModelResponse?> GetModelByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AIModelStatusResponse?> GetModelStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SyncModelsResponse> SyncModelsWithAIServiceAsync(CancellationToken cancellationToken = default);
    Task<AIModelResponse> SetModelActiveStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
