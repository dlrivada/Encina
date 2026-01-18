using Encina.Dapper.PostgreSQL.Outbox;

namespace Encina.UnitTests.Dapper.PostgreSQL.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxMessage"/>.
/// </summary>
public sealed class OutboxMessageTests
{
    #region IsProcessed Tests

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
    public void IsProcessed_WhenProcessedWithError_ReturnsFalse()
    {
        var message = new OutboxMessage { ProcessedAtUtc = DateTime.UtcNow, ErrorMessage = "Error" };
        message.IsProcessed.ShouldBeFalse();
    }

    #endregion

    #region IsDeadLettered Tests

    [Fact]
    public void IsDeadLettered_WhenRetryCountExceedsMax_ReturnsTrue()
    {
        var message = new OutboxMessage { RetryCount = 5, ProcessedAtUtc = null };
        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountBelowMax_ReturnsFalse()
    {
        var message = new OutboxMessage { RetryCount = 2, ProcessedAtUtc = null };
        message.IsDeadLettered(maxRetries: 3).ShouldBeFalse();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_SetAndGetCorrectly()
    {
        var id = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = id,
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 1
        };

        message.Id.ShouldBe(id);
        message.NotificationType.ShouldBe("Test");
    }

    #endregion
}
