namespace Encina.Compliance.Consent;

/// <summary>
/// Represents an entry in the consent audit trail for demonstrating compliance.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 7(1) requires that where processing is based on consent, the controller
/// shall be able to demonstrate that the data subject has consented to the processing.
/// Audit entries provide a complete, immutable trail of all consent-related actions.
/// </para>
/// <para>
/// Each entry captures who performed the action, when it occurred, and any contextual
/// metadata that supports demonstrability. The audit trail should never be modified
/// or deleted, as it serves as legal evidence of consent management.
/// </para>
/// </remarks>
public sealed record ConsentAuditEntry
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Identifier of the data subject whose consent was affected.
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// The processing purpose associated with this consent action.
    /// </summary>
    public required string Purpose { get; init; }

    /// <summary>
    /// The type of consent action that was performed.
    /// </summary>
    public required ConsentAuditAction Action { get; init; }

    /// <summary>
    /// Timestamp when the action occurred (UTC).
    /// </summary>
    public required DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>
    /// Identifier of the actor who performed or triggered the action.
    /// </summary>
    /// <remarks>
    /// This could be the data subject themselves (for granting/withdrawing consent),
    /// the system (for automated expiration), or an administrator (for version changes).
    /// </remarks>
    /// <example>"user-123", "system", "admin@company.com"</example>
    public required string PerformedBy { get; init; }

    /// <summary>
    /// The IP address of the actor at the time of the action.
    /// </summary>
    /// <remarks>
    /// Optional. May be <c>null</c> for system-initiated actions (e.g., automated expiration)
    /// or actions performed through channels where IP address is not available.
    /// </remarks>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Additional metadata associated with this audit entry.
    /// </summary>
    /// <remarks>
    /// Extensible key-value pairs for storing contextual information such as the
    /// consent version at the time of action, the user agent, or the reason for withdrawal.
    /// </remarks>
    public required IReadOnlyDictionary<string, object?> Metadata { get; init; }
}
