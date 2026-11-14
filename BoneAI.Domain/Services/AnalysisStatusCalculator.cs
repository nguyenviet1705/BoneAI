using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;

namespace BoneAI.Domain.Services;

public static class AnalysisStatusCalculator
{
    /// <summary>
    /// Calculates the aggregate status of an Analysis based on the state of its individual AnalysisRuns.
    /// Rules:
    /// - Completed: All runs completed successfully.
    /// - PartiallyCompleted: At least one run completed and at least one run failed, with none pending or processing.
    /// - Failed: All runs failed, with no successful runs.
    /// - Processing: At least one run is currently Processing, or runs remain Pending while execution is active.
    /// - Pending: All runs remain in Pending status (execution has not started).
    /// </summary>
    public static AnalysisStatus Calculate(IEnumerable<AnalysisRun> runs)
    {
        var runList = runs.ToList();
        if (runList.Count == 0)
        {
            return AnalysisStatus.Pending;
        }

        var hasPending = runList.Any(r => r.Status == AnalysisRunStatus.Pending);
        var hasProcessing = runList.Any(r => r.Status == AnalysisRunStatus.Processing);
        var completedCount = runList.Count(r => r.Status == AnalysisRunStatus.Completed);
        var failedCount = runList.Count(r => r.Status == AnalysisRunStatus.Failed);

        // Active processing state
        if (hasProcessing || (hasPending && (completedCount > 0 || failedCount > 0 || runList.Any(r => r.StartedAt.HasValue))))
        {
            return AnalysisStatus.Processing;
        }

        // Untouched pending state
        if (hasPending)
        {
            return AnalysisStatus.Pending;
        }

        // All succeeded
        if (completedCount == runList.Count)
        {
            return AnalysisStatus.Completed;
        }

        // All failed
        if (failedCount == runList.Count)
        {
            return AnalysisStatus.Failed;
        }

        // Some succeeded, some failed, none remaining
        if (completedCount > 0 && failedCount > 0)
        {
            return AnalysisStatus.PartiallyCompleted;
        }

        return AnalysisStatus.Processing;
    }
}
