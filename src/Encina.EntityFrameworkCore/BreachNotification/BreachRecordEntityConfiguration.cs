using Encina.Compliance.BreachNotification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.BreachNotification;

/// <summary>
/// EF Core configuration for <see cref="BreachRecordEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the <c>BreachRecords</c> table with indexes on <c>StatusValue</c> and
/// <c>DetectedAtUtc</c> to support efficient queries during breach notification
/// lifecycle management.
/// </para>
/// <para>
/// Enum values (<c>StatusValue</c>, <c>SeverityValue</c>, <c>SubjectNotificationExemptionValue</c>)
/// are stored as integers for cross-provider compatibility.
/// <c>CategoriesOfDataAffected</c> is stored as a JSON string.
/// </para>
/// </remarks>
internal sealed class BreachRecordEntityConfiguration : IEntityTypeConfiguration<BreachRecordEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BreachRecordEntity> builder)
    {
        builder.ToTable("BreachRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Nature)
            .IsRequired()
            .HasMaxLength(4096);

        builder.Property(x => x.ApproximateSubjectsAffected)
            .IsRequired();

        builder.Property(x => x.CategoriesOfDataAffected)
            .IsRequired()
            .HasMaxLength(4096);

        builder.Property(x => x.DPOContactDetails)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.LikelyConsequences)
            .IsRequired()
            .HasMaxLength(4096);

        builder.Property(x => x.MeasuresTaken)
            .IsRequired()
            .HasMaxLength(4096);

        builder.Property(x => x.DetectedAtUtc)
            .IsRequired();

        builder.Property(x => x.NotificationDeadlineUtc)
            .IsRequired();

        builder.Property(x => x.NotifiedAuthorityAtUtc)
            .IsRequired(false);

        builder.Property(x => x.NotifiedSubjectsAtUtc)
            .IsRequired(false);

        builder.Property(x => x.SeverityValue)
            .IsRequired();

        builder.Property(x => x.StatusValue)
            .IsRequired();

        builder.Property(x => x.DelayReason)
            .IsRequired(false)
            .HasMaxLength(4096);

        builder.Property(x => x.SubjectNotificationExemptionValue)
            .IsRequired();

        builder.Property(x => x.ResolvedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.ResolutionSummary)
            .IsRequired(false)
            .HasMaxLength(4096);

        builder.HasIndex(x => x.StatusValue)
            .HasDatabaseName("IX_BreachRecords_StatusValue");

        builder.HasIndex(x => x.DetectedAtUtc)
            .HasDatabaseName("IX_BreachRecords_DetectedAtUtc");
    }
}
