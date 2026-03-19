using Encina.Security.Audit;

namespace Encina.Audit.Marten.Projections;

/// <summary>
/// Marten document representing a projected, query-optimized view of a read audit trail entry.
/// </summary>
/// <remarks>
/// <para>
/// This read model is populated by the <see cref="ReadAuditEntryProjection"/> from
/// <see cref="Events.ReadAuditEntryRecordedEvent"/> events. PII fields are decrypted during
/// projection processing when temporal keys are available.
/// </para>
/// <para>
/// When temporal keys have been destroyed via crypto-shredding, PII fields contain the
/// <see cref="MartenAuditOptions.ShreddedPlaceholder"/> value (default: <c>"[SHREDDED]"</c>)
/// and <see cref="IsShredded"/> is set to <c>true</c>.
/// </para>
/// </remarks>
public sealed class ReadAuditEntryReadModel
{
    // ── Identity ────────────────────────────────────────────────────────

    /// <summary>
    /// Unique identifier for this read audit entry (Marten document identity).
    /// </summary>
    public Guid Id { get; set; }

    // ── Structural fields (always queryable) ────────────────────────────

    /// <summary>
    /// The type of entity that was accessed.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The specific entity identifier that was accessed.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// UTC timestamp when the data was accessed.
    /// </summary>
    public DateTimeOffset AccessedAtUtc { get; set; }

    /// <summary>
    /// How the data was accessed.
    /// </summary>
    public ReadAccessMethod AccessMethod { get; set; }

    /// <summary>
    /// Number of entities returned by the read operation.
    /// </summary>
    public int EntityCount { get; set; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenant applications.
    /// </summary>
    public string? TenantId { get; set; }

    // ── PII fields (decrypted or shredded) ──────────────────────────────

    /// <summary>
    /// User ID who accessed the data. Contains <c>[SHREDDED]</c> after crypto-shredding.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Declared purpose for accessing this data (GDPR Art. 15). Contains <c>[SHREDDED]</c> after crypto-shredding.
    /// </summary>
    public string? Purpose { get; set; }

    /// <summary>
    /// Metadata dictionary serialized as JSON. Contains <c>[SHREDDED]</c> after crypto-shredding.
    /// </summary>
    public string? MetadataJson { get; set; }

    // ── Crypto-shredding tracking ───────────────────────────────────────

    /// <summary>
    /// Indicates whether this entry's PII fields have been crypto-shredded.
    /// </summary>
    public bool IsShredded { get; set; }

    /// <summary>
    /// The temporal key period used to encrypt this entry's PII fields (e.g., <c>"2026-03"</c>).
    /// </summary>
    public string TemporalKeyPeriod { get; set; } = string.Empty;
}
