using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Encina.Compliance.Consent;

/// <summary>
/// Derives the deterministic event stream identifier of a consent from its natural key
/// (tenant, data subject, purpose), so that at most one consent stream exists per key (#3769).
/// </summary>
/// <remarks>
/// <para>
/// The identifier is an RFC 9562 version 8 UUID built from the first 16 bytes of the SHA-256 hash
/// of a fixed namespace GUID followed by every key part, each prefixed with its UTF-8 byte length
/// as a 32-bit big-endian integer. The length prefixes make the encoding injective, so parts can
/// never collide through a separator (<c>("ab", "c")</c> and <c>("a", "bc")</c> yield different
/// identifiers).
/// </para>
/// <para>
/// Normalization: a <c>null</c> or empty tenant is the literal <c>"-"</c> (the same "no tenant"
/// value the consent cache keys and tenant comparisons use); the data subject identifier and the
/// purpose are hashed exactly as given (ordinal, no trimming or case folding), matching the
/// ordinal equality of the read-side lookups.
/// </para>
/// <para>
/// The algorithm lives in <see cref="Derive"/>, which takes the namespace GUID and the key parts,
/// so other consent aggregates keyed the same way (the exemption stream id of #811, a key that
/// later gains the channel of #1199) reuse it under their own namespace instead of duplicating it.
/// The namespace and the encoding are part of the persisted data: changing either changes the
/// stream id of every existing consent.
/// </para>
/// </remarks>
internal static class ConsentStreamId
{
    /// <summary>
    /// The fixed namespace of consent stream identifiers. Never change it: every stored consent
    /// stream id is derived from it.
    /// </summary>
    internal static readonly Guid ConsentNamespace = new("a0555850-c4bf-473e-956c-53e21bc86432");

    /// <summary>
    /// The key part that stands for "no tenant" (a <c>null</c> or empty tenant identifier).
    /// </summary>
    internal const string NoTenant = "-";

    /// <summary>
    /// Returns the consent stream identifier for a (tenant, data subject, purpose) key.
    /// </summary>
    /// <param name="tenantId">The effective tenant, or <c>null</c>/empty when there is no tenant.</param>
    /// <param name="dataSubjectId">The data subject identifier, compared ordinally.</param>
    /// <param name="purpose">The processing purpose, compared ordinally.</param>
    /// <returns>The deterministic version 8 UUID of the consent stream.</returns>
    internal static Guid For(string? tenantId, string dataSubjectId, string purpose) =>
        Derive(ConsentNamespace, NormalizeTenant(tenantId), dataSubjectId, purpose);

    /// <summary>
    /// Normalizes a tenant identifier to a key part: <see cref="NoTenant"/> for <c>null</c> or empty.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The tenant key part.</returns>
    internal static string NormalizeTenant(string? tenantId) =>
        string.IsNullOrEmpty(tenantId) ? NoTenant : tenantId;

    /// <summary>
    /// Derives an RFC 9562 version 8 UUID from SHA-256 of the namespace followed by the
    /// length-prefixed UTF-8 encoding of each part.
    /// </summary>
    /// <param name="namespaceId">The namespace that separates one kind of key from another.</param>
    /// <param name="parts">The key parts, in order; none may be <c>null</c>.</param>
    /// <returns>The deterministic identifier.</returns>
    /// <exception cref="ArgumentNullException">A part is <c>null</c>.</exception>
    internal static Guid Derive(Guid namespaceId, params ReadOnlySpan<string> parts)
    {
        const int NamespaceLength = 16;

        var length = NamespaceLength;
        foreach (var part in parts)
        {
            ArgumentNullException.ThrowIfNull(part, nameof(parts));
            length += sizeof(int) + Encoding.UTF8.GetByteCount(part);
        }

        var buffer = new byte[length];
        namespaceId.TryWriteBytes(buffer, bigEndian: true, out _);

        var offset = NamespaceLength;
        foreach (var part in parts)
        {
            var written = Encoding.UTF8.GetBytes(part, buffer.AsSpan(offset + sizeof(int)));
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset, sizeof(int)), written);
            offset += sizeof(int) + written;
        }

        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(buffer, hash);

        // RFC 9562 section 5.8: version 8 in the high nibble of octet 6, variant 0b10 in octet 8.
        hash[6] = (byte)((hash[6] & 0x0F) | 0x80);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

        return new Guid(hash[..NamespaceLength], bigEndian: true);
    }
}
