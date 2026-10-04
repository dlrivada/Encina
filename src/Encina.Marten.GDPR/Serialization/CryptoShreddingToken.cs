using System.Buffers.Text;
using System.Globalization;

namespace Encina.Marten.GDPR;

/// <summary>
/// The v2 crypto-shredding token stored in place of a <c>[CryptoShredded]</c> value:
/// <c>cs2:{version}:{base64url(nonce ‖ ciphertext ‖ tag)}</c>, or the tombstone <c>cs2:erased</c>.
/// </summary>
/// <remarks>
/// <para>
/// The token carries no subject id: the subject comes from the owner's subject-id sibling and is bound to the
/// ciphertext through the AES-GCM associated data, so a token copied onto another subject's object fails the
/// integrity check. Parsing is strict; there is no prefix guessing.
/// </para>
/// </remarks>
internal readonly struct CryptoShreddingToken
{
    /// <summary>The tombstone written when the placeholder of a forgotten subject is saved again.</summary>
    internal const string Tombstone = "cs2:erased";

    /// <summary>The token prefix.</summary>
    internal const string Prefix = "cs2:";

    /// <summary>The AES-GCM nonce size in bytes.</summary>
    internal const int NonceSize = 12;

    /// <summary>The AES-GCM tag size in bytes.</summary>
    internal const int TagSize = 16;

    private CryptoShreddingToken(int version, byte[] payload)
    {
        Version = version;
        Payload = payload;
    }

    /// <summary>Gets the key version that encrypted the value.</summary>
    internal int Version { get; }

    /// <summary>Gets the decoded payload: nonce, ciphertext and tag.</summary>
    internal byte[] Payload { get; }

    /// <summary>Gets the nonce.</summary>
    internal ReadOnlySpan<byte> Nonce => Payload.AsSpan(0, NonceSize);

    /// <summary>Gets the ciphertext.</summary>
    internal ReadOnlySpan<byte> Ciphertext => Payload.AsSpan(NonceSize, Payload.Length - NonceSize - TagSize);

    /// <summary>Gets the authentication tag.</summary>
    internal ReadOnlySpan<byte> Tag => Payload.AsSpan(Payload.Length - TagSize, TagSize);

    /// <summary>
    /// Formats a token.
    /// </summary>
    internal static string Format(int version, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag)
    {
        var payload = new byte[nonce.Length + ciphertext.Length + tag.Length];
        nonce.CopyTo(payload);
        ciphertext.CopyTo(payload.AsSpan(nonce.Length));
        tag.CopyTo(payload.AsSpan(nonce.Length + ciphertext.Length));
        return string.Create(CultureInfo.InvariantCulture, $"{Prefix}{version}:{Base64Url.EncodeToString(payload)}");
    }

    /// <summary>
    /// Parses a v2 token. The tombstone is not a token (see <see cref="IsTombstone"/>).
    /// </summary>
    /// <param name="value">The stored value.</param>
    /// <param name="token">The parsed token.</param>
    /// <returns><c>true</c> when <paramref name="value"/> is a well-formed token.</returns>
    internal static bool TryParse(string value, out CryptoShreddingToken token)
    {
        token = default;
        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = value.AsSpan(Prefix.Length);
        var separator = rest.IndexOf(':');
        if (separator <= 0
            || !int.TryParse(rest[..separator], NumberStyles.None, CultureInfo.InvariantCulture, out var version)
            || version < 1)
        {
            return false;
        }

        return TryDecode(rest[(separator + 1)..], version, out token);
    }

    private static bool TryDecode(ReadOnlySpan<char> encoded, int version, out CryptoShreddingToken token)
    {
        token = default;
        if (!Base64Url.IsValid(encoded, out var length) || length < NonceSize + TagSize)
        {
            return false;
        }

        var payload = new byte[length];
        if (!Base64Url.TryDecodeFromChars(encoded, payload, out var written) || written != length)
        {
            return false;
        }

        token = new CryptoShreddingToken(version, payload);
        return true;
    }

    /// <summary>Gets whether the stored value is the tombstone.</summary>
    internal static bool IsTombstone(string value) => string.Equals(value, Tombstone, StringComparison.Ordinal);
}
