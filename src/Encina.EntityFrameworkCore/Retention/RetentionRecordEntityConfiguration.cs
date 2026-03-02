using Encina.Compliance.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Retention;

/// <summary>
/// EF Core configuration for <see cref="RetentionRecordEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the <c>RetentionRecords</c> table with indexes on <c>EntityId</c>,
/// <c>ExpiresAtUtc</c>, and <c>StatusValue</c> to support efficient queries during
/// enforcement cycles.
/// </para>
/// <para>
/// The <see cref="RetentionRecordEntity.StatusValue"/> column stores the
/// <see cref="Model.RetentionStatus"/> enum as an integer for cross-provider compatibility.
/// </para>
/// </remarks>
internal sealed class RetentionRecordEntityConfiguration : IEntityTypeConfiguration<RetentionRecordEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RetentionRecordEntity> builder)
    {
        builder.ToTable("RetentionRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.EntityId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.DataCategory)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.PolicyId)
            .IsRequired(false)
            .HasMaxLength(32);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.StatusValue)
            .IsRequired();

        builder.Property(x => x.DeletedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.LegalHoldId)
            .IsRequired(false)
            .HasMaxLength(32);

        builder.HasIndex(x => x.EntityId)
            .HasDatabaseName("IX_RetentionRecords_EntityId");

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("IX_RetentionRecords_ExpiresAtUtc");

        builder.HasIndex(x => x.StatusValue)
            .HasDatabaseName("IX_RetentionRecords_StatusValue");
    }
}
