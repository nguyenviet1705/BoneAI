using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoneAI.Infrastructure.Repositories;

public class XRayImageRepository : IXRayImageRepository
{
    private readonly BoneAIDbContext _context;

    public XRayImageRepository(BoneAIDbContext context)
    {
        _context = context;
    }

    public async Task<XRayImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.XRayImages
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<XRayImage> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.XRayImages.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(XRayImage image, CancellationToken cancellationToken = default)
    {
        await _context.XRayImages.AddAsync(image, cancellationToken);
    }

    public async Task<bool> HasAnalysisReferencesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Analyses
            .IgnoreQueryFilters()
            .AnyAsync(a => a.XRayImageId == id, cancellationToken);
    }

    public Task DeleteAsync(XRayImage image, CancellationToken cancellationToken = default)
    {
        _context.XRayImages.Remove(image);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
