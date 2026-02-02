using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Entity Framework Core configuration for <see cref="AuditEntryEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// This configuration is provider-agnostic and supports SQLite, SQL Server,
/// PostgreSQL, and MySQL through EF Core's database provider abstraction.
/// </para>
/// <para>
/// <b>Indexes</b> optimized for common audit query patterns:
/// <list type="bullet">
/// <item><description>Composite index on (EntityType, EntityId) for entity history lookups</description></item>
/// <item><description>Index on TimestampUtc for time-based queries</description></item>
/// <item><description>Index on Outcome for filtering by operation result</description></item>
/// <item><description>Filtered index on UserId for user activity tracking</description></item>
/// <item><description>Filtered index on TenantId for multi-tenant queries</description></item>
/// <item><description>Filtered index on CorrelationId for request tracing</description></item>
/// <item><description>Filtered index on Action for action-based filtering</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class AuditEntryEntityConfiguration : IEntityTypeConfiguration<AuditEntryEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AuditEntryEntity> builder)
    {
        builder.ToTable("SecurityAuditEntries");

        builder.HasKey(x => x.Id);

        // Primary identifiers
        builder.Property(x => x.Id)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .IsRequired()
            .HasMaxLength(256);

        // User and tenant context
        builder.Property(x => x.UserId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.TenantId)
            .IsRequired(false)
            .HasMaxLength(128);

        // Operation details
        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.EntityId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.Outcome)
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .IsRequired(false)
            .HasMaxLength(2048);

        // Timestamps
        builder.Property(x => x.TimestampUtc)
            .IsRequired();

        builder.Property(x => x.StartedAtUtc)
            .IsRequired();

        builder.Property(x => x.CompletedAtUtc)
            .IsRequired();

        // Client context
        builder.Property(x => x.IpAddress)
            .IsRequired(false)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.UserAgent)
            .IsRequired(false)
            .HasMaxLength(512);

        // Payload data
        builder.Property(x => x.RequestPayloadHash)
            .IsRequired(false)
            .HasMaxLength(64); // SHA-256 hex string

        builder.Property(x => x.RequestPayload)
            .IsRequired(false);

        builder.Property(x => x.ResponsePayload)
            .IsRequired(false);

        builder.Property(x => x.Metadata)
            .IsRequired(false);

        // Composite index for efficient entity history lookups
        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_SecurityAuditEntries_Entity");

        // Index for time-based queries
        builder.HasIndex(x => x.TimestampUtc)
            .HasDatabaseName("IX_SecurityAuditEntries_Timestamp");

        // Index for outcome-based filtering
        builder.HasIndex(x => x.Outcome)
            .HasDatabaseName("IX_SecurityAuditEntries_Outcome");

        // Filtered index on UserId for user activity tracking
        builder.HasIndex(x => x.UserId)
            .HasFilter("UserId IS NOT NULL")
            .HasDatabaseName("IX_SecurityAuditEntries_UserId");

        // Filtered index on TenantId for multi-tenant queries
        builder.HasIndex(x => x.TenantId)
            .HasFilter("TenantId IS NOT NULL")
            .HasDatabaseName("IX_SecurityAuditEntries_TenantId");

        // Filtered index on CorrelationId for request correlation tracking
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_SecurityAuditEntries_CorrelationId");

        // Filtered index on Action for action-based filtering
        builder.HasIndex(x => x.Action)
            .HasDatabaseName("IX_SecurityAuditEntries_Action");
    }
}
