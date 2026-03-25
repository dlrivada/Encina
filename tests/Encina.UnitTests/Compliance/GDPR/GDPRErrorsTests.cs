using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Export;
using FluentAssertions;
using LanguageExt;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="GDPRErrors"/> and <see cref="RoPAExportErrors"/>.
/// </summary>
public class GDPRErrorsTests
{
    [Fact]
    public void UnregisteredActivity_ShouldContainRequestTypeName()
    {
        // Act
        var error = GDPRErrors.UnregisteredActivity(typeof(string));

        // Assert
        error.Message.Should().Contain("String");
        error.Message.Should().Contain("Article 30");
    }

    [Fact]
    public void ComplianceValidationFailed_ShouldContainErrors()
    {
        // Act
        var error = GDPRErrors.ComplianceValidationFailed(
            typeof(string), ["Missing consent", "No lawful basis"]);

        // Assert
        error.Message.Should().Contain("String");
        error.Message.Should().Contain("Missing consent");
        error.Message.Should().Contain("No lawful basis");
    }

    [Fact]
    public void RegistryLookupFailed_ShouldContainInnerError()
    {
        // Arrange
        var inner = EncinaError.New("Database connection failed");

        // Act
        var error = GDPRErrors.RegistryLookupFailed(typeof(string), inner);

        // Assert
        error.Message.Should().Contain("String");
        error.Message.Should().Contain("Database connection failed");
    }

    [Fact]
    public void RoPAExportErrors_SerializationFailed_ShouldContainFormatAndReason()
    {
        // Act
        var error = RoPAExportErrors.SerializationFailed("JSON", "Invalid character");

        // Assert
        error.Message.Should().Contain("JSON");
        error.Message.Should().Contain("Invalid character");
    }

    // -- Error codes --

    [Fact]
    public void ErrorCodes_ShouldFollowConvention()
    {
        GDPRErrors.UnregisteredActivityCode.Should().StartWith("gdpr.");
        GDPRErrors.ComplianceValidationFailedCode.Should().StartWith("gdpr.");
        GDPRErrors.RegistryLookupFailedCode.Should().StartWith("gdpr.");
        RoPAExportErrors.SerializationFailedCode.Should().StartWith("gdpr.");
    }

    // -- Lawful Basis errors (Article 6) --

    [Fact]
    public void LawfulBasisNotDeclared_ShouldContainRequestTypeAndArticle()
    {
        var error = GDPRErrors.LawfulBasisNotDeclared(typeof(double));
        error.GetCode().IfNone("").Should().Be(GDPRErrors.LawfulBasisNotDeclaredCode);
        error.Message.Should().Contain("Double");
        error.Message.Should().Contain("Article 6(1)");
    }

    [Fact]
    public void ConsentNotFound_WithoutSubjectId_ShouldCreateError()
    {
        var error = GDPRErrors.ConsentNotFound(typeof(string));
        error.GetCode().IfNone("").Should().Be(GDPRErrors.ConsentNotFoundCode);
        error.Message.Should().Contain("String");
        error.Message.Should().Contain("Article 6(1)(a)");
    }

    [Fact]
    public void ConsentNotFound_WithSubjectId_ShouldCreateError()
    {
        var error = GDPRErrors.ConsentNotFound(typeof(string), "user-123");
        error.GetCode().IfNone("").Should().Be(GDPRErrors.ConsentNotFoundCode);
    }

    [Fact]
    public void LIANotFound_WithType_ShouldContainRequestType()
    {
        var error = GDPRErrors.LIANotFound(typeof(int));
        error.GetCode().IfNone("").Should().Be(GDPRErrors.LIANotFoundCode);
        error.Message.Should().Contain("Int32");
        error.Message.Should().Contain("Article 6(1)(f)");
    }

    [Fact]
    public void LIANotFound_WithReference_ShouldContainReference()
    {
        var error = GDPRErrors.LIANotFound("LIA-2024-001");
        error.GetCode().IfNone("").Should().Be(GDPRErrors.LIANotFoundCode);
        error.Message.Should().Contain("LIA-2024-001");
    }

    [Fact]
    public void LIANotApproved_WithType_ShouldContainReferenceAndType()
    {
        var error = GDPRErrors.LIANotApproved(typeof(string), "LIA-REF-42");
        error.GetCode().IfNone("").Should().Be(GDPRErrors.LIANotApprovedCode);
        error.Message.Should().Contain("LIA-REF-42");
        error.Message.Should().Contain("String");
    }

