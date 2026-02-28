using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using FluentAssertions;
using LanguageExt;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="AnonymizationErrors"/> factory methods and error code constants.
/// </summary>
public class AnonymizationErrorsTests
{
    #region KeyNotFound Tests

    [Fact]
    public void KeyNotFound_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.KeyNotFound("key-001");
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.KeyNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void KeyNotFound_ShouldIncludeKeyIdInMessage()
    {
        var error = AnonymizationErrors.KeyNotFound("key-001");
        error.Message.Should().Contain("key-001");
    }

    #endregion

    #region KeyRotationFailed Tests

    [Fact]
    public void KeyRotationFailed_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.KeyRotationFailed("key-001", "Provider unavailable");
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.KeyRotationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void KeyRotationFailed_ShouldIncludeKeyIdAndReasonInMessage()
    {
        var error = AnonymizationErrors.KeyRotationFailed("key-001", "Provider unavailable");
        error.Message.Should().Contain("key-001");
        error.Message.Should().Contain("Provider unavailable");
    }

    #endregion

    #region NoActiveKey Tests

    [Fact]
    public void NoActiveKey_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.NoActiveKey();
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.NoActiveKeyCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void NoActiveKey_ShouldHaveDescriptiveMessage()
    {
        var error = AnonymizationErrors.NoActiveKey();
        error.Message.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region EncryptionFailed Tests

    [Fact]
    public void EncryptionFailed_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.EncryptionFailed("key-002");
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.EncryptionFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void EncryptionFailed_ShouldIncludeKeyIdInMessage()
    {
        var error = AnonymizationErrors.EncryptionFailed("key-002");
        error.Message.Should().Contain("key-002");
    }

    [Fact]
    public void EncryptionFailed_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Bad padding");
        var error = AnonymizationErrors.EncryptionFailed("key-002", innerException);
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.EncryptionFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region DecryptionFailed Tests

    [Fact]
    public void DecryptionFailed_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.DecryptionFailed("key-003");
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.DecryptionFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void DecryptionFailed_ShouldIncludeKeyIdInMessage()
    {
        var error = AnonymizationErrors.DecryptionFailed("key-003");
        error.Message.Should().Contain("key-003");
    }

    [Fact]
    public void DecryptionFailed_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Tag mismatch");
        var error = AnonymizationErrors.DecryptionFailed("key-003", innerException);
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.DecryptionFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region TokenNotFound Tests

    [Fact]
    public void TokenNotFound_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.TokenNotFound("tok_abc123");
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.TokenNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void TokenNotFound_ShouldIncludeTokenInMessage()
    {
        var error = AnonymizationErrors.TokenNotFound("tok_abc123");
        error.Message.Should().Contain("tok_abc123");
    }

    #endregion

    #region TokenizationFailed Tests

    [Fact]
    public void TokenizationFailed_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.TokenizationFailed("Hash collision detected");
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.TokenizationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void TokenizationFailed_ShouldIncludeInputMessageInMessage()
    {
        var error = AnonymizationErrors.TokenizationFailed("Hash collision detected");
        error.Message.Should().Contain("Hash collision detected");
    }

    [Fact]
    public void TokenizationFailed_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Store unavailable");
        var error = AnonymizationErrors.TokenizationFailed("Hash collision detected", innerException);
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.TokenizationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region TechniqueNotApplicable Tests

    [Fact]
    public void TechniqueNotApplicable_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.TechniqueNotApplicable(
            AnonymizationTechnique.Generalization, "Email", typeof(string));
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.TechniqueNotApplicableCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void TechniqueNotApplicable_ShouldIncludeFieldNameInMessage()
    {
        var error = AnonymizationErrors.TechniqueNotApplicable(
            AnonymizationTechnique.Perturbation, "Email", typeof(string));
        error.Message.Should().Contain("Email");
    }

    #endregion

    #region TechniqueNotRegistered Tests

    [Fact]
    public void TechniqueNotRegistered_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.TechniqueNotRegistered(AnonymizationTechnique.KAnonymity);
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.TechniqueNotRegisteredCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void TechniqueNotRegistered_ShouldIncludeTechniqueInMessage()
    {
        var error = AnonymizationErrors.TechniqueNotRegistered(AnonymizationTechnique.KAnonymity);
        error.Message.Should().Contain("KAnonymity");
    }

    #endregion

    #region AnonymizationFailed Tests

    [Fact]
    public void AnonymizationFailed_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.AnonymizationFailed("Name", "Null value not supported");
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.AnonymizationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void AnonymizationFailed_ShouldIncludeFieldNameInMessage()
    {
        var error = AnonymizationErrors.AnonymizationFailed("Name", "Null value not supported");
        error.Message.Should().Contain("Name");
    }

    [Fact]
    public void AnonymizationFailed_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Technique error");
        var error = AnonymizationErrors.AnonymizationFailed("Name", "Null value not supported", innerException);
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.AnonymizationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region PseudonymizationFailed Tests

    [Fact]
    public void PseudonymizationFailed_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.PseudonymizationFailed("Key expired");
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.PseudonymizationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void PseudonymizationFailed_ShouldIncludeInputMessageInMessage()
    {
        var error = AnonymizationErrors.PseudonymizationFailed("Key expired");
        error.Message.Should().Contain("Key expired");
    }

    [Fact]
    public void PseudonymizationFailed_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("HMAC failure");
        var error = AnonymizationErrors.PseudonymizationFailed("Key expired", innerException);
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.PseudonymizationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region DepseudonymizationFailed Tests

    [Fact]
    public void DepseudonymizationFailed_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.DepseudonymizationFailed("HMAC-based pseudonym cannot be reversed");
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.DepseudonymizationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void DepseudonymizationFailed_ShouldIncludeInputMessageInMessage()
    {
        var error = AnonymizationErrors.DepseudonymizationFailed("HMAC-based pseudonym cannot be reversed");
        error.Message.Should().Contain("HMAC-based pseudonym cannot be reversed");
    }

    #endregion

    #region RiskAssessmentFailed Tests

    [Fact]
    public void RiskAssessmentFailed_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.RiskAssessmentFailed("Dataset too small for k-anonymity");
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.RiskAssessmentFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void RiskAssessmentFailed_ShouldIncludeInputMessageInMessage()
    {
        var error = AnonymizationErrors.RiskAssessmentFailed("Dataset too small for k-anonymity");
        error.Message.Should().Contain("Dataset too small for k-anonymity");
    }

    [Fact]
    public void RiskAssessmentFailed_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Metric calculation overflow");
        var error = AnonymizationErrors.RiskAssessmentFailed("Dataset too small for k-anonymity", innerException);
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.RiskAssessmentFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region StoreError Tests

    [Fact]
    public void StoreError_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.StoreError("AddEntry", "Connection timeout");
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void StoreError_ShouldIncludeOperationInMessage()
    {
        var error = AnonymizationErrors.StoreError("AddEntry", "Connection timeout");
        error.Message.Should().Contain("AddEntry");
    }

    [Fact]
    public void StoreError_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Deadlock");
        var error = AnonymizationErrors.StoreError("AddEntry", "Connection timeout", innerException);
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region InvalidParameter Tests

    [Fact]
    public void InvalidParameter_ShouldReturnCorrectCode()
    {
        var error = AnonymizationErrors.InvalidParameter("granularity", "Must be greater than zero");
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.InvalidParameterCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void InvalidParameter_ShouldIncludeParameterNameInMessage()
    {
        var error = AnonymizationErrors.InvalidParameter("granularity", "Must be greater than zero");
        error.Message.Should().Contain("granularity");
    }

    #endregion

    #region Error Code Constants Tests

    [Theory]
    [InlineData(nameof(AnonymizationErrors.KeyNotFoundCode), "anonymization.key_not_found")]
    [InlineData(nameof(AnonymizationErrors.KeyRotationFailedCode), "anonymization.key_rotation_failed")]
    [InlineData(nameof(AnonymizationErrors.NoActiveKeyCode), "anonymization.no_active_key")]
    [InlineData(nameof(AnonymizationErrors.EncryptionFailedCode), "anonymization.encryption_failed")]
    [InlineData(nameof(AnonymizationErrors.DecryptionFailedCode), "anonymization.decryption_failed")]
    [InlineData(nameof(AnonymizationErrors.TokenNotFoundCode), "anonymization.token_not_found")]
    [InlineData(nameof(AnonymizationErrors.TokenizationFailedCode), "anonymization.tokenization_failed")]
    [InlineData(nameof(AnonymizationErrors.TechniqueNotApplicableCode), "anonymization.technique_not_applicable")]
    [InlineData(nameof(AnonymizationErrors.TechniqueNotRegisteredCode), "anonymization.technique_not_registered")]
    [InlineData(nameof(AnonymizationErrors.AnonymizationFailedCode), "anonymization.anonymization_failed")]
    [InlineData(nameof(AnonymizationErrors.PseudonymizationFailedCode), "anonymization.pseudonymization_failed")]
    [InlineData(nameof(AnonymizationErrors.DepseudonymizationFailedCode), "anonymization.depseudonymization_failed")]
    [InlineData(nameof(AnonymizationErrors.RiskAssessmentFailedCode), "anonymization.risk_assessment_failed")]
    [InlineData(nameof(AnonymizationErrors.StoreErrorCode), "anonymization.store_error")]
    [InlineData(nameof(AnonymizationErrors.InvalidParameterCode), "anonymization.invalid_parameter")]
    public void ErrorCodeConstant_ShouldHaveCorrectValue(string constantName, string expectedValue)
    {
        var actualValue = typeof(AnonymizationErrors)
            .GetField(constantName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
            .GetValue(null) as string;

        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void ErrorCodes_ShouldFollowAnonymizationConvention()
    {
        AnonymizationErrors.KeyNotFoundCode.Should().StartWith("anonymization.");
        AnonymizationErrors.KeyRotationFailedCode.Should().StartWith("anonymization.");
        AnonymizationErrors.NoActiveKeyCode.Should().StartWith("anonymization.");
        AnonymizationErrors.EncryptionFailedCode.Should().StartWith("anonymization.");
        AnonymizationErrors.DecryptionFailedCode.Should().StartWith("anonymization.");
        AnonymizationErrors.TokenNotFoundCode.Should().StartWith("anonymization.");
        AnonymizationErrors.TokenizationFailedCode.Should().StartWith("anonymization.");
        AnonymizationErrors.TechniqueNotApplicableCode.Should().StartWith("anonymization.");
        AnonymizationErrors.TechniqueNotRegisteredCode.Should().StartWith("anonymization.");
        AnonymizationErrors.AnonymizationFailedCode.Should().StartWith("anonymization.");
        AnonymizationErrors.PseudonymizationFailedCode.Should().StartWith("anonymization.");
        AnonymizationErrors.DepseudonymizationFailedCode.Should().StartWith("anonymization.");
        AnonymizationErrors.RiskAssessmentFailedCode.Should().StartWith("anonymization.");
        AnonymizationErrors.StoreErrorCode.Should().StartWith("anonymization.");
        AnonymizationErrors.InvalidParameterCode.Should().StartWith("anonymization.");
    }

    [Fact]
    public void ErrorCodes_ShouldAllBeUnique()
    {
        var codes = new[]
        {
            AnonymizationErrors.KeyNotFoundCode,
            AnonymizationErrors.KeyRotationFailedCode,
            AnonymizationErrors.NoActiveKeyCode,
            AnonymizationErrors.EncryptionFailedCode,
            AnonymizationErrors.DecryptionFailedCode,
            AnonymizationErrors.TokenNotFoundCode,
            AnonymizationErrors.TokenizationFailedCode,
            AnonymizationErrors.TechniqueNotApplicableCode,
            AnonymizationErrors.TechniqueNotRegisteredCode,
            AnonymizationErrors.AnonymizationFailedCode,
            AnonymizationErrors.PseudonymizationFailedCode,
            AnonymizationErrors.DepseudonymizationFailedCode,
            AnonymizationErrors.RiskAssessmentFailedCode,
            AnonymizationErrors.StoreErrorCode,
            AnonymizationErrors.InvalidParameterCode
        };

        codes.Should().OnlyHaveUniqueItems();
    }

    #endregion
}
