using System.Collections.Immutable;
using System.Security.Cryptography;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using FluentAssertions;
using LanguageExt;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Encryption;

public sealed class AesGcmFieldEncryptorTests : IDisposable
{
    private readonly InMemoryKeyProvider _keyProvider;
    private readonly AesGcmFieldEncryptor _sut;
    private readonly byte[] _testKey;
    private const string TestKeyId = "test-key-v1";

    public AesGcmFieldEncryptorTests()
    {
        _keyProvider = new InMemoryKeyProvider();
        _testKey = new byte[32];
        RandomNumberGenerator.Fill(_testKey);
        _keyProvider.AddKey(TestKeyId, _testKey);
        _keyProvider.SetCurrentKey(TestKeyId);
        _sut = new AesGcmFieldEncryptor(_keyProvider);
    }

    public void Dispose()
    {
        _keyProvider.Clear();
    }

    #region EncryptStringAsync

    [Fact]
    public async Task EncryptStringAsync_ValidPlaintext_ReturnsEncryptedValue()
    {
        var context = new EncryptionContext { Purpose = "test" };

        var result = await _sut.EncryptStringAsync("hello world", context);

        result.IsRight.Should().BeTrue();
        var encrypted = result.Match(Right: v => v, Left: _ => default);
        encrypted.KeyId.Should().Be(TestKeyId);
        encrypted.Algorithm.Should().Be(EncryptionAlgorithm.Aes256Gcm);
        encrypted.Nonce.Length.Should().Be(12);
        encrypted.Tag.Length.Should().Be(16);
        encrypted.Ciphertext.IsDefaultOrEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task EncryptStringAsync_EmptyString_ReturnsEncryptedValue()
    {
        var context = new EncryptionContext { Purpose = "test" };

        var result = await _sut.EncryptStringAsync(string.Empty, context);

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task EncryptStringAsync_SamePlaintext_ProducesDifferentCiphertext()
    {
        var context = new EncryptionContext { Purpose = "test" };
        const string plaintext = "hello world";

        var result1 = await _sut.EncryptStringAsync(plaintext, context);
        var result2 = await _sut.EncryptStringAsync(plaintext, context);

        result1.IsRight.Should().BeTrue();
        result2.IsRight.Should().BeTrue();

        var enc1 = result1.Match(Right: v => v, Left: _ => default);
        var enc2 = result2.Match(Right: v => v, Left: _ => default);

        // Different nonces → different ciphertext
        enc1.Nonce.Should().NotEqual(enc2.Nonce);
    }

    [Fact]
    public async Task EncryptStringAsync_WithExplicitKeyId_UsesSpecifiedKey()
    {
        const string altKeyId = "alt-key";
        var altKey = new byte[32];
        RandomNumberGenerator.Fill(altKey);
        _keyProvider.AddKey(altKeyId, altKey);

        var context = new EncryptionContext { KeyId = altKeyId, Purpose = "test" };

        var result = await _sut.EncryptStringAsync("hello", context);

        result.IsRight.Should().BeTrue();
        var encrypted = result.Match(Right: v => v, Left: _ => default);
        encrypted.KeyId.Should().Be(altKeyId);
    }

    [Fact]
    public async Task EncryptStringAsync_KeyNotFound_ReturnsError()
    {
        var context = new EncryptionContext { KeyId = "nonexistent-key", Purpose = "test" };

        var result = await _sut.EncryptStringAsync("hello", context);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task EncryptStringAsync_InvalidKeySize_ReturnsError()
    {
        const string shortKeyId = "short-key";
        _keyProvider.AddKey(shortKeyId, new byte[16]); // 128-bit, but AES-256 needs 256-bit

        var context = new EncryptionContext { KeyId = shortKeyId, Purpose = "test" };

        var result = await _sut.EncryptStringAsync("hello", context);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task EncryptStringAsync_NoCurrentKey_ReturnsError()
    {
        var emptyProvider = new InMemoryKeyProvider();
        var encryptor = new AesGcmFieldEncryptor(emptyProvider);
        var context = new EncryptionContext { Purpose = "test" };

        var result = await encryptor.EncryptStringAsync("hello", context);

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region DecryptStringAsync

    [Fact]
    public async Task DecryptStringAsync_ValidEncryptedValue_ReturnsOriginalPlaintext()
    {
        var context = new EncryptionContext { Purpose = "test" };
        const string original = "hello world";

        var encryptResult = await _sut.EncryptStringAsync(original, context);
        var encrypted = encryptResult.Match(Right: v => v, Left: _ => default);

        var decryptResult = await _sut.DecryptStringAsync(encrypted, context);

        decryptResult.IsRight.Should().BeTrue();
        var decrypted = decryptResult.Match(Right: v => v, Left: _ => string.Empty);
        decrypted.Should().Be(original);
    }

    [Fact]
    public async Task DecryptStringAsync_UnicodeContent_RoundtripsCorrectly()
    {
        var context = new EncryptionContext { Purpose = "test" };
        const string original = "こんにちは世界 🌍 αβγδ Ñ";

        var encryptResult = await _sut.EncryptStringAsync(original, context);
        var encrypted = encryptResult.Match(Right: v => v, Left: _ => default);

        var decryptResult = await _sut.DecryptStringAsync(encrypted, context);

        decryptResult.IsRight.Should().BeTrue();
        var decrypted = decryptResult.Match(Right: v => v, Left: _ => string.Empty);
        decrypted.Should().Be(original);
    }

    [Fact]
    public async Task DecryptStringAsync_WrongKey_ReturnsError()
    {
        var context = new EncryptionContext { Purpose = "test" };

        var encryptResult = await _sut.EncryptStringAsync("hello", context);
        var encrypted = encryptResult.Match(Right: v => v, Left: _ => default);

        // Create a new encryptor with a different key
        var wrongProvider = new InMemoryKeyProvider();
        var wrongKey = new byte[32];
        RandomNumberGenerator.Fill(wrongKey);
        wrongProvider.AddKey(TestKeyId, wrongKey);
        wrongProvider.SetCurrentKey(TestKeyId);
        var wrongEncryptor = new AesGcmFieldEncryptor(wrongProvider);

        var decryptResult = await wrongEncryptor.DecryptStringAsync(encrypted, context);

        decryptResult.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task DecryptStringAsync_TamperedCiphertext_ReturnsError()
    {
        var context = new EncryptionContext { Purpose = "test" };

        var encryptResult = await _sut.EncryptStringAsync("hello", context);
        var encrypted = encryptResult.Match(Right: v => v, Left: _ => default);

        // Tamper with ciphertext
        var tamperedCiphertext = encrypted.Ciphertext.ToArray();
        tamperedCiphertext[0] ^= 0xFF;

        var tampered = new EncryptedValue
        {
            Algorithm = encrypted.Algorithm,
            KeyId = encrypted.KeyId,
            Nonce = encrypted.Nonce,
            Tag = encrypted.Tag,
            Ciphertext = [.. tamperedCiphertext]
        };

        var decryptResult = await _sut.DecryptStringAsync(tampered, context);

        decryptResult.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task DecryptStringAsync_EmptyCiphertext_ReturnsError()
    {
        var context = new EncryptionContext { Purpose = "test" };

        var empty = new EncryptedValue
        {
            Algorithm = EncryptionAlgorithm.Aes256Gcm,
            KeyId = TestKeyId,
            Nonce = ImmutableArray<byte>.Empty,
            Tag = ImmutableArray<byte>.Empty,
            Ciphertext = ImmutableArray<byte>.Empty
        };

        var result = await _sut.DecryptStringAsync(empty, context);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task DecryptStringAsync_UnsupportedAlgorithm_ReturnsError()
    {
        var context = new EncryptionContext { Purpose = "test" };

        var invalid = new EncryptedValue
        {
            Algorithm = (EncryptionAlgorithm)99,
            KeyId = TestKeyId,
            Nonce = [.. new byte[12]],
            Tag = [.. new byte[16]],
            Ciphertext = [.. new byte[10]]
        };

        var result = await _sut.DecryptStringAsync(invalid, context);

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region EncryptBytesAsync / DecryptBytesAsync

    [Fact]
    public async Task EncryptBytesAsync_ValidData_Roundtrips()
    {
        var context = new EncryptionContext { Purpose = "test" };
        var original = new byte[] { 1, 2, 3, 4, 5 };

        var encryptResult = await _sut.EncryptBytesAsync(original, context);
        encryptResult.IsRight.Should().BeTrue();

        var encrypted = encryptResult.Match(Right: v => v, Left: _ => default);
        var decryptResult = await _sut.DecryptBytesAsync(encrypted, context);

        decryptResult.IsRight.Should().BeTrue();
        var decrypted = decryptResult.Match(Right: v => v, Left: _ => []);
        decrypted.Should().BeEquivalentTo(original);
    }

    #endregion

    #region Associated Data

    [Fact]
    public async Task EncryptDecrypt_WithAssociatedData_Roundtrips()
    {
        var context = new EncryptionContext
        {
            Purpose = "test",
            AssociatedData = [.. System.Text.Encoding.UTF8.GetBytes("tenant-123")]
        };
        const string original = "sensitive data";

        var encryptResult = await _sut.EncryptStringAsync(original, context);
        var encrypted = encryptResult.Match(Right: v => v, Left: _ => default);

        var decryptResult = await _sut.DecryptStringAsync(encrypted, context);

        decryptResult.IsRight.Should().BeTrue();
        var decrypted = decryptResult.Match(Right: v => v, Left: _ => string.Empty);
        decrypted.Should().Be(original);
    }

    [Fact]
    public async Task DecryptStringAsync_DifferentAssociatedData_ReturnsError()
    {
        var encryptContext = new EncryptionContext
        {
            Purpose = "test",
            AssociatedData = [.. System.Text.Encoding.UTF8.GetBytes("tenant-A")]
        };
        var decryptContext = new EncryptionContext
        {
            Purpose = "test",
            AssociatedData = [.. System.Text.Encoding.UTF8.GetBytes("tenant-B")]
        };

        var encryptResult = await _sut.EncryptStringAsync("data", encryptContext);
        var encrypted = encryptResult.Match(Right: v => v, Left: _ => default);

        var decryptResult = await _sut.DecryptStringAsync(encrypted, decryptContext);

        decryptResult.IsLeft.Should().BeTrue();
    }

    #endregion

    #region Key Rotation

    [Fact]
    public async Task DecryptStringAsync_AfterKeyRotation_StillDecryptsWithOldKey()
    {
        var context = new EncryptionContext { Purpose = "test" };

        // Encrypt with original key
        var encryptResult = await _sut.EncryptStringAsync("hello", context);
        var encrypted = encryptResult.Match(Right: v => v, Left: _ => default);

        // Rotate key
        await _keyProvider.RotateKeyAsync();

        // Decrypt with old key — should still work because KeyId is stored in EncryptedValue
        var decryptResult = await _sut.DecryptStringAsync(encrypted, context);

        decryptResult.IsRight.Should().BeTrue();
        var decrypted = decryptResult.Match(Right: v => v, Left: _ => string.Empty);
        decrypted.Should().Be("hello");
    }

    #endregion
}
