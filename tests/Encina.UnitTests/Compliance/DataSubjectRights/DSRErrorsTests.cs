using Encina.Compliance.DataSubjectRights;
using LanguageExt;
using Shouldly;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="DSRErrors"/> factory methods.
/// </summary>
public class DSRErrorsTests
{
    #region RequestNotFound Tests

    [Fact]
    public void RequestNotFound_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.RequestNotFound("req-001");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.RequestNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void RequestNotFound_ShouldIncludeRequestIdInMessage()
    {
        var error = DSRErrors.RequestNotFound("req-001");
        error.Message.ShouldContain("req-001");
    }

    #endregion

    #region RequestAlreadyCompleted Tests

    [Fact]
    public void RequestAlreadyCompleted_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.RequestAlreadyCompleted("req-001");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.RequestAlreadyCompletedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void RequestAlreadyCompleted_ShouldIncludeRequestIdInMessage()
    {
        var error = DSRErrors.RequestAlreadyCompleted("req-001");
        error.Message.ShouldContain("req-001");
    }

    #endregion

    #region IdentityNotVerified Tests

    [Fact]
    public void IdentityNotVerified_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.IdentityNotVerified("req-001");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.IdentityNotVerifiedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region RestrictionActive Tests

    [Fact]
    public void RestrictionActive_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.RestrictionActive("subject-1");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.RestrictionActiveCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void RestrictionActive_ShouldIncludeSubjectIdInMessage()
    {
        var error = DSRErrors.RestrictionActive("subject-1");
        error.Message.ShouldContain("subject-1");
    }

    #endregion

    #region ErasureFailed Tests

    [Fact]
    public void ErasureFailed_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.ErasureFailed("subject-1", "Database error");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.ErasureFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void ErasureFailed_ShouldIncludeSubjectIdAndMessageInMessage()
    {
        var error = DSRErrors.ErasureFailed("subject-1", "Database error");
        error.Message.ShouldContain("subject-1");
        error.Message.ShouldContain("Database error");
    }

    #endregion

    #region ExportFailed Tests

    [Fact]
    public void ExportFailed_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.ExportFailed("subject-1", ExportFormat.JSON, "Serialization error");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.ExportFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region FormatNotSupported Tests

    [Fact]
    public void FormatNotSupported_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.FormatNotSupported(ExportFormat.XML);
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.FormatNotSupportedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void FormatNotSupported_ShouldIncludeFormatInMessage()
    {
        var error = DSRErrors.FormatNotSupported(ExportFormat.XML);
        error.Message.ShouldContain("XML");
    }

    #endregion

    #region DeadlineExpired Tests

    [Fact]
    public void DeadlineExpired_ShouldReturnCorrectCode()
    {
        var deadline = new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero);
        var error = DSRErrors.DeadlineExpired("req-001", deadline);
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.DeadlineExpiredCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region ExemptionApplies Tests

    [Fact]
    public void ExemptionApplies_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.ExemptionApplies("subject-1", ErasureExemption.LegalObligation, "Tax records");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.ExemptionAppliesCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region SubjectNotFound Tests

    [Fact]
    public void SubjectNotFound_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.SubjectNotFound("subject-1");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.SubjectNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region LocatorFailed Tests

    [Fact]
    public void LocatorFailed_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.LocatorFailed("subject-1", "Connection timeout");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.LocatorFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region StoreError Tests

    [Fact]
    public void StoreError_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.StoreError("Create", "Duplicate key");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void StoreError_ShouldIncludeOperationInMessage()
    {
        var error = DSRErrors.StoreError("Create", "Duplicate key");
        error.Message.ShouldContain("Create");
    }

    #endregion

    #region RectificationFailed Tests

    [Fact]
    public void RectificationFailed_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.RectificationFailed("subject-1", "Email", "Field not found");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.RectificationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region ObjectionRejected Tests

    [Fact]
    public void ObjectionRejected_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.ObjectionRejected("subject-1", "DirectMarketing", "Legitimate interest");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.ObjectionRejectedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region InvalidRequest Tests

    [Fact]
    public void InvalidRequest_ShouldReturnCorrectCode()
    {
        var error = DSRErrors.InvalidRequest("Missing subject ID");
        error.GetCode().Match(
            Some: code => code.ShouldBe(DSRErrors.InvalidRequestCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region Error Code Constants Tests

    [Fact]
    public void ErrorCodes_ShouldFollowDSRConvention()
    {
        // All DSR error codes should start with "dsr."
        DSRErrors.RequestNotFoundCode.ShouldStartWith("dsr.");
        DSRErrors.RequestAlreadyCompletedCode.ShouldStartWith("dsr.");
        DSRErrors.IdentityNotVerifiedCode.ShouldStartWith("dsr.");
        DSRErrors.RestrictionActiveCode.ShouldStartWith("dsr.");
        DSRErrors.ErasureFailedCode.ShouldStartWith("dsr.");
        DSRErrors.ExportFailedCode.ShouldStartWith("dsr.");
        DSRErrors.FormatNotSupportedCode.ShouldStartWith("dsr.");
        DSRErrors.DeadlineExpiredCode.ShouldStartWith("dsr.");
        DSRErrors.ExemptionAppliesCode.ShouldStartWith("dsr.");
        DSRErrors.SubjectNotFoundCode.ShouldStartWith("dsr.");
        DSRErrors.LocatorFailedCode.ShouldStartWith("dsr.");
        DSRErrors.StoreErrorCode.ShouldStartWith("dsr.");
        DSRErrors.RectificationFailedCode.ShouldStartWith("dsr.");
        DSRErrors.ObjectionRejectedCode.ShouldStartWith("dsr.");
        DSRErrors.InvalidRequestCode.ShouldStartWith("dsr.");
    }

    [Fact]
    public void ErrorCodes_ShouldAllBeUnique()
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
            DSRErrors.InvalidRequestCode
        };

        codes.ShouldBeUnique();
    }

    #endregion
}
