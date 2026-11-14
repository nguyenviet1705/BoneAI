using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoneAI.Infrastructure.Repositories;

public class AnalysisRepository : IAnalysisRepository
{
    private readonly BoneAIDbContext _context;

    public AnalysisRepository(BoneAIDbContext context)
    {
        _context = context;
    }

    public async Task<Analysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Analyses
            .Include(a => a.AnalysisRuns)
                .ThenInclude(r => r.AIModel)
            .Include(a => a.AnalysisRuns)
                .ThenInclude(r => r.Detections)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Analysis?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        return await _context.Analyses
            .Include(a => a.AnalysisRuns)
                .ThenInclude(r => r.AIModel)
            .FirstOrDefaultAsync(a => a.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AddAsync(Analysis analysis, CancellationToken cancellationToken = default)
    {
        await _context.Analyses.AddAsync(analysis, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
