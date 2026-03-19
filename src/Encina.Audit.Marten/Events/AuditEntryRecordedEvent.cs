using Encina.Security.Audit;

namespace Encina.Audit.Marten.Events;

/// <summary>
/// Event-sourced event representing an audit trail entry being recorded.
/// </summary>
/// <remarks>
/// <para>
/// This event is appended to Marten event streams by <c>MartenAuditStore.RecordAsync</c>.
/// PII-sensitive fields are stored as <see cref="EncryptedField"/> values encrypted with
/// temporal keys, while structural fields remain in plaintext for querying and compliance analysis.
/// </para>
/// <para>
/// <b>Plaintext fields</b> (always queryable, even after crypto-shredding):
/// <c>Id</c>, <c>CorrelationId</c>, <c>Action</c>, <c>EntityType</c>, <c>EntityId</c>,
/// <c>Outcome</c>, <c>ErrorMessage</c>, <c>TimestampUtc</c>, <c>StartedAtUtc</c>,
/// <c>CompletedAtUtc</c>, <c>RequestPayloadHash</c>, <c>TenantId</c>.
/// </para>
/// <para>
/// <b>Encrypted fields</b> (shredded when temporal keys are destroyed):
/// <c>EncryptedUserId</c>, <c>EncryptedIpAddress</c>, <c>EncryptedUserAgent</c>,
/// <c>EncryptedRequestPayload</c>, <c>EncryptedResponsePayload</c>, <c>EncryptedMetadata</c>.
/// </para>
/// </remarks>
public sealed record AuditEntryRecordedEvent
{
    // ── Plaintext structural fields ─────────────────────────────────────

    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// The action performed (e.g., "Create", "Update", "Delete").
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// The type of entity being operated on (e.g., "Order", "Customer").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// The specific entity identifier, if applicable.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// The outcome of the operation as an integer (maps to <see cref="AuditOutcome"/>).
    /// </summary>
    /// <remarks>
    /// Stored as <c>int</c> to avoid serialization issues with enums across versions.
    /// Cast to <see cref="AuditOutcome"/> when reading: <c>(AuditOutcome)Outcome</c>.
    /// </remarks>
    public required int Outcome { get; init; }

    /// <summary>
    /// Error message when the outcome is not Success.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// UTC timestamp when the operation was executed.
    /// </summary>
    public required DateTime TimestampUtc { get; init; }

    /// <summary>
    /// UTC timestamp when the operation started execution.
    /// </summary>
    public required DateTimeOffset StartedAtUtc { get; init; }

    /// <summary>
    /// UTC timestamp when the operation completed execution.
    /// </summary>
    public required DateTimeOffset CompletedAtUtc { get; init; }

    /// <summary>
    /// SHA-256 hash of the sanitized request payload (tamper detection).
    /// </summary>
    public string? RequestPayloadHash { get; init; }

    /// <summary>
    /// Tenant ID for multi-tenant applications. Stored in plaintext to enable tenant-scoped queries.
    /// </summary>
    public string? TenantId { get; init; }

    // ── Encrypted PII fields ────────────────────────────────────────────

    /// <summary>
    /// Encrypted user ID who initiated the operation.
    /// </summary>
    public EncryptedField? EncryptedUserId { get; init; }

    /// <summary>
    /// Encrypted IP address of the client.
    /// </summary>
    public EncryptedField? EncryptedIpAddress { get; init; }

    /// <summary>
    /// Encrypted User-Agent header from the HTTP request.
    /// </summary>
    public EncryptedField? EncryptedUserAgent { get; init; }

    /// <summary>
    /// Encrypted full request payload (after sensitive data redaction).
    /// </summary>
    public EncryptedField? EncryptedRequestPayload { get; init; }

    /// <summary>
    /// Encrypted full response payload (after sensitive data redaction).
    /// </summary>
    public EncryptedField? EncryptedResponsePayload { get; init; }

    /// <summary>
    /// Encrypted metadata dictionary serialized as JSON.
    /// </summary>
    public EncryptedField? EncryptedMetadata { get; init; }

    // ── Key tracking ────────────────────────────────────────────────────

    /// <summary>
    /// The temporal key period used to encrypt PII fields (e.g., <c>"2026-03"</c>).
    /// </summary>
    /// <remarks>
    /// Stored in plaintext to enable efficient key lookup during decryption in projections
    /// and to identify which entries are affected when a temporal key is destroyed.
    /// </remarks>
    public required string TemporalKeyPeriod { get; init; }
}
