namespace Encina.Compliance.Retention;

/// <summary>
/// Persistence entity for <see cref="Model.RetentionAuditEntry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a retention audit entry,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// All properties map directly to <see cref="Model.RetentionAuditEntry"/> without type
/// transformations, since the domain model already uses primitive types (strings, <see cref="DateTimeOffset"/>).
/// </para>
/// <para>
/// Use <see cref="RetentionAuditEntryMapper"/> to convert between this entity and
/// <see cref="Model.RetentionAuditEntry"/>.
/// </para>
/// </remarks>
public sealed class RetentionAuditEntryEntity
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The action that was performed.
    /// </summary>
    /// <remarks>
    /// Examples: "PolicyCreated", "RecordTracked", "EnforcementExecuted",
    /// "RecordDeleted", "LegalHoldApplied", "LegalHoldReleased".
    /// An INDEX may be created on this column for action-type queries.
    /// </remarks>
    public required string Action { get; set; }

    /// <summary>
    /// Identifier of the data entity affected by this action.
    /// </summary>
    /// <remarks>
    /// An INDEX should be created on this column for efficient entity-scoped audit queries.
    /// </remarks>
    public string? EntityId { get; set; }

    /// <summary>
    /// The data category affected by this action.
    /// </summary>
    public string? DataCategory { get; set; }

    /// <summary>
    /// Additional details about the action performed.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Identifier of the user or system that performed the action.
    /// </summary>
    public string? PerformedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the action occurred (UTC).
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; set; }
}
