using Encina.Compliance.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Retention;

/// <summary>
/// EF Core configuration for <see cref="RetentionPolicyEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the <c>RetentionPolicies</c> table with a unique index on <c>DataCategory</c>
/// to enforce the one-policy-per-category constraint.
/// </para>
/// <para>
/// The <see cref="RetentionPolicyEntity.RetentionPeriodTicks"/> column stores the retention
/// period as ticks for cross-provider portability. The <see cref="RetentionPolicyEntity.PolicyTypeValue"/>
/// column stores the <see cref="Model.RetentionPolicyType"/> enum as an integer.
/// </para>
/// </remarks>
internal sealed class RetentionPolicyEntityConfiguration : IEntityTypeConfiguration<RetentionPolicyEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RetentionPolicyEntity> builder)
    {
        builder.ToTable("RetentionPolicies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.DataCategory)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.RetentionPeriodTicks)
            .IsRequired();

        builder.Property(x => x.AutoDelete)
            .IsRequired();

        builder.Property(x => x.Reason)
            .IsRequired(false)
            .HasMaxLength(1024);

        builder.Property(x => x.LegalBasis)
            .IsRequired(false)
            .HasMaxLength(512);

        builder.Property(x => x.PolicyTypeValue)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastModifiedAtUtc)
            .IsRequired(false);

        builder.HasIndex(x => x.DataCategory)
            .IsUnique()
            .HasDatabaseName("IX_RetentionPolicies_DataCategory");
    }
}
