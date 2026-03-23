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

    /// <summary>
    /// Gets the correlation identifier linking this record to the originating request or saga.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the identifier of the actor (user, service, system) that triggered the auditable action.
    /// Required for EU AI Act Art. 14 human oversight traceability.
    /// </summary>
    public string? ActorId { get; init; }

    /// <summary>
    /// Gets the tenant identifier for multi-tenant deployments.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets the module identifier for modular monolith deployments.
    /// </summary>
    public string? ModuleId { get; init; }

    /// <summary>
    /// Gets extensibility metadata (regulation reference, department, classification level, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
