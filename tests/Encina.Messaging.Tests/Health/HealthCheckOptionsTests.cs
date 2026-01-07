using Encina.Messaging.Health;
using Shouldly;

namespace Encina.Messaging.Tests.Health;

/// <summary>
/// Unit tests for health check options classes.
/// </summary>
public sealed class HealthCheckOptionsTests
{
    #region OutboxHealthCheckOptions Tests

    [Fact]
    public void OutboxHealthCheckOptions_DefaultValues()
    {
        // Arrange & Act
        var options = new OutboxHealthCheckOptions();

        // Assert
        options.PendingMessageWarningThreshold.ShouldBe(100);
        options.PendingMessageCriticalThreshold.ShouldBe(1000);
    }

    [Fact]
    public void OutboxHealthCheckOptions_SetPendingMessageWarningThreshold()
    {
        // Arrange
        var options = new OutboxHealthCheckOptions();

        // Act
        options.PendingMessageWarningThreshold = 50;

        // Assert
        options.PendingMessageWarningThreshold.ShouldBe(50);
    }

    [Fact]
    public void OutboxHealthCheckOptions_SetPendingMessageCriticalThreshold()
    {
        // Arrange
        var options = new OutboxHealthCheckOptions();

        // Act
        options.PendingMessageCriticalThreshold = 500;

        // Assert
        options.PendingMessageCriticalThreshold.ShouldBe(500);
    }

    #endregion

    #region DeadLetterHealthCheckOptions Tests

    [Fact]
    public void DeadLetterHealthCheckOptions_DefaultValues()
    {
        // Arrange & Act
        var options = new DeadLetterHealthCheckOptions();

        // Assert
        options.PendingMessageWarningThreshold.ShouldBe(10);
        options.PendingMessageCriticalThreshold.ShouldBe(100);
        options.OldMessageThreshold.ShouldBe(TimeSpan.FromHours(24));
    }

    [Fact]
    public void DeadLetterHealthCheckOptions_SetPendingMessageWarningThreshold()
    {
        // Arrange
        var options = new DeadLetterHealthCheckOptions();

        // Act
        options.PendingMessageWarningThreshold = 5;

        // Assert
        options.PendingMessageWarningThreshold.ShouldBe(5);
    }

    [Fact]
    public void DeadLetterHealthCheckOptions_SetPendingMessageCriticalThreshold()
    {
        // Arrange
        var options = new DeadLetterHealthCheckOptions();

        // Act
        options.PendingMessageCriticalThreshold = 200;

        // Assert
        options.PendingMessageCriticalThreshold.ShouldBe(200);
    }

    [Fact]
    public void DeadLetterHealthCheckOptions_SetOldMessageThreshold()
    {
        // Arrange
        var options = new DeadLetterHealthCheckOptions();

        // Act
        options.OldMessageThreshold = TimeSpan.FromHours(12);

        // Assert
        options.OldMessageThreshold.ShouldBe(TimeSpan.FromHours(12));
    }

    [Fact]
    public void DeadLetterHealthCheckOptions_SetOldMessageThreshold_ToNull()
    {
        // Arrange
        var options = new DeadLetterHealthCheckOptions();

        // Act
        options.OldMessageThreshold = null;

        // Assert
        options.OldMessageThreshold.ShouldBeNull();
    }

    #endregion

    #region SagaHealthCheckOptions Tests

    [Fact]
    public void SagaHealthCheckOptions_DefaultValues()
    {
        // Arrange & Act
        var options = new SagaHealthCheckOptions();

        // Assert
        options.StuckSagaThreshold.ShouldBe(TimeSpan.FromMinutes(30));
        options.SagaWarningThreshold.ShouldBe(10);
        options.SagaCriticalThreshold.ShouldBe(50);
    }

    [Fact]
    public void SagaHealthCheckOptions_SetStuckSagaThreshold()
    {
        // Arrange
        var options = new SagaHealthCheckOptions();

        // Act
        options.StuckSagaThreshold = TimeSpan.FromHours(1);

        // Assert
        options.StuckSagaThreshold.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public void SagaHealthCheckOptions_SetSagaWarningThreshold()
    {
        // Arrange
        var options = new SagaHealthCheckOptions();

        // Act
        options.SagaWarningThreshold = 5;

        // Assert
        options.SagaWarningThreshold.ShouldBe(5);
    }

    [Fact]
    public void SagaHealthCheckOptions_SetSagaCriticalThreshold()
    {
        // Arrange
        var options = new SagaHealthCheckOptions();

        // Act
        options.SagaCriticalThreshold = 100;

        // Assert
        options.SagaCriticalThreshold.ShouldBe(100);
    }

    #endregion

    #region SchedulingHealthCheckOptions Tests

    [Fact]
    public void SchedulingHealthCheckOptions_DefaultValues()
    {
        // Arrange & Act
        var options = new SchedulingHealthCheckOptions();

        // Assert
        options.OverdueTolerance.ShouldBe(TimeSpan.FromMinutes(5));
        options.OverdueWarningThreshold.ShouldBe(10);
        options.OverdueCriticalThreshold.ShouldBe(50);
    }

    [Fact]
    public void SchedulingHealthCheckOptions_SetOverdueTolerance()
    {
        // Arrange
        var options = new SchedulingHealthCheckOptions();

        // Act
        options.OverdueTolerance = TimeSpan.FromMinutes(10);

        // Assert
        options.OverdueTolerance.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void SchedulingHealthCheckOptions_SetOverdueWarningThreshold()
    {
        // Arrange
        var options = new SchedulingHealthCheckOptions();

        // Act
        options.OverdueWarningThreshold = 20;

        // Assert
        options.OverdueWarningThreshold.ShouldBe(20);
    }

    [Fact]
    public void SchedulingHealthCheckOptions_SetOverdueCriticalThreshold()
    {
        // Arrange
        var options = new SchedulingHealthCheckOptions();

        // Act
        options.OverdueCriticalThreshold = 100;

        // Assert
        options.OverdueCriticalThreshold.ShouldBe(100);
    }

    #endregion
}
