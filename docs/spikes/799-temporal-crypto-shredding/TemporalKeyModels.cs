// ============================================================================
// DRAFT MODELS — Spike #799 output
// This file is a design artifact, NOT production code.
// Final implementation will live in src/Encina.Audit.Marten/
// ============================================================================

namespace Encina.Audit.Marten;

/// <summary>
/// Defines the time-based granularity for temporal encryption key partitioning.
/// </summary>
/// <remarks>
/// <para>
/// The granularity determines how audit entries are grouped for encryption.
/// All entries within the same period share one encryption key. When purging,
/// entire periods are shredded (key deleted) at once.
/// </para>
/// <para>
/// Choose based on compliance requirements:
/// </para>
/// <list type="table">
/// <listheader>
///   <term>Granularity</term>
///   <description>Typical Use</description>
/// </listheader>
/// <item>
///   <term><see cref="Monthly"/></term>
///   <description>GDPR, PCI-DSS — fine-grained, most precise purge boundaries</description>
/// </item>
/// <item>
///   <term><see cref="Quarterly"/></term>
///   <description>HIPAA — balanced operational overhead</description>
/// </item>
/// <item>
///   <term><see cref="Yearly"/></term>
///   <description>SOX — minimal key management, coarse purge boundaries</description>
/// </item>
/// </list>
/// </remarks>
public enum TemporalKeyGranularity
{
    /// <summary>
    /// One key per calendar month. Period format: <c>yyyy-MM</c> (e.g., <c>2026-03</c>).
    /// </summary>
    Monthly = 0,

    /// <summary>
    /// One key per calendar quarter. Period format: <c>yyyy-QN</c> (e.g., <c>2026-Q1</c>).
    /// </summary>
    Quarterly = 1,

    /// <summary>
    /// One key per calendar year. Period format: <c>yyyy</c> (e.g., <c>2026</c>).
    /// </summary>
    Yearly = 2
}

/// <summary>
/// Status of a temporal encryption key.
/// </summary>
public enum TemporalKeyStatus
{
    /// <summary>
    /// Key is active and used for encrypting new audit entries in the current period.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Period has ended but the key is retained for decryption (within retention window).
    /// </summary>
    Expired = 1,

    /// <summary>
    /// Key has been permanently deleted. Data encrypted with this key is unrecoverable.
    /// </summary>
    Deleted = 2
}

/// <summary>
/// Contains the encryption key material and metadata for a temporal period.
/// </summary>
/// <remarks>
/// Returned by <c>ITemporalKeyProvider.GetOrCreateKeyForTimestampAsync</c> and
/// <c>ITemporalKeyProvider.GetKeyByPeriodIdAsync</c>. The <see cref="PeriodId"/>
/// is stored alongside encrypted audit entry fields for later decryption lookup.
/// </remarks>
public sealed record TemporalKeyInfo
{
    /// <summary>
    /// Unique identifier for the period (e.g., <c>"audit-temporal:monthly:2026-03"</c>).
    /// Stored alongside encrypted data as the key identifier (<c>kid</c>).
    /// </summary>
    public required string PeriodId { get; init; }

    /// <summary>
    /// The 256-bit AES encryption key material.
    /// </summary>
    public required byte[] KeyMaterial { get; init; }

    /// <summary>
    /// The granularity of this key's time period.
    /// </summary>
    public required TemporalKeyGranularity Granularity { get; init; }

    /// <summary>
    /// Start of the time period (inclusive, UTC).
    /// </summary>
    public required DateTime PeriodStartUtc { get; init; }

    /// <summary>
    /// End of the time period (exclusive, UTC).
    /// </summary>
    public required DateTime PeriodEndUtc { get; init; }

    /// <summary>
    /// Current status of the key.
    /// </summary>
    public required TemporalKeyStatus Status { get; init; }
}

