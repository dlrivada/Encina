using Encina.Compliance.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Retention;

/// <summary>
/// EF Core configuration for <see cref="LegalHoldEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the <c>LegalHolds</c> table with indexes on <c>EntityId</c> and a composite
/// index on <c>(EntityId, ReleasedAtUtc)</c> to support efficient active-hold queries
/// during enforcement cycles.
/// </para>
/// <para>
/// Active holds are identified by <c>ReleasedAtUtc IS NULL</c>. The composite index
/// optimizes the <see cref="ILegalHoldStore.IsUnderHoldAsync"/> query which is called
/// before every deletion attempt.
/// </para>
/// </remarks>
internal sealed class LegalHoldEntityConfiguration : IEntityTypeConfiguration<LegalHoldEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LegalHoldEntity> builder)
    {
        builder.ToTable("LegalHolds");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.EntityId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.AppliedByUserId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.AppliedAtUtc)
            .IsRequired();

        builder.Property(x => x.ReleasedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.ReleasedByUserId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.HasIndex(x => x.EntityId)
            .HasDatabaseName("IX_LegalHolds_EntityId");

        builder.HasIndex(x => new { x.EntityId, x.ReleasedAtUtc })
            .HasDatabaseName("IX_LegalHolds_EntityId_ReleasedAtUtc");
    }
}
