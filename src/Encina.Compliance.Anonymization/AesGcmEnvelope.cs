using System.Security.Cryptography;
using System.Text;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// AES-256-GCM helper shared by <see cref="DefaultTokenizer"/> and <see cref="DefaultPseudonymizer"/>.
/// Packs and unpacks <c>[nonce (12 bytes)][tag (16 bytes)][ciphertext]</c>.
/// </summary>
/// <remarks>
/// Callers MUST check <see cref="IsValidKey"/> before <see cref="Encrypt"/> or
/// <see cref="TryDecrypt"/>, so a wrong-length key never reaches <see cref="AesGcm"/>.
/// </remarks>
internal static class AesGcmEnvelope
{
    internal const int KeySizeBytes = 32;
    internal const int NonceSizeBytes = 12;
    internal const int TagSizeBytes = 16;

    /// <summary>Returns <c>true</c> when the key is exactly 32 bytes (AES-256).</summary>
    internal static bool IsValidKey(byte[] key) => key.Length == KeySizeBytes;

    /// <summary>Encrypts a plaintext string with a key already checked by <see cref="IsValidKey"/>.</summary>
    internal static byte[] Encrypt(string plaintext, byte[] key)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var result = new byte[NonceSizeBytes + TagSizeBytes + plaintextBytes.Length];

        var nonce = result.AsSpan(0, NonceSizeBytes);
        var tag = result.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = result.AsSpan(NonceSizeBytes + TagSizeBytes);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return result;
    }

    /// <summary>
    /// Decrypts a packed blob with a key already checked by <see cref="IsValidKey"/>.
    /// Returns <c>false</c> for a blob too short for nonce and tag and for any authentication
    /// failure, without exposing the cause.
    /// </summary>
    internal static bool TryDecrypt(byte[] packed, byte[] key, out string plaintext)
    {
        plaintext = string.Empty;

        if (packed.Length < NonceSizeBytes + TagSizeBytes)
        {
            return false;
        }

        var nonce = packed.AsSpan(0, NonceSizeBytes);
        var tag = packed.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = packed.AsSpan(NonceSizeBytes + TagSizeBytes);
        var buffer = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Decrypt(nonce, ciphertext, tag, buffer);
        }
        catch (CryptographicException)
        {
            return false;
        }

        plaintext = Encoding.UTF8.GetString(buffer);
        return true;
    }

    /// <summary>Decodes Base64 without throwing; <c>false</c> for any non-Base64 text.</summary>
    internal static bool TryDecodeBase64(string value, out byte[] bytes)
    {
        var buffer = new byte[(value.Length / 4 * 3) + 3];
        if (Convert.TryFromBase64String(value, buffer, out var written))
        {
            bytes = buffer.AsSpan(0, written).ToArray();
            return true;
        }

        bytes = [];
        return false;
    }
}
