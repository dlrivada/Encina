using Encina.Security.Encryption;
using FluentAssertions;

namespace Encina.UnitTests.Security.Encryption;

public sealed class EncryptionErrorsTests
{
    [Fact]
    public void KeyNotFound_ReturnsErrorWithCorrectCode()
    {
        var error = EncryptionErrors.KeyNotFound("key-123");

        error.GetCode().IfNone("").Should().Be(EncryptionErrors.KeyNotFoundCode);
        error.Message.Should().Contain("key-123");
    }

    [Fact]
    public void KeyNotFound_ReturnsErrorWithKeyIdInDetails()
    {
        var error = EncryptionErrors.KeyNotFound("key-456");

        var details = error.GetDetails();
        details.Should().ContainKey("keyId");
        details["keyId"].Should().Be("key-456");
    }

    [Fact]
    public void DecryptionFailed_WithPropertyName_IncludesPropertyInMessage()
    {
        var error = EncryptionErrors.DecryptionFailed("key-1", propertyName: "Email");

        error.GetCode().IfNone("").Should().Be(EncryptionErrors.DecryptionFailedCode);
        error.Message.Should().Contain("Email");
        error.Message.Should().Contain("key-1");
    }

    [Fact]
    public void DecryptionFailed_WithoutPropertyName_DoesNotContainProperty()
    {
        var error = EncryptionErrors.DecryptionFailed("key-1");

        error.GetCode().IfNone("").Should().Be(EncryptionErrors.DecryptionFailedCode);
        error.Message.Should().Contain("key-1");
    }

    [Fact]
    public void DecryptionFailed_WithException_PreservesException()
    {
        var ex = new InvalidOperationException("test error");
        var error = EncryptionErrors.DecryptionFailed("key-1", exception: ex);

        error.Exception.IsSome.Should().BeTrue();
    }

    [Fact]
    public void InvalidCiphertext_WithPropertyName_IncludesPropertyInMessage()
    {
        var error = EncryptionErrors.InvalidCiphertext(propertyName: "SSN");

        error.GetCode().IfNone("").Should().Be(EncryptionErrors.InvalidCiphertextCode);
        error.Message.Should().Contain("SSN");
    }

    [Fact]
    public void InvalidCiphertext_WithoutPropertyName_HasGenericMessage()
    {
        var error = EncryptionErrors.InvalidCiphertext();

        error.GetCode().IfNone("").Should().Be(EncryptionErrors.InvalidCiphertextCode);
        error.Message.Should().Contain("Invalid");
    }

    [Fact]
    public void AlgorithmNotSupported_IncludesAlgorithmInMessage()
    {
        var error = EncryptionErrors.AlgorithmNotSupported((EncryptionAlgorithm)99);

        error.GetCode().IfNone("").Should().Be(EncryptionErrors.AlgorithmNotSupportedCode);
        error.Message.Should().Contain("99");
    }

    [Fact]
    public void KeyRotationFailed_ReturnsCorrectCode()
    {
        var error = EncryptionErrors.KeyRotationFailed();

        error.GetCode().IfNone("").Should().Be(EncryptionErrors.KeyRotationFailedCode);
    }

    [Fact]
    public void KeyRotationFailed_WithException_PreservesException()
    {
        var ex = new InvalidOperationException("rotation error");
        var error = EncryptionErrors.KeyRotationFailed(ex);

        error.Exception.IsSome.Should().BeTrue();
    }

    [Theory]
    [InlineData(EncryptionErrors.KeyNotFoundCode)]
    [InlineData(EncryptionErrors.DecryptionFailedCode)]
    [InlineData(EncryptionErrors.InvalidCiphertextCode)]
    [InlineData(EncryptionErrors.AlgorithmNotSupportedCode)]
    [InlineData(EncryptionErrors.KeyRotationFailedCode)]
    public void AllErrorCodes_StartWithEncryptionPrefix(string code)
    {
        code.Should().StartWith("encryption.");
    }

    [Fact]
    public void AllErrors_ContainStageMetadata()
    {
        var errors = new[]
        {
            EncryptionErrors.KeyNotFound("k"),
            EncryptionErrors.DecryptionFailed("k"),
            EncryptionErrors.InvalidCiphertext(),
            EncryptionErrors.AlgorithmNotSupported(EncryptionAlgorithm.Aes256Gcm),
            EncryptionErrors.KeyRotationFailed()
        };

        foreach (var error in errors)
        {
            var details = error.GetDetails();
            details.Should().ContainKey("stage");
            details["stage"].Should().Be("encryption");
        }
    }
}
