using System.Security.Cryptography;

using Encina.Audit.Marten.Events;

using FsCheck;
using FsCheck.Xunit;

using Shouldly;

namespace Encina.PropertyTests.Marten.Audit;

/// <summary>
/// Property-based tests for <see cref="EncryptedField"/> encryption/decryption invariants using FsCheck.
/// Tests fundamental properties that must hold for all inputs.
/// </summary>
[Trait("Category", "Property")]
[Trait("Provider", "Marten")]
public sealed class EncryptedFieldPropertyTests
{
    private static byte[] GenerateKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    /// <summary>
    /// Encrypt then decrypt always returns the original plaintext for any non-null string.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool EncryptDecrypt_RoundTrip_ReturnsOriginal(NonNull<string> plaintext)
    {
        var key = GenerateKey();
        var keyId = "temporal:prop-test:v1";

        var field = EncryptedField.Encrypt(plaintext.Get, key, keyId);
        var decrypted = field.Decrypt(key);

        return decrypted == plaintext.Get;
    }

    /// <summary>
    /// Encrypted field always has IsEncrypted = true for non-null plaintext.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Encrypt_NonNull_IsAlwaysEncrypted(NonNull<string> plaintext)
    {
        var key = GenerateKey();
        var keyId = "temporal:prop-test:v1";

        var field = EncryptedField.Encrypt(plaintext.Get, key, keyId);

        return field.IsEncrypted;
    }

    /// <summary>
    /// Encrypted value never contains the original plaintext (for strings longer than 3 chars).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Encrypt_ValueDoesNotContainPlaintext(NonEmptyString plaintext)
    {
        var text = plaintext.Get;
        if (text.Length < 4) return true; // skip very short strings that might appear in base64 by chance

        var key = GenerateKey();
        var keyId = "temporal:prop-test:v1";

        var field = EncryptedField.Encrypt(text, key, keyId);

        return field.Value is not null && !field.Value.Contains(text);
    }

    /// <summary>
    /// Same plaintext encrypted twice with same key produces different ciphertext (random nonce).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Encrypt_SameInput_ProducesDifferentOutput(NonNull<string> plaintext)
    {
        var key = GenerateKey();
        var keyId = "temporal:prop-test:v1";

        var field1 = EncryptedField.Encrypt(plaintext.Get, key, keyId);
        var field2 = EncryptedField.Encrypt(plaintext.Get, key, keyId);

        // Both should decrypt to the same value
        var d1 = field1.Decrypt(key);
        var d2 = field2.Decrypt(key);

        if (d1 != plaintext.Get || d2 != plaintext.Get) return false;

        // Encrypted values should differ (random nonce)
        return field1.Value != field2.Value;
    }

    /// <summary>
    /// DecryptOrPlaceholder with null key always returns the placeholder.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DecryptOrPlaceholder_NullKey_AlwaysReturnsPlaceholder(NonNull<string> plaintext)
    {
        var key = GenerateKey();
        var keyId = "temporal:prop-test:v1";
        var placeholder = "[SHREDDED]";

        var field = EncryptedField.Encrypt(plaintext.Get, key, keyId);
        var result = field.DecryptOrPlaceholder(null, placeholder);

        return result == placeholder;
    }

    /// <summary>
    /// KeyId in encrypted field always matches the provided keyId.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Encrypt_KeyId_MatchesProvided(NonNull<string> plaintext, PositiveInt version)
    {
        var key = GenerateKey();
        var keyId = $"temporal:prop-test:v{version.Get}";

        var field = EncryptedField.Encrypt(plaintext.Get, key, keyId);

        return field.KeyId == keyId;
    }
}
