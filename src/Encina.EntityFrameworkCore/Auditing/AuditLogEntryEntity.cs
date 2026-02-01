using Encina.DomainModeling.Auditing;

namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Entity Framework Core entity for persisting audit log entries.
/// </summary>
/// <remarks>
/// <para>
/// This entity maps to the "AuditLogs" table and stores detailed audit trail
/// information including the before/after state of entity changes.
/// </para>
/// <para>
/// The entity is optimized for common query patterns:
/// <list type="bullet">
/// <item><description>History lookups by entity type and ID (composite index)</description></item>
/// <item><description>Time-based queries for auditing reports (timestamp index)</description></item>
/// <item><description>User activity tracking (filtered index on UserId)</description></item>
/// <item><description>Correlation tracking for request tracing (filtered index on CorrelationId)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class AuditLogEntryEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit log entry.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the type name of the entity that was changed (e.g., "Order", "Customer").
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the string representation of the entity's primary key.
    /// </summary>
    public required string EntityId { get; set; }

    /// <summary>
    /// Gets or sets the type of action performed on the entity.
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who performed the action, or <c>null</c> if unknown.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the action occurred.
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized representation of the entity's state before the change.
    /// </summary>
    /// <value>
    /// <c>null</c> for <see cref="AuditAction.Created"/> actions.
    /// </value>
    public string? OldValues { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized representation of the entity's state after the change.
    /// </summary>
    /// <value>
    /// <c>null</c> for <see cref="AuditAction.Deleted"/> actions.
    /// </value>
    public string? NewValues { get; set; }

    /// <summary>
    /// Gets or sets an optional correlation ID to group related audit entries.
    /// </summary>
    public string? CorrelationId { get; set; }
}
