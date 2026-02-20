#pragma warning disable CA2012 // ValueTask should not be awaited multiple times - used via .AsTask().Result in sync property tests

using System.Security.Cryptography;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Security.Encryption;

/// <summary>
/// Property-based tests for field-level encryption invariants.
/// Uses <see cref="AesGcmFieldEncryptor"/> and <see cref="InMemoryKeyProvider"/>
/// to verify behavioral properties that must hold for all valid inputs.
/// </summary>
public sealed class EncryptionPropertyTests
{
    #region Roundtrip Invariants

    [Property(MaxTest = 50)]
    public bool EncryptThenDecrypt_String_ReturnsOriginal(NonEmptyString value)
    {
        var (encryptor, _) = CreateEncryptor();
        var context = new EncryptionContext();

        var encryptResult = encryptor.EncryptStringAsync(value.Get, context).AsTask().Result;
        if (!encryptResult.IsRight) return false;

        var encrypted = (EncryptedValue)encryptResult;
        var decryptResult = encryptor.DecryptStringAsync(encrypted, context).AsTask().Result;
        if (!decryptResult.IsRight) return false;

        var decrypted = (string)decryptResult;
        return decrypted == value.Get;
    }

    [Property(MaxTest = 50)]
    public bool EncryptThenDecrypt_Bytes_ReturnsOriginal(byte[] plaintext)
    {
        if (plaintext is null || plaintext.Length == 0) return true; // skip trivial

        var (encryptor, _) = CreateEncryptor();
        var context = new EncryptionContext();

        var encryptResult = encryptor.EncryptBytesAsync(plaintext, context).AsTask().Result;
        if (!encryptResult.IsRight) return false;

        var encrypted = (EncryptedValue)encryptResult;
        var decryptResult = encryptor.DecryptBytesAsync(encrypted, context).AsTask().Result;
        if (!decryptResult.IsRight) return false;

        var decrypted = (byte[])decryptResult;
        return plaintext.SequenceEqual(decrypted);
    }

    [Property(MaxTest = 30)]
    public bool Orchestrator_EncryptThenDecrypt_PreservesValues(NonEmptyString email, NonEmptyString name)
    {
        var (_, keyProvider) = CreateEncryptor();
        var fieldEncryptor = new AesGcmFieldEncryptor(keyProvider);
        var logger = NullLogger<EncryptionOrchestrator>.Instance;
        var orchestrator = new EncryptionOrchestrator(fieldEncryptor, logger);
        var context = RequestContext.CreateForTest(tenantId: "prop-tenant");

        EncryptedPropertyCache.ClearCache();

        var command = new TestOrchestratorCommand { Email = email.Get, Name = name.Get };

        var encryptResult = orchestrator.EncryptAsync(command, context).AsTask().Result;
        if (!encryptResult.IsRight) return false;

        // After encryption, Email should be encrypted
        if (!command.Email.StartsWith("ENC:v1:", StringComparison.Ordinal)) return false;
        // Name should be unchanged
        if (command.Name != name.Get) return false;

        var decryptResult = orchestrator.DecryptAsync(command, context).AsTask().Result;
        if (!decryptResult.IsRight) return false;

        return command.Email == email.Get && command.Name == name.Get;
    }

    #endregion

    #region Uniqueness / Nonce Freshness

    [Property(MaxTest = 50)]
    public bool SamePlaintext_ProducesDifferentCiphertext(NonEmptyString value)
    {
        var (encryptor, _) = CreateEncryptor();
        var context = new EncryptionContext();

        var result1 = encryptor.EncryptStringAsync(value.Get, context).AsTask().Result;
        var result2 = encryptor.EncryptStringAsync(value.Get, context).AsTask().Result;

        if (!result1.IsRight || !result2.IsRight) return false;

        var encrypted1 = (EncryptedValue)result1;
        var encrypted2 = (EncryptedValue)result2;

        // Nonces must differ (cryptographic uniqueness)
        return !encrypted1.Nonce.SequenceEqual(encrypted2.Nonce);
    }

    [Property(MaxTest = 30)]
    public bool Nonce_IsAlways12Bytes(NonEmptyString value)
    {
        var (encryptor, _) = CreateEncryptor();
        var context = new EncryptionContext();

        var result = encryptor.EncryptStringAsync(value.Get, context).AsTask().Result;
        if (!result.IsRight) return false;

        var encrypted = (EncryptedValue)result;
        return encrypted.Nonce.Length == 12;
    }

    [Property(MaxTest = 30)]
    public bool Tag_IsAlways16Bytes(NonEmptyString value)
    {
        var (encryptor, _) = CreateEncryptor();
        var context = new EncryptionContext();

        var result = encryptor.EncryptStringAsync(value.Get, context).AsTask().Result;
        if (!result.IsRight) return false;

        var encrypted = (EncryptedValue)result;
        return encrypted.Tag.Length == 16;
    }

    #endregion

    #region Tamper Detection

    [Property(MaxTest = 30)]
    public bool TamperedCiphertext_FailsDecryption(NonEmptyString value)
    {
        var (encryptor, _) = CreateEncryptor();
        var context = new EncryptionContext();

        var encryptResult = encryptor.EncryptStringAsync(value.Get, context).AsTask().Result;
        if (!encryptResult.IsRight) return false;

        var encrypted = (EncryptedValue)encryptResult;

        // Tamper with the ciphertext by flipping a byte
        if (encrypted.Ciphertext.Length == 0) return true; // empty edge case
        var tampered = encrypted.Ciphertext.ToArray();
        tampered[0] ^= 0xFF;

        var tamperedValue = new EncryptedValue
        {
            Algorithm = encrypted.Algorithm,
            KeyId = encrypted.KeyId,
            Nonce = encrypted.Nonce,
            Tag = encrypted.Tag,
            Ciphertext = [.. tampered]
        };

        var decryptResult = encryptor.DecryptStringAsync(tamperedValue, context).AsTask().Result;
        return decryptResult.IsLeft; // Should fail
    }

