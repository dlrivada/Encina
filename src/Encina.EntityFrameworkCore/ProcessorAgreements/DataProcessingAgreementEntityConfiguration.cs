using Encina.Compliance.ProcessorAgreements;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.ProcessorAgreements;

/// <summary>
/// EF Core configuration for <see cref="DataProcessingAgreementEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the <c>DataProcessingAgreements</c> table with indexes on <c>ProcessorId</c>,
/// <c>StatusValue</c>, and <c>ExpiresAtUtc</c> to support efficient queries during
/// active DPA lookup, status filtering, and expiration monitoring.
/// </para>
/// <para>
/// The 8 mandatory terms from Article 28(3)(a)-(h) are stored as individual boolean columns
/// to enable efficient querying (e.g., "find all DPAs missing security measures"). The
/// <see cref="DataProcessingAgreementEntity.ProcessingPurposesJson"/> column stores a JSON
/// array of strings.
/// </para>
/// </remarks>
internal sealed class DataProcessingAgreementEntityConfiguration : IEntityTypeConfiguration<DataProcessingAgreementEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DataProcessingAgreementEntity> builder)
    {
        builder.ToTable("DataProcessingAgreements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ProcessorId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.StatusValue)
            .IsRequired();

        builder.Property(x => x.SignedAtUtc)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired(false);

        builder.Property(x => x.HasSCCs)
            .IsRequired();

        builder.Property(x => x.ProcessingPurposesJson)
            .IsRequired();

        // Mandatory terms (Article 28(3)(a)-(h))
        builder.Property(x => x.ProcessOnDocumentedInstructions).IsRequired();
        builder.Property(x => x.ConfidentialityObligations).IsRequired();
        builder.Property(x => x.SecurityMeasures).IsRequired();
        builder.Property(x => x.SubProcessorRequirements).IsRequired();
        builder.Property(x => x.DataSubjectRightsAssistance).IsRequired();
        builder.Property(x => x.ComplianceAssistance).IsRequired();
        builder.Property(x => x.DataDeletionOrReturn).IsRequired();
        builder.Property(x => x.AuditRights).IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.ModuleId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastUpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.ProcessorId)
            .HasDatabaseName("IX_DataProcessingAgreements_ProcessorId");

        builder.HasIndex(x => x.StatusValue)
            .HasDatabaseName("IX_DataProcessingAgreements_StatusValue");

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("IX_DataProcessingAgreements_ExpiresAtUtc");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_DataProcessingAgreements_TenantId");
    }
}
