using Encina.Dapper.SqlServer.Scheduling;

namespace Encina.UnitTests.Dapper.SqlServer.Scheduling;

/// <summary>
/// Unit tests for <see cref="ScheduledMessage"/>.
/// </summary>
public sealed class ScheduledMessageTests
{
    #region IsDue Tests

    [Fact]
    public void IsDue_WhenScheduledTimeInPastAndNotProcessed_ReturnsTrue()
    {
        var message = new ScheduledMessage { ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5), ProcessedAtUtc = null };
        message.IsDue().ShouldBeTrue();
    }

    [Fact]
    public void IsDue_WhenScheduledTimeInFuture_ReturnsFalse()
    {
        var message = new ScheduledMessage { ScheduledAtUtc = DateTime.UtcNow.AddHours(1), ProcessedAtUtc = null };
        message.IsDue().ShouldBeFalse();
    }

    [Fact]
    public void IsDue_WhenAlreadyProcessedAndNotRecurring_ReturnsFalse()
    {
        var message = new ScheduledMessage { ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5), ProcessedAtUtc = DateTime.UtcNow, IsRecurring = false };
        message.IsDue().ShouldBeFalse();
    }

    #endregion

    #region IsProcessed Tests

    [Fact]
    public void IsProcessed_WhenProcessedAndNoError_ReturnsTrue()
    {
        var message = new ScheduledMessage { ProcessedAtUtc = DateTime.UtcNow, ErrorMessage = null };
        message.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public void IsProcessed_WhenNotProcessed_ReturnsFalse()
    {
        var message = new ScheduledMessage { ProcessedAtUtc = null };
        message.IsProcessed.ShouldBeFalse();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_SetAndGetCorrectly()
    {
        var id = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = id,
            RequestType = "ReminderCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(2),
            CreatedAtUtc = DateTime.UtcNow,
            CronExpression = "0 9 * * *",
            IsRecurring = true
        };

        message.Id.ShouldBe(id);
        message.IsRecurring.ShouldBeTrue();
    }

    #endregion
}
