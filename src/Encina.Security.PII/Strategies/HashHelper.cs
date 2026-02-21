using System.Security.Cryptography;
using System.Text;

namespace Encina.Security.PII.Strategies;

/// <summary>
/// Utility for computing deterministic hashes of PII values.
/// </summary>
internal static class HashHelper
{
    /// <summary>
    /// Computes a SHA-256 hash of the value with an optional salt.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <param name="salt">Optional salt for additional security.</param>
    /// <returns>A lowercase hex-encoded hash string.</returns>
    internal static string ComputeHash(string value, string? salt)
    {
        var input = salt is not null
            ? salt + value
            : value;

        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
