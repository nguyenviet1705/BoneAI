using BoneAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoneAI.Infrastructure.Persistence.Configurations;

public class AIModelConfiguration : IEntityTypeConfiguration<AIModel>
{
    public void Configure(EntityTypeBuilder<AIModel> builder)
    {
        builder.ToTable("AIModels");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ModelKey)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("yolov8m");

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Version)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.Framework)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("PyTorch");

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
    }
}
