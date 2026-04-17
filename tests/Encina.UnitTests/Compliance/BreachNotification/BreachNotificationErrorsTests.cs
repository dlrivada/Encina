#pragma warning disable CA2012

using Encina.Compliance.BreachNotification;
using Shouldly;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="BreachNotificationErrors"/> factory methods and error code constants.
/// </summary>
public class BreachNotificationErrorsTests
{
    #region NotFound Tests

    [Fact]
    public void NotFound_ShouldReturnCorrectError()
    {
        // Act
        var error = BreachNotificationErrors.NotFound("breach-123");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.NotFoundCode),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("breach-123");
    }

    #endregion

    #region AlreadyExists Tests

    [Fact]
    public void AlreadyExists_ShouldReturnCorrectError()
    {
        // Act
        var error = BreachNotificationErrors.AlreadyExists("breach-456");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.AlreadyExistsCode),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("breach-456");
    }

    #endregion

    #region AlreadyResolved Tests

    [Fact]
    public void AlreadyResolved_ShouldReturnCorrectError()
    {
        // Act
        var error = BreachNotificationErrors.AlreadyResolved("breach-789");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.AlreadyResolvedCode),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("breach-789");
    }

    #endregion

    #region DetectionFailed Tests

    [Fact]
    public void DetectionFailed_ShouldReturnCorrectError()
    {
        // Act
        var error = BreachNotificationErrors.DetectionFailed("Rule engine timeout");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.DetectionFailedCode),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("Rule engine timeout");
    }

    [Fact]
    public void DetectionFailed_WithException_ShouldReturnCorrectCode()
    {
        // Arrange
        var innerException = new InvalidOperationException("Connection lost");

        // Act
        var error = BreachNotificationErrors.DetectionFailed("Detection unavailable", innerException);

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.DetectionFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region StoreError Tests

    [Fact]
    public void StoreError_ShouldReturnCorrectError()
    {
        // Act
        var error = BreachNotificationErrors.StoreError("RecordBreach", "Database unavailable");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("RecordBreach");
        error.Message.ShouldContain("Database unavailable");
    }

    [Fact]
    public void StoreError_WithException_ShouldReturnCorrectCode()
    {
        // Arrange
        var innerException = new InvalidOperationException("Deadlock");

        // Act
        var error = BreachNotificationErrors.StoreError("UpdateBreach", "Write failed", innerException);

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region DeadlineExpired Tests

    [Fact]
    public void DeadlineExpired_ShouldReturnCorrectErrorWithHours()
    {
        // Arrange
        const double hoursOverdue = 12.5;
        var expectedHours = hoursOverdue.ToString("F1", System.Globalization.CultureInfo.CurrentCulture);

        // Act
        var error = BreachNotificationErrors.DeadlineExpired("breach-deadline-001", hoursOverdue);

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.DeadlineExpiredCode),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("breach-deadline-001");
        error.Message.ShouldContain(expectedHours);
        error.Message.ShouldContain("72-hour");
    }

    #endregion

    #region BreachDetected Tests

    [Fact]
    public void BreachDetected_ShouldReturnCorrectError()
    {
        // Act
        var error = BreachNotificationErrors.BreachDetected(
            "GetCustomerDataQuery",
            "UnauthorizedAccessRule, MassDataExfiltrationRule");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.BreachDetectedCode),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("GetCustomerDataQuery");
        error.Message.ShouldContain("UnauthorizedAccessRule, MassDataExfiltrationRule");
    }

    #endregion

    #region Other Factory Methods Tests

    [Fact]
    public void NotificationFailed_ShouldReturnCorrectCode()
    {
        // Act
        var error = BreachNotificationErrors.NotificationFailed("breach-nf-001", "SMTP timeout");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.NotificationFailedCode),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("breach-nf-001");
        error.Message.ShouldContain("SMTP timeout");
    }

    [Fact]
    public void AuthorityNotificationFailed_ShouldReturnCorrectCode()
    {
        // Act
        var error = BreachNotificationErrors.AuthorityNotificationFailed("breach-anf-001", "API unreachable");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.AuthorityNotificationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void SubjectNotificationFailed_ShouldReturnCorrectCode()
    {
        // Act
        var error = BreachNotificationErrors.SubjectNotificationFailed("breach-snf-001", "Email service down");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.SubjectNotificationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void InvalidParameter_ShouldReturnCorrectCode()
    {
        // Act
        var error = BreachNotificationErrors.InvalidParameter("nature", "Cannot be empty");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.InvalidParameterCode),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("nature");
    }

    [Fact]
    public void PhasedReportFailed_ShouldReturnCorrectCode()
    {
        // Act
        var error = BreachNotificationErrors.PhasedReportFailed("breach-pr-001", "Report number already exists");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.PhasedReportFailedCode),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("breach-pr-001");
    }

    [Fact]
    public void ExemptionInvalid_ShouldReturnCorrectCode()
    {
        // Act
        var error = BreachNotificationErrors.ExemptionInvalid("breach-ei-001", "Encryption not verified");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.ExemptionInvalidCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void RuleEvaluationFailed_ShouldReturnCorrectCode()
    {
        // Act
        var error = BreachNotificationErrors.RuleEvaluationFailed("UnauthorizedAccessRule", "Timeout");

        // Assert
        error.GetCode().Match(
            Some: code => code.ShouldBe(BreachNotificationErrors.RuleEvaluationFailedCode),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("UnauthorizedAccessRule");
    }

    #endregion

    #region Error Code Constants Tests

    [Fact]
    public void ErrorCodes_ShouldFollowBreachPrefix()
    {
        BreachNotificationErrors.NotFoundCode.ShouldStartWith("breach.");
        BreachNotificationErrors.AlreadyExistsCode.ShouldStartWith("breach.");
        BreachNotificationErrors.AlreadyResolvedCode.ShouldStartWith("breach.");
        BreachNotificationErrors.DetectionFailedCode.ShouldStartWith("breach.");
        BreachNotificationErrors.StoreErrorCode.ShouldStartWith("breach.");
        BreachNotificationErrors.DeadlineExpiredCode.ShouldStartWith("breach.");
        BreachNotificationErrors.BreachDetectedCode.ShouldStartWith("breach.");
        BreachNotificationErrors.NotificationFailedCode.ShouldStartWith("breach.");
        BreachNotificationErrors.AuthorityNotificationFailedCode.ShouldStartWith("breach.");
        BreachNotificationErrors.SubjectNotificationFailedCode.ShouldStartWith("breach.");
        BreachNotificationErrors.InvalidParameterCode.ShouldStartWith("breach.");
        BreachNotificationErrors.PhasedReportFailedCode.ShouldStartWith("breach.");
        BreachNotificationErrors.ExemptionInvalidCode.ShouldStartWith("breach.");
        BreachNotificationErrors.RuleEvaluationFailedCode.ShouldStartWith("breach.");
    }

    [Fact]
    public void ErrorCodes_ShouldAllBeUnique()
    {
        var codes = new[]
        {
            BreachNotificationErrors.NotFoundCode,
            BreachNotificationErrors.AlreadyExistsCode,
            BreachNotificationErrors.AlreadyResolvedCode,
            BreachNotificationErrors.DetectionFailedCode,
            BreachNotificationErrors.StoreErrorCode,
            BreachNotificationErrors.DeadlineExpiredCode,
            BreachNotificationErrors.BreachDetectedCode,
            BreachNotificationErrors.NotificationFailedCode,
            BreachNotificationErrors.AuthorityNotificationFailedCode,
            BreachNotificationErrors.SubjectNotificationFailedCode,
            BreachNotificationErrors.InvalidParameterCode,
            BreachNotificationErrors.PhasedReportFailedCode,
            BreachNotificationErrors.ExemptionInvalidCode,
            BreachNotificationErrors.RuleEvaluationFailedCode
        };

        codes.ShouldBeUnique();
    }

    [Theory]
    [InlineData(nameof(BreachNotificationErrors.NotFoundCode), "breach.not_found")]
    [InlineData(nameof(BreachNotificationErrors.AlreadyExistsCode), "breach.already_exists")]
    [InlineData(nameof(BreachNotificationErrors.AlreadyResolvedCode), "breach.already_resolved")]
    [InlineData(nameof(BreachNotificationErrors.DetectionFailedCode), "breach.detection_failed")]
    [InlineData(nameof(BreachNotificationErrors.StoreErrorCode), "breach.store_error")]
    [InlineData(nameof(BreachNotificationErrors.DeadlineExpiredCode), "breach.deadline_expired")]
    [InlineData(nameof(BreachNotificationErrors.BreachDetectedCode), "breach.detected")]
    public void ErrorCodeConstant_ShouldHaveCorrectValue(string constantName, string expectedValue)
    {
        var actualValue = typeof(BreachNotificationErrors)
            .GetField(constantName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
            .GetValue(null) as string;

        actualValue.ShouldBe(expectedValue);
    }

    #endregion
}