    [Fact]
    public void LIANotApproved_WithOutcome_ShouldContainOutcome()
    {
        var error = GDPRErrors.LIANotApproved("LIA-2024-002", LIAOutcome.RequiresReview);
        error.GetCode().IfNone("").Should().Be(GDPRErrors.LIANotApprovedCode);
        error.Message.Should().Contain("LIA-2024-002");
        error.Message.Should().Contain("RequiresReview");
    }

    [Fact]
    public void LIANotApproved_WithRejectedOutcome_ShouldIncludeOutcome()
    {
        var error = GDPRErrors.LIANotApproved("LIA-REJ", LIAOutcome.Rejected);
        error.GetCode().IfNone("").Should().Be(GDPRErrors.LIANotApprovedCode);
        error.Message.Should().Contain("Rejected");
    }

    // -- Store errors --

    [Fact]
    public void LawfulBasisStoreError_ShouldContainOperationAndMessage()
    {
        var error = GDPRErrors.LawfulBasisStoreError("Register", "DB down");
        error.GetCode().IfNone("").Should().Be(GDPRErrors.LawfulBasisStoreErrorCode);
        error.Message.Should().Contain("Register");
        error.Message.Should().Contain("DB down");
    }

    [Fact]
    public void LIAStoreError_ShouldContainOperationAndMessage()
    {
        var error = GDPRErrors.LIAStoreError("GetByReference", "Timeout");
        error.GetCode().IfNone("").Should().Be(GDPRErrors.LIAStoreErrorCode);
        error.Message.Should().Contain("GetByReference");
        error.Message.Should().Contain("Timeout");
    }

    [Fact]
    public void ConsentProviderNotRegistered_ShouldMentionIConsentStatusProvider()
    {
        var error = GDPRErrors.ConsentProviderNotRegistered(typeof(string));
        error.GetCode().IfNone("").Should().Be(GDPRErrors.ConsentProviderNotRegisteredCode);
        error.Message.Should().Contain("IConsentStatusProvider");
    }

    // -- Processing Activity errors --

    [Fact]
    public void ProcessingActivityStoreError_ShouldContainOperationAndMessage()
    {
        var error = GDPRErrors.ProcessingActivityStoreError("GetAll", "Connection refused");
        error.GetCode().IfNone("").Should().Be(GDPRErrors.ProcessingActivityStoreErrorCode);
        error.Message.Should().Contain("GetAll");
    }

    [Fact]
    public void ProcessingActivityDuplicate_ShouldContainRequestTypeName()
    {
        var error = GDPRErrors.ProcessingActivityDuplicate("MyRequest");
        error.GetCode().IfNone("").Should().Be(GDPRErrors.ProcessingActivityDuplicateCode);
        error.Message.Should().Contain("MyRequest");
    }

    [Fact]
    public void ProcessingActivityNotFound_ShouldContainRequestTypeName()
    {
        var error = GDPRErrors.ProcessingActivityNotFound("MissingRequest");
        error.GetCode().IfNone("").Should().Be(GDPRErrors.ProcessingActivityNotFoundCode);
        error.Message.Should().Contain("MissingRequest");
    }

    // -- All error codes should have expected values --

    [Fact]
    public void AllErrorCodes_ShouldHaveExpectedValues()
    {
        GDPRErrors.LawfulBasisNotDeclaredCode.Should().Be("gdpr.lawful_basis_not_declared");
        GDPRErrors.ConsentNotFoundCode.Should().Be("gdpr.consent_not_found");
        GDPRErrors.LIANotFoundCode.Should().Be("gdpr.lia_not_found");
        GDPRErrors.LIANotApprovedCode.Should().Be("gdpr.lia_not_approved");
        GDPRErrors.ConsentProviderNotRegisteredCode.Should().Be("gdpr.consent_provider_not_registered");
        GDPRErrors.LawfulBasisStoreErrorCode.Should().Be("gdpr.lawful_basis_store_error");
        GDPRErrors.LIAStoreErrorCode.Should().Be("gdpr.lia_store_error");
        GDPRErrors.ProcessingActivityStoreErrorCode.Should().Be("gdpr.processing_activity_store_error");
        GDPRErrors.ProcessingActivityDuplicateCode.Should().Be("gdpr.processing_activity_duplicate");
        GDPRErrors.ProcessingActivityNotFoundCode.Should().Be("gdpr.processing_activity_not_found");
    }
}
