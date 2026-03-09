using Encina.Security.ABAC.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.ABAC;

/// <summary>
/// Entity Framework Core configuration for <see cref="PolicySetEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps the <see cref="PolicySetEntity"/> to the <c>abac_policy_sets</c> table.
/// This configuration is provider-agnostic and supports SQLite, SQL Server,
/// PostgreSQL, and MySQL through EF Core's database provider abstraction.
/// </para>
/// <para>
/// The <see cref="PolicySetEntity.PolicyJson"/> column stores the full serialized
/// policy set graph as JSON text. EF Core's provider-specific JSON column types
/// (e.g., <c>JSONB</c> for PostgreSQL) should be configured at the provider level
/// if needed.
/// </para>
/// <para>
/// <b>Indexes</b>:
/// <list type="bullet">
/// <item><description>Composite index on (IsEnabled, Priority) for efficient enabled policy set retrieval ordered by priority</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class PolicySetEntityConfiguration : IEntityTypeConfiguration<PolicySetEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<PolicySetEntity> builder)
    {
        builder.ToTable("abac_policy_sets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Version)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.Description)
            .IsRequired(false);

        builder.Property(x => x.PolicyJson)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.Priority)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        // Composite index for filtering enabled policy sets ordered by priority
        builder.HasIndex(x => new { x.IsEnabled, x.Priority })
            .HasDatabaseName("IX_abac_policy_sets_IsEnabled_Priority");
    }
}
