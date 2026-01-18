using Encina.ADO.SqlServer.Outbox;

namespace Encina.UnitTests.ADO.SqlServer.Outbox;

public sealed class OutboxMessageTests
{
    [Fact]
    public void IsProcessed_WhenProcessedAndNoError_ReturnsTrue()
    {
        var message = new OutboxMessage
        {
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorMessage = null
        };

        message.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public void IsProcessed_WhenNotProcessed_ReturnsFalse()
    {
        var message = new OutboxMessage { ProcessedAtUtc = null };

        message.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void IsProcessed_WhenProcessedWithError_ReturnsFalse()
    {
        var message = new OutboxMessage
        {
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorMessage = "Some error"
        };

        message.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountExceedsMax_ReturnsTrue()
    {
        var message = new OutboxMessage
        {
            RetryCount = 5,
            ProcessedAtUtc = null
        };

        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountBelowMax_ReturnsFalse()
    {
        var message = new OutboxMessage
        {
            RetryCount = 2,
            ProcessedAtUtc = null
        };

        message.IsDeadLettered(maxRetries: 3).ShouldBeFalse();
    }
}
