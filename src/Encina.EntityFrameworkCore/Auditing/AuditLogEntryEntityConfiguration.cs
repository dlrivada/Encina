using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Entity Framework Core configuration for <see cref="AuditLogEntryEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// This configuration is provider-agnostic and supports SQLite, SQL Server,
/// PostgreSQL, and MySQL through EF Core's database provider abstraction.
/// </para>
/// <para>
/// <b>Indexes</b>:
/// <list type="bullet">
/// <item><description>Composite index on (EntityType, EntityId) for efficient history lookups</description></item>
/// <item><description>Index on TimestampUtc for time-based queries</description></item>
/// <item><description>Filtered index on UserId for user activity tracking</description></item>
/// <item><description>Filtered index on CorrelationId for request correlation tracking</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class AuditLogEntryEntityConfiguration : IEntityTypeConfiguration<AuditLogEntryEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AuditLogEntryEntity> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.EntityId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Action)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.TimestampUtc)
            .IsRequired();

        builder.Property(x => x.OldValues)
            .IsRequired(false);

        builder.Property(x => x.NewValues)
            .IsRequired(false);

        builder.Property(x => x.CorrelationId)
            .IsRequired(false)
            .HasMaxLength(256);

        // Composite index for efficient history lookups by entity
        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");

        // Index for time-based queries
        builder.HasIndex(x => x.TimestampUtc)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        // Filtered index on UserId for user activity tracking
        builder.HasIndex(x => x.UserId)
            .HasFilter("UserId IS NOT NULL")
            .HasDatabaseName("IX_AuditLogs_UserId");

        // Filtered index on CorrelationId for request correlation tracking
        builder.HasIndex(x => x.CorrelationId)
            .HasFilter("CorrelationId IS NOT NULL")
            .HasDatabaseName("IX_AuditLogs_CorrelationId");
    }
}
