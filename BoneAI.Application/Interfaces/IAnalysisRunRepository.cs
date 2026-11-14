using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;

namespace BoneAI.Application.Interfaces;

public interface IAnalysisRunRepository
{
    Task<AnalysisRun?> ClaimNextPendingRunAsync(TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task<AnalysisRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnalysisRun>> GetRunsByAnalysisIdAsync(Guid analysisId, CancellationToken cancellationToken = default);
    Task UpdateRunAsync(AnalysisRun run, CancellationToken cancellationToken = default);
    Task SaveDetectionsAsync(Guid runId, IEnumerable<Detection> detections, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
