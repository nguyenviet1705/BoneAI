using BoneAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoneAI.Infrastructure.Persistence.Configurations;

public class AnalysisConfiguration : IEntityTypeConfiguration<Analysis>
{
    public void Configure(EntityTypeBuilder<Analysis> builder)
    {
        builder.ToTable("Analyses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.XRayImageId)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.IdempotencyKey)
            .HasMaxLength(128);

        builder.HasIndex(a => a.XRayImageId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.CreatedAt);

        builder.HasIndex(a => a.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.HasMany(a => a.AnalysisRuns)
            .WithOne(r => r.Analysis)
            .HasForeignKey(r => r.AnalysisId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
