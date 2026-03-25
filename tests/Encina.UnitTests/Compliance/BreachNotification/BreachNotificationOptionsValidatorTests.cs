using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="BreachNotificationOptionsValidator"/>.
/// </summary>
public sealed class BreachNotificationOptionsValidatorTests
{
    private readonly BreachNotificationOptionsValidator _sut = new();

    [Fact]
    public void Validate_WithDefaultOptions_ShouldSucceed()
    {
        // Arrange
        var options = new BreachNotificationOptions();

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NullOptions_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Validate(null, null!);
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Validate_InvalidEnforcementMode_ShouldFail()
    {
        // Arrange
        var options = new BreachNotificationOptions
        {
            EnforcementMode = (BreachDetectionEnforcementMode)999
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("EnforcementMode");
    }

    [Fact]
    public void Validate_ZeroNotificationDeadlineHours_ShouldFail()
    {
        // Arrange
        var options = new BreachNotificationOptions
        {
            NotificationDeadlineHours = 0
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("NotificationDeadlineHours");
    }

    [Fact]
    public void Validate_NegativeNotificationDeadlineHours_ShouldFail()
    {
        // Arrange
        var options = new BreachNotificationOptions
        {
            NotificationDeadlineHours = -1
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("NotificationDeadlineHours");
    }

    [Fact]
    public void Validate_AlertThresholdWithNonPositiveValue_ShouldFail()
    {
        // Arrange
        var options = new BreachNotificationOptions
        {
            AlertAtHoursRemaining = [24, 0, 6]
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("AlertAtHoursRemaining");
        result.FailureMessage.ShouldContain("non-positive");
    }

    [Fact]
    public void Validate_AlertThresholdExceedsDeadline_ShouldFail()
    {
        // Arrange
        var options = new BreachNotificationOptions
        {
            NotificationDeadlineHours = 72,
            AlertAtHoursRemaining = [48, 72]
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("AlertAtHoursRemaining");
        result.FailureMessage.ShouldContain("NotificationDeadlineHours");
    }

    [Fact]
    public void Validate_ZeroDeadlineCheckInterval_ShouldFail()
    {
        // Arrange
        var options = new BreachNotificationOptions
        {
            DeadlineCheckInterval = TimeSpan.Zero
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("DeadlineCheckInterval");
    }

    [Fact]
    public void Validate_NegativeDeadlineCheckInterval_ShouldFail()
    {
        // Arrange
        var options = new BreachNotificationOptions
        {
            DeadlineCheckInterval = TimeSpan.FromMinutes(-5)
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("DeadlineCheckInterval");
    }

    [Fact]
    public void Validate_InvalidSubjectNotificationSeverityThreshold_ShouldFail()
    {
        // Arrange
        var options = new BreachNotificationOptions
        {
            SubjectNotificationSeverityThreshold = (BreachSeverity)99
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("SubjectNotificationSeverityThreshold");
    }

    [Fact]
    public void Validate_EmptyAlertArray_ShouldSucceed()
    {
        // Arrange
        var options = new BreachNotificationOptions
        {
            AlertAtHoursRemaining = []
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MultipleFailures_ShouldReportAll()
    {
        // Arrange
        var options = new BreachNotificationOptions
        {
            EnforcementMode = (BreachDetectionEnforcementMode)999,
            NotificationDeadlineHours = -1,
            DeadlineCheckInterval = TimeSpan.Zero,
            SubjectNotificationSeverityThreshold = (BreachSeverity)99
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("EnforcementMode");
        result.FailureMessage.ShouldContain("NotificationDeadlineHours");
        result.FailureMessage.ShouldContain("DeadlineCheckInterval");
        result.FailureMessage.ShouldContain("SubjectNotificationSeverityThreshold");
    }
}
