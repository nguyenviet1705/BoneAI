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

    public async Task<IReadOnlyList<AIModel>> GetActiveModelsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AIModels
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }
}
