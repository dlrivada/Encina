using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Export;
using Shouldly;
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
        error.Message.ShouldContain("String");
        error.Message.ShouldContain("Article 30");
    }

    [Fact]
    public void ComplianceValidationFailed_ShouldContainErrors()
    {
        // Act
        var error = GDPRErrors.ComplianceValidationFailed(
            typeof(string), ["Missing consent", "No lawful basis"]);

        // Assert
        error.Message.ShouldContain("String");
        error.Message.ShouldContain("Missing consent");
        error.Message.ShouldContain("No lawful basis");
    }

    [Fact]
    public void RegistryLookupFailed_ShouldContainInnerError()
    {
        // Arrange
        var inner = EncinaError.New("Database connection failed");

        // Act
        var error = GDPRErrors.RegistryLookupFailed(typeof(string), inner);

        // Assert
        error.Message.ShouldContain("String");
        error.Message.ShouldContain("Database connection failed");
    }

    [Fact]
    public void RoPAExportErrors_SerializationFailed_ShouldContainFormatAndReason()
    {
        // Act
        var error = RoPAExportErrors.SerializationFailed("JSON", "Invalid character");

        // Assert
        error.Message.ShouldContain("JSON");
        error.Message.ShouldContain("Invalid character");
    }

    // -- Error codes --

    [Fact]
    public void ErrorCodes_ShouldFollowConvention()
    {
        GDPRErrors.UnregisteredActivityCode.ShouldStartWith("gdpr.");
        GDPRErrors.ComplianceValidationFailedCode.ShouldStartWith("gdpr.");
        GDPRErrors.RegistryLookupFailedCode.ShouldStartWith("gdpr.");
        RoPAExportErrors.SerializationFailedCode.ShouldStartWith("gdpr.");
    }

    // -- Lawful Basis errors (Article 6) --

    [Fact]
    public void LawfulBasisNotDeclared_ShouldContainRequestTypeAndArticle()
    {
        var error = GDPRErrors.LawfulBasisNotDeclared(typeof(double));
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.LawfulBasisNotDeclaredCode);
        error.Message.ShouldContain("Double");
        error.Message.ShouldContain("Article 6(1)");
    }

    [Fact]
    public void ConsentNotFound_WithoutSubjectId_ShouldCreateError()
    {
        var error = GDPRErrors.ConsentNotFound(typeof(string));
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.ConsentNotFoundCode);
        error.Message.ShouldContain("String");
        error.Message.ShouldContain("Article 6(1)(a)");
    }

    [Fact]
    public void ConsentNotFound_WithSubjectId_ShouldCreateError()
    {
        var error = GDPRErrors.ConsentNotFound(typeof(string), "user-123");
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.ConsentNotFoundCode);
    }

    [Fact]
    public void LIANotFound_WithType_ShouldContainRequestType()
    {
        var error = GDPRErrors.LIANotFound(typeof(int));
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.LIANotFoundCode);
        error.Message.ShouldContain("Int32");
        error.Message.ShouldContain("Article 6(1)(f)");
    }

    [Fact]
    public void LIANotFound_WithReference_ShouldContainReference()
    {
        var error = GDPRErrors.LIANotFound("LIA-2024-001");
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.LIANotFoundCode);
        error.Message.ShouldContain("LIA-2024-001");
    }

    [Fact]
    public void LIANotApproved_WithType_ShouldContainReferenceAndType()
    {
        var error = GDPRErrors.LIANotApproved(typeof(string), "LIA-REF-42");
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.LIANotApprovedCode);
        error.Message.ShouldContain("LIA-REF-42");
        error.Message.ShouldContain("String");
    }

    [Fact]
    public void LIANotApproved_WithOutcome_ShouldContainOutcome()
    {
        var error = GDPRErrors.LIANotApproved("LIA-2024-002", LIAOutcome.RequiresReview);
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.LIANotApprovedCode);
        error.Message.ShouldContain("LIA-2024-002");
        error.Message.ShouldContain("RequiresReview");
    }

    [Fact]
    public void LIANotApproved_WithRejectedOutcome_ShouldIncludeOutcome()
    {
        var error = GDPRErrors.LIANotApproved("LIA-REJ", LIAOutcome.Rejected);
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.LIANotApprovedCode);
        error.Message.ShouldContain("Rejected");
    }

    // -- Store errors --

    [Fact]
    public void LawfulBasisStoreError_ShouldContainOperationAndMessage()
    {
        var error = GDPRErrors.LawfulBasisStoreError("Register", "DB down");
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.LawfulBasisStoreErrorCode);
        error.Message.ShouldContain("Register");
        error.Message.ShouldContain("DB down");
    }

    [Fact]
    public void LIAStoreError_ShouldContainOperationAndMessage()
    {
        var error = GDPRErrors.LIAStoreError("GetByReference", "Timeout");
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.LIAStoreErrorCode);
        error.Message.ShouldContain("GetByReference");
        error.Message.ShouldContain("Timeout");
    }

    [Fact]
    public void ConsentProviderNotRegistered_ShouldMentionIConsentStatusProvider()
    {
        var error = GDPRErrors.ConsentProviderNotRegistered(typeof(string));
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.ConsentProviderNotRegisteredCode);
        error.Message.ShouldContain("IConsentStatusProvider");
    }

    // -- Processing Activity errors --

    [Fact]
    public void ProcessingActivityStoreError_ShouldContainOperationAndMessage()
    {
        var error = GDPRErrors.ProcessingActivityStoreError("GetAll", "Connection refused");
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.ProcessingActivityStoreErrorCode);
        error.Message.ShouldContain("GetAll");
    }

    [Fact]
    public void ProcessingActivityDuplicate_ShouldContainRequestTypeName()
    {
        var error = GDPRErrors.ProcessingActivityDuplicate("MyRequest");
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.ProcessingActivityDuplicateCode);
        error.Message.ShouldContain("MyRequest");
    }

    [Fact]
    public void ProcessingActivityNotFound_ShouldContainRequestTypeName()
    {
        var error = GDPRErrors.ProcessingActivityNotFound("MissingRequest");
        error.GetCode().IfNone("").ShouldBe(GDPRErrors.ProcessingActivityNotFoundCode);
        error.Message.ShouldContain("MissingRequest");
    }

    // -- All error codes should have expected values --

    [Fact]
    public void AllErrorCodes_ShouldHaveExpectedValues()
    {
        GDPRErrors.LawfulBasisNotDeclaredCode.ShouldBe("gdpr.lawful_basis_not_declared");
        GDPRErrors.ConsentNotFoundCode.ShouldBe("gdpr.consent_not_found");
        GDPRErrors.LIANotFoundCode.ShouldBe("gdpr.lia_not_found");
        GDPRErrors.LIANotApprovedCode.ShouldBe("gdpr.lia_not_approved");
        GDPRErrors.ConsentProviderNotRegisteredCode.ShouldBe("gdpr.consent_provider_not_registered");
        GDPRErrors.LawfulBasisStoreErrorCode.ShouldBe("gdpr.lawful_basis_store_error");
        GDPRErrors.LIAStoreErrorCode.ShouldBe("gdpr.lia_store_error");
        GDPRErrors.ProcessingActivityStoreErrorCode.ShouldBe("gdpr.processing_activity_store_error");
        GDPRErrors.ProcessingActivityDuplicateCode.ShouldBe("gdpr.processing_activity_duplicate");
        GDPRErrors.ProcessingActivityNotFoundCode.ShouldBe("gdpr.processing_activity_not_found");
    }
}