/// <summary>
/// Administrative information about a temporal key period (without key material).
/// </summary>
/// <remarks>
/// Returned by <c>ITemporalKeyProvider.ListPeriodsAsync</c>. Does not include
/// the actual key material for security reasons.
/// </remarks>
public sealed record TemporalPeriodInfo
{
    /// <summary>
    /// Unique identifier for the period (e.g., <c>"audit-temporal:monthly:2026-03"</c>).
    /// </summary>
    public required string PeriodId { get; init; }

    /// <summary>
    /// The granularity of this period.
    /// </summary>
    public required TemporalKeyGranularity Granularity { get; init; }

    /// <summary>
    /// Start of the time period (inclusive, UTC).
    /// </summary>
    public required DateTime PeriodStartUtc { get; init; }

    /// <summary>
    /// End of the time period (exclusive, UTC).
    /// </summary>
    public required DateTime PeriodEndUtc { get; init; }

    /// <summary>
    /// Current status of the key for this period.
    /// </summary>
    public required TemporalKeyStatus Status { get; init; }

    /// <summary>
    /// When the key was first created (UTC).
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// When the key was deleted (shredded), if applicable.
    /// </summary>
    public DateTime? ShreddedAtUtc { get; init; }

    /// <summary>
    /// Estimated number of audit entries encrypted with this period's key.
    /// Obtained from the audit projection's period summary.
    /// </summary>
    public int EstimatedEntryCount { get; init; }
}

/// <summary>
/// Contains the outcome of a temporal crypto-shredding operation (period-based purge).
/// </summary>
/// <remarks>
/// Returned by <c>ITemporalKeyProvider.ShredPeriodsOlderThanAsync</c>. Provides metrics
/// about the shredding operation for compliance reporting and operational monitoring.
/// </remarks>
public sealed record TemporalShreddingResult
{
    /// <summary>
    /// Number of temporal keys that were permanently deleted.
    /// </summary>
    public required int PeriodsShredded { get; init; }

    /// <summary>
    /// Estimated total number of audit entries affected (rendered unreadable).
    /// </summary>
    public required int EstimatedEntriesAffected { get; init; }

    /// <summary>
    /// The cutoff date used for the shredding operation.
    /// </summary>
    public required DateTime OlderThanUtc { get; init; }

    /// <summary>
    /// The period IDs that were shredded.
    /// </summary>
    public required IReadOnlyList<string> ShreddedPeriodIds { get; init; }

    /// <summary>
    /// Timestamp when the shredding operation was completed (UTC).
    /// </summary>
    public required DateTime ShreddedAtUtc { get; init; }
}

/// <summary>
/// Marten document for storing temporal encryption keys.
/// </summary>
/// <remarks>
/// Stored in the Marten document store's <c>temporal_keys</c> collection.
/// The <see cref="EncryptedKeyMaterial"/> is encrypted with a master key
/// (via <c>IMasterKeyProvider</c>) before storage.
/// </remarks>
public sealed record TemporalKeyDocument
{
    /// <summary>
    /// Period identifier, used as the document ID
    /// (e.g., <c>"audit-temporal:monthly:2026-03"</c>).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The granularity of this key's time period.
    /// </summary>
    public required TemporalKeyGranularity Granularity { get; init; }

    /// <summary>
    /// Start of the time period (inclusive, UTC).
    /// </summary>
    public required DateTime PeriodStartUtc { get; init; }

    /// <summary>
    /// End of the time period (exclusive, UTC).
    /// </summary>
    public required DateTime PeriodEndUtc { get; init; }

    /// <summary>
    /// The AES-256 key material, encrypted with the master key.
    /// <c>null</c> when the key has been deleted (shredded).
    /// </summary>
    public byte[]? EncryptedKeyMaterial { get; init; }

    /// <summary>
    /// Current status of the key.
    /// </summary>
    public required TemporalKeyStatus Status { get; init; }

    /// <summary>
    /// When the key was created (UTC).
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// When the key was deleted (shredded), if applicable.
    /// </summary>
    public DateTime? ShreddedAtUtc { get; init; }
}
