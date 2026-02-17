using Encina.ADO.MySQL.Inbox;

namespace Encina.UnitTests.ADO.MySQL.Inbox;

/// <summary>
/// Unit tests for <see cref="InboxMessage"/>.
/// </summary>
public sealed class InboxMessageTests
{
    #region IsProcessed Tests

    [Fact]
    public void IsProcessed_WhenProcessedAndNoError_ReturnsTrue()
    {
        // Arrange
        var message = new InboxMessage
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
        var message = new InboxMessage
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
        var message = new InboxMessage
        {
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorMessage = "Processing failed"
        };

        // Act & Assert
        message.IsProcessed.ShouldBeFalse();
    }

    #endregion

    #region IsExpired Tests

    [Fact]
    public void IsExpired_WhenExpiresInPast_ReturnsTrue()
    {
        // Arrange
        var message = new InboxMessage
        {
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };

        // Act & Assert
        message.IsExpired().ShouldBeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresInFuture_ReturnsFalse()
    {
        // Arrange
        var message = new InboxMessage
        {
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        // Act & Assert
        message.IsExpired().ShouldBeFalse();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_SetAndGetCorrectly()
    {
        // Arrange
        var messageId = "msg-12345";
        var receivedAt = DateTime.UtcNow;
        var expiresAt = DateTime.UtcNow.AddHours(24);
        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "CreateOrderCommand",
            ReceivedAtUtc = receivedAt,
            ExpiresAtUtc = expiresAt,
            Response = "{\"success\":true}",
            Metadata = "{\"source\":\"api\"}",
            RetryCount = 1,
            NextRetryAtUtc = nextRetry
        };

        // Assert
        message.MessageId.ShouldBe(messageId);
        message.RequestType.ShouldBe("CreateOrderCommand");
        message.ReceivedAtUtc.ShouldBe(receivedAt);
        message.ExpiresAtUtc.ShouldBe(expiresAt);
        message.Response.ShouldBe("{\"success\":true}");
        message.Metadata.ShouldBe("{\"source\":\"api\"}");
        message.RetryCount.ShouldBe(1);
        message.NextRetryAtUtc.ShouldBe(nextRetry);
    }

    #endregion
}
