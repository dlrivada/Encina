using Encina.Compliance.BreachNotification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.BreachNotification;

/// <summary>
/// EF Core configuration for <see cref="BreachAuditEntryEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the <c>BreachAuditEntries</c> table with an index on <c>BreachId</c>
/// to support efficient audit trail queries by breach identifier.
/// </para>
/// <para>
/// Audit entries are append-only and should never be modified or deleted, as they serve
/// as legal evidence of breach notification measures per GDPR Article 33(5).
/// </para>
/// </remarks>
internal sealed class BreachAuditEntryEntityConfiguration : IEntityTypeConfiguration<BreachAuditEntryEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BreachAuditEntryEntity> builder)
    {
        builder.ToTable("BreachAuditEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.BreachId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Detail)
            .IsRequired(false)
            .HasMaxLength(4096);

        builder.Property(x => x.PerformedByUserId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.BreachId)
            .HasDatabaseName("IX_BreachAuditEntries_BreachId");
    }
}
