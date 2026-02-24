using Encina.Compliance.GDPR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.ProcessingActivity;

/// <summary>
/// EF Core configuration for <see cref="ProcessingActivityEntity"/>.
/// </summary>
public sealed class ProcessingActivityEntityConfiguration : IEntityTypeConfiguration<ProcessingActivityEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ProcessingActivityEntity> builder)
    {
        builder.ToTable("ProcessingActivities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(x => x.RequestTypeName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Purpose)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.LawfulBasisValue)
            .IsRequired();

        builder.Property(x => x.CategoriesOfDataSubjectsJson)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.CategoriesOfPersonalDataJson)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.RecipientsJson)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.ThirdCountryTransfers)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(x => x.Safeguards)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(x => x.RetentionPeriodTicks)
            .IsRequired();

        builder.Property(x => x.SecurityMeasures)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastUpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.RequestTypeName)
            .IsUnique()
            .HasDatabaseName("IX_ProcessingActivities_RequestTypeName");
    }
}
