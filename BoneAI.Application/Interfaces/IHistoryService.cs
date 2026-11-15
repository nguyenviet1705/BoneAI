using BoneAI.Application.DTOs;

namespace BoneAI.Application.Interfaces;

public interface IHistoryService
{
    Task<AnalysisHistoryResponse> GetHistoryAsync(GetHistoryQuery query, CancellationToken cancellationToken = default);
    Task<AnalysisResultDto> GetHistoryDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteHistoryAsync(Guid id, CancellationToken cancellationToken = default);
}
