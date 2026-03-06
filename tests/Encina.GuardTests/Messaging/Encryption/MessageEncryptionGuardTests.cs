using System.Collections.Immutable;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Health;
using Encina.Messaging.Encryption.Model;
using Encina.Messaging.Encryption.Serialization;
using Encina.Messaging.Serialization;
using Encina.Security.Encryption.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.GuardTests.Messaging.Encryption;

public class MessageEncryptionGuardTests
{
    #region DefaultMessageEncryptionProvider

    [Fact]
    public void DefaultMessageEncryptionProvider_NullFieldEncryptor_ThrowsArgumentNullException()
    {
        var keyProvider = Substitute.For<IKeyProvider>();

        var act = () => new DefaultMessageEncryptionProvider(null!, keyProvider);

        act.Should().Throw<ArgumentNullException>().WithParameterName("fieldEncryptor");
    }

    [Fact]
    public void DefaultMessageEncryptionProvider_NullKeyProvider_ThrowsArgumentNullException()
    {
        var fieldEncryptor = Substitute.For<IFieldEncryptor>();

        var act = () => new DefaultMessageEncryptionProvider(fieldEncryptor, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("keyProvider");
    }

    [Fact]
    public async Task DefaultMessageEncryptionProvider_EncryptAsync_NullContext_ThrowsArgumentNullException()
    {
        var provider = new DefaultMessageEncryptionProvider(
            Substitute.For<IFieldEncryptor>(),
            Substitute.For<IKeyProvider>());

        var act = async () => await provider.EncryptAsync(new byte[] { 1 }, null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task DefaultMessageEncryptionProvider_DecryptAsync_NullPayload_ThrowsArgumentNullException()
    {
        var provider = new DefaultMessageEncryptionProvider(
            Substitute.For<IFieldEncryptor>(),
            Substitute.For<IKeyProvider>());
        var context = new MessageEncryptionContext();

        var act = async () => await provider.DecryptAsync(null!, context);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("payload");
    }

    [Fact]
    public async Task DefaultMessageEncryptionProvider_DecryptAsync_NullContext_ThrowsArgumentNullException()
    {
        var provider = new DefaultMessageEncryptionProvider(
            Substitute.For<IFieldEncryptor>(),
            Substitute.For<IKeyProvider>());
        var payload = new EncryptedPayload
        {
            Ciphertext = ImmutableArray<byte>.Empty,
            KeyId = "k",
            Algorithm = "a",
            Nonce = ImmutableArray<byte>.Empty,
            Tag = ImmutableArray<byte>.Empty,
            Version = 1
        };

        var act = async () => await provider.DecryptAsync(payload, null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    #endregion

    #region EncryptingMessageSerializer

    [Fact]
    public void EncryptingMessageSerializer_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new EncryptingMessageSerializer(
            null!,
            Substitute.For<IMessageEncryptionProvider>(),
            Options.Create(new MessageEncryptionOptions()),
            NullLogger<EncryptingMessageSerializer>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("inner");
    }

    [Fact]
    public void EncryptingMessageSerializer_NullProvider_ThrowsArgumentNullException()
    {
        var act = () => new EncryptingMessageSerializer(
            Substitute.For<IMessageSerializer>(),
            null!,
            Options.Create(new MessageEncryptionOptions()),
            NullLogger<EncryptingMessageSerializer>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("provider");
    }

    [Fact]
    public void EncryptingMessageSerializer_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new EncryptingMessageSerializer(
            Substitute.For<IMessageSerializer>(),
            Substitute.For<IMessageEncryptionProvider>(),
            null!,
            NullLogger<EncryptingMessageSerializer>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void EncryptingMessageSerializer_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new EncryptingMessageSerializer(
            Substitute.For<IMessageSerializer>(),
            Substitute.For<IMessageEncryptionProvider>(),
            Options.Create(new MessageEncryptionOptions()),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region DefaultTenantKeyResolver

    [Fact]
    public void DefaultTenantKeyResolver_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultTenantKeyResolver(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void DefaultTenantKeyResolver_ResolveKeyId_NullTenantId_ThrowsArgumentException()
    {
        var resolver = new DefaultTenantKeyResolver(Options.Create(new MessageEncryptionOptions()));

        var act = () => resolver.ResolveKeyId(null!);

        act.Should().Throw<ArgumentException>().WithParameterName("tenantId");
    }

    [Fact]
    public void DefaultTenantKeyResolver_ResolveKeyId_EmptyTenantId_ThrowsArgumentException()
    {
        var resolver = new DefaultTenantKeyResolver(Options.Create(new MessageEncryptionOptions()));

        var act = () => resolver.ResolveKeyId(string.Empty);

        act.Should().Throw<ArgumentException>().WithParameterName("tenantId");
    }

    #endregion

    #region MessageEncryptionHealthCheck

    [Fact]
    public void MessageEncryptionHealthCheck_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new MessageEncryptionHealthCheck(
            null!,
            NullLogger<MessageEncryptionHealthCheck>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void MessageEncryptionHealthCheck_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new MessageEncryptionHealthCheck(
            Substitute.For<IServiceProvider>(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region EncryptedPayloadFormatter

    [Fact]
    public void EncryptedPayloadFormatter_Format_NullPayload_ThrowsArgumentNullException()
    {
        var act = () => EncryptedPayloadFormatter.Format(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("payload");
    }

    #endregion
}
