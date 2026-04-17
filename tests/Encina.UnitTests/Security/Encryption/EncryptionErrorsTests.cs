using Encina.Security.Encryption;
using Shouldly;

namespace Encina.UnitTests.Security.Encryption;

public sealed class EncryptionErrorsTests
{
    [Fact]
    public void KeyNotFound_ReturnsErrorWithCorrectCode()
    {
        var error = EncryptionErrors.KeyNotFound("key-123");

        error.GetCode().IfNone("").ShouldBe(EncryptionErrors.KeyNotFoundCode);
        error.Message.ShouldContain("key-123");
    }

    [Fact]
    public void KeyNotFound_ReturnsErrorWithKeyIdInDetails()
    {
        var error = EncryptionErrors.KeyNotFound("key-456");

        var details = error.GetDetails();
        details.ShouldContainKey("keyId");
        details["keyId"].ShouldBe("key-456");
    }

    [Fact]
    public void DecryptionFailed_WithPropertyName_IncludesPropertyInMessage()
    {
        var error = EncryptionErrors.DecryptionFailed("key-1", propertyName: "Email");

        error.GetCode().IfNone("").ShouldBe(EncryptionErrors.DecryptionFailedCode);
        error.Message.ShouldContain("Email");
        error.Message.ShouldContain("key-1");
    }

    [Fact]
    public void DecryptionFailed_WithoutPropertyName_DoesNotContainProperty()
    {
        var error = EncryptionErrors.DecryptionFailed("key-1");

        error.GetCode().IfNone("").ShouldBe(EncryptionErrors.DecryptionFailedCode);
        error.Message.ShouldContain("key-1");
    }

    [Fact]
    public void DecryptionFailed_WithException_PreservesException()
    {
        var ex = new InvalidOperationException("test error");
        var error = EncryptionErrors.DecryptionFailed("key-1", exception: ex);

        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void InvalidCiphertext_WithPropertyName_IncludesPropertyInMessage()
    {
        var error = EncryptionErrors.InvalidCiphertext(propertyName: "SSN");

        error.GetCode().IfNone("").ShouldBe(EncryptionErrors.InvalidCiphertextCode);
        error.Message.ShouldContain("SSN");
    }

    [Fact]
    public void InvalidCiphertext_WithoutPropertyName_HasGenericMessage()
    {
        var error = EncryptionErrors.InvalidCiphertext();

        error.GetCode().IfNone("").ShouldBe(EncryptionErrors.InvalidCiphertextCode);
        error.Message.ShouldContain("Invalid");
    }

    [Fact]
    public void AlgorithmNotSupported_IncludesAlgorithmInMessage()
    {
        var error = EncryptionErrors.AlgorithmNotSupported((EncryptionAlgorithm)99);

        error.GetCode().IfNone("").ShouldBe(EncryptionErrors.AlgorithmNotSupportedCode);
        error.Message.ShouldContain("99");
    }

    [Fact]
    public void KeyRotationFailed_ReturnsCorrectCode()
    {
        var error = EncryptionErrors.KeyRotationFailed();

        error.GetCode().IfNone("").ShouldBe(EncryptionErrors.KeyRotationFailedCode);
    }

    [Fact]
    public void KeyRotationFailed_WithException_PreservesException()
    {
        var ex = new InvalidOperationException("rotation error");
        var error = EncryptionErrors.KeyRotationFailed(ex);

        error.Exception.IsSome.ShouldBeTrue();
    }

    [Theory]
    [InlineData(EncryptionErrors.KeyNotFoundCode)]
    [InlineData(EncryptionErrors.DecryptionFailedCode)]
    [InlineData(EncryptionErrors.InvalidCiphertextCode)]
    [InlineData(EncryptionErrors.AlgorithmNotSupportedCode)]
    [InlineData(EncryptionErrors.KeyRotationFailedCode)]
    public void AllErrorCodes_StartWithEncryptionPrefix(string code)
    {
        code.ShouldStartWith("encryption.");
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
            details.ShouldContainKey("stage");
            details["stage"].ShouldBe("encryption");
        }
    }
}
