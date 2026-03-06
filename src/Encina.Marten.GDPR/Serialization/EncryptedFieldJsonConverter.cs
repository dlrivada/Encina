using System.Collections.Immutable;
using System.Text.Json;

using Encina.Security.Encryption;

namespace Encina.Marten.GDPR;

/// <summary>
/// Converts between <see cref="EncryptedValue"/> and a compact JSON string representation
/// used to store encrypted PII fields within Marten event data.
/// </summary>
/// <remarks>
/// <para>
/// The compact JSON format is:
/// <code>
/// {"__enc":true,"v":1,"kid":"subject:user123:v1","ct":"base64","n":"base64","t":"base64","alg":0}
/// </code>
/// </para>
/// <para>
/// The <c>__enc</c> marker enables fast detection of encrypted fields during deserialization
/// without attempting JSON parsing on every string property. The <c>v</c> field is a format
/// version number (currently <c>1</c>) for future-proofing the envelope structure.
/// </para>
/// <para>
/// This class is internal and used exclusively by <see cref="CryptoShredderSerializer"/>
/// for serializing and deserializing encrypted PII within event JSON.
/// </para>
/// </remarks>
internal static class EncryptedFieldJsonConverter
{
    private const string EncryptedMarkerPrefix = "{\"__enc\":true";
    private const int FormatVersion = 1;

    /// <summary>
    /// Serializes an <see cref="EncryptedValue"/> to its compact JSON string representation.
    /// </summary>
    /// <param name="encryptedValue">The encrypted value containing ciphertext and cryptographic metadata.</param>
    /// <returns>A compact JSON string with the encrypted envelope format.</returns>
    internal static string Serialize(EncryptedValue encryptedValue)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteBoolean("__enc", true);
        writer.WriteNumber("v", FormatVersion);
        writer.WriteString("kid", encryptedValue.KeyId);
        writer.WriteBase64String("ct", encryptedValue.Ciphertext.AsSpan());
        writer.WriteBase64String("n", encryptedValue.Nonce.AsSpan());
        writer.WriteBase64String("t", encryptedValue.Tag.AsSpan());
        writer.WriteNumber("alg", (int)encryptedValue.Algorithm);
        writer.WriteEndObject();

        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Attempts to parse a compact JSON string into an <see cref="EncryptedValue"/>.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>
    /// The parsed <see cref="EncryptedValue"/> if the string is a valid encrypted field envelope;
    /// otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// Returns <c>null</c> for any input that is not a valid encrypted field envelope,
    /// including malformed JSON, missing required fields, or strings that do not start
    /// with the <c>{"__enc":true</c> marker.
    /// </remarks>
    internal static EncryptedValue? TryParse(string? json)
    {
        if (!IsEncryptedField(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json!);
            var root = document.RootElement;

            if (!root.TryGetProperty("kid", out var kidElement) ||
                kidElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            if (!root.TryGetProperty("ct", out var ctElement))
            {
                return null;
            }

            if (!root.TryGetProperty("n", out var nonceElement))
            {
                return null;
            }

            var keyId = kidElement.GetString()!;
            var ciphertext = ctElement.GetBytesFromBase64();
            var nonce = nonceElement.GetBytesFromBase64();

            byte[] tag = [];
            if (root.TryGetProperty("t", out var tagElement))
            {
                tag = tagElement.GetBytesFromBase64();
            }

            var algorithm = EncryptionAlgorithm.Aes256Gcm;
            if (root.TryGetProperty("alg", out var algElement) &&
                algElement.TryGetInt32(out var algValue))
            {
                algorithm = (EncryptionAlgorithm)algValue;
            }

            return new EncryptedValue
            {
                KeyId = keyId,
                Ciphertext = [.. ciphertext],
                Nonce = [.. nonce],
                Tag = [.. tag],
                Algorithm = algorithm
            };
        }
        catch (JsonException)
        {
            return null;
        }
        catch (FormatException)
        {
            // Base64 decoding failure
            return null;
        }
    }

    /// <summary>
    /// Checks whether a string value represents an encrypted field envelope.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <returns>
    /// <c>true</c> if the value starts with the <c>{"__enc":true</c> marker;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This is a fast-path check using <see cref="string.StartsWith(string, StringComparison)"/>
    /// to avoid JSON parsing overhead for non-encrypted string properties.
    /// </remarks>
    internal static bool IsEncryptedField(string? value)
    {
        return value is not null &&
               value.StartsWith(EncryptedMarkerPrefix, StringComparison.Ordinal);
    }
}
