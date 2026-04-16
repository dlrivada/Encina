#pragma warning disable CA2012 // ValueTask consumed by NSubstitute mock setup

using System.Collections.Immutable;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Model;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Encryption;

public class DefaultMessageEncryptionProviderTests
{
    private readonly IFieldEncryptor _fieldEncryptor = Substitute.For<IFieldEncryptor>();
    private readonly IKeyProvider _keyProvider = Substitute.For<IKeyProvider>();
    private readonly DefaultMessageEncryptionProvider _provider;

    public DefaultMessageEncryptionProviderTests()
    {
        _provider = new DefaultMessageEncryptionProvider(_fieldEncryptor, _keyProvider);
    }

    [Fact]
    public async Task EncryptAsync_WithExplicitKeyId_UsesProvidedKey()
    {
        // Arrange
        var context = new MessageEncryptionContext { KeyId = "explicit-key", MessageType = "TestMessage" };
        var plaintext = new byte[] { 1, 2, 3 };

        var encryptedValue = new EncryptedValue
        {
            Ciphertext = ImmutableArray.Create<byte>(10, 20, 30),
            KeyId = "explicit-key",
            Algorithm = EncryptionAlgorithm.Aes256Gcm,
            Nonce = ImmutableArray.Create<byte>(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12),
            Tag = ImmutableArray.Create<byte>(100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115)
        };

        _fieldEncryptor.EncryptBytesAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<EncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, EncryptedValue>(encryptedValue));

        // Act
        var result = await _provider.EncryptAsync(plaintext, context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var payload = result.Match(Right: p => p, Left: _ => null!);
        payload.KeyId.ShouldBe("explicit-key");
        payload.Algorithm.ShouldBe("AES-256-GCM");

        // Should NOT call GetCurrentKeyIdAsync when explicit key is provided
        await _keyProvider.DidNotReceive().GetCurrentKeyIdAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EncryptAsync_WithoutKeyId_ResolvesCurrentKey()
    {
        // Arrange
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };
        var plaintext = new byte[] { 1, 2, 3 };

        _keyProvider.GetCurrentKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, string>("resolved-key"));

        var encryptedValue = new EncryptedValue
        {
            Ciphertext = ImmutableArray.Create<byte>(10, 20),
            KeyId = "resolved-key",
            Algorithm = EncryptionAlgorithm.Aes256Gcm,
            Nonce = ImmutableArray.Create<byte>(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12),
            Tag = ImmutableArray.Create<byte>(100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115)
        };

        _fieldEncryptor.EncryptBytesAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<EncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, EncryptedValue>(encryptedValue));

        // Act
        var result = await _provider.EncryptAsync(plaintext, context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _keyProvider.Received(1).GetCurrentKeyIdAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EncryptAsync_KeyResolutionFails_ReturnsLeftError()
    {
        // Arrange
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };
        var error = EncinaErrors.Create("key.not_found", "No current key");

        _keyProvider.GetCurrentKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, string>(error));

        // Act
        var result = await _provider.EncryptAsync(new byte[] { 1 }, context);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DecryptAsync_ValidPayload_DelegatesAndReturnsDecryptedBytes()
    {
        // Arrange
        var payload = new EncryptedPayload
        {
            Ciphertext = ImmutableArray.Create<byte>(10, 20, 30),
            KeyId = "my-key",
            Algorithm = "AES-256-GCM",
            Nonce = ImmutableArray.Create<byte>(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12),
            Tag = ImmutableArray.Create<byte>(100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115),
            Version = 1
        };
        var context = new MessageEncryptionContext { KeyId = "my-key" };
        byte[] decryptedBytes = [99, 98, 97];

        var dummyEncryptedValue = new EncryptedValue
        {
            Ciphertext = ImmutableArray<byte>.Empty,
            Algorithm = EncryptionAlgorithm.Aes256Gcm,
            KeyId = string.Empty,
            Nonce = ImmutableArray<byte>.Empty
        };
        _fieldEncryptor.DecryptBytesAsync(dummyEncryptedValue, null!, CancellationToken.None)
            .ReturnsForAnyArgs(ci => Right<EncinaError, byte[]>(decryptedBytes));

        // Act
        var result = await _provider.DecryptAsync(payload, context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var bytes = result.Match(Right: b => b, Left: _ => default);
        bytes.ShouldBe([99, 98, 97]);
    }

    [Fact]
    public async Task EncryptAsync_NullContext_ThrowsArgumentNullException()
    {
        var act = async () => await _provider.EncryptAsync(new byte[] { 1 }, null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task DecryptAsync_NullPayload_ThrowsArgumentNullException()
    {
        var context = new MessageEncryptionContext();
        var act = async () => await _provider.DecryptAsync(null!, context);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("payload");
    }

    [Fact]
    public async Task DecryptAsync_NullContext_ThrowsArgumentNullException()
    {
        var payload = new EncryptedPayload
        {
            Ciphertext = ImmutableArray<byte>.Empty,
            KeyId = "k",
            Algorithm = "a",
            Nonce = ImmutableArray<byte>.Empty,
            Tag = ImmutableArray<byte>.Empty,
            Version = 1
        };

        var act = async () => await _provider.DecryptAsync(payload, null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }
}
