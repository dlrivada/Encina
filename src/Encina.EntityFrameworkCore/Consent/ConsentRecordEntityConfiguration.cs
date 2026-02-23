using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Consent;

/// <summary>
/// EF Core configuration for <see cref="ConsentRecordEntity"/>.
/// </summary>
public sealed class ConsentRecordEntityConfiguration : IEntityTypeConfiguration<ConsentRecordEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ConsentRecordEntity> builder)
    {
        builder.ToTable("ConsentRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SubjectId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Purpose)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.ConsentVersionId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.GivenAtUtc)
            .IsRequired();

        builder.Property(x => x.WithdrawnAtUtc)
            .IsRequired(false);

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired(false);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.IpAddress)
            .IsRequired(false)
            .HasMaxLength(45);

        builder.Property(x => x.ProofOfConsent)
            .IsRequired(false);

        builder.Property(x => x.Metadata)
            .IsRequired(false);

        builder.HasIndex(x => x.SubjectId)
            .HasDatabaseName("IX_ConsentRecords_SubjectId");

        builder.HasIndex(x => new { x.SubjectId, x.Purpose })
            .HasDatabaseName("IX_ConsentRecords_SubjectId_Purpose");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_ConsentRecords_Status");
    }
}
