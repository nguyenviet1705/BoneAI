using BoneAI.Application.DTOs;

namespace BoneAI.Application.Interfaces;

public interface IAnalysisService
{
    Task<AnalysisSubmissionResponse> SubmitAsync(SubmitAnalysisRequest request, CancellationToken cancellationToken = default);
    Task<AnalysisDetailResponse> GetStatusAsync(Guid id, CancellationToken cancellationToken = default);
}
