namespace Encina.Security.AntiTampering;

/// <summary>
/// Supported HMAC algorithms for request signing.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SHA256"/> is the recommended default for most applications. It provides
/// a strong balance of security and performance for request integrity verification.
/// </para>
/// <para>
/// <see cref="SHA384"/> and <see cref="SHA512"/> are available for scenarios requiring
/// higher security margins, such as financial APIs or government integrations.
/// </para>
/// </remarks>
public enum HMACAlgorithm
{
    /// <summary>
    /// HMAC with SHA-256 hash function.
    /// </summary>
    /// <remarks>
    /// 256-bit digest. Recommended for most applications.
    /// Provides strong integrity guarantees with good performance.
    /// </remarks>
    SHA256 = 0,

    /// <summary>
    /// HMAC with SHA-384 hash function.
    /// </summary>
    /// <remarks>
    /// 384-bit digest. Use when compliance requires SHA-384 or higher.
    /// </remarks>
    SHA384 = 1,

    /// <summary>
    /// HMAC with SHA-512 hash function.
    /// </summary>
    /// <remarks>
    /// 512-bit digest. Use for maximum security margin in high-sensitivity scenarios.
    /// </remarks>
    SHA512 = 2
}
