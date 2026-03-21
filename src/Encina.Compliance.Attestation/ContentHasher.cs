using System.Security.Cryptography;
using System.Text;

namespace Encina.Compliance.Attestation;

/// <summary>
/// Computes deterministic SHA-256 content hashes for audit records.
/// </summary>
internal static class ContentHasher
{
    /// <summary>
    /// Computes a hex-encoded SHA-256 hash of the given content.
    /// </summary>
    internal static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }
}
