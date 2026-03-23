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
    /// Gets or sets the hash algorithm to use. Default is SHA-256.
    /// </summary>
    /// <remarks>
    /// Reserved for future use. The current implementation always uses SHA-256
    /// via <see cref="ContentHasher.ComputeSha256"/>. Setting this property
    /// has no effect on the actual hashing behavior yet.
    /// </remarks>
    public HashAlgorithmName HashAlgorithm { get; set; } = HashAlgorithmName.SHA256;

    /// <summary>
    /// Gets or sets the HMAC-SHA256 signing key for chain entries.
    /// When null (default), a cryptographically random 32-byte key is generated at startup.
    /// </summary>
    /// <remarks>
    /// Provide a persistent key to enable chain verification across process restarts.
    /// The key must be kept secret; exposure allows an attacker to forge chain entries.
    /// Keys shorter than 32 bytes are padded internally by HMACSHA256.
    /// </remarks>
    public byte[]? HmacKey { get; set; }
}
