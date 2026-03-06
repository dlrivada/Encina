using System.Collections.Immutable;
using Encina.Messaging.Encryption.Model;

namespace Encina.Messaging.Encryption;

/// <summary>
/// Static helper for parsing and formatting the encrypted payload string representation.
/// </summary>
/// <remarks>
/// <para>
/// The encrypted payload format is:
/// <c>ENC:v{version}:{keyId}:{algorithm}:{base64Nonce}:{base64Tag}:{base64Ciphertext}</c>
/// </para>
/// <para>
/// Example: <c>ENC:v1:msg-key-2024:AES-256-GCM:dGVzdG5vbmNl:dGVzdHRhZw==:Y2lwaGVydGV4dA==</c>
/// </para>
/// <para>
/// The version field (<c>v1</c>) enables forward-compatible format changes. New versions
/// can be added without breaking decryption of existing payloads.
/// </para>
/// <para>
/// This class is thread-safe and all methods are pure functions with no shared mutable state.
/// </para>
/// </remarks>
public static class EncryptedPayloadFormatter
{
    /// <summary>
    /// The prefix that identifies an encrypted payload string.
    /// </summary>
    public const string EncryptedPrefix = "ENC:";

    /// <summary>
    /// The version 1 prefix for encrypted payload strings.
    /// </summary>
    internal const string V1Prefix = "ENC:v1:";

    private const int V1PartCount = 7;
    private const int CurrentVersion = 1;

    /// <summary>
    /// Formats an <see cref="EncryptedPayload"/> into its string representation for storage.
    /// </summary>
    /// <param name="payload">The encrypted payload to format.</param>
    /// <returns>
    /// A string in the format <c>ENC:v1:{keyId}:{algorithm}:{base64Nonce}:{base64Tag}:{base64Ciphertext}</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <c>null</c>.</exception>
    public static string Format(EncryptedPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var nonce = Convert.ToBase64String(payload.Nonce.AsSpan());
        var tag = Convert.ToBase64String(payload.Tag.AsSpan());
        var ciphertext = Convert.ToBase64String(payload.Ciphertext.AsSpan());

        return $"ENC:v{payload.Version}:{payload.KeyId}:{payload.Algorithm}:{nonce}:{tag}:{ciphertext}";
    }

    /// <summary>
    /// Attempts to parse an encrypted payload string into an <see cref="EncryptedPayload"/> instance.
    /// </summary>
    /// <param name="content">The string content to parse.</param>
    /// <param name="payload">
    /// When this method returns <c>true</c>, contains the parsed <see cref="EncryptedPayload"/>;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="content"/> is a valid encrypted payload string;
    /// <c>false</c> if it is <c>null</c>, empty, malformed, or uses an unsupported version.
    /// </returns>
    /// <remarks>
    /// This method is fault-tolerant and never throws exceptions. Malformed Base64 segments,
    /// missing parts, or unknown versions all result in a <c>false</c> return.
    /// </remarks>
    public static bool TryParse(string? content, out EncryptedPayload? payload)
    {
        payload = null;

        if (string.IsNullOrEmpty(content) || !content.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        // Currently only v1 is supported
        if (!content.StartsWith(V1Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return TryParseV1(content, out payload);
    }

    /// <summary>
    /// Determines whether the specified content is an encrypted payload string.
    /// </summary>
    /// <param name="content">The string content to check.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="content"/> starts with the encrypted payload prefix;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsEncrypted(string? content)
    {
        return !string.IsNullOrEmpty(content) && content.StartsWith(EncryptedPrefix, StringComparison.Ordinal);
    }

    private static bool TryParseV1(string content, out EncryptedPayload? payload)
    {
        payload = null;

        var parts = content.Split(':');

        // Expected: ENC, v1, keyId, algorithm, nonce, tag, ciphertext
        if (parts.Length != V1PartCount)
        {
            return false;
        }

        var keyId = parts[2];
        var algorithm = parts[3];

        if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(algorithm))
        {
            return false;
        }

        try
        {
            var nonce = Convert.FromBase64String(parts[4]);
            var tag = Convert.FromBase64String(parts[5]);
            var ciphertext = Convert.FromBase64String(parts[6]);

            payload = new EncryptedPayload
            {
                KeyId = keyId,
                Algorithm = algorithm,
                Nonce = [.. nonce],
                Tag = [.. tag],
                Ciphertext = [.. ciphertext],
                Version = CurrentVersion
            };

            return true;
        }
        catch (FormatException)
        {
            // Invalid Base64 — malformed payload
            return false;
        }
    }
}
