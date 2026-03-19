using Encina.Security.Audit;

namespace Encina.Audit.Marten.Events;

/// <summary>
/// Event-sourced event representing a read audit trail entry being recorded.
/// </summary>
/// <remarks>
/// <para>
/// This event is appended to Marten event streams by <c>MartenReadAuditStore.LogReadAsync</c>.
/// PII-sensitive fields are stored as <see cref="EncryptedField"/> values encrypted with
/// temporal keys, while structural fields remain in plaintext for querying and compliance analysis.
/// </para>
/// <para>
/// <b>Plaintext fields</b> (always queryable, even after crypto-shredding):
/// <c>Id</c>, <c>EntityType</c>, <c>EntityId</c>, <c>AccessedAtUtc</c>,
/// <c>AccessMethod</c>, <c>EntityCount</c>, <c>CorrelationId</c>, <c>TenantId</c>.
/// </para>
/// <para>
/// <b>Encrypted fields</b> (shredded when temporal keys are destroyed):
/// <c>EncryptedUserId</c>, <c>EncryptedPurpose</c>, <c>EncryptedMetadata</c>.
/// </para>
/// </remarks>
public sealed record ReadAuditEntryRecordedEvent
{
    // ── Plaintext structural fields ─────────────────────────────────────

    /// <summary>
    /// Unique identifier for this read audit entry.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The type of entity that was accessed (e.g., "Patient", "FinancialRecord").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// The specific entity identifier that was accessed.
    /// </summary>
    public required string? EntityId { get; init; }

    /// <summary>
    /// UTC timestamp when the data was accessed.
    /// </summary>
    public required DateTimeOffset AccessedAtUtc { get; init; }

    /// <summary>
    /// How the data was accessed, as an integer (maps to <see cref="ReadAccessMethod"/>).
    /// </summary>
    /// <remarks>
    /// Stored as <c>int</c> to avoid serialization issues with enums across versions.
    /// Cast to <see cref="ReadAccessMethod"/> when reading: <c>(ReadAccessMethod)AccessMethod</c>.
    /// </remarks>
    public required int AccessMethod { get; init; }

    /// <summary>
    /// Number of entities returned by the read operation.
    /// </summary>
    public required int EntityCount { get; init; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Tenant ID for multi-tenant applications. Stored in plaintext to enable tenant-scoped queries.
    /// </summary>
    public string? TenantId { get; init; }

    // ── Encrypted PII fields ────────────────────────────────────────────

    /// <summary>
    /// Encrypted user ID who accessed the data.
    /// </summary>
    public EncryptedField? EncryptedUserId { get; init; }

    /// <summary>
    /// Encrypted declared purpose for accessing this data (GDPR Art. 15 compliance).
    /// </summary>
    public EncryptedField? EncryptedPurpose { get; init; }

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
