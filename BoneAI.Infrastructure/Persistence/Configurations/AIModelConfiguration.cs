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

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Version)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.Description)
            .HasMaxLength(500);

        builder.Property(m => m.IsActive)
            .IsRequired();

        builder.HasIndex(m => new { m.Name, m.Version })
            .IsUnique();

        builder.HasIndex(m => m.IsActive);

        builder.HasMany(m => m.AnalysisRuns)
            .WithOne(r => r.AIModel)
            .HasForeignKey(r => r.AIModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
