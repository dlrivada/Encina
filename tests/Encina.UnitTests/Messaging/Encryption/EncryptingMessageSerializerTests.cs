#pragma warning disable CA2012 // ValueTask consumed by NSubstitute mock setup

using System.Collections.Immutable;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Attributes;
using Encina.Messaging.Encryption.Model;
using Encina.Messaging.Encryption.Serialization;
using Encina.Messaging.Serialization;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Encryption;

public class EncryptingMessageSerializerTests : IDisposable
{
    private readonly IMessageSerializer _inner = Substitute.For<IMessageSerializer>();
    private readonly IMessageEncryptionProvider _provider = Substitute.For<IMessageEncryptionProvider>();

    public EncryptingMessageSerializerTests()
    {
        EncryptedMessageAttributeCache.ClearCache();
    }

    public void Dispose()
    {
        EncryptedMessageAttributeCache.ClearCache();
    }

    [Fact]
    public void Serialize_EncryptionDisabled_ReturnsPlainJson()
    {
        // Arrange
        var options = CreateOptions(enabled: false);
        var serializer = CreateSerializer(options);
        _inner.Serialize(Arg.Any<TestMessage>()).Returns("{\"Name\":\"test\"}");

        // Act
        var result = serializer.Serialize(new TestMessage { Name = "test" });

        // Assert
        result.ShouldBe("{\"Name\":\"test\"}");
        _ = _provider.DidNotReceive().EncryptAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Serialize_NotRequired_ReturnsPlainJson()
    {
        // Arrange — EncryptAllMessages=false and no [EncryptedMessage] attribute
        var options = CreateOptions(enabled: true, encryptAll: false);
        var serializer = CreateSerializer(options);
        _inner.Serialize(Arg.Any<TestMessage>()).Returns("{\"Name\":\"test\"}");

        // Act
        var result = serializer.Serialize(new TestMessage { Name = "test" });

        // Assert
        result.ShouldBe("{\"Name\":\"test\"}");
    }

    [Fact]
    public void Serialize_EncryptAllMessages_EncryptsAndFormats()
    {
        // Arrange
        var options = CreateOptions(enabled: true, encryptAll: true);
        var serializer = CreateSerializer(options);
        _inner.Serialize(Arg.Any<TestMessage>()).Returns("{\"Name\":\"test\"}");

        var encryptedPayload = new EncryptedPayload
        {
            Ciphertext = ImmutableArray.Create<byte>(1, 2, 3),
            KeyId = "test-key",
            Algorithm = "AES-256-GCM",
            Nonce = ImmutableArray.Create<byte>(10, 20, 30),
            Tag = ImmutableArray.Create<byte>(40, 50, 60),
            Version = 1
        };

        _provider.EncryptAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, EncryptedPayload>(encryptedPayload));

        // Act
        var result = serializer.Serialize(new TestMessage { Name = "test" });

        // Assert
        result.ShouldStartWith("ENC:v1:test-key:AES-256-GCM:");
    }

    [Fact]
    public void Serialize_AttributeMarkedMessage_EncryptsRegardlessOfGlobalSetting()
    {
        // Arrange — EncryptAllMessages=false but message has [EncryptedMessage]
        var options = CreateOptions(enabled: true, encryptAll: false);
        var serializer = CreateSerializer(options);
        _inner.Serialize(Arg.Any<EncryptedTestMessage>()).Returns("{\"Data\":\"secret\"}");

        var encryptedPayload = new EncryptedPayload
        {
            Ciphertext = ImmutableArray.Create<byte>(1, 2, 3),
            KeyId = "attr-key",
            Algorithm = "AES-256-GCM",
            Nonce = ImmutableArray.Create<byte>(10, 20, 30),
            Tag = ImmutableArray.Create<byte>(40, 50, 60),
            Version = 1
        };

        _provider.EncryptAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, EncryptedPayload>(encryptedPayload));

        // Act
        var result = serializer.Serialize(new EncryptedTestMessage { Data = "secret" });

