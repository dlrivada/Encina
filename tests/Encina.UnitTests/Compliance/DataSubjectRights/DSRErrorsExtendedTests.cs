using Encina.Compliance.DataSubjectRights;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Extended unit tests for <see cref="DSRErrors"/> covering error factory methods
/// not covered by existing tests, focusing on message content and metadata.
/// </summary>
public class DSRErrorsExtendedTests
{
    [Fact]
    public void RestrictionActive_MessageContainsArticle18Reference()
    {
        var error = DSRErrors.RestrictionActive("subject-1");

        error.Message.ShouldContain("subject-1");
        error.Message.ShouldContain("Article 18");
    }

    [Fact]
    public void ErasureFailed_MessageContainsSubjectIdAndReason()
    {
        var error = DSRErrors.ErasureFailed("subject-1", "Database error");

        error.Message.ShouldContain("subject-1");
        error.Message.ShouldContain("Database error");
    }

    [Fact]
    public void ExportFailed_MessageContainsFormatAndSubject()
    {
        var error = DSRErrors.ExportFailed("subject-1", ExportFormat.JSON, "Serialization error");

        error.Message.ShouldContain("subject-1");
        error.Message.ShouldContain("JSON");
        error.Message.ShouldContain("Serialization error");
    }

    [Fact]
    public void FormatNotSupported_MessageContainsFormat()
    {
        var error = DSRErrors.FormatNotSupported(ExportFormat.CSV);

        error.Message.ShouldContain("CSV");
    }

    [Fact]
    public void DeadlineExpired_MessageContainsArticle12Reference()
    {
        var deadline = new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero);
        var error = DSRErrors.DeadlineExpired("req-123", deadline);

        error.Message.ShouldContain("req-123");
        error.Message.ShouldContain("Article 12(3)");
    }

    [Fact]
    public void ExemptionApplies_MessageContainsAllDetails()
    {
        var error = DSRErrors.ExemptionApplies("subject-1", ErasureExemption.LegalObligation, "Tax records");

        error.Message.ShouldContain("subject-1");
        error.Message.ShouldContain("LegalObligation");
        error.Message.ShouldContain("Tax records");
    }

    [Fact]
    public void SubjectNotFound_MessageContainsSubjectId()
    {
        var error = DSRErrors.SubjectNotFound("subject-1");

        error.Message.ShouldContain("subject-1");
    }

    [Fact]
    public void LocatorFailed_MessageContainsSubjectAndReason()
    {
        var error = DSRErrors.LocatorFailed("subject-1", "Connection timeout");

        error.Message.ShouldContain("subject-1");
        error.Message.ShouldContain("Connection timeout");
    }

    [Fact]
    public void StoreError_MessageContainsOperationAndDetail()
    {
        var error = DSRErrors.StoreError("Create", "Duplicate key");

        error.Message.ShouldContain("Create");
        error.Message.ShouldContain("Duplicate key");
    }

    [Fact]
    public void RectificationFailed_MessageContainsAllFields()
    {
        var error = DSRErrors.RectificationFailed("subject-1", "Email", "Invalid format");

        error.Message.ShouldContain("subject-1");
        error.Message.ShouldContain("Email");
        error.Message.ShouldContain("Invalid format");
    }

    [Fact]
    public void ObjectionRejected_MessageContainsAllFields()
    {
        var error = DSRErrors.ObjectionRejected("subject-1", "Marketing", "Compelling interest");

        error.Message.ShouldContain("subject-1");
        error.Message.ShouldContain("Marketing");
        error.Message.ShouldContain("Compelling interest");
    }

    [Fact]
    public void InvalidRequest_MessageContainsReason()
    {
        var error = DSRErrors.InvalidRequest("Missing subject ID");

        error.Message.ShouldContain("Missing subject ID");
    }

    [Fact]
    public void ServiceError_MessageContainsExceptionInfo()
    {
        var ex = new InvalidOperationException("test error");
        var error = DSRErrors.ServiceError("HandleErasure", ex);

        error.Message.ShouldContain("HandleErasure");
        error.Message.ShouldContain("test error");
    }

    [Fact]
    public void EventHistoryUnavailable_MessageContainsRequestId()
    {
        var requestId = Guid.NewGuid();
        var error = DSRErrors.EventHistoryUnavailable(requestId);

        error.Message.ShouldContain(requestId.ToString());
    }

    [Fact]
    public void AllErrorCodes_AreUnique()
    {
        var codes = new[]
        {
            DSRErrors.RequestNotFoundCode,
            DSRErrors.RequestAlreadyCompletedCode,
            DSRErrors.IdentityNotVerifiedCode,
            DSRErrors.RestrictionActiveCode,
            DSRErrors.ErasureFailedCode,
            DSRErrors.ExportFailedCode,
            DSRErrors.FormatNotSupportedCode,
            DSRErrors.DeadlineExpiredCode,
            DSRErrors.ExemptionAppliesCode,
            DSRErrors.SubjectNotFoundCode,
            DSRErrors.LocatorFailedCode,
            DSRErrors.StoreErrorCode,
            DSRErrors.RectificationFailedCode,
            DSRErrors.ObjectionRejectedCode,
            DSRErrors.InvalidRequestCode,
            DSRErrors.ServiceErrorCode,
            DSRErrors.EventHistoryUnavailableCode
        };

        codes.Distinct().Count().ShouldBe(codes.Length);
    }

    [Fact]
    public void AllErrorCodes_StartWithDsrPrefix()
    {
        var codes = new[]
        {
            DSRErrors.RequestNotFoundCode,
            DSRErrors.RequestAlreadyCompletedCode,
            DSRErrors.IdentityNotVerifiedCode,
            DSRErrors.RestrictionActiveCode,
            DSRErrors.ErasureFailedCode,
            DSRErrors.ExportFailedCode,
            DSRErrors.FormatNotSupportedCode,
            DSRErrors.DeadlineExpiredCode,
            DSRErrors.ExemptionAppliesCode,
            DSRErrors.SubjectNotFoundCode,
            DSRErrors.LocatorFailedCode,
            DSRErrors.StoreErrorCode,
            DSRErrors.RectificationFailedCode,
            DSRErrors.ObjectionRejectedCode,
            DSRErrors.InvalidRequestCode,
            DSRErrors.ServiceErrorCode,
            DSRErrors.EventHistoryUnavailableCode
        };

        foreach (var code in codes)
        {
            code.ShouldStartWith("dsr.");
        }
    }

    [Fact]
    public void ServiceError_ErrorCode_MatchesExpected()
    {
        var error = DSRErrors.ServiceError("test", new InvalidOperationException("ex"));
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.ServiceErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void EventHistoryUnavailable_ErrorCode_MatchesExpected()
    {
        var error = DSRErrors.EventHistoryUnavailable(Guid.NewGuid());
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.EventHistoryUnavailableCode),
            None: () => Assert.Fail("Expected error code"));
    }
}
