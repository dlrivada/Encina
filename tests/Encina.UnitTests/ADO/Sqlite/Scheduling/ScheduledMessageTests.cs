using Encina.ADO.Sqlite.Scheduling;

namespace Encina.UnitTests.ADO.Sqlite.Scheduling;

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
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorMessage = "Some error"
        };

        // Act & Assert
        message.IsProcessed.ShouldBeFalse();
    }

    #endregion

    #region IsDue Tests

    [Fact]
    public void IsDue_WhenScheduledTimeInPast_ReturnsTrue()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ProcessedAtUtc = null,
            IsRecurring = false
        };

        // Act & Assert
        message.IsDue().ShouldBeTrue();
    }

    [Fact]
    public void IsDue_WhenScheduledTimeInFuture_ReturnsFalse()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            ProcessedAtUtc = null,
            IsRecurring = false
        };

        // Act & Assert
        message.IsDue().ShouldBeFalse();
    }

    [Fact]
    public void IsDue_WhenAlreadyProcessedAndNotRecurring_ReturnsFalse()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            IsRecurring = false
        };

        // Act & Assert
        message.IsDue().ShouldBeFalse();
    }

    [Fact]
    public void IsDue_WhenRecurringAndNextRetryInPast_ReturnsTrue()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            ScheduledAtUtc = DateTime.UtcNow.AddDays(-1),
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(-5),
            ProcessedAtUtc = null,
            IsRecurring = true
        };

        // Act & Assert
        message.IsDue().ShouldBeTrue();
    }

    [Fact]
    public void IsDue_WhenNextRetryInFuture_ReturnsFalse()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            NextRetryAtUtc = DateTime.UtcNow.AddHours(1),
            ProcessedAtUtc = null,
            IsRecurring = false
        };

        // Act & Assert
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
            RetryCount = 5,
            ProcessedAtUtc = DateTime.UtcNow
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
        var scheduledAt = DateTime.UtcNow.AddHours(1);
        var createdAt = DateTime.UtcNow;

        // Act
        var message = new ScheduledMessage
        {
            Id = id,
            RequestType = "TestType",
            Content = "TestContent",
            ScheduledAtUtc = scheduledAt,
            CreatedAtUtc = createdAt,
            IsRecurring = true,
            CronExpression = "0 0 * * *",
            RetryCount = 2
        };

        // Assert
        message.Id.ShouldBe(id);
        message.RequestType.ShouldBe("TestType");
        message.Content.ShouldBe("TestContent");
        message.ScheduledAtUtc.ShouldBe(scheduledAt);
        message.CreatedAtUtc.ShouldBe(createdAt);
        message.IsRecurring.ShouldBeTrue();
        message.CronExpression.ShouldBe("0 0 * * *");
        message.RetryCount.ShouldBe(2);
    }

    #endregion
}
