#pragma warning disable CA2012 // ValueTask consumed by NSubstitute mock setup

using System.Collections.Immutable;
using System.Text;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.DataProtection;
using Encina.Messaging.Encryption.Model;
using LanguageExt;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Encryption.DataProtection;

public class DataProtectionMessageEncryptionProviderTests
{
    private readonly ILogger<DataProtectionMessageEncryptionProvider> _logger =
        NullLogger<DataProtectionMessageEncryptionProvider>.Instance;

    // --- Constructor null guard tests ---

    [Fact]
    public void Constructor_NullDataProtectionProvider_ThrowsArgumentNullException()
    {
        var options = Options.Create(new DataProtectionEncryptionOptions());

        Should.Throw<ArgumentNullException>(() => new DataProtectionMessageEncryptionProvider(null!, options, _logger))
            .ParamName.ShouldBe("dataProtectionProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var dpProvider = Substitute.For<IDataProtectionProvider>();
        dpProvider.CreateProtector(Arg.Any<string>()).Returns(Substitute.For<IDataProtector>());

        Should.Throw<ArgumentNullException>(() => new DataProtectionMessageEncryptionProvider(dpProvider, null!, _logger))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var dpProvider = Substitute.For<IDataProtectionProvider>();
        dpProvider.CreateProtector(Arg.Any<string>()).Returns(Substitute.For<IDataProtector>());
        var options = Options.Create(new DataProtectionEncryptionOptions());

        Should.Throw<ArgumentNullException>(() => new DataProtectionMessageEncryptionProvider(dpProvider, options, null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        var dpProvider = Substitute.For<IDataProtectionProvider>();
        dpProvider.CreateProtector(Arg.Any<string>()).Returns(Substitute.For<IDataProtector>());
        var options = Options.Create(new DataProtectionEncryptionOptions());

        Should.NotThrow(() => new DataProtectionMessageEncryptionProvider(dpProvider, options, _logger));
    }

    [Fact]
    public void Constructor_UsesConfiguredPurpose()
    {
        var dpProvider = Substitute.For<IDataProtectionProvider>();
        dpProvider.CreateProtector(Arg.Any<string>()).Returns(Substitute.For<IDataProtector>());
        var options = Options.Create(new DataProtectionEncryptionOptions { Purpose = "Custom.Purpose" });

        _ = new DataProtectionMessageEncryptionProvider(dpProvider, options, _logger);

        dpProvider.Received(1).CreateProtector("Custom.Purpose");
    }

    [Fact]
    public void Constructor_UsesDefaultPurpose_WhenNotConfigured()
    {
        var dpProvider = Substitute.For<IDataProtectionProvider>();
        dpProvider.CreateProtector(Arg.Any<string>()).Returns(Substitute.For<IDataProtector>());
        var options = Options.Create(new DataProtectionEncryptionOptions());

        _ = new DataProtectionMessageEncryptionProvider(dpProvider, options, _logger);

        dpProvider.Received(1).CreateProtector("Encina.Messaging.Encryption");
    }

    // --- EncryptAsync tests ---

    [Fact]
    public async Task EncryptAsync_NullContext_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await provider.EncryptAsync(new byte[] { 1, 2, 3 }, null!));
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task EncryptAsync_ValidInput_ReturnsRightWithEncryptedPayload()
    {
        var plaintext = Encoding.UTF8.GetBytes("Hello, World!");
        var protectedBytes = new byte[] { 99, 98, 97, 96, 95 };

        var protector = Substitute.For<IDataProtector>();
        protector.Protect(Arg.Any<byte[]>()).Returns(protectedBytes);

        var provider = CreateProvider(protector);
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };

        var result = await provider.EncryptAsync(plaintext, context);

        result.IsRight.ShouldBeTrue();
        var payload = result.Match(Right: p => p, Left: _ => default!);
        payload.ShouldNotBeNull();
    }

    [Fact]
    public async Task EncryptAsync_ValidInput_SetsKeyIdToDataProtection()
    {
        var protector = CreateSuccessfulProtector();
        var provider = CreateProvider(protector);
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };

        var result = await provider.EncryptAsync(new byte[] { 1 }, context);

        var payload = result.Match(Right: p => p, Left: _ => default!);
        payload.KeyId.ShouldBe("data-protection");
    }

