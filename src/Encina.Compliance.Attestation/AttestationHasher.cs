using System.Security.Cryptography;
using System.Text;

using Encina.Compliance.Attestation.Model;

namespace Encina.Compliance.Attestation;

/// <summary>
/// Provides the canonical content hashing algorithm for audit record attestation.
/// Third-party providers implementing <see cref="Abstractions.IAuditAttestationProvider"/>
/// should use this class to ensure hash compatibility with the built-in providers.
/// </summary>
public static class AttestationHasher
{
    /// <summary>
    /// Computes the canonical SHA-256 content hash for an audit record.
    /// The hash is computed over the record's <see cref="AuditRecord.SerializedContent"/>.
    /// </summary>
    /// <param name="record">The audit record to hash.</param>
    /// <returns>A lowercase hex-encoded SHA-256 hash string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is null.</exception>
    public static string ComputeContentHash(AuditRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return ComputeSha256(record.SerializedContent);
    }

    /// <summary>
    /// Computes a lowercase hex-encoded SHA-256 hash of the given content string.
    /// </summary>
    /// <param name="content">The content to hash.</param>
    /// <returns>A lowercase hex-encoded SHA-256 hash string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    public static string ComputeSha256(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }
}
