using Encina.ADO.PostgreSQL.Scheduling;

namespace Encina.UnitTests.ADO.PostgreSQL.Scheduling;

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
}
