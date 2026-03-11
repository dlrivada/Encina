using Encina.Compliance.DPIA;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.DPIA;

/// <summary>
/// EF Core configuration for <see cref="DPIAAssessmentEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the <c>DPIAAssessments</c> table with indexes on <c>RequestTypeName</c>,
/// <c>StatusValue</c>, and <c>NextReviewAtUtc</c> to support efficient queries during
/// assessment lookup, status filtering, and expiration monitoring.
/// </para>
/// <para>
/// The <see cref="DPIAAssessmentEntity.StatusValue"/> column stores the
/// <see cref="Model.DPIAAssessmentStatus"/> enum as an integer for cross-provider compatibility.
/// The <see cref="DPIAAssessmentEntity.ResultJson"/> and <see cref="DPIAAssessmentEntity.DPOConsultationJson"/>
/// columns store complex nested objects as JSON text.
/// </para>
/// </remarks>
internal sealed class DPIAAssessmentEntityConfiguration : IEntityTypeConfiguration<DPIAAssessmentEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DPIAAssessmentEntity> builder)
    {
        builder.ToTable("DPIAAssessments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(x => x.RequestTypeName)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.StatusValue)
            .IsRequired();

        builder.Property(x => x.ProcessingType)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.Reason)
            .IsRequired(false);

        builder.Property(x => x.ResultJson)
            .IsRequired(false);

        builder.Property(x => x.DPOConsultationJson)
            .IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ApprovedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.NextReviewAtUtc)
            .IsRequired(false);

        builder.HasIndex(x => x.RequestTypeName)
            .IsUnique()
            .HasDatabaseName("UX_DPIAAssessments_RequestTypeName");

        builder.HasIndex(x => x.StatusValue)
            .HasDatabaseName("IX_DPIAAssessments_StatusValue");

        builder.HasIndex(x => x.NextReviewAtUtc)
            .HasDatabaseName("IX_DPIAAssessments_NextReviewAtUtc");
    }
}
