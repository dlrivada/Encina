namespace Encina.Audit.Marten.Crypto;

/// <summary>
/// Contains information about a temporal encryption key, including its material and lifecycle status.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="ITemporalKeyProvider"/> operations. The <see cref="KeyMaterial"/>
/// contains the raw AES-256 key bytes (32 bytes) used for encryption and decryption.
/// </para>
/// <para>
/// <b>Security:</b> Key material must never be logged, serialized to disk, or exposed
/// in error messages or diagnostics.
/// </para>
/// </remarks>
public sealed record TemporalKeyInfo
{
    /// <summary>
    /// The time period this key covers (e.g., <c>"2026-03"</c>, <c>"2026-Q1"</c>, <c>"2026"</c>).
    /// </summary>
    public required string Period { get; init; }

    /// <summary>
    /// AES-256 key material (32 bytes) used for field-level encryption/decryption.
    /// </summary>
    /// <remarks>
    /// Generated via <see cref="System.Security.Cryptography.RandomNumberGenerator"/> on first use.
    /// Must NEVER be logged or exposed in diagnostics.
    /// </remarks>
    public required byte[] KeyMaterial { get; init; }

    /// <summary>
    /// Version number of this key (monotonically increasing, starting at 1).
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Current lifecycle status of this key version.
    /// </summary>
    public required TemporalKeyStatus Status { get; init; }

    /// <summary>
    /// Timestamp when this key version was created (UTC).
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Timestamp when this key was destroyed via crypto-shredding (UTC), or <c>null</c> if still active.
    /// </summary>
    public DateTimeOffset? DestroyedAtUtc { get; init; }

    /// <summary>
    /// Formats the key identifier following the convention <c>"temporal:{period}:v{version}"</c>.
    /// </summary>
    public string KeyId => $"temporal:{Period}:v{Version}";
}
