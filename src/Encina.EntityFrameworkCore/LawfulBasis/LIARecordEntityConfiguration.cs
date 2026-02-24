using Encina.Compliance.GDPR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.LawfulBasis;

/// <summary>
/// EF Core configuration for <see cref="LIARecordEntity"/>.
/// </summary>
public sealed class LIARecordEntityConfiguration : IEntityTypeConfiguration<LIARecordEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LIARecordEntity> builder)
    {
        builder.ToTable("LIARecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.Purpose)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.LegitimateInterest)
            .IsRequired();

        builder.Property(x => x.Benefits)
            .IsRequired();

        builder.Property(x => x.ConsequencesIfNotProcessed)
            .IsRequired();

        builder.Property(x => x.NecessityJustification)
            .IsRequired();

        builder.Property(x => x.AlternativesConsideredJson)
            .IsRequired();

        builder.Property(x => x.DataMinimisationNotes)
            .IsRequired();

        builder.Property(x => x.NatureOfData)
            .IsRequired();

        builder.Property(x => x.ReasonableExpectations)
            .IsRequired();

        builder.Property(x => x.ImpactAssessment)
            .IsRequired();

        builder.Property(x => x.SafeguardsJson)
            .IsRequired();

        builder.Property(x => x.OutcomeValue)
            .IsRequired();

        builder.Property(x => x.Conclusion)
            .IsRequired();

        builder.Property(x => x.Conditions)
            .IsRequired(false);

        builder.Property(x => x.AssessedAtUtc)
            .IsRequired();

        builder.Property(x => x.AssessedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.DPOInvolvement)
            .IsRequired();

        builder.Property(x => x.NextReviewAtUtc)
            .IsRequired(false);

        builder.HasIndex(x => x.OutcomeValue)
            .HasDatabaseName("IX_LIARecords_OutcomeValue");
    }
}
