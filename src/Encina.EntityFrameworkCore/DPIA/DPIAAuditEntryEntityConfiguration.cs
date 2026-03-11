using Encina.Compliance.DPIA;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.DPIA;

/// <summary>
/// EF Core configuration for <see cref="DPIAAuditEntryEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the <c>DPIAAuditEntries</c> table with an index on <c>AssessmentId</c>
/// to support efficient audit trail queries for compliance reporting.
/// </para>
/// <para>
/// Audit entries are append-only and should never be modified or deleted, as they serve
/// as legal evidence of DPIA assessment actions per GDPR Article 5(2).
/// </para>
/// </remarks>
internal sealed class DPIAAuditEntryEntityConfiguration : IEntityTypeConfiguration<DPIAAuditEntryEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DPIAAuditEntryEntity> builder)
    {
        builder.ToTable("DPIAAuditEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(x => x.AssessmentId)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.PerformedBy)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.Property(x => x.Details)
            .IsRequired(false);

        builder.HasIndex(x => x.AssessmentId)
            .HasDatabaseName("IX_DPIAAuditEntries_AssessmentId");
    }
}
