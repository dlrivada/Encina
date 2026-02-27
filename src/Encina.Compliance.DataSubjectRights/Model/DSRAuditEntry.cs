namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Represents an entry in the DSR audit trail for demonstrating compliance.
/// </summary>
/// <remarks>
/// <para>
/// Each audit entry records a specific action taken during the processing of a
/// Data Subject Rights request. The audit trail is immutable and provides evidence
/// of compliance with GDPR obligations.
/// </para>
/// <para>
/// Audit entries cover the entire request lifecycle: receipt, identity verification,
/// processing steps, completion or rejection, extensions, and third-party notifications.
/// </para>
/// </remarks>
public sealed record DSRAuditEntry
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Identifier of the DSR request this audit entry belongs to.
    /// </summary>
    public required string DSRRequestId { get; init; }

    /// <summary>
    /// The action that was performed.
    /// </summary>
    /// <example>
    /// "RequestReceived", "IdentityVerified", "ErasureExecuted",
    /// "ThirdPartyNotified", "RequestCompleted", "RequestRejected"
    /// </example>
    public required string Action { get; init; }

    /// <summary>
    /// Additional details about the action performed.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no additional context is needed.
    /// For erasure actions, this may contain the number of fields erased.
    /// For rejection, this may contain the reason.
    /// </remarks>
    public string? Detail { get; init; }

    /// <summary>
    /// Identifier of the user or system that performed the action.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for automated system actions (e.g., deadline expiration).
    /// </remarks>
    public string? PerformedByUserId { get; init; }

    /// <summary>
    /// Timestamp when the action occurred (UTC).
    /// </summary>
    public required DateTimeOffset OccurredAtUtc { get; init; }
}
