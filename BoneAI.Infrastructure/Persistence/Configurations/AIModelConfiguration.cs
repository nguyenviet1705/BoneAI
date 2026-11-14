using BoneAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoneAI.Infrastructure.Persistence.Configurations;

public class AIModelConfiguration : IEntityTypeConfiguration<AIModel>
{
    public const string DefaultMockModelId = "11111111-1111-1111-1111-111111111111";

    public void Configure(EntityTypeBuilder<AIModel> builder)
    {
        builder.ToTable("AIModels");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ModelKey)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("mock-bone-fracture-v1");

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Version)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.Framework)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("MockProvider");

        builder.Property(m => m.Description)
            .HasMaxLength(500);

        builder.Property(m => m.ArtifactPath)
            .HasMaxLength(500);

        builder.Property(m => m.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(m => m.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.HasIndex(m => new { m.ModelKey, m.Version })
            .IsUnique();

        builder.HasIndex(m => new { m.Name, m.Version });

        builder.HasIndex(m => m.IsActive);
        builder.HasIndex(m => m.IsAvailable);

        builder.HasMany(m => m.AnalysisRuns)
            .WithOne(r => r.AIModel)
            .HasForeignKey(r => r.AIModelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed initial development model
        builder.HasData(new AIModel
        {
            Id = Guid.Parse(DefaultMockModelId),
            ModelKey = "mock-bone-fracture-v1",
            Name = "mock-bone-fracture-v1",
            Version = "1.0.0",
            Framework = "MockProvider",
            Description = "Development and contract-testing mock adapter for pediatric wrist fracture detection",
            IsActive = true,
            IsAvailable = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
