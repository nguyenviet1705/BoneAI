using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoneAI.Infrastructure.Repositories;

public class AnalysisRunRepository : IAnalysisRunRepository
{
    private readonly BoneAIDbContext _context;

    public AnalysisRunRepository(BoneAIDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisRun?> ClaimNextPendingRunAsync(TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var now = DateTime.UtcNow;

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var run = await _context.AnalysisRuns
                .Include(r => r.Analysis)
                    .ThenInclude(a => a!.XRayImage)
                .Include(r => r.AIModel)
                .Where(r => r.Status == AnalysisRunStatus.Pending ||
                            (r.Status == AnalysisRunStatus.Processing && r.LeaseUntil != null && r.LeaseUntil < now))
                .OrderBy(r => r.StartedAt ?? DateTime.MaxValue)
                .FirstOrDefaultAsync(cancellationToken);

            if (run == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            run.Status = AnalysisRunStatus.Processing;
            run.LeaseUntil = now.Add(leaseDuration);
            run.StartedAt ??= now;

            if (run.Analysis != null && run.Analysis.Status != AnalysisStatus.Processing)
            {
                run.Analysis.Status = AnalysisStatus.Processing;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return run;
        });
    }

    public async Task<AnalysisRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AnalysisRuns
            .Include(r => r.Analysis)
                .ThenInclude(a => a!.XRayImage)
            .Include(r => r.AIModel)
            .Include(r => r.Detections)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AnalysisRun>> GetRunsByAnalysisIdAsync(Guid analysisId, CancellationToken cancellationToken = default)
    {
        return await _context.AnalysisRuns
            .Where(r => r.AnalysisId == analysisId)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateRunAsync(AnalysisRun run, CancellationToken cancellationToken = default)
    {
        _context.AnalysisRuns.Update(run);
        return Task.CompletedTask;
    }

    public async Task SaveDetectionsAsync(Guid runId, IEnumerable<Detection> detections, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Detections
            .Where(d => d.AnalysisRunId == runId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _context.Detections.RemoveRange(existing);
        }

        await _context.Detections.AddRangeAsync(detections, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
