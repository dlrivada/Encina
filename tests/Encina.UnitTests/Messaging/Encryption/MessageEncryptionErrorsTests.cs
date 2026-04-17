using Encina.Messaging.Encryption;
using Shouldly;

namespace Encina.UnitTests.Messaging.Encryption;

public class MessageEncryptionErrorsTests
{
    [Fact]
    public void EncryptionFailed_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.EncryptionFailed("MyMessage");

        error.GetCode().IfNone(string.Empty).ShouldBe(MessageEncryptionErrors.EncryptionFailedCode);
        error.Message.ShouldContain("MyMessage");
    }

    [Fact]
    public void DecryptionFailed_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.DecryptionFailed("key-1", "MyMessage");

        error.GetCode().IfNone(string.Empty).ShouldBe(MessageEncryptionErrors.DecryptionFailedCode);
        error.Message.ShouldContain("key-1");
    }

    [Fact]
    public void KeyNotFound_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.KeyNotFound("missing-key");

        error.GetCode().IfNone(string.Empty).ShouldBe(MessageEncryptionErrors.KeyNotFoundCode);
        error.Message.ShouldContain("missing-key");
    }

    [Fact]
    public void InvalidPayload_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.InvalidPayload("bad format");

        error.GetCode().IfNone(string.Empty).ShouldBe(MessageEncryptionErrors.InvalidPayloadCode);
        error.Message.ShouldContain("bad format");
    }

    [Fact]
    public void ProviderUnavailable_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.ProviderUnavailable("connection refused");

        error.GetCode().IfNone(string.Empty).ShouldBe(MessageEncryptionErrors.ProviderUnavailableCode);
        error.Message.ShouldContain("connection refused");
    }

    [Fact]
    public void SerializationFailed_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.SerializationFailed("OrderPlaced");

        error.GetCode().IfNone(string.Empty).ShouldBe(MessageEncryptionErrors.SerializationFailedCode);
    }

    [Fact]
    public void DeserializationFailed_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.DeserializationFailed("OrderPlaced");

        error.GetCode().IfNone(string.Empty).ShouldBe(MessageEncryptionErrors.DeserializationFailedCode);
    }

    [Fact]
    public void TenantKeyResolutionFailed_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.TenantKeyResolutionFailed("tenant-1");

        error.GetCode().IfNone(string.Empty).ShouldBe(MessageEncryptionErrors.TenantKeyResolutionFailedCode);
        error.Message.ShouldContain("tenant-1");
    }

    [Fact]
    public void UnsupportedVersion_ReturnsErrorWithCorrectCode()
    {
        var error = MessageEncryptionErrors.UnsupportedVersion(99);

        error.GetCode().IfNone(string.Empty).ShouldBe(MessageEncryptionErrors.UnsupportedVersionCode);
        error.Message.ShouldContain("99");
    }

    [Fact]
    public void AllErrorCodes_HaveCorrectPrefix()
    {
        MessageEncryptionErrors.EncryptionFailedCode.ShouldStartWith("msg_encryption.");
        MessageEncryptionErrors.DecryptionFailedCode.ShouldStartWith("msg_encryption.");
        MessageEncryptionErrors.KeyNotFoundCode.ShouldStartWith("msg_encryption.");
        MessageEncryptionErrors.InvalidPayloadCode.ShouldStartWith("msg_encryption.");
        MessageEncryptionErrors.ProviderUnavailableCode.ShouldStartWith("msg_encryption.");
        MessageEncryptionErrors.SerializationFailedCode.ShouldStartWith("msg_encryption.");
        MessageEncryptionErrors.DeserializationFailedCode.ShouldStartWith("msg_encryption.");
        MessageEncryptionErrors.TenantKeyResolutionFailedCode.ShouldStartWith("msg_encryption.");
        MessageEncryptionErrors.UnsupportedVersionCode.ShouldStartWith("msg_encryption.");
    }
}
