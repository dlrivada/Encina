using System.Security.Cryptography;

using Encina.Audit.Marten.Events;

using Shouldly;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="EncryptedField"/> AES-256-GCM encryption and decryption.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class EncryptedFieldTests
{
    private static byte[] GenerateKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    [Fact]
    public void Encrypt_NonNullPlaintext_ReturnsEncryptedField()
    {
        // Arrange
        var key = GenerateKey();
        var keyId = "temporal:2026-03:v1";
        var plaintext = "user@example.com";

        // Act
        var field = EncryptedField.Encrypt(plaintext, key, keyId);

        // Assert
        field.ShouldNotBeNull();
        field.Value.ShouldNotBeNullOrWhiteSpace();
        field.KeyId.ShouldBe(keyId);
        field.IsEncrypted.ShouldBeTrue();
        field.Value!.Contains(plaintext, StringComparison.Ordinal).ShouldBeFalse("Plaintext should not appear in encrypted value");
    }

    [Fact]
    public void Encrypt_NullPlaintext_ReturnsFieldWithNullValue()
    {
        // Arrange
        var key = GenerateKey();
        var keyId = "temporal:2026-03:v1";

        // Act
        var field = EncryptedField.Encrypt(null, key, keyId);

        // Assert
        field.Value.ShouldBeNull();
        field.KeyId.ShouldBeNull();
        field.IsEncrypted.ShouldBeFalse();
    }

    [Fact]
    public void Decrypt_WithCorrectKey_ReturnsOriginalPlaintext()
    {
        // Arrange
        var key = GenerateKey();
        var keyId = "temporal:2026-03:v1";
        var plaintext = "sensitive-user-id-12345";

        var field = EncryptedField.Encrypt(plaintext, key, keyId);

        // Act
        var decrypted = field.Decrypt(key);

        // Assert
        decrypted.ShouldBe(plaintext);
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsCryptographicException()
    {
        // Arrange
        var encryptKey = GenerateKey();
        var wrongKey = GenerateKey();
        var keyId = "temporal:2026-03:v1";
        var plaintext = "sensitive-data";

        var field = EncryptedField.Encrypt(plaintext, encryptKey, keyId);

        // Act & Assert
        Should.Throw<Exception>(() => field.Decrypt(wrongKey));
    }

    [Fact]
    public void DecryptOrPlaceholder_WithNullKeyMaterial_ReturnsShreddedPlaceholder()
    {
        // Arrange
        var key = GenerateKey();
        var keyId = "temporal:2026-03:v1";
        var plaintext = "sensitive-data";
        var placeholder = "[SHREDDED]";

        var field = EncryptedField.Encrypt(plaintext, key, keyId);

        // Act
        var result = field.DecryptOrPlaceholder(null, placeholder);

        // Assert
        result.ShouldBe(placeholder);
    }

    [Fact]
    public void DecryptOrPlaceholder_WithValidKey_ReturnsDecryptedValue()
    {
        // Arrange
        var key = GenerateKey();
        var keyId = "temporal:2026-03:v1";
        var plaintext = "sensitive-data";

        var field = EncryptedField.Encrypt(plaintext, key, keyId);

        // Act
        var result = field.DecryptOrPlaceholder(key);

        // Assert
        result.ShouldBe(plaintext);
    }

    [Fact]
    public void Encrypt_EmptyString_EncryptsAndDecryptsCorrectly()
    {
        // Arrange
        var key = GenerateKey();
        var keyId = "temporal:2026-03:v1";

        // Act
        var field = EncryptedField.Encrypt("", key, keyId);
        var decrypted = field.Decrypt(key);

        // Assert
        decrypted.ShouldBe("");
    }

    [Fact]
    public void Encrypt_LongString_EncryptsAndDecryptsCorrectly()
    {
        // Arrange
        var key = GenerateKey();
        var keyId = "temporal:2026-03:v1";
        var plaintext = new string('A', 10_000);

        // Act
        var field = EncryptedField.Encrypt(plaintext, key, keyId);
        var decrypted = field.Decrypt(key);

        // Assert
        decrypted.ShouldBe(plaintext);
    }

    [Fact]
    public void Encrypt_UnicodeString_EncryptsAndDecryptsCorrectly()
    {
        // Arrange
        var key = GenerateKey();
        var keyId = "temporal:2026-03:v1";
        var plaintext = "Hola mundo! \u00e9\u00e8\u00ea \u00fc\u00f1 \ud83d\ude80";

        // Act
        var field = EncryptedField.Encrypt(plaintext, key, keyId);
        var decrypted = field.Decrypt(key);

        // Assert
        decrypted.ShouldBe(plaintext);
    }

    [Fact]
    public void Encrypt_SamePlaintext_ProducesDifferentCiphertext()
    {
        // Arrange — random nonce should make each encryption unique
        var key = GenerateKey();
        var keyId = "temporal:2026-03:v1";
        var plaintext = "deterministic-input";

        // Act
        var field1 = EncryptedField.Encrypt(plaintext, key, keyId);
        var field2 = EncryptedField.Encrypt(plaintext, key, keyId);

        // Assert — both decrypt to same plaintext
        field1.Decrypt(key).ShouldBe(plaintext);
        field2.Decrypt(key).ShouldBe(plaintext);

        // But the encrypted envelopes should differ (random nonce)
        field1.Value.ShouldNotBe(field2.Value);
    }

    [Fact]
    public void IsEncrypted_WithNullValue_ReturnsFalse()
    {
        // Arrange
        var field = new EncryptedField { Value = null, KeyId = null };

        // Act & Assert
        field.IsEncrypted.ShouldBeFalse();
    }

    [Fact]
    public void IsEncrypted_WithEncryptedValue_ReturnsTrue()
    {
        // Arrange
        var key = GenerateKey();
        var field = EncryptedField.Encrypt("test", key, "temporal:2026-03:v1");

        // Act & Assert
        field.IsEncrypted.ShouldBeTrue();
    }
}