        // Assert
        result.ShouldStartWith("ENC:");
    }

    [Fact]
    public void Serialize_EncryptionFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = CreateOptions(enabled: true, encryptAll: true);
        var serializer = CreateSerializer(options);
        _inner.Serialize(Arg.Any<TestMessage>()).Returns("{\"Name\":\"test\"}");

        var error = MessageEncryptionErrors.EncryptionFailed("TestMessage");
        _provider.EncryptAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, EncryptedPayload>(error));

        // Act
        Action act = () => serializer.Serialize(new TestMessage { Name = "test" });

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("TestMessage");
    }

    [Fact]
    public void Deserialize_PlainJson_DelegatesToInner()
    {
        // Arrange
        var options = CreateOptions();
        var serializer = CreateSerializer(options);
        _inner.Deserialize<TestMessage>("{\"Name\":\"test\"}").Returns(new TestMessage { Name = "test" });

        // Act
        var result = serializer.Deserialize<TestMessage>("{\"Name\":\"test\"}");

        // Assert
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("test");
    }

    [Fact]
    public void Deserialize_EncryptedPayload_DecryptsAndDeserializes()
    {
        // Arrange
        var options = CreateOptions();
        var serializer = CreateSerializer(options);

        var payload = new EncryptedPayload
        {
            Ciphertext = ImmutableArray.Create<byte>(1, 2, 3),
            KeyId = "my-key",
            Algorithm = "AES-256-GCM",
            Nonce = ImmutableArray.Create<byte>(10, 20, 30),
            Tag = ImmutableArray.Create<byte>(40, 50, 60),
            Version = 1
        };
        var encryptedString = EncryptedPayloadFormatter.Format(payload);

        var jsonBytes = System.Text.Encoding.UTF8.GetBytes("{\"Name\":\"decrypted\"}");
        _provider.DecryptAsync(
            Arg.Any<EncryptedPayload>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ImmutableArray<byte>>(ImmutableArray.Create(jsonBytes)));

        _inner.Deserialize<TestMessage>("{\"Name\":\"decrypted\"}").Returns(new TestMessage { Name = "decrypted" });

        // Act
        var result = serializer.Deserialize<TestMessage>(encryptedString);

        // Assert
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("decrypted");
    }

    [Fact]
    public void Deserialize_DecryptionFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = CreateOptions();
        var serializer = CreateSerializer(options);

        var payload = new EncryptedPayload
        {
            Ciphertext = ImmutableArray.Create<byte>(1, 2, 3),
            KeyId = "bad-key",
            Algorithm = "AES-256-GCM",
            Nonce = ImmutableArray.Create<byte>(10, 20, 30),
            Tag = ImmutableArray.Create<byte>(40, 50, 60),
            Version = 1
        };
        var encryptedString = EncryptedPayloadFormatter.Format(payload);

        var error = MessageEncryptionErrors.DecryptionFailed("bad-key");
        _provider.DecryptAsync(
            Arg.Any<EncryptedPayload>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ImmutableArray<byte>>(error));

        // Act
        Action act = () => serializer.Deserialize<TestMessage>(encryptedString);

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("bad-key");
    }

    [Fact]
    public void DeserializeWithType_PlainJson_DelegatesToInner()
    {
        // Arrange
        var options = CreateOptions();
        var serializer = CreateSerializer(options);
        _inner.Deserialize("{\"Name\":\"test\"}", typeof(TestMessage)).Returns(new TestMessage { Name = "test" });

        // Act
        var result = serializer.Deserialize("{\"Name\":\"test\"}", typeof(TestMessage));

        // Assert
        result.ShouldNotBeNull();
        ((TestMessage)result!).Name.ShouldBe("test");
    }

    private EncryptingMessageSerializer CreateSerializer(IOptions<MessageEncryptionOptions>? options = null)
    {
        options ??= CreateOptions();
        return new EncryptingMessageSerializer(
            _inner,
            _provider,
            options,
            NullLogger<EncryptingMessageSerializer>.Instance);
    }

    private static IOptions<MessageEncryptionOptions> CreateOptions(
        bool enabled = true,
        bool encryptAll = false,
        string? defaultKeyId = null)
    {
        return Options.Create(new MessageEncryptionOptions
        {
            Enabled = enabled,
            EncryptAllMessages = encryptAll,
            DefaultKeyId = defaultKeyId
        });
    }

    public class TestMessage
    {
        public string? Name { get; set; }
    }

    [EncryptedMessage]
    public class EncryptedTestMessage
    {
        public string? Data { get; set; }
    }
}
