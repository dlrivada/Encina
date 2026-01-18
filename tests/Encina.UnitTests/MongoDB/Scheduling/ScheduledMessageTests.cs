using Encina.MongoDB.Scheduling;

namespace Encina.UnitTests.MongoDB.Scheduling;

public sealed class ScheduledMessageTests
{
    [Fact]
    public void IsDue_WhenScheduledTimeInPast_ReturnsTrue()
    {
        var message = new ScheduledMessage { ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5) };

        message.IsDue().ShouldBeTrue();
    }

    [Fact]
    public void IsDue_WhenScheduledTimeInFuture_ReturnsFalse()
    {
        var message = new ScheduledMessage { ScheduledAtUtc = DateTime.UtcNow.AddHours(1) };

        message.IsDue().ShouldBeFalse();
    }

    [Fact]
    public void IsProcessed_WhenProcessedAtUtcHasValue_ReturnsTrue()
    {
        var message = new ScheduledMessage { ProcessedAtUtc = DateTime.UtcNow };

        message.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public void IsProcessed_WhenProcessedAtUtcIsNull_ReturnsFalse()
    {
        var message = new ScheduledMessage { ProcessedAtUtc = null };

        message.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountEqualsMaxRetries_ReturnsTrue()
    {
        var message = new ScheduledMessage { RetryCount = 3 };

        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountBelowMaxRetries_ReturnsFalse()
    {
        var message = new ScheduledMessage { RetryCount = 2 };

        message.IsDeadLettered(maxRetries: 3).ShouldBeFalse();
    }
}
