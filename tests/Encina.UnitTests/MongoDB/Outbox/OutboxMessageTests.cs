using Encina.MongoDB.Outbox;

namespace Encina.UnitTests.MongoDB.Outbox;

public sealed class OutboxMessageTests
{
    [Fact]
    public void IsProcessed_WhenProcessedAtUtcHasValue_ReturnsTrue()
    {
        var message = new OutboxMessage { ProcessedAtUtc = DateTime.UtcNow };

        message.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public void IsProcessed_WhenProcessedAtUtcIsNull_ReturnsFalse()
    {
        var message = new OutboxMessage { ProcessedAtUtc = null };

        message.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountEqualsMaxRetries_ReturnsTrue()
    {
        var message = new OutboxMessage { RetryCount = 3 };

        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountExceedsMaxRetries_ReturnsTrue()
    {
        var message = new OutboxMessage { RetryCount = 5 };

        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountBelowMaxRetries_ReturnsFalse()
    {
        var message = new OutboxMessage { RetryCount = 2 };

        message.IsDeadLettered(maxRetries: 3).ShouldBeFalse();
    }
}
