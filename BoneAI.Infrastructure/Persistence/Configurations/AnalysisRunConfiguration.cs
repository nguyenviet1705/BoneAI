using BoneAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoneAI.Infrastructure.Persistence.Configurations;

public class AnalysisRunConfiguration : IEntityTypeConfiguration<AnalysisRun>
{
    public void Configure(EntityTypeBuilder<AnalysisRun> builder)
    {
        builder.ToTable("AnalysisRuns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.AnalysisId)
            .IsRequired();

        builder.Property(r => r.AIModelId)
            .IsRequired();

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(r => r.StartedAt);

        builder.Property(r => r.CompletedAt);

        builder.Property(r => r.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(r => r.AnalysisId);
        builder.HasIndex(r => r.AIModelId);
        builder.HasIndex(r => new { r.AnalysisId, r.AIModelId });

        builder.HasOne(r => r.Analysis)
            .WithMany(a => a.AnalysisRuns)
            .HasForeignKey(r => r.AnalysisId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict delete behavior to avoid SQL Server multiple cascade path errors
        // and preserve execution audit history
        builder.HasOne(r => r.AIModel)
            .WithMany(m => m.AnalysisRuns)
            .HasForeignKey(r => r.AIModelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Detections)
            .WithOne(d => d.AnalysisRun)
            .HasForeignKey(d => d.AnalysisRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
