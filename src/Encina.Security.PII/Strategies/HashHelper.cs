using System.Security.Cryptography;
using System.Text;

namespace Encina.Security.PII.Strategies;

/// <summary>
/// Utility for computing deterministic hashes of PII values.
/// </summary>
internal static class HashHelper
{
    /// <summary>
    /// Computes the keyed hash of the value: HMAC-SHA256 when a key is supplied,
    /// plain SHA-256 otherwise.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <param name="key">
    /// The key material (UTF-8). When <c>null</c> the value is hashed with an unkeyed
    /// SHA-256, which is only reachable through <see cref="PIIOptions.AllowUnkeyedHash"/>.
    /// An empty key is a (weak) key, never "no key"; <see cref="PIIOptionsValidator"/> rejects it.
    /// </param>
    /// <returns>A lowercase hex-encoded hash string (64 characters).</returns>
    internal static string ComputeHash(string value, string? key)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);

        var hash = key is null
            ? SHA256.HashData(valueBytes)
            : HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), valueBytes);

        return Convert.ToHexStringLower(hash);
    }
}
