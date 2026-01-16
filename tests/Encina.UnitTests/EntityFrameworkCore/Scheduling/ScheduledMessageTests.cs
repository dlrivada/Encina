using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Scheduling;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Scheduling;

/// <summary>
/// Unit tests for <see cref="ScheduledMessage"/>.
/// </summary>
public sealed class ScheduledMessageTests
{
    #region IsProcessed Tests

    [Fact]
    public void IsProcessed_WhenProcessedAndNoError_ReturnsTrue()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            RequestType = "TestCommand",
            Content = "{}",
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorMessage = null
        };

        // Act & Assert
        message.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public void IsProcessed_WhenNotProcessed_ReturnsFalse()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            RequestType = "TestCommand",
            Content = "{}",
            ProcessedAtUtc = null
        };

        // Act & Assert
        message.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void IsProcessed_WhenProcessedWithError_ReturnsFalse()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            RequestType = "TestCommand",
            Content = "{}",
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorMessage = "Execution failed"
        };

        // Act & Assert
        message.IsProcessed.ShouldBeFalse();
    }

    #endregion

    #region IsDue Tests

    [Fact]
    public void IsDue_WhenScheduledInPastAndNotProcessed_ReturnsTrue()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            RequestType = "TestCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ProcessedAtUtc = null
        };

        // Act & Assert
        message.IsDue().ShouldBeTrue();
    }

    [Fact]
    public void IsDue_WhenScheduledInFuture_ReturnsFalse()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            RequestType = "TestCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            ProcessedAtUtc = null
        };

        // Act & Assert
        message.IsDue().ShouldBeFalse();
    }

    [Fact]
    public void IsDue_WhenScheduledNowAndNotProcessed_ReturnsTrue()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            RequestType = "TestCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = null
        };

        // Act & Assert
        message.IsDue().ShouldBeTrue();
    }

    [Fact]
    public void IsDue_WhenScheduledInPastButAlreadyProcessed_ReturnsFalse()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            RequestType = "TestCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            ErrorMessage = null
        };

        // Act & Assert - IsProcessed is true, so IsDue is false
        message.IsDue().ShouldBeFalse();
    }

    #endregion

    #region IsDeadLettered Tests

    [Fact]
    public void IsDeadLettered_WhenRetryCountExceedsMaxAndNotProcessed_ReturnsTrue()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            RequestType = "TestCommand",
            Content = "{}",
            RetryCount = 5,
            ProcessedAtUtc = null
        };

        // Act & Assert
        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountEqualsMaxAndNotProcessed_ReturnsTrue()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            RequestType = "TestCommand",
            Content = "{}",
            RetryCount = 3,
            ProcessedAtUtc = null
        };

        // Act & Assert
        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountBelowMax_ReturnsFalse()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            RequestType = "TestCommand",
            Content = "{}",
            RetryCount = 2,
            ProcessedAtUtc = null
        };

        // Act & Assert
        message.IsDeadLettered(maxRetries: 3).ShouldBeFalse();
    }

    [Fact]
    public void IsDeadLettered_WhenProcessed_ReturnsFalse()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            RequestType = "TestCommand",
            Content = "{}",
            RetryCount = 5,
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorMessage = null
        };

        // Act & Assert
        message.IsDeadLettered(maxRetries: 3).ShouldBeFalse();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_SetAndGetCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow.AddHours(24);
        var createdAt = DateTime.UtcNow;
        var processedAt = DateTime.UtcNow.AddHours(25);
        var nextRetry = DateTime.UtcNow.AddHours(26);
        var lastExecuted = DateTime.UtcNow.AddHours(24);

        // Act
        var message = new ScheduledMessage
        {
            Id = id,
            RequestType = "SendReminderCommand",
            Content = "{\"userId\":123}",
            ScheduledAtUtc = scheduledAt,
            CreatedAtUtc = createdAt,
            ProcessedAtUtc = processedAt,
            ErrorMessage = null,
            RetryCount = 1,
            NextRetryAtUtc = nextRetry,
            CorrelationId = "corr-123",
            Metadata = "{\"priority\":\"high\"}",
            IsRecurring = true,
            CronExpression = "0 0 * * *",
            LastExecutedAtUtc = lastExecuted
        };

        // Assert
        message.Id.ShouldBe(id);
        message.RequestType.ShouldBe("SendReminderCommand");
        message.Content.ShouldBe("{\"userId\":123}");
        message.ScheduledAtUtc.ShouldBe(scheduledAt);
        message.CreatedAtUtc.ShouldBe(createdAt);
        message.ProcessedAtUtc.ShouldBe(processedAt);
        message.ErrorMessage.ShouldBeNull();
        message.RetryCount.ShouldBe(1);
        message.NextRetryAtUtc.ShouldBe(nextRetry);
        message.CorrelationId.ShouldBe("corr-123");
        message.Metadata.ShouldBe("{\"priority\":\"high\"}");
        message.IsRecurring.ShouldBeTrue();
        message.CronExpression.ShouldBe("0 0 * * *");
        message.LastExecutedAtUtc.ShouldBe(lastExecuted);
    }

    [Fact]
    public void NullableProperties_CanBeSetToNull()
    {
        // Arrange & Act
        var message = new ScheduledMessage
        {
            RequestType = "Test",
            Content = "{}",
            ProcessedAtUtc = null,
            ErrorMessage = null,
            NextRetryAtUtc = null,
            CorrelationId = null,
            Metadata = null,
            CronExpression = null,
            LastExecutedAtUtc = null
        };

        // Assert
        message.ProcessedAtUtc.ShouldBeNull();
        message.ErrorMessage.ShouldBeNull();
        message.NextRetryAtUtc.ShouldBeNull();
        message.CorrelationId.ShouldBeNull();
        message.Metadata.ShouldBeNull();
        message.CronExpression.ShouldBeNull();
        message.LastExecutedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void DefaultRetryCount_IsZero()
    {
        // Arrange & Act
        var message = new ScheduledMessage
        {
            RequestType = "Test",
            Content = "{}"
        };

        // Assert
        message.RetryCount.ShouldBe(0);
    }

    [Fact]
    public void DefaultIsRecurring_IsFalse()
    {
        // Arrange & Act
        var message = new ScheduledMessage
        {
            RequestType = "Test",
            Content = "{}"
        };

        // Assert
        message.IsRecurring.ShouldBeFalse();
    }

    #endregion
}
