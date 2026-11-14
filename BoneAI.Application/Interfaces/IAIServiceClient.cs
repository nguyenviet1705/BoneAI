using BoneAI.Application.DTOs;

namespace BoneAI.Application.Interfaces;

public interface IAIServiceClient
{
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<bool> CheckReadinessAsync(CancellationToken cancellationToken = default);
    Task<InferenceClientResponse> ExecuteInferenceAsync(InferenceClientRequest request, CancellationToken cancellationToken = default);
}
