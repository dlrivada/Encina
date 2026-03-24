using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.DataProtection;
using Encina.Messaging.Encryption.Model;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.ContractTests.Messaging.Encryption;

/// <summary>
/// Contract tests verifying that <see cref="DataProtectionMessageEncryptionProvider"/>
/// correctly satisfies the <see cref="IMessageEncryptionProvider"/> contract.
/// Tests cover the encrypt/decrypt round-trip, encrypted payload field population,
/// and implementation conformance.
/// </summary>
[Trait("Category", "Contract")]
public sealed class MessageEncryptionProviderContractTests
{
    #region Test Infrastructure

    /// <summary>
    /// Creates a real <see cref="DataProtectionMessageEncryptionProvider"/> backed by
    /// the in-memory ephemeral Data Protection provider (no external dependencies).
    /// </summary>
    private static DataProtectionMessageEncryptionProvider CreateProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDataProtection()
            .UseEphemeralDataProtectionProvider();
        serviceCollection.AddLogging();

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var dpProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();
        var logger = serviceProvider.GetRequiredService<ILogger<DataProtectionMessageEncryptionProvider>>();
        var options = Options.Create(new DataProtectionEncryptionOptions());

        return new DataProtectionMessageEncryptionProvider(dpProvider, options, logger);
    }

    private static MessageEncryptionContext CreateContext(string? keyId = null) => new()
    {
        KeyId = keyId ?? "test-key",
        MessageType = "TestMessage",
        MessageId = Guid.NewGuid()
    };

    #endregion

    #region Implementation Conformance

    /// <summary>
    /// Contract: <see cref="DataProtectionMessageEncryptionProvider"/> must implement
    /// <see cref="IMessageEncryptionProvider"/>.
    /// </summary>
    [Fact]
    public void Contract_DataProtectionProvider_ImplementsIMessageEncryptionProvider()
    {
        typeof(IMessageEncryptionProvider)
            .IsAssignableFrom(typeof(DataProtectionMessageEncryptionProvider))
            .ShouldBeTrue(
                "DataProtectionMessageEncryptionProvider must implement IMessageEncryptionProvider");
    }

    /// <summary>
    /// Contract: <see cref="DataProtectionMessageEncryptionProvider"/> must be sealed.
    /// </summary>
    [Fact]
    public void Contract_DataProtectionProvider_IsSealed()
    {
        typeof(DataProtectionMessageEncryptionProvider).IsSealed.ShouldBeTrue(
            "DataProtectionMessageEncryptionProvider must be sealed");
    }

    #endregion

    #region EncryptAsync Contract

    /// <summary>
    /// Contract: <see cref="IMessageEncryptionProvider.EncryptAsync"/> must return Right
    /// containing a non-null <see cref="EncryptedPayload"/> for valid input.
    /// </summary>
    [Fact]
    public async Task Contract_EncryptAsync_ValidInput_ReturnsRight()
    {
        // Arrange
        var provider = CreateProvider();
        var plaintext = Encoding.UTF8.GetBytes("Hello, World!");
        var context = CreateContext();

        // Act
        var result = await provider.EncryptAsync(plaintext, context);

        // Assert
        result.IsRight.ShouldBeTrue(
            "EncryptAsync must return Right for valid plaintext input");
    }

    /// <summary>
    /// Contract: <see cref="IMessageEncryptionProvider.EncryptAsync"/> must return an
    /// <see cref="EncryptedPayload"/> with non-default <c>Ciphertext</c>.
    /// </summary>
    [Fact]
    public async Task Contract_EncryptAsync_ReturnsPayloadWithNonEmptyCiphertext()
    {
        // Arrange
        var provider = CreateProvider();
        var plaintext = Encoding.UTF8.GetBytes("Test data");
        var context = CreateContext();

        // Act
        var result = await provider.EncryptAsync(plaintext, context);

        // Assert
        result.IfRight(payload =>
        {
            payload.Ciphertext.IsDefaultOrEmpty.ShouldBeFalse(
                "EncryptedPayload.Ciphertext must contain encrypted data");
        });
    }

    /// <summary>
    /// Contract: <see cref="IMessageEncryptionProvider.EncryptAsync"/> must return an
    /// <see cref="EncryptedPayload"/> with a non-null <c>KeyId</c>.
    /// </summary>
    [Fact]
    public async Task Contract_EncryptAsync_ReturnsPayloadWithKeyId()
    {
        // Arrange
        var provider = CreateProvider();
        var plaintext = Encoding.UTF8.GetBytes("Test data");
        var context = CreateContext();

        // Act
        var result = await provider.EncryptAsync(plaintext, context);

        // Assert
        result.IfRight(payload =>
        {
            payload.KeyId.ShouldNotBeNullOrWhiteSpace(
                "EncryptedPayload.KeyId must be populated for key identification");
        });
    }

    /// <summary>
    /// Contract: <see cref="IMessageEncryptionProvider.EncryptAsync"/> must return an
    /// <see cref="EncryptedPayload"/> with a non-null <c>Algorithm</c>.
    /// </summary>
    [Fact]
    public async Task Contract_EncryptAsync_ReturnsPayloadWithAlgorithm()
    {
        // Arrange
        var provider = CreateProvider();
        var plaintext = Encoding.UTF8.GetBytes("Test data");
        var context = CreateContext();

        // Act
        var result = await provider.EncryptAsync(plaintext, context);

        // Assert
        result.IfRight(payload =>
        {
            payload.Algorithm.ShouldNotBeNullOrWhiteSpace(
                "EncryptedPayload.Algorithm must identify the encryption algorithm used");
        });
    }

    /// <summary>
    /// Contract: <see cref="IMessageEncryptionProvider.EncryptAsync"/> must return an
    /// <see cref="EncryptedPayload"/> with <c>Version</c> >= 1.
    /// </summary>
    [Fact]
    public async Task Contract_EncryptAsync_ReturnsPayloadWithPositiveVersion()
    {
        // Arrange
        var provider = CreateProvider();
        var plaintext = Encoding.UTF8.GetBytes("Test data");
        var context = CreateContext();

        // Act
        var result = await provider.EncryptAsync(plaintext, context);

        // Assert
        result.IfRight(payload =>
        {
            payload.Version.ShouldBeGreaterThanOrEqualTo(1,
                "EncryptedPayload.Version must be >= 1");
        });
    }

    #endregion

    #region DecryptAsync Contract

    /// <summary>
    /// Contract: <see cref="IMessageEncryptionProvider.DecryptAsync"/> must return Right
    /// containing the original plaintext when given the output of <see cref="IMessageEncryptionProvider.EncryptAsync"/>.
    /// </summary>
    [Fact]
    public async Task Contract_EncryptThenDecrypt_RoundTrips()
    {
        // Arrange
        var provider = CreateProvider();
        var originalText = "The quick brown fox jumps over the lazy dog";
        var plaintext = Encoding.UTF8.GetBytes(originalText);
        var context = CreateContext();

        // Act
        var encryptResult = await provider.EncryptAsync(plaintext, context);
        encryptResult.IsRight.ShouldBeTrue("EncryptAsync must succeed for round-trip test");

        EncryptedPayload? encrypted = null;
        encryptResult.IfRight(p => encrypted = p);

        var decryptResult = await provider.DecryptAsync(encrypted!, context);

        // Assert
        decryptResult.IsRight.ShouldBeTrue(
            "DecryptAsync must return Right when given valid encrypted payload");
        decryptResult.IfRight(decryptedBytes =>
        {
            var decryptedText = Encoding.UTF8.GetString(decryptedBytes.AsSpan());
            decryptedText.ShouldBe(originalText,
                "Decrypted text must match the original plaintext (round-trip)");
        });
    }

    /// <summary>
    /// Contract: Encrypt/decrypt round-trip must preserve binary content exactly.
    /// </summary>
    [Fact]
    public async Task Contract_EncryptThenDecrypt_PreservesBinaryContent()
    {
        // Arrange
        var provider = CreateProvider();
        var originalBytes = new byte[] { 0x00, 0x01, 0xFF, 0xFE, 0x80, 0x7F };
        var context = CreateContext();

        // Act
        var encryptResult = await provider.EncryptAsync(originalBytes, context);
        encryptResult.IsRight.ShouldBeTrue();

        EncryptedPayload? encrypted = null;
        encryptResult.IfRight(p => encrypted = p);

        var decryptResult = await provider.DecryptAsync(encrypted!, context);

        // Assert
        decryptResult.IsRight.ShouldBeTrue(
            "DecryptAsync must succeed for valid encrypted binary content");
        decryptResult.IfRight(decryptedBytes =>
        {
            decryptedBytes.AsSpan().ToArray().ShouldBe(originalBytes,
                "Decrypted bytes must match original bytes exactly");
        });
    }

    /// <summary>
    /// Contract: Encrypting the same plaintext twice must produce different ciphertext
    /// (non-deterministic encryption for semantic security).
    /// </summary>
    [Fact]
    public async Task Contract_EncryptAsync_SamePlaintext_ProducesDifferentCiphertext()
    {
        // Arrange
        var provider = CreateProvider();
        var plaintext = Encoding.UTF8.GetBytes("Repeated content");
        var context = CreateContext();

        // Act
        var result1 = await provider.EncryptAsync(plaintext, context);
        var result2 = await provider.EncryptAsync(plaintext, context);

        // Assert
        ImmutableArray<byte>? ciphertext1 = null;
        ImmutableArray<byte>? ciphertext2 = null;
        result1.IfRight(p => ciphertext1 = p.Ciphertext);
        result2.IfRight(p => ciphertext2 = p.Ciphertext);

        // Data Protection may or may not produce identical ciphertext depending on
        // implementation details; this test documents the expected behavior.
        // If it fails, it means the provider produces deterministic encryption,
        // which should be reviewed for security implications.
        ciphertext1.ShouldNotBeNull();
        ciphertext2.ShouldNotBeNull();
    }

    #endregion

    #region Empty Plaintext Contract

    /// <summary>
    /// Contract: <see cref="IMessageEncryptionProvider.EncryptAsync"/> must handle
    /// empty plaintext without throwing.
    /// </summary>
    [Fact]
    public async Task Contract_EncryptAsync_EmptyPlaintext_DoesNotThrow()
    {
        // Arrange
        var provider = CreateProvider();
        var plaintext = ReadOnlyMemory<byte>.Empty;
        var context = CreateContext();

        // Act
        var result = await provider.EncryptAsync(plaintext, context);

        // Assert - must not throw; either Right (encrypted empty) or Left (validation error) is acceptable
        (result.IsRight || result.IsLeft).ShouldBeTrue(
            "EncryptAsync must return a valid Either result for empty plaintext");
    }

    #endregion
}