    [Property(MaxTest = 30)]
    public bool TamperedTag_FailsDecryption(NonEmptyString value)
    {
        var (encryptor, _) = CreateEncryptor();
        var context = new EncryptionContext();

        var encryptResult = encryptor.EncryptStringAsync(value.Get, context).AsTask().Result;
        if (!encryptResult.IsRight) return false;

        var encrypted = (EncryptedValue)encryptResult;

        // Tamper with the authentication tag
        var tamperedTag = encrypted.Tag.ToArray();
        tamperedTag[0] ^= 0xFF;

        var tamperedValue = new EncryptedValue
        {
            Algorithm = encrypted.Algorithm,
            KeyId = encrypted.KeyId,
            Nonce = encrypted.Nonce,
            Tag = [.. tamperedTag],
            Ciphertext = encrypted.Ciphertext
        };

        var decryptResult = encryptor.DecryptStringAsync(tamperedValue, context).AsTask().Result;
        return decryptResult.IsLeft; // Should fail
    }

    #endregion

    #region Key Rotation Properties

    [Property(MaxTest = 20)]
    public bool KeyRotation_OldDataDecryptableWithOldKey(NonEmptyString value)
    {
        var keyProvider = new InMemoryKeyProvider();
        var key1 = new byte[32];
        RandomNumberGenerator.Fill(key1);
        keyProvider.AddKey("key-v1", key1);
        keyProvider.SetCurrentKey("key-v1");

        var encryptor = new AesGcmFieldEncryptor(keyProvider);
        var context = new EncryptionContext();

        // Encrypt with key-v1
        var encryptResult = encryptor.EncryptStringAsync(value.Get, context).AsTask().Result;
        if (!encryptResult.IsRight) return false;

        var encrypted = (EncryptedValue)encryptResult;

        // Rotate key
        var key2 = new byte[32];
        RandomNumberGenerator.Fill(key2);
        keyProvider.AddKey("key-v2", key2);
        keyProvider.SetCurrentKey("key-v2");

        // Old data should still be decryptable (uses KeyId from EncryptedValue)
        var decryptResult = encryptor.DecryptStringAsync(encrypted, context).AsTask().Result;
        if (!decryptResult.IsRight) return false;

        var decrypted = (string)decryptResult;
        return decrypted == value.Get;
    }

    #endregion

    #region Error Code Invariants

    [Property(MaxTest = 50)]
    public bool AllErrorCodes_StartWithEncryptionPrefix(NonEmptyString keyId)
    {
        const string prefix = "encryption.";

        var errors = new[]
        {
            EncryptionErrors.KeyNotFound(keyId.Get),
            EncryptionErrors.DecryptionFailed(keyId.Get),
            EncryptionErrors.InvalidCiphertext(),
            EncryptionErrors.AlgorithmNotSupported(EncryptionAlgorithm.Aes256Gcm),
            EncryptionErrors.KeyRotationFailed()
        };

        return errors.All(e =>
        {
            var code = e.GetCode().IfNone(string.Empty);
            return code.StartsWith(prefix, StringComparison.Ordinal);
        });
    }

    #endregion

    #region InMemoryKeyProvider Properties

    [Property(MaxTest = 30)]
    public bool InMemoryKeyProvider_AddThenGet_ReturnsSameKey(NonEmptyString keyId)
    {
        var provider = new InMemoryKeyProvider();
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);

        provider.AddKey(keyId.Get, key);
        var result = provider.GetKeyAsync(keyId.Get).AsTask().Result;
        if (!result.IsRight) return false;

        var retrieved = (byte[])result;
        return key.SequenceEqual(retrieved);
    }

    [Property(MaxTest = 20)]
    public Property InMemoryKeyProvider_Rotate_ReturnsNewKeyId()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 5)),
            rotationCount =>
            {
                var provider = new InMemoryKeyProvider();
                var key = new byte[32];
                RandomNumberGenerator.Fill(key);
                provider.AddKey("initial-key", key);
                provider.SetCurrentKey("initial-key");

                var previousKeyId = "initial-key";
                for (var i = 0; i < rotationCount; i++)
                {
                    var rotateResult = provider.RotateKeyAsync().AsTask().Result;
                    rotateResult.IsRight.ShouldBeTrue($"Rotation {i + 1} should succeed");

                    var newKeyId = rotateResult.Match(
                        Right: id => id,
                        Left: _ => string.Empty);

                    newKeyId.ShouldNotBeNullOrWhiteSpace($"Rotation {i + 1} should return a non-empty key ID");

                    // New key should be different from previous
                    newKeyId.ShouldNotBe(previousKeyId, $"Rotation {i + 1} key ID should differ");
                    previousKeyId = newKeyId;
                }
            });
    }

    #endregion

    #region Helpers

    private static (AesGcmFieldEncryptor Encryptor, InMemoryKeyProvider KeyProvider) CreateEncryptor()
    {
        var keyProvider = new InMemoryKeyProvider();
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        keyProvider.AddKey("test-key", key);
        keyProvider.SetCurrentKey("test-key");

        return (new AesGcmFieldEncryptor(keyProvider), keyProvider);
    }

    #endregion

    #region Test Types

    private sealed class TestOrchestratorCommand
    {
        [Encrypt(Purpose = "Email")]
        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
