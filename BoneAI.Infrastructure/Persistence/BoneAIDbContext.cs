using BoneAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BoneAI.Infrastructure.Persistence;

public class BoneAIDbContext : DbContext
{
    public BoneAIDbContext(DbContextOptions<BoneAIDbContext> options) : base(options)
    {
    }

    public DbSet<XRayImage> XRayImages => Set<XRayImage>();
    public DbSet<AIModel> AIModels => Set<AIModel>();
    public DbSet<Analysis> Analyses => Set<Analysis>();
    public DbSet<AnalysisRun> AnalysisRuns => Set<AnalysisRun>();
    public DbSet<Detection> Detections => Set<Detection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BoneAIDbContext).Assembly);
    }
}
