namespace Encina.DomainModeling.Auditing;

/// <summary>
/// Represents a detailed audit log entry capturing changes made to an entity.
/// </summary>
/// <remarks>
/// <para>
/// This record captures both the action performed and the before/after state
/// of the entity, enabling complete audit trail reconstruction.
/// </para>
/// <para>
/// The <see cref="OldValues"/> and <see cref="NewValues"/> properties store
/// JSON-serialized representations of the entity's state before and after
/// the change, respectively.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var entry = new AuditLogEntry(
///     Id: Guid.NewGuid().ToString(),
///     EntityType: "Order",
///     EntityId: "order-123",
///     Action: AuditAction.Updated,
///     UserId: "user-456",
///     TimestampUtc: DateTime.UtcNow,
///     OldValues: "{\"Status\":\"Pending\"}",
///     NewValues: "{\"Status\":\"Shipped\"}",
///     CorrelationId: "corr-789");
/// </code>
/// </example>
/// <param name="Id">The unique identifier for this audit log entry.</param>
/// <param name="EntityType">The type name of the entity that was changed (e.g., "Order", "Customer").</param>
/// <param name="EntityId">The string representation of the entity's primary key.</param>
/// <param name="Action">The type of action performed on the entity.</param>
/// <param name="UserId">The ID of the user who performed the action, or <c>null</c> if unknown.</param>
/// <param name="TimestampUtc">The UTC timestamp when the action occurred.</param>
/// <param name="OldValues">
/// JSON-serialized representation of the entity's state before the change.
/// <c>null</c> for <see cref="AuditAction.Created"/> actions.
/// </param>
/// <param name="NewValues">
/// JSON-serialized representation of the entity's state after the change.
/// <c>null</c> for <see cref="AuditAction.Deleted"/> actions.
/// </param>
/// <param name="CorrelationId">
/// An optional correlation ID to group related audit entries (e.g., from the same request or transaction).
/// </param>
public sealed record AuditLogEntry(
    string Id,
    string EntityType,
    string EntityId,
    AuditAction Action,
    string? UserId,
    DateTime TimestampUtc,
    string? OldValues,
    string? NewValues,
    string? CorrelationId);
