using Encina.ADO.Oracle.Outbox;

namespace Encina.UnitTests.ADO.Oracle.Outbox;

public sealed class OutboxMessageTests
{
    [Fact]
    public void IsProcessed_WhenProcessedAndNoError_ReturnsTrue()
    {
        var message = new OutboxMessage { ProcessedAtUtc = DateTime.UtcNow, ErrorMessage = null };
        message.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public void IsProcessed_WhenNotProcessed_ReturnsFalse()
    {
        var message = new OutboxMessage { ProcessedAtUtc = null };
        message.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountExceedsMax_ReturnsTrue()
    {
        var message = new OutboxMessage { RetryCount = 5, ProcessedAtUtc = null };
        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }
}
