using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Encina.Audit.Marten.Events;

/// <summary>
/// Represents an encrypted PII field value within an audit entry event, using AES-256-GCM
/// authenticated encryption with a temporal key.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EncryptedField"/> stores the encrypted ciphertext along with its cryptographic
/// metadata (nonce, authentication tag, key identifier) in a compact JSON envelope format:
/// <code>
/// {"__enc":true,"v":1,"kid":"temporal:2026-03:v1","ct":"base64","n":"base64","t":"base64"}
/// </code>
/// </para>
/// <para>
/// When the temporal key has been destroyed via crypto-shredding, decryption returns the
/// configured placeholder (default: <c>"[SHREDDED]"</c>) instead of failing with an exception.
/// </para>
/// <para>
/// This type is serialized as a JSON string within event payloads. Marten stores the
/// encrypted envelope as a regular string property in the event's JSONB column.
/// </para>
/// </remarks>
public sealed record EncryptedField
{
    /// <summary>
    /// Size of the AES-GCM nonce in bytes (96 bits).
    /// </summary>
    internal const int NonceSizeInBytes = 12;

    /// <summary>
    /// Size of the AES-GCM authentication tag in bytes (128 bits).
    /// </summary>
    internal const int TagSizeInBytes = 16;

    private const int KeySizeInBytes = 32;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private const string EncryptedMarkerPrefix = "{\"__enc\":true";
    private const int FormatVersion = 1;

    /// <summary>
    /// The compact JSON envelope string containing the encrypted data and cryptographic metadata.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the original plaintext value was <c>null</c>.
    /// When populated, starts with <c>{"__enc":true</c> for fast detection.
    /// </remarks>
    public string? Value { get; init; }

    /// <summary>
    /// The temporal key identifier used for encryption (e.g., <c>"temporal:2026-03:v1"</c>).
    /// </summary>
    /// <remarks>
    /// Stored alongside the encrypted data for key lookup during decryption.
    /// <c>null</c> when the original plaintext value was <c>null</c>.
    /// </remarks>
    public string? KeyId { get; init; }

    /// <summary>
    /// Encrypts a plaintext string value using AES-256-GCM with the provided temporal key material.
    /// </summary>
    /// <param name="plaintext">The plaintext value to encrypt. Can be <c>null</c>.</param>
    /// <param name="keyMaterial">The AES-256 key material (32 bytes).</param>
    /// <param name="keyId">The temporal key identifier (e.g., <c>"temporal:2026-03:v1"</c>).</param>
    /// <returns>
    /// An <see cref="EncryptedField"/> containing the encrypted envelope, or an empty field
    /// if the plaintext is <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keyMaterial"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="keyId"/> is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// var encrypted = EncryptedField.Encrypt("user@example.com", keyMaterial, "temporal:2026-03:v1");
    /// // encrypted.Value contains the JSON envelope with ciphertext
    /// </code>
    /// </example>
    public static EncryptedField Encrypt(string? plaintext, byte[] keyMaterial, string keyId)
    {
        ArgumentNullException.ThrowIfNull(keyMaterial);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        if (plaintext is null)
        {
            return new EncryptedField { Value = null, KeyId = null };
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSizeInBytes];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeInBytes];

        using var aesGcm = new AesGcm(keyMaterial, TagSizeInBytes);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var envelopeJson = SerializeEnvelope(keyId, ciphertext, nonce, tag);

