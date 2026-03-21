using System.Security.Cryptography;

namespace Encina.Compliance.Attestation;

/// <summary>
/// Configuration for the <see cref="Providers.HashChainAttestationProvider"/>.
/// </summary>
public sealed class HashChainOptions
{
    /// <summary>
    /// Gets or sets the storage path for persisting the hash chain (optional).
    /// When null, the chain is kept in-memory only.
    /// </summary>
    public string? StoragePath { get; set; }

    /// <summary>
    /// Gets or sets the hash algorithm to use. Default is SHA-256.
    /// </summary>
    public HashAlgorithmName HashAlgorithm { get; set; } = HashAlgorithmName.SHA256;
}
