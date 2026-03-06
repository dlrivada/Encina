using Encina;
using Encina.Marten.GDPR;

using Shouldly;

namespace Encina.UnitTests.Marten.GDPR;

public sealed class CryptoShreddingErrorsTests
{
    [Fact]
    public void SubjectForgotten_ReturnsCorrectCode()
    {
        // Act
        var error = CryptoShreddingErrors.SubjectForgotten("user-42");

        // Assert
        error.GetEncinaCode().ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
        error.Message.ShouldContain("user-42");
    }

    [Fact]
    public void EncryptionFailed_ReturnsCorrectCode()
    {
        // Act
        var error = CryptoShreddingErrors.EncryptionFailed("user-42", "Email");

        // Assert
        error.GetEncinaCode().ShouldBe(CryptoShreddingErrors.EncryptionFailedCode);
        error.Message.ShouldContain("user-42");
        error.Message.ShouldContain("Email");
    }

    [Fact]
    public void EncryptionFailed_WithException_IncludesException()
    {
        // Arrange
        var ex = new InvalidOperationException("test");

        // Act
        var error = CryptoShreddingErrors.EncryptionFailed("user-1", "Name", ex);

        // Assert
        error.GetEncinaCode().ShouldBe(CryptoShreddingErrors.EncryptionFailedCode);
        error.Exception.ShouldBe(ex);
    }

    [Fact]
    public void DecryptionFailed_ReturnsCorrectCode()
    {
        // Act
        var error = CryptoShreddingErrors.DecryptionFailed("user-42", "Email");

        // Assert
        error.GetEncinaCode().ShouldBe(CryptoShreddingErrors.DecryptionFailedCode);
        error.Message.ShouldContain("Email");
    }

    [Fact]
    public void KeyRotationFailed_ReturnsCorrectCode()
    {
        // Act
        var error = CryptoShreddingErrors.KeyRotationFailed("user-42");

        // Assert
        error.GetEncinaCode().ShouldBe(CryptoShreddingErrors.KeyRotationFailedCode);
        error.Message.ShouldContain("user-42");
    }

    [Fact]
    public void KeyStoreError_ReturnsCorrectCode()
    {
        // Act
        var error = CryptoShreddingErrors.KeyStoreError("GetKey");

        // Assert
        error.GetEncinaCode().ShouldBe(CryptoShreddingErrors.KeyStoreErrorCode);
        error.Message.ShouldContain("GetKey");
    }

    [Fact]
    public void InvalidSubjectId_ReturnsCorrectCode()
    {
        // Act
        var error = CryptoShreddingErrors.InvalidSubjectId("bad-id");

        // Assert
        error.GetEncinaCode().ShouldBe(CryptoShreddingErrors.InvalidSubjectIdCode);
        error.Message.ShouldContain("bad-id");
    }

    [Fact]
    public void InvalidSubjectId_NullInput_HandlesGracefully()
    {
        // Act
        var error = CryptoShreddingErrors.InvalidSubjectId(null);

        // Assert
        error.GetEncinaCode().ShouldBe(CryptoShreddingErrors.InvalidSubjectIdCode);
        error.Message.ShouldContain("(null)");
    }

    [Fact]
    public void KeyAlreadyExists_ReturnsCorrectCode()
    {
        // Act
        var error = CryptoShreddingErrors.KeyAlreadyExists("user-42");

        // Assert
        error.GetEncinaCode().ShouldBe(CryptoShreddingErrors.KeyAlreadyExistsCode);
        error.Message.ShouldContain("user-42");
    }

    [Fact]
    public void SerializationError_ReturnsCorrectCode()
    {
        // Act
        var error = CryptoShreddingErrors.SerializationError(typeof(string));

        // Assert
        error.GetEncinaCode().ShouldBe(CryptoShreddingErrors.SerializationErrorCode);
        error.Message.ShouldContain("String");
    }

    [Fact]
    public void AttributeMisconfigured_ReturnsCorrectCode()
    {
        // Act
        var error = CryptoShreddingErrors.AttributeMisconfigured(
            "Email", typeof(string), "Missing [PersonalData]");

        // Assert
        error.GetEncinaCode().ShouldBe(CryptoShreddingErrors.AttributeMisconfiguredCode);
        error.Message.ShouldContain("Email");
        error.Message.ShouldContain("Missing [PersonalData]");
    }

    [Fact]
    public void AllErrorCodes_AreUnique()
    {
        // Arrange
        var codes = new[]
        {
            CryptoShreddingErrors.SubjectForgottenCode,
            CryptoShreddingErrors.EncryptionFailedCode,
            CryptoShreddingErrors.DecryptionFailedCode,
            CryptoShreddingErrors.KeyRotationFailedCode,
            CryptoShreddingErrors.KeyStoreErrorCode,
            CryptoShreddingErrors.InvalidSubjectIdCode,
            CryptoShreddingErrors.KeyAlreadyExistsCode,
            CryptoShreddingErrors.SerializationErrorCode,
            CryptoShreddingErrors.AttributeMisconfiguredCode
        };

        // Assert
        codes.Distinct().Count().ShouldBe(codes.Length);
    }

    [Fact]
    public void AllErrorCodes_StartWithCryptoPrefix()
    {
        // Arrange
        var codes = new[]
        {
            CryptoShreddingErrors.SubjectForgottenCode,
            CryptoShreddingErrors.EncryptionFailedCode,
            CryptoShreddingErrors.DecryptionFailedCode,
            CryptoShreddingErrors.KeyRotationFailedCode,
            CryptoShreddingErrors.KeyStoreErrorCode,
            CryptoShreddingErrors.InvalidSubjectIdCode,
            CryptoShreddingErrors.KeyAlreadyExistsCode,
            CryptoShreddingErrors.SerializationErrorCode,
            CryptoShreddingErrors.AttributeMisconfiguredCode
        };

        // Assert
        codes.ShouldAllBe(c => c.StartsWith("crypto.", StringComparison.Ordinal));
    }
}
