using System.Security.Cryptography;

namespace Encina.Compliance.Attestation;

/// <summary>
/// Configuration for the <see cref="Providers.HashChainAttestationProvider"/>.
/// </summary>
public sealed class HashChainOptions
{
    /// <summary>
    /// Gets or sets the storage path for persisting the hash chain (optional).
    /// When null (default), the chain is kept in-memory only and lost on process restart.
    /// </summary>
    /// <remarks>
    /// Persistence is not yet implemented. This property is reserved for a future
    /// file-backed provider that will rehydrate the chain on startup.
    /// </remarks>
    public string? StoragePath { get; set; }

    /// <summary>
    /// Gets or sets the hash algorithm used for content hashing and HMAC chain signatures.
    /// Default is <see cref="HashAlgorithmName.SHA256"/>.
    /// </summary>
    /// <remarks>
    /// Supported algorithms: SHA-256, SHA-384, SHA-512.
    /// The same algorithm is used for both content hashes and HMAC chain signatures.
    /// When no <see cref="HmacKey"/> is provided, the auto-generated key size matches
    /// the algorithm (32 bytes for SHA-256, 48 for SHA-384, 64 for SHA-512).
    /// </remarks>
    public HashAlgorithmName HashAlgorithm { get; set; } = HashAlgorithmName.SHA256;

    /// <summary>
    /// Gets or sets the HMAC signing key for chain entries.
    /// When null (default), a cryptographically random key is generated at startup
    /// with a size matching the configured <see cref="HashAlgorithm"/>.
    /// </summary>
    /// <remarks>
    /// Provide a persistent key to enable chain verification across process restarts.
    /// The key must be kept secret; exposure allows an attacker to forge chain entries.
    /// </remarks>
    public byte[]? HmacKey { get; set; }
}
