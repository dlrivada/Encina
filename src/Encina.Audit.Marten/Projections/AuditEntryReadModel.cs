using Encina.Security.Audit;

namespace Encina.Audit.Marten.Projections;

/// <summary>
/// Marten document representing a projected, query-optimized view of an audit trail entry.
/// </summary>
/// <remarks>
/// <para>
/// This read model is populated by the <see cref="AuditEntryProjection"/> from
/// <see cref="Events.AuditEntryRecordedEvent"/> events. PII fields are decrypted during
/// projection processing when temporal keys are available.
/// </para>
/// <para>
/// When temporal keys have been destroyed via crypto-shredding, PII fields contain the
/// <see cref="MartenAuditOptions.ShreddedPlaceholder"/> value (default: <c>"[SHREDDED]"</c>)
/// and <see cref="IsShredded"/> is set to <c>true</c>.
/// </para>
/// <para>
/// All fields from <see cref="AuditEntry"/> are present, plus <see cref="IsShredded"/>
/// and <see cref="TemporalKeyPeriod"/> for crypto-shredding tracking.
/// </para>
/// </remarks>
public sealed class AuditEntryReadModel
{
    // ── Identity ────────────────────────────────────────────────────────

    /// <summary>
    /// Unique identifier for this audit entry (Marten document identity).
    /// </summary>
    public Guid Id { get; set; }

    // ── Structural fields (always queryable) ────────────────────────────

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// The action performed (e.g., "Create", "Update", "Delete").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The type of entity being operated on.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The specific entity identifier, if applicable.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// The outcome of the operation.
    /// </summary>
    public AuditOutcome Outcome { get; set; }

    /// <summary>
    /// Error message when the outcome is not Success.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// UTC timestamp when the operation was executed.
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the operation started execution.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the operation completed execution.
    /// </summary>
    public DateTimeOffset CompletedAtUtc { get; set; }

    /// <summary>
    /// SHA-256 hash of the sanitized request payload (tamper detection).
    /// </summary>
    public string? RequestPayloadHash { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenant applications.
    /// </summary>
    public string? TenantId { get; set; }

    // ── PII fields (decrypted or shredded) ──────────────────────────────

    /// <summary>
    /// User ID who initiated the operation. Contains <c>[SHREDDED]</c> after crypto-shredding.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// IP address of the client. Contains <c>[SHREDDED]</c> after crypto-shredding.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User-Agent header. Contains <c>[SHREDDED]</c> after crypto-shredding.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Full request payload (after sensitive data redaction). Contains <c>[SHREDDED]</c> after crypto-shredding.
    /// </summary>
    public string? RequestPayload { get; set; }

    /// <summary>
    /// Full response payload (after sensitive data redaction). Contains <c>[SHREDDED]</c> after crypto-shredding.
    /// </summary>
    public string? ResponsePayload { get; set; }

    /// <summary>
    /// Metadata dictionary serialized as JSON. Contains <c>[SHREDDED]</c> after crypto-shredding.
    /// </summary>
    public string? MetadataJson { get; set; }

    // ── Crypto-shredding tracking ───────────────────────────────────────

    /// <summary>
    /// Indicates whether this entry's PII fields have been crypto-shredded.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, PII fields (<see cref="UserId"/>, <see cref="IpAddress"/>,
    /// <see cref="UserAgent"/>, <see cref="RequestPayload"/>, <see cref="ResponsePayload"/>,
    /// <see cref="MetadataJson"/>) contain placeholder values instead of actual data.
    /// </remarks>
    public bool IsShredded { get; set; }

    /// <summary>
    /// The temporal key period used to encrypt this entry's PII fields (e.g., <c>"2026-03"</c>).
    /// </summary>
    public string TemporalKeyPeriod { get; set; } = string.Empty;
}
