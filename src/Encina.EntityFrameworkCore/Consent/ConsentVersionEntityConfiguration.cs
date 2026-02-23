using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Consent;

/// <summary>
/// EF Core configuration for <see cref="ConsentVersionEntity"/>.
/// </summary>
public sealed class ConsentVersionEntityConfiguration : IEntityTypeConfiguration<ConsentVersionEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ConsentVersionEntity> builder)
    {
        builder.ToTable("ConsentVersions");
        builder.HasKey(x => x.VersionId);

        builder.Property(x => x.VersionId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Purpose)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.EffectiveFromUtc)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.RequiresExplicitReconsent)
            .IsRequired();

        builder.HasIndex(x => x.Purpose)
            .HasDatabaseName("IX_ConsentVersions_Purpose");

        builder.HasIndex(x => new { x.Purpose, x.EffectiveFromUtc })
            .HasDatabaseName("IX_ConsentVersions_Purpose_EffectiveFromUtc");
    }
}
