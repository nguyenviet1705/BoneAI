using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoneAI.Infrastructure.Repositories;

public class AIModelRepository : IAIModelRepository
{
    private readonly BoneAIDbContext _context;

    public AIModelRepository(BoneAIDbContext context)
    {
        _context = context;
    }

    public async Task<AIModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AIModels
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<AIModel?> GetByModelKeyAndVersionAsync(string modelKey, string version, CancellationToken cancellationToken = default)
    {
        return await _context.AIModels
            .FirstOrDefaultAsync(m => m.ModelKey == modelKey && m.Version == version, cancellationToken);
    }

    public async Task<IReadOnlyList<AIModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AIModels
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AIModel>> GetActiveModelsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AIModels
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AIModel>> GetActiveAndAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AIModels
            .AsNoTracking()
            .Where(m => m.IsActive && m.IsAvailable)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AIModel model, CancellationToken cancellationToken = default)
    {
        await _context.AIModels.AddAsync(model, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
