using Encina.Compliance.ProcessorAgreements;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.ProcessorAgreements;

/// <summary>
/// EF Core configuration for <see cref="ProcessorAgreementAuditEntryEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the <c>ProcessorAgreementAuditEntries</c> table with an index on <c>ProcessorId</c>
/// to support efficient audit trail queries for compliance reporting.
/// </para>
/// <para>
/// Audit entries are append-only and should never be modified or deleted, as they serve
/// as legal evidence of processor agreement actions per GDPR Article 5(2).
/// </para>
/// </remarks>
internal sealed class ProcessorAgreementAuditEntryEntityConfiguration : IEntityTypeConfiguration<ProcessorAgreementAuditEntryEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ProcessorAgreementAuditEntryEntity> builder)
    {
        builder.ToTable("ProcessorAgreementAuditEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ProcessorId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.DPAId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Detail)
            .IsRequired(false);

        builder.Property(x => x.PerformedByUserId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.ProcessorId)
            .HasDatabaseName("IX_ProcessorAgreementAuditEntries_ProcessorId");
    }
}
