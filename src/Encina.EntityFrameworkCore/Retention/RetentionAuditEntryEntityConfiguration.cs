using Encina.Compliance.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Retention;

/// <summary>
/// EF Core configuration for <see cref="RetentionAuditEntryEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the <c>RetentionAuditEntries</c> table with indexes on <c>EntityId</c> and
/// <c>OccurredAtUtc</c> to support efficient audit trail queries for compliance reporting.
/// </para>
/// <para>
/// Audit entries are append-only and should never be modified or deleted, as they serve
/// as legal evidence of retention and deletion measures per GDPR Article 5(2).
/// </para>
/// </remarks>
internal sealed class RetentionAuditEntryEntityConfiguration : IEntityTypeConfiguration<RetentionAuditEntryEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RetentionAuditEntryEntity> builder)
    {
        builder.ToTable("RetentionAuditEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.EntityId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.DataCategory)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.Detail)
            .IsRequired(false)
            .HasMaxLength(2048);

        builder.Property(x => x.PerformedByUserId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.EntityId)
            .HasDatabaseName("IX_RetentionAuditEntries_EntityId");

        builder.HasIndex(x => x.OccurredAtUtc)
            .HasDatabaseName("IX_RetentionAuditEntries_OccurredAtUtc");
    }
}
