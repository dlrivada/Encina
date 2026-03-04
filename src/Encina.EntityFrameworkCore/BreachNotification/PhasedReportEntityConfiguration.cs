using Encina.Compliance.BreachNotification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.BreachNotification;

/// <summary>
/// EF Core configuration for <see cref="PhasedReportEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the <c>BreachPhasedReports</c> table with an index on <c>BreachId</c>
/// for efficient phased report queries by breach identifier.
/// </para>
/// <para>
/// Phased reports are stored in a normalized table separate from breach records
/// to support one-to-many relationships per GDPR Article 33(4).
/// </para>
/// </remarks>
internal sealed class PhasedReportEntityConfiguration : IEntityTypeConfiguration<PhasedReportEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<PhasedReportEntity> builder)
    {
        builder.ToTable("BreachPhasedReports");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.BreachId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.ReportNumber)
            .IsRequired();

        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(8192);

        builder.Property(x => x.SubmittedAtUtc)
            .IsRequired();

        builder.Property(x => x.SubmittedByUserId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.HasIndex(x => x.BreachId)
            .HasDatabaseName("IX_BreachPhasedReports_BreachId");
    }
}
