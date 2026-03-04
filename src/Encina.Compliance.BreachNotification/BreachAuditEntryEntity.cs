namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Persistence entity for <see cref="Model.BreachAuditEntry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a breach audit entry,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Audit entries are immutable compliance records. Once persisted, they should never be
/// modified or deleted, per GDPR Article 33(5) documentation requirements.
/// </para>
/// <para>
/// Use <see cref="BreachAuditEntryMapper"/> to convert between this entity and
/// <see cref="Model.BreachAuditEntry"/>.
/// </para>
/// </remarks>
public sealed class BreachAuditEntryEntity
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Identifier of the breach this audit entry relates to.
    /// </summary>
    /// <remarks>
    /// An INDEX should be created on this column for efficient breach-based lookups.
    /// </remarks>
    public required string BreachId { get; set; }

    /// <summary>
    /// The action that was performed.
    /// </summary>
    public required string Action { get; set; }

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
