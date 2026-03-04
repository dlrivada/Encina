using Encina.Security.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Entity Framework Core configuration for <see cref="ReadAuditEntryEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// This configuration is provider-agnostic and supports SQLite, SQL Server,
/// PostgreSQL, and MySQL through EF Core's database provider abstraction.
/// </para>
/// <para>
/// <b>Table</b>: "ReadAuditEntries"
/// </para>
/// <para>
/// <b>Indexes</b> optimized for common read audit query patterns:
/// <list type="bullet">
/// <item><description>Composite index on (EntityType, EntityId) for entity access history lookups</description></item>
/// <item><description>Index on AccessedAtUtc for time-based queries and retention purges</description></item>
/// <item><description>Filtered index on UserId for user access tracking (GDPR Art. 15)</description></item>
/// <item><description>Filtered index on TenantId for multi-tenant access queries</description></item>
/// <item><description>Filtered index on CorrelationId for request correlation tracking</description></item>
/// <item><description>Index on AccessMethod for access vector analysis</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ReadAuditEntryEntityConfiguration : IEntityTypeConfiguration<ReadAuditEntryEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ReadAuditEntryEntity> builder)
    {
        builder.ToTable("ReadAuditEntries");

        builder.HasKey(x => x.Id);

        // Primary identifier
        builder.Property(x => x.Id)
            .IsRequired();

        // Entity context
        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.EntityId)
            .IsRequired(false)
            .HasMaxLength(256);

        // User and tenant context
        builder.Property(x => x.UserId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.TenantId)
            .IsRequired(false)
            .HasMaxLength(128);

        // Timestamp
        builder.Property(x => x.AccessedAtUtc)
            .IsRequired();

        // Correlation
        builder.Property(x => x.CorrelationId)
            .IsRequired(false)
            .HasMaxLength(256);

        // GDPR Art. 15 - purpose of access
        builder.Property(x => x.Purpose)
            .IsRequired(false)
            .HasMaxLength(1024);

        // Access method (enum ordinal)
        builder.Property(x => x.AccessMethod)
            .IsRequired();

        // Entity count for bulk read volume tracking
        builder.Property(x => x.EntityCount)
            .IsRequired();

        // JSON metadata
        builder.Property(x => x.Metadata)
            .IsRequired(false);

        // Composite index for efficient entity access history lookups
        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_ReadAuditEntries_Entity");

        // Index for time-based queries and retention purges
        builder.HasIndex(x => x.AccessedAtUtc)
            .HasDatabaseName("IX_ReadAuditEntries_AccessedAt");

        // Filtered index on UserId for GDPR Art. 15 user access tracking
        builder.HasIndex(x => x.UserId)
            .HasFilter("UserId IS NOT NULL")
            .HasDatabaseName("IX_ReadAuditEntries_UserId");

        // Filtered index on TenantId for multi-tenant access queries
        builder.HasIndex(x => x.TenantId)
            .HasFilter("TenantId IS NOT NULL")
            .HasDatabaseName("IX_ReadAuditEntries_TenantId");

        // Filtered index on CorrelationId for request correlation tracking
        builder.HasIndex(x => x.CorrelationId)
            .HasFilter("CorrelationId IS NOT NULL")
            .HasDatabaseName("IX_ReadAuditEntries_CorrelationId");

        // Index on AccessMethod for access vector analysis
        builder.HasIndex(x => x.AccessMethod)
            .HasDatabaseName("IX_ReadAuditEntries_AccessMethod");
    }
}
