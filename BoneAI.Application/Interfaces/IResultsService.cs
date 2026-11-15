using BoneAI.Application.DTOs;

namespace BoneAI.Application.Interfaces;

public interface IResultsService
{
    Task<AnalysisResultDto> GetAnalysisResultAsync(Guid analysisId, CancellationToken cancellationToken = default);
    Task<AnalysisRunResultDto> GetAnalysisRunResultAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<AnalysisComparisonDto> GetAnalysisComparisonAsync(Guid analysisId, CancellationToken cancellationToken = default);
    Task<ImageFileResult> GetRunResultImageAsync(Guid runId, CancellationToken cancellationToken = default);
}
