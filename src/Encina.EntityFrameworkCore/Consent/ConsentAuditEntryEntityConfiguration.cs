using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Consent;

/// <summary>
/// EF Core configuration for <see cref="ConsentAuditEntryEntity"/>.
/// </summary>
public sealed class ConsentAuditEntryEntityConfiguration : IEntityTypeConfiguration<ConsentAuditEntryEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ConsentAuditEntryEntity> builder)
    {
        builder.ToTable("ConsentAuditEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SubjectId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Purpose)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Action)
            .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.Property(x => x.PerformedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.IpAddress)
            .IsRequired(false)
            .HasMaxLength(45);

        builder.Property(x => x.Metadata)
            .IsRequired(false);

        builder.HasIndex(x => x.SubjectId)
            .HasDatabaseName("IX_ConsentAuditEntries_SubjectId");

        builder.HasIndex(x => new { x.SubjectId, x.Purpose })
            .HasDatabaseName("IX_ConsentAuditEntries_SubjectId_Purpose");

        builder.HasIndex(x => x.OccurredAtUtc)
            .HasDatabaseName("IX_ConsentAuditEntries_OccurredAtUtc");
    }
}
