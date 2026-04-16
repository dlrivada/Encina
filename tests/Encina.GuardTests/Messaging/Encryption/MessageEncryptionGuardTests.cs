using System.Collections.Immutable;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Health;
using Encina.Messaging.Encryption.Model;
using Encina.Messaging.Encryption.Serialization;
using Encina.Messaging.Serialization;
using Encina.Security.Encryption.Abstractions;
using Shouldly;
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

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("fieldEncryptor");
    }

    [Fact]
    public void DefaultMessageEncryptionProvider_NullKeyProvider_ThrowsArgumentNullException()
    {
        var fieldEncryptor = Substitute.For<IFieldEncryptor>();

        var act = () => new DefaultMessageEncryptionProvider(fieldEncryptor, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("keyProvider");
    }

    [Fact]
    public async Task DefaultMessageEncryptionProvider_EncryptAsync_NullContext_ThrowsArgumentNullException()
    {
        var provider = new DefaultMessageEncryptionProvider(
            Substitute.For<IFieldEncryptor>(),
            Substitute.For<IKeyProvider>());

        var act = async () => await provider.EncryptAsync(new byte[] { 1 }, null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task DefaultMessageEncryptionProvider_DecryptAsync_NullPayload_ThrowsArgumentNullException()
    {
        var provider = new DefaultMessageEncryptionProvider(
            Substitute.For<IFieldEncryptor>(),
            Substitute.For<IKeyProvider>());
        var context = new MessageEncryptionContext();

        var act = async () => await provider.DecryptAsync(null!, context);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("payload");
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

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
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

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void EncryptingMessageSerializer_NullProvider_ThrowsArgumentNullException()
    {
        var act = () => new EncryptingMessageSerializer(
            Substitute.For<IMessageSerializer>(),
            null!,
            Options.Create(new MessageEncryptionOptions()),
            NullLogger<EncryptingMessageSerializer>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("provider");
    }

    [Fact]
    public void EncryptingMessageSerializer_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new EncryptingMessageSerializer(
            Substitute.For<IMessageSerializer>(),
            Substitute.For<IMessageEncryptionProvider>(),
            null!,
            NullLogger<EncryptingMessageSerializer>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void EncryptingMessageSerializer_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new EncryptingMessageSerializer(
            Substitute.For<IMessageSerializer>(),
            Substitute.For<IMessageEncryptionProvider>(),
            Options.Create(new MessageEncryptionOptions()),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region DefaultTenantKeyResolver

    [Fact]
    public void DefaultTenantKeyResolver_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultTenantKeyResolver(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void DefaultTenantKeyResolver_ResolveKeyId_NullTenantId_ThrowsArgumentException()
    {
        var resolver = new DefaultTenantKeyResolver(Options.Create(new MessageEncryptionOptions()));

        var act = () => resolver.ResolveKeyId(null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("tenantId");
    }

    [Fact]
    public void DefaultTenantKeyResolver_ResolveKeyId_EmptyTenantId_ThrowsArgumentException()
    {
        var resolver = new DefaultTenantKeyResolver(Options.Create(new MessageEncryptionOptions()));

        var act = () => resolver.ResolveKeyId(string.Empty);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("tenantId");
    }

    #endregion

    #region MessageEncryptionHealthCheck

    [Fact]
    public void MessageEncryptionHealthCheck_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new MessageEncryptionHealthCheck(
            null!,
            NullLogger<MessageEncryptionHealthCheck>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void MessageEncryptionHealthCheck_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new MessageEncryptionHealthCheck(
            Substitute.For<IServiceProvider>(),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region EncryptedPayloadFormatter

    [Fact]
    public void EncryptedPayloadFormatter_Format_NullPayload_ThrowsArgumentNullException()
    {
        var act = () => EncryptedPayloadFormatter.Format(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("payload");
    }

    #endregion
}