    [Fact]
    public async Task EncryptAsync_ValidInput_SetsAlgorithmToDataProtection()
    {
        var protector = CreateSuccessfulProtector();
        var provider = CreateProvider(protector);
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };

        var result = await provider.EncryptAsync(new byte[] { 1 }, context);

        var payload = result.Match(Right: p => p, Left: _ => default!);
        payload.Algorithm.ShouldBe("DataProtection");
    }

    [Fact]
    public async Task EncryptAsync_ValidInput_SetsVersionTo1()
    {
        var protector = CreateSuccessfulProtector();
        var provider = CreateProvider(protector);
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };

        var result = await provider.EncryptAsync(new byte[] { 1 }, context);

        var payload = result.Match(Right: p => p, Left: _ => default!);
        payload.Version.ShouldBe(1);
    }

    [Fact]
    public async Task EncryptAsync_ValidInput_SetsEmptyNonceAndTag()
    {
        var protector = CreateSuccessfulProtector();
        var provider = CreateProvider(protector);
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };

        var result = await provider.EncryptAsync(new byte[] { 1 }, context);

        var payload = result.Match(Right: p => p, Left: _ => default!);
        payload.Nonce.ShouldBeEmpty();
        payload.Tag.ShouldBeEmpty();
    }

    [Fact]
    public async Task EncryptAsync_ValidInput_CiphertextMatchesProtectorOutput()
    {
        var protectedBytes = new byte[] { 10, 20, 30 };
        var protector = Substitute.For<IDataProtector>();
        protector.Protect(Arg.Any<byte[]>()).Returns(protectedBytes);

        var provider = CreateProvider(protector);
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };

        var result = await provider.EncryptAsync(new byte[] { 1, 2, 3 }, context);

        var payload = result.Match(Right: p => p, Left: _ => default!);
        payload.Ciphertext.ShouldBe(protectedBytes);
    }

    [Fact]
    public async Task EncryptAsync_ProtectorThrows_ReturnsLeftEncryptionFailed()
    {
        var protector = Substitute.For<IDataProtector>();
        protector.Protect(Arg.Any<byte[]>())
            .Returns<byte[]>(x => throw new InvalidOperationException("Protector failed"));

        var provider = CreateProvider(protector);
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };

        var result = await provider.EncryptAsync(new byte[] { 1 }, context);

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("encryption failed");
    }

    [Fact]
    public async Task EncryptAsync_NullMessageType_UsesUnknownInLogging()
    {
        var protector = CreateSuccessfulProtector();
        var provider = CreateProvider(protector);
        var context = new MessageEncryptionContext { MessageType = null };

        // Should not throw, uses "unknown" for logging
        var result = await provider.EncryptAsync(new byte[] { 1 }, context);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task EncryptAsync_EmptyPlaintext_SuccessfullyEncrypts()
    {
        var protector = CreateSuccessfulProtector();
        var provider = CreateProvider(protector);
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };

        var result = await provider.EncryptAsync(ReadOnlyMemory<byte>.Empty, context);

        result.IsRight.ShouldBeTrue();
    }

    // --- DecryptAsync tests ---

    [Fact]
    public async Task DecryptAsync_NullPayload_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();
        var context = new MessageEncryptionContext();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await provider.DecryptAsync(null!, context));
        ex.ParamName.ShouldBe("payload");
    }

    [Fact]
    public async Task DecryptAsync_NullContext_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();
        var payload = CreatePayload();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await provider.DecryptAsync(payload, null!));
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task DecryptAsync_ValidPayload_ReturnsRightWithDecryptedBytes()
    {
        var originalBytes = new byte[] { 1, 2, 3, 4, 5 };
        var protector = Substitute.For<IDataProtector>();
        protector.Unprotect(Arg.Any<byte[]>()).Returns(originalBytes);

        var provider = CreateProvider(protector);
        var payload = CreatePayload(new byte[] { 99, 98, 97 });
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };

        var result = await provider.DecryptAsync(payload, context);

        result.IsRight.ShouldBeTrue();
        var decrypted = result.Match(Right: b => b, Left: _ => default);
        decrypted.ShouldBe(originalBytes);
    }

    [Fact]
    public async Task DecryptAsync_ProtectorThrows_ReturnsLeftDecryptionFailed()
    {
        var protector = Substitute.For<IDataProtector>();
        protector.Unprotect(Arg.Any<byte[]>())
            .Returns<byte[]>(x => throw new System.Security.Cryptography.CryptographicException("Invalid data"));

        var provider = CreateProvider(protector);
        var payload = CreatePayload(new byte[] { 1, 2, 3 });
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };

        var result = await provider.DecryptAsync(payload, context);

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("decryption failed");
    }

    [Fact]
    public async Task DecryptAsync_GenericException_ReturnsLeftDecryptionFailed()
    {
        var protector = Substitute.For<IDataProtector>();
        protector.Unprotect(Arg.Any<byte[]>())
            .Returns<byte[]>(x => throw new InvalidOperationException("Something went wrong"));

        var provider = CreateProvider(protector);
        var payload = CreatePayload(new byte[] { 1, 2, 3 });
        var context = new MessageEncryptionContext { MessageType = "TestMessage" };

        var result = await provider.DecryptAsync(payload, context);

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("decryption failed");
    }

    // --- Round-trip test with EphemeralDataProtectionProvider ---

    [Fact]
    public async Task EncryptThenDecrypt_WithEphemeralProvider_ReturnsOriginalData()
    {
        var dpProvider = new EphemeralDataProtectionProvider();
        var options = Options.Create(new DataProtectionEncryptionOptions());
        var provider = new DataProtectionMessageEncryptionProvider(dpProvider, options, _logger);

        var originalData = Encoding.UTF8.GetBytes("Sensitive message payload");
        var context = new MessageEncryptionContext { MessageType = "RoundTripTest" };

        var encryptResult = await provider.EncryptAsync(originalData, context);
        encryptResult.IsRight.ShouldBeTrue();

        var encryptedPayload = encryptResult.Match(Right: p => p, Left: _ => default!);

        var decryptResult = await provider.DecryptAsync(encryptedPayload, context);
        decryptResult.IsRight.ShouldBeTrue();

        var decryptedBytes = decryptResult.Match(Right: b => b, Left: _ => default);
        decryptedBytes.ToArray().ShouldBe(originalData);
    }

    [Fact]
    public async Task EncryptThenDecrypt_EmptyPayload_RoundTripsSuccessfully()
    {
        var dpProvider = new EphemeralDataProtectionProvider();
        var options = Options.Create(new DataProtectionEncryptionOptions());
        var provider = new DataProtectionMessageEncryptionProvider(dpProvider, options, _logger);

        var context = new MessageEncryptionContext { MessageType = "EmptyTest" };

        var encryptResult = await provider.EncryptAsync(System.Array.Empty<byte>(), context);
        encryptResult.IsRight.ShouldBeTrue();

        var encryptedPayload = encryptResult.Match(Right: p => p, Left: _ => default!);

        var decryptResult = await provider.DecryptAsync(encryptedPayload, context);
        decryptResult.IsRight.ShouldBeTrue();

        var decryptedBytes = decryptResult.Match(Right: b => b, Left: _ => default);
        decryptedBytes.ToArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task EncryptThenDecrypt_LargePayload_RoundTripsSuccessfully()
    {
        var dpProvider = new EphemeralDataProtectionProvider();
        var options = Options.Create(new DataProtectionEncryptionOptions());
        var provider = new DataProtectionMessageEncryptionProvider(dpProvider, options, _logger);

        var originalData = new byte[64 * 1024]; // 64KB payload
        Random.Shared.NextBytes(originalData);
        var context = new MessageEncryptionContext { MessageType = "LargePayloadTest" };

        var encryptResult = await provider.EncryptAsync(originalData, context);
        encryptResult.IsRight.ShouldBeTrue();

        var encryptedPayload = encryptResult.Match(Right: p => p, Left: _ => default!);

        var decryptResult = await provider.DecryptAsync(encryptedPayload, context);
        decryptResult.IsRight.ShouldBeTrue();

        var decryptedBytes = decryptResult.Match(Right: b => b, Left: _ => default);
        decryptedBytes.ToArray().ShouldBe(originalData);
    }

    [Fact]
    public async Task Encrypt_DifferentPurposes_ProduceDifferentCiphertext()
    {
        var dpProvider = new EphemeralDataProtectionProvider();
        var plaintext = Encoding.UTF8.GetBytes("Same data");
        var context = new MessageEncryptionContext { MessageType = "Test" };

        var provider1 = new DataProtectionMessageEncryptionProvider(
            dpProvider,
            Options.Create(new DataProtectionEncryptionOptions { Purpose = "Purpose.A" }),
            _logger);

        var provider2 = new DataProtectionMessageEncryptionProvider(
            dpProvider,
            Options.Create(new DataProtectionEncryptionOptions { Purpose = "Purpose.B" }),
            _logger);

        var result1 = await provider1.EncryptAsync(plaintext, context);
        var result2 = await provider2.EncryptAsync(plaintext, context);

        var payload1 = result1.Match(Right: p => p, Left: _ => default!);
        var payload2 = result2.Match(Right: p => p, Left: _ => default!);

        // Different purposes should produce different ciphertext
        payload1.Ciphertext.ShouldNotBe(payload2.Ciphertext);
    }

    [Fact]
    public async Task Decrypt_WithDifferentPurpose_ReturnsLeft()
    {
        var dpProvider = new EphemeralDataProtectionProvider();
        var plaintext = Encoding.UTF8.GetBytes("Test data");
        var context = new MessageEncryptionContext { MessageType = "Test" };

        var encryptProvider = new DataProtectionMessageEncryptionProvider(
            dpProvider,
            Options.Create(new DataProtectionEncryptionOptions { Purpose = "Purpose.Encrypt" }),
            _logger);

        var decryptProvider = new DataProtectionMessageEncryptionProvider(
            dpProvider,
            Options.Create(new DataProtectionEncryptionOptions { Purpose = "Purpose.Decrypt" }),
            _logger);

        var encryptResult = await encryptProvider.EncryptAsync(plaintext, context);
        var payload = encryptResult.Match(Right: p => p, Left: _ => default!);

        // Decrypting with different purpose should fail
        var decryptResult = await decryptProvider.DecryptAsync(payload, context);
        decryptResult.IsLeft.ShouldBeTrue();
    }

    // --- IMessageEncryptionProvider interface conformance ---

    [Fact]
    public void ImplementsIMessageEncryptionProvider()
    {
        var provider = CreateProvider();
        provider.ShouldBeAssignableTo<IMessageEncryptionProvider>();
    }

    // --- Helpers ---

    private DataProtectionMessageEncryptionProvider CreateProvider(IDataProtector? protector = null)
    {
        protector ??= Substitute.For<IDataProtector>();
        var dpProvider = Substitute.For<IDataProtectionProvider>();
        dpProvider.CreateProtector(Arg.Any<string>()).Returns(protector);
        var options = Options.Create(new DataProtectionEncryptionOptions());
        return new DataProtectionMessageEncryptionProvider(dpProvider, options, _logger);
    }

    private static IDataProtector CreateSuccessfulProtector()
    {
        var protector = Substitute.For<IDataProtector>();
        protector.Protect(Arg.Any<byte[]>()).Returns(ci => new byte[] { 42, 43, 44 });
        return protector;
    }

    private static EncryptedPayload CreatePayload(byte[]? ciphertext = null)
    {
        return new EncryptedPayload
        {
            Ciphertext = (ciphertext ?? [1, 2, 3]).ToImmutableArray(),
            KeyId = "data-protection",
            Algorithm = "DataProtection",
            Nonce = ImmutableArray<byte>.Empty,
            Tag = ImmutableArray<byte>.Empty,
            Version = 1
        };
    }
}
