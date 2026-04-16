#pragma warning disable CA2012

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using Shouldly;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="BreachNotificationOptions"/>.
/// </summary>
public class BreachNotificationOptionsTests
{
    private static readonly int[] ExpectedDefaultAlertHours = [48, 24, 12, 6, 1];
    private static readonly int[] CustomAlertHours = [24, 12, 1];

    #region Default Values

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new BreachNotificationOptions();

        // Assert
        options.EnforcementMode.ShouldBe(BreachDetectionEnforcementMode.Warn);
        options.PublishNotifications.ShouldBeTrue();
        options.TrackAuditTrail.ShouldBeTrue();
        options.NotificationDeadlineHours.ShouldBe(72);
        options.AutoNotifyOnHighSeverity.ShouldBeFalse();
        options.PhasedReportingEnabled.ShouldBeTrue();
        options.EnableDeadlineMonitoring.ShouldBeFalse();
        options.DeadlineCheckInterval.ShouldBe(TimeSpan.FromMinutes(15));
        options.AddHealthCheck.ShouldBeFalse();
        options.SupervisoryAuthority.ShouldBeNull();
        options.UnauthorizedAccessThreshold.ShouldBe(5);
        options.DataExfiltrationThresholdMB.ShouldBe(100);
        options.AnomalousQueryThreshold.ShouldBe(1000);
        options.AssembliesToScan.ShouldBeEmpty();
    }

    [Fact]
    public void AlertAtHoursRemaining_Default_ShouldContainExpectedValues()
    {
        // Arrange & Act
        var options = new BreachNotificationOptions();

        // Assert
        options.AlertAtHoursRemaining.ShouldBe(ExpectedDefaultAlertHours);
    }

    [Fact]
    public void SubjectNotificationSeverityThreshold_Default_ShouldBeHigh()
    {
        // Arrange & Act
        var options = new BreachNotificationOptions();

        // Assert
        options.SubjectNotificationSeverityThreshold.ShouldBe(BreachSeverity.High);
    }

    #endregion

    #region AddDetectionRule Tests

    [Fact]
    public void AddDetectionRule_ValidType_ShouldAddToList()
    {
        // Arrange
        var options = new BreachNotificationOptions();

        // Act
        options.AddDetectionRule<FakeDetectionRule>();

        // Assert
        options.DetectionRuleTypes.ShouldHaveSingleItem()
            .ShouldBe(typeof(FakeDetectionRule));
    }

    [Fact]
    public void AddDetectionRule_DuplicateType_ShouldNotAddTwice()
    {
        // Arrange
        var options = new BreachNotificationOptions();

        // Act
        options.AddDetectionRule<FakeDetectionRule>();
        options.AddDetectionRule<FakeDetectionRule>();

        // Assert
        options.DetectionRuleTypes.ShouldHaveSingleItem();
    }

    [Fact]
    public void AddDetectionRule_ShouldReturnSameInstance_ForChaining()
    {
        // Arrange
        var options = new BreachNotificationOptions();

        // Act
        var result = options.AddDetectionRule<FakeDetectionRule>();

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddDetectionRule_MultipleTypes_ShouldAddAll()
    {
        // Arrange
        var options = new BreachNotificationOptions();

        // Act
        options
            .AddDetectionRule<FakeDetectionRule>()
            .AddDetectionRule<AnotherFakeDetectionRule>();

        // Assert
        options.DetectionRuleTypes.Count.ShouldBe(2);
        options.DetectionRuleTypes.ShouldContain(typeof(FakeDetectionRule));
        options.DetectionRuleTypes.ShouldContain(typeof(AnotherFakeDetectionRule));
    }

    #endregion

    #region Property Modification Tests

    [Fact]
    public void EnforcementMode_SetToBlock_ShouldRetain()
    {
        // Arrange
        var options = new BreachNotificationOptions();

        // Act
        options.EnforcementMode = BreachDetectionEnforcementMode.Block;

        // Assert
        options.EnforcementMode.ShouldBe(BreachDetectionEnforcementMode.Block);
    }

    [Fact]
    public void NotificationDeadlineHours_SetCustomValue_ShouldRetain()
    {
        // Arrange
        var options = new BreachNotificationOptions();

        // Act
        options.NotificationDeadlineHours = 48;

        // Assert
        options.NotificationDeadlineHours.ShouldBe(48);
    }

    [Fact]
    public void AlertAtHoursRemaining_SetCustomValues_ShouldRetain()
    {
        // Arrange
        var options = new BreachNotificationOptions();

        // Act
        options.AlertAtHoursRemaining = [24, 12, 1];

        // Assert
        options.AlertAtHoursRemaining.ShouldBe(CustomAlertHours);
    }

    #endregion

    #region Fake Detection Rules

    private sealed class FakeDetectionRule : IBreachDetectionRule
    {
        public string Name => "FakeRule";

        public ValueTask<Either<EncinaError, Option<PotentialBreach>>> EvaluateAsync(
            SecurityEvent securityEvent,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(
                Right<EncinaError, Option<PotentialBreach>>(None));
    }

    private sealed class AnotherFakeDetectionRule : IBreachDetectionRule
    {
        public string Name => "AnotherFakeRule";

        public ValueTask<Either<EncinaError, Option<PotentialBreach>>> EvaluateAsync(
            SecurityEvent securityEvent,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(
                Right<EncinaError, Option<PotentialBreach>>(None));
    }

    #endregion
}
