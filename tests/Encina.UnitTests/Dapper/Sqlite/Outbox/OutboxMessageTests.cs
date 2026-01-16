using Encina.Dapper.Sqlite;
using Encina.Dapper.Sqlite.Outbox;

namespace Encina.UnitTests.Dapper.Sqlite.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxMessage"/>.
/// </summary>
public sealed class OutboxMessageTests
{
    #region IsProcessed Tests

    [Fact]
    public void IsProcessed_WhenProcessedAndNoError_ReturnsTrue()
    {
        // Arrange
        var message = new OutboxMessage
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
        var message = new OutboxMessage
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
        var message = new OutboxMessage
        {
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorMessage = "Some error"
        };

        // Act & Assert
        message.IsProcessed.ShouldBeFalse();
    }

    #endregion

    #region IsDeadLettered Tests

    [Fact]
    public void IsDeadLettered_WhenRetryCountExceedsMaxAndNotProcessed_ReturnsTrue()
    {
        // Arrange
        var message = new OutboxMessage
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
        var message = new OutboxMessage
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
        var message = new OutboxMessage
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
        var message = new OutboxMessage
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
        var createdAt = DateTime.UtcNow;
        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        var message = new OutboxMessage
        {
            Id = id,
            NotificationType = "OrderCreated",
            Content = "{\"orderId\":1}",
            CreatedAtUtc = createdAt,
            RetryCount = 2,
            NextRetryAtUtc = nextRetry
        };

        // Assert
        message.Id.ShouldBe(id);
        message.NotificationType.ShouldBe("OrderCreated");
        message.Content.ShouldBe("{\"orderId\":1}");
        message.CreatedAtUtc.ShouldBe(createdAt);
        message.RetryCount.ShouldBe(2);
        message.NextRetryAtUtc.ShouldBe(nextRetry);
    }

    #endregion
}
