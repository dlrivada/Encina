using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Encina.Marten.GDPR;

/// <summary>
/// AES-256-GCM encryption of one <c>[CryptoShredded]</c> value into a <see cref="CryptoShreddingToken"/>, with
/// associated data <c>encina:cs2:{subjectId}:{version}</c> that binds the ciphertext to its subject and key version.
/// </summary>
internal static class CryptoShreddingFieldCipher
{
    /// <summary>The only accepted key size: <see cref="AesGcm"/> would silently accept 16- and 24-byte keys.</summary>
    internal const int KeySize = 32;

    /// <summary>
    /// Builds an <see cref="AesGcm"/> after checking the key material; <see cref="AesGcm"/> copies the key, so the
    /// caller's array is never kept.
    /// </summary>
    /// <param name="keyMaterial">The key material returned by the key provider.</param>
    /// <returns>The cipher, or <c>null</c> when the key is not exactly 32 bytes.</returns>
    internal static AesGcm? CreateAes(byte[]? keyMaterial) =>
        keyMaterial is { Length: KeySize } ? new AesGcm(keyMaterial, CryptoShreddingToken.TagSize) : null;

    /// <summary>Encrypts a value with a fresh random nonce.</summary>
    internal static string Encrypt(AesGcm aes, string subjectId, int version, string plaintext)
    {
        Span<byte> nonce = stackalloc byte[CryptoShreddingToken.NonceSize];
        RandomNumberGenerator.Fill(nonce);
        return Encrypt(aes, subjectId, version, plaintext, nonce);
    }

    /// <summary>Encrypts a value with the given nonce (tests use it for determinism).</summary>
    internal static string Encrypt(AesGcm aes, string subjectId, int version, string plaintext, ReadOnlySpan<byte> nonce)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        Span<byte> tag = stackalloc byte[CryptoShreddingToken.TagSize];
        try
        {
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, AssociatedData(subjectId, version));
            return CryptoShreddingToken.Format(version, nonce, ciphertext, tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    /// <summary>
    /// Decrypts a token.
    /// </summary>
    /// <exception cref="AuthenticationTagMismatchException">The tag or the associated data does not match.</exception>
    internal static string Decrypt(AesGcm aes, string subjectId, in CryptoShreddingToken token)
    {
        var plaintext = new byte[token.Ciphertext.Length];
        try
        {
            aes.Decrypt(token.Nonce, token.Ciphertext, token.Tag, plaintext, AssociatedData(subjectId, token.Version));
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    internal static byte[] AssociatedData(string subjectId, int version) =>
        Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"encina:cs2:{subjectId}:{version}"));
}
