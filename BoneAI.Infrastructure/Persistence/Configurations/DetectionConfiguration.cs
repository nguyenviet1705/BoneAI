using BoneAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoneAI.Infrastructure.Persistence.Configurations;

public class DetectionConfiguration : IEntityTypeConfiguration<Detection>
{
    public void Configure(EntityTypeBuilder<Detection> builder)
    {
        builder.ToTable("Detections");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.AnalysisRunId)
            .IsRequired();

        builder.Property(d => d.ClassName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Confidence)
            .IsRequired()
            .HasPrecision(5, 4);

        builder.Property(d => d.X)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(d => d.Y)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(d => d.Width)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(d => d.Height)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.HasIndex(d => d.AnalysisRunId);
        builder.HasIndex(d => d.ClassName);

        builder.HasOne(d => d.AnalysisRun)
            .WithMany(r => r.Detections)
            .HasForeignKey(d => d.AnalysisRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
