using Encina.ADO.SqlServer.Scheduling;

namespace Encina.UnitTests.ADO.SqlServer.Scheduling;

public sealed class ScheduledMessageTests
{
    [Fact]
    public void IsDue_WhenScheduledTimeInPast_ReturnsTrue()
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

    [Fact]
    public void IsDeadLettered_WhenRetryCountExceedsMax_ReturnsTrue()
    {
        var message = new ScheduledMessage { RetryCount = 5, ProcessedAtUtc = null };
        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountBelowMax_ReturnsFalse()
    {
        var message = new ScheduledMessage { RetryCount = 2, ProcessedAtUtc = null };
        message.IsDeadLettered(maxRetries: 3).ShouldBeFalse();
    }
}
