namespace Encina.Audit.Marten.Crypto;

/// <summary>
/// Marten document entity for persisting temporal encryption key material in PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// Each document represents a single versioned encryption key for a time period. The
/// <see cref="Id"/> follows the convention <c>"temporal:{period}:v{version}"</c>,
/// which maps directly to the key identifier stored in encrypted audit entry fields.
/// </para>
/// <para>
/// Documents are managed by <c>MartenTemporalKeyProvider</c> through Marten's
/// <c>IDocumentSession</c>. When temporal keys are destroyed via crypto-shredding,
/// key documents are hard-deleted from PostgreSQL and a <see cref="TemporalKeyDestroyedMarker"/>
/// is stored to track the destruction.
/// </para>
/// <para>
/// A computed index on <see cref="Period"/> ensures efficient lookups when querying
/// all key versions for a given time period.
/// </para>
/// </remarks>
public sealed class TemporalKeyDocument
{
    /// <summary>
    /// Unique key identifier following the convention <c>"temporal:{period}:v{version}"</c>.
    /// </summary>
    /// <remarks>
    /// Serves as the Marten document identity and matches the key identifier stored
    /// in encrypted audit entry PII fields for version-specific decryption.
    /// </remarks>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The time period this key covers (e.g., <c>"2026-03"</c>, <c>"2026-Q1"</c>, <c>"2026"</c>).
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// AES-256 key material (32 bytes) used for field-level encryption/decryption.
    /// </summary>
    /// <remarks>
    /// Key material is generated via <see cref="System.Security.Cryptography.RandomNumberGenerator"/>
    /// and must NEVER be logged or exposed in diagnostics.
    /// </remarks>
    public byte[] KeyMaterial { get; set; } = [];

    /// <summary>
    /// Version number of this key (monotonically increasing, starting at 1).
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Current lifecycle status of this key version.
    /// </summary>
    public TemporalKeyStatus Status { get; set; }

    /// <summary>
    /// Timestamp when this key version was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
