namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Persistence entity for <see cref="DSRAuditEntry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a DSR audit trail entry,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// The audit trail is immutable and provides evidence of compliance with GDPR obligations.
/// Each entry records a specific action taken during the processing of a DSR request.
/// </para>
/// <para>
/// Use <see cref="DSRAuditEntryMapper"/> to convert between this entity and <see cref="DSRAuditEntry"/>.
/// </para>
/// </remarks>
public sealed class DSRAuditEntryEntity
{
    /// <summary>
    /// Unique identifier for this audit entry record (GUID as string).
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Identifier of the DSR request this audit entry belongs to.
    /// </summary>
    public required string DSRRequestId { get; set; }

    /// <summary>
    /// The action that was performed (e.g., "RequestReceived", "IdentityVerified", "ErasureExecuted").
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Additional details about the action performed, if applicable.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Identifier of the user or system that performed the action, if applicable.
    /// </summary>
    public string? PerformedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the action occurred (UTC).
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; set; }
}
