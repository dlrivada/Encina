namespace Encina.Compliance.Attestation.Model;

/// <summary>
/// Represents an audit record to be attested.
/// </summary>
public sealed record AuditRecord
{
    /// <summary>
    /// Gets the unique identifier for this audit record.
    /// </summary>
    public required Guid RecordId { get; init; }

    /// <summary>
    /// Gets the type discriminator for the audit event (e.g., "HumanOversightDecision", "DataAccessEvent").
    /// </summary>
    public required string RecordType { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the auditable event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>
    /// Gets the serialized content of the audit record (typically JSON).
    /// </summary>
    public required string SerializedContent { get; init; }
}
