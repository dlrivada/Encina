using Encina.Messaging.Encryption;
using FluentAssertions;

namespace Encina.UnitTests.Messaging.Encryption;

public class MessageEncryptionErrorsTests
{
    [Fact]
    public void EncryptionFailed_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.EncryptionFailed("MyMessage");

        error.GetCode().IfNone(string.Empty).Should().Be(MessageEncryptionErrors.EncryptionFailedCode);
        error.Message.Should().Contain("MyMessage");
    }

    [Fact]
    public void DecryptionFailed_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.DecryptionFailed("key-1", "MyMessage");

        error.GetCode().IfNone(string.Empty).Should().Be(MessageEncryptionErrors.DecryptionFailedCode);
        error.Message.Should().Contain("key-1");
    }

    [Fact]
    public void KeyNotFound_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.KeyNotFound("missing-key");

        error.GetCode().IfNone(string.Empty).Should().Be(MessageEncryptionErrors.KeyNotFoundCode);
        error.Message.Should().Contain("missing-key");
    }

    [Fact]
    public void InvalidPayload_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.InvalidPayload("bad format");

        error.GetCode().IfNone(string.Empty).Should().Be(MessageEncryptionErrors.InvalidPayloadCode);
        error.Message.Should().Contain("bad format");
    }

    [Fact]
    public void ProviderUnavailable_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.ProviderUnavailable("connection refused");

        error.GetCode().IfNone(string.Empty).Should().Be(MessageEncryptionErrors.ProviderUnavailableCode);
        error.Message.Should().Contain("connection refused");
    }

    [Fact]
    public void SerializationFailed_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.SerializationFailed("OrderPlaced");

        error.GetCode().IfNone(string.Empty).Should().Be(MessageEncryptionErrors.SerializationFailedCode);
    }

    [Fact]
    public void DeserializationFailed_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.DeserializationFailed("OrderPlaced");

        error.GetCode().IfNone(string.Empty).Should().Be(MessageEncryptionErrors.DeserializationFailedCode);
    }

    [Fact]
    public void TenantKeyResolutionFailed_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.TenantKeyResolutionFailed("tenant-1");

        error.GetCode().IfNone(string.Empty).Should().Be(MessageEncryptionErrors.TenantKeyResolutionFailedCode);
        error.Message.Should().Contain("tenant-1");
    }

    [Fact]
    public void UnsupportedVersion_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.UnsupportedVersion(99);

        error.GetCode().IfNone(string.Empty).Should().Be(MessageEncryptionErrors.UnsupportedVersionCode);
        error.Message.Should().Contain("99");
    }

    [Fact]
    public void AllErrorCodes_HaveCorrectPrefix()
    {
        MessageEncryptionErrors.EncryptionFailedCode.Should().StartWith("msg_encryption.");
        MessageEncryptionErrors.DecryptionFailedCode.Should().StartWith("msg_encryption.");
        MessageEncryptionErrors.KeyNotFoundCode.Should().StartWith("msg_encryption.");
        MessageEncryptionErrors.InvalidPayloadCode.Should().StartWith("msg_encryption.");
        MessageEncryptionErrors.ProviderUnavailableCode.Should().StartWith("msg_encryption.");
        MessageEncryptionErrors.SerializationFailedCode.Should().StartWith("msg_encryption.");
        MessageEncryptionErrors.DeserializationFailedCode.Should().StartWith("msg_encryption.");
        MessageEncryptionErrors.TenantKeyResolutionFailedCode.Should().StartWith("msg_encryption.");
        MessageEncryptionErrors.UnsupportedVersionCode.Should().StartWith("msg_encryption.");
    }
}
