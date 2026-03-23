using System.Security.Cryptography;
using System.Text;

namespace Encina.Compliance.Attestation;

/// <summary>
/// Computes deterministic content hashes for audit records.
/// Supports SHA-256, SHA-384, and SHA-512.
/// </summary>
internal static class ContentHasher
{
    /// <summary>
    /// Computes a hex-encoded SHA-256 hash of the given content.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    internal static string ComputeSha256(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }

    /// <summary>
    /// Computes a hex-encoded hash of the given content using the specified algorithm.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="algorithm"/> is not supported.</exception>
    internal static string ComputeHash(string content, HashAlgorithmName algorithm)
    {
        ArgumentNullException.ThrowIfNull(content);

        var input = Encoding.UTF8.GetBytes(content);
        byte[] hash;

        if (algorithm == HashAlgorithmName.SHA256)
            hash = SHA256.HashData(input);
        else if (algorithm == HashAlgorithmName.SHA384)
            hash = SHA384.HashData(input);
        else if (algorithm == HashAlgorithmName.SHA512)
            hash = SHA512.HashData(input);
        else
            throw new ArgumentException(
                $"Hash algorithm '{algorithm.Name}' is not supported. Use SHA256, SHA384, or SHA512.",
                nameof(algorithm));

        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Creates an HMAC instance matching the specified hash algorithm.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="algorithm"/> is not supported.</exception>
    internal static HMAC CreateHmac(byte[] key, HashAlgorithmName algorithm)
    {
        if (algorithm == HashAlgorithmName.SHA256)
            return new HMACSHA256(key);
        if (algorithm == HashAlgorithmName.SHA384)
            return new HMACSHA384(key);
        if (algorithm == HashAlgorithmName.SHA512)
            return new HMACSHA512(key);

        throw new ArgumentException(
            $"Hash algorithm '{algorithm.Name}' is not supported. Use SHA256, SHA384, or SHA512.",
            nameof(algorithm));
    }

    /// <summary>
    /// Returns the recommended HMAC key size in bytes for the specified algorithm.
    /// </summary>
    internal static int GetRecommendedKeySize(HashAlgorithmName algorithm)
    {
        if (algorithm == HashAlgorithmName.SHA256) return 32;
        if (algorithm == HashAlgorithmName.SHA384) return 48;
        if (algorithm == HashAlgorithmName.SHA512) return 64;
        return 32; // fallback
    }
}
