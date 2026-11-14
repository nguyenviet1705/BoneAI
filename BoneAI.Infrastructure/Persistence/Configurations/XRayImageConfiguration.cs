using BoneAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoneAI.Infrastructure.Persistence.Configurations;

public class XRayImageConfiguration : IEntityTypeConfiguration<XRayImage>
{
    public void Configure(EntityTypeBuilder<XRayImage> builder)
    {
        builder.ToTable("XRayImages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.FileSize)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.CreatedAt);

        builder.HasMany(x => x.Analyses)
            .WithOne(a => a.XRayImage)
            .HasForeignKey(a => a.XRayImageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