        return new EncryptedField
        {
            Value = envelopeJson,
            KeyId = keyId
        };
    }

    /// <summary>
    /// Decrypts this encrypted field using the provided temporal key material.
    /// </summary>
    /// <param name="keyMaterial">The AES-256 key material (32 bytes) for the temporal period.</param>
    /// <returns>The decrypted plaintext string, or <c>null</c> if the original value was <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keyMaterial"/> is null.</exception>
    /// <exception cref="EncryptedFieldDecryptionException">
    /// Thrown (it derives from <see cref="CryptographicException"/>) for every stored-value or key failure: the
    /// ciphertext or tag is tampered, the key is incorrect or not 32 bytes, the nonce or tag has the wrong size,
    /// the envelope is malformed, or the value carries no <c>__enc</c> marker. The message is fixed and never
    /// includes the envelope, a key identifier, plaintext or the message of the underlying exception. Recovery
    /// steps: <c>docs/features/audit-marten.md</c>, section "Recover from an unreadable audit event".
    /// </exception>
    /// <example>
    /// <code>
    /// var decrypted = encryptedField.Decrypt(keyMaterial);
    /// // decrypted == "user@example.com"
    /// </code>
    /// </example>
    public string? Decrypt(byte[] keyMaterial)
    {
        ArgumentNullException.ThrowIfNull(keyMaterial);

        if (Value is null)
        {
            return null;
        }

        var parsed = ParseEnvelope(Value);
        ValidateSizes(keyMaterial, parsed);

        var plaintext = new byte[parsed.Ciphertext.Length];

        try
        {
            using var aesGcm = new AesGcm(keyMaterial, TagSizeInBytes);
            aesGcm.Decrypt(parsed.Nonce, parsed.Ciphertext, parsed.Tag, plaintext);
        }
        catch (CryptographicException)
        {
            // Deliberately drops the cause: its message must never reach the daemon logs (AGENTS.md section 3).
            throw new EncryptedFieldDecryptionException(EncryptedFieldDecryptionFailureReason.IntegrityCheckFailed);
        }

        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>
    /// Attempts to decrypt this encrypted field, returning the placeholder if the key is not available.
    /// </summary>
    /// <param name="keyMaterial">
    /// The AES-256 key material (32 bytes), or <c>null</c> if the key has been destroyed.
    /// </param>
    /// <param name="shreddedPlaceholder">
    /// The placeholder value to return when the key is <c>null</c> (destroyed).
    /// Defaults to <c>"[SHREDDED]"</c>.
    /// </param>
    /// <returns>
    /// The decrypted plaintext, the placeholder if the key was destroyed, or <c>null</c>
    /// if the original value was <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method returns the placeholder only for a legitimately destroyed key (<c>null</c> key material,
    /// crypto-shredding). Every other failure to read the stored value (tampered, wrong key, malformed or
    /// unmarked envelope, wrong sizes) is never reported as erasure: it surfaces as
    /// <see cref="EncryptedFieldDecryptionException"/> so the projection stops (integrity over availability).
    /// This is the primary decryption method used by projections. Recovery steps:
    /// <c>docs/features/audit-marten.md</c>, section "Recover from an unreadable audit event".
    /// </remarks>
    /// <exception cref="EncryptedFieldDecryptionException">
    /// Thrown when the key material is present but the stored value cannot be read; see <see cref="Decrypt(byte[])"/>.
    /// </exception>
    public string? DecryptOrPlaceholder(byte[]? keyMaterial, string shreddedPlaceholder = MartenOperationAuditOptions.DefaultShreddedPlaceholder)
    {
        if (Value is null)
        {
            return null;
        }

        return keyMaterial is null ? shreddedPlaceholder : Decrypt(keyMaterial);
    }

    /// <summary>
    /// Checks whether the <see cref="Value"/> contains an encrypted envelope.
    /// </summary>
    /// <returns><c>true</c> if the value is an encrypted JSON envelope; otherwise, <c>false</c>.</returns>
    public bool IsEncrypted =>
        Value is not null && Value.StartsWith(EncryptedMarkerPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Serializes the encrypted data into the compact JSON envelope format.
    /// </summary>
    private static string SerializeEnvelope(string keyId, byte[] ciphertext, byte[] nonce, byte[] tag)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteBoolean("__enc", true);
        writer.WriteNumber("v", FormatVersion);
        writer.WriteString("kid", keyId);
        writer.WriteBase64String("ct", ciphertext);
        writer.WriteBase64String("n", nonce);
        writer.WriteBase64String("t", tag);
        writer.WriteEndObject();

        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Parses a stored value as an encrypted field envelope.
    /// </summary>
    /// <returns>The parsed components.</returns>
    /// <exception cref="EncryptedFieldDecryptionException">
    /// The value has no <c>__enc</c> marker, is not valid JSON, or lacks a Base64 string <c>ct</c>, <c>n</c> or <c>t</c>.
    /// </exception>
    private static ParsedEnvelope ParseEnvelope(string json)
    {
        if (!json.StartsWith(EncryptedMarkerPrefix, StringComparison.Ordinal))
        {
            throw new EncryptedFieldDecryptionException(EncryptedFieldDecryptionFailureReason.NotEncrypted);
        }

        try
        {
            using var document = JsonDocument.Parse(StrictUtf8.GetBytes(json));
            var root = document.RootElement;

            return new ParsedEnvelope(
                Ciphertext: ReadBase64(root, "ct"),
                Nonce: ReadBase64(root, "n"),
                Tag: ReadBase64(root, "t"));
        }
        catch (JsonException)
        {
            throw Malformed();
        }
        catch (EncoderFallbackException)
        {
            throw Malformed();
        }
    }

    private static byte[] ReadBase64(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.String
            && element.TryGetBytesFromBase64(out var bytes))
        {
            return bytes;
        }

        throw Malformed();
    }

    private static EncryptedFieldDecryptionException Malformed() =>
        new(EncryptedFieldDecryptionFailureReason.EnvelopeMalformed);

    private static void ValidateSizes(byte[] keyMaterial, ParsedEnvelope parsed)
    {
        if (keyMaterial.Length != KeySizeInBytes
            || parsed.Nonce.Length != NonceSizeInBytes
            || parsed.Tag.Length != TagSizeInBytes)
        {
            throw new EncryptedFieldDecryptionException(EncryptedFieldDecryptionFailureReason.InvalidKeyOrParameters);
        }
    }

    /// <summary>
    /// Parsed components of an encrypted field envelope.
    /// </summary>
    private readonly record struct ParsedEnvelope(byte[] Ciphertext, byte[] Nonce, byte[] Tag);
}
