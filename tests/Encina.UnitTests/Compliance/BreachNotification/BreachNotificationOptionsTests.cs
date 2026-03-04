#pragma warning disable CA2012

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using FluentAssertions;
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
        options.EnforcementMode.Should().Be(BreachDetectionEnforcementMode.Warn);
        options.PublishNotifications.Should().BeTrue();
        options.TrackAuditTrail.Should().BeTrue();
        options.NotificationDeadlineHours.Should().Be(72);
        options.AutoNotifyOnHighSeverity.Should().BeFalse();
        options.PhasedReportingEnabled.Should().BeTrue();
        options.EnableDeadlineMonitoring.Should().BeFalse();
        options.DeadlineCheckInterval.Should().Be(TimeSpan.FromMinutes(15));
        options.AddHealthCheck.Should().BeFalse();
        options.SupervisoryAuthority.Should().BeNull();
        options.UnauthorizedAccessThreshold.Should().Be(5);
        options.DataExfiltrationThresholdMB.Should().Be(100);
        options.AnomalousQueryThreshold.Should().Be(1000);
        options.AssembliesToScan.Should().BeEmpty();
    }

    [Fact]
    public void AlertAtHoursRemaining_Default_ShouldContainExpectedValues()
    {
        // Arrange & Act
        var options = new BreachNotificationOptions();

        // Assert
        options.AlertAtHoursRemaining.Should().BeEquivalentTo(
            ExpectedDefaultAlertHours,
            opt => opt.WithStrictOrdering());
    }

    [Fact]
    public void SubjectNotificationSeverityThreshold_Default_ShouldBeHigh()
    {
        // Arrange & Act
        var options = new BreachNotificationOptions();

        // Assert
        options.SubjectNotificationSeverityThreshold.Should().Be(BreachSeverity.High);
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
        options.DetectionRuleTypes.Should().ContainSingle()
            .Which.Should().Be(typeof(FakeDetectionRule));
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
        options.DetectionRuleTypes.Should().ContainSingle();
    }

    [Fact]
    public void AddDetectionRule_ShouldReturnSameInstance_ForChaining()
    {
        // Arrange
        var options = new BreachNotificationOptions();

        // Act
        var result = options.AddDetectionRule<FakeDetectionRule>();

        // Assert
        result.Should().BeSameAs(options);
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
        options.DetectionRuleTypes.Should().HaveCount(2);
        options.DetectionRuleTypes.Should().Contain(typeof(FakeDetectionRule));
        options.DetectionRuleTypes.Should().Contain(typeof(AnotherFakeDetectionRule));
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
        options.EnforcementMode.Should().Be(BreachDetectionEnforcementMode.Block);
    }

    [Fact]
    public void NotificationDeadlineHours_SetCustomValue_ShouldRetain()
    {
        // Arrange
        var options = new BreachNotificationOptions();

        // Act
        options.NotificationDeadlineHours = 48;

        // Assert
        options.NotificationDeadlineHours.Should().Be(48);
    }

    [Fact]
    public void AlertAtHoursRemaining_SetCustomValues_ShouldRetain()
    {
        // Arrange
        var options = new BreachNotificationOptions();

        // Act
        options.AlertAtHoursRemaining = [24, 12, 1];

        // Assert
        options.AlertAtHoursRemaining.Should().BeEquivalentTo(
            CustomAlertHours,
            opt => opt.WithStrictOrdering());
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
