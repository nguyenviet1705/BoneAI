using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
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
            .Include(a => a.XRayImage)
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

    public async Task<(IReadOnlyList<AnalysisHistoryItemDto> Items, int TotalCount)> GetPagedHistoryAsync(
        int page,
        int pageSize,
        AnalysisStatus? status,
        Guid? modelId,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Analyses.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (modelId.HasValue)
        {
            query = query.Where(a => a.AnalysisRuns.Any(r => r.AIModelId == modelId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = (sortBy.ToLowerInvariant(), sortOrder.ToLowerInvariant()) switch
        {
            ("createdat", "asc") => query.OrderBy(a => a.CreatedAt).ThenBy(a => a.Id),
            ("createdat", "desc") => query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id),
            ("status", "asc") => query.OrderBy(a => a.Status).ThenBy(a => a.Id),
            ("status", "desc") => query.OrderByDescending(a => a.Status).ThenByDescending(a => a.Id),
            _ => query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AnalysisHistoryItemDto
            {
                AnalysisId = a.Id,
                ImageId = a.XRayImageId,
                OriginalImageUrl = $"/api/images/{a.XRayImageId}/file",
                Status = a.Status.ToString(),
                ModelCount = a.AnalysisRuns.Count,
                CompletedModelCount = a.AnalysisRuns.Count(r => r.Status == AnalysisRunStatus.Completed),
                FailedModelCount = a.AnalysisRuns.Count(r => r.Status == AnalysisRunStatus.Failed),
                CreatedAt = a.CreatedAt,
                CompletedAt = a.AnalysisRuns.Max(r => (DateTime?)r.CompletedAt)
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Analysis?> GetByIdForDeletionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Analyses
            .Include(a => a.AnalysisRuns)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}
