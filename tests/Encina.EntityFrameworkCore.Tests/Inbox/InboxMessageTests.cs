using Encina.EntityFrameworkCore.Inbox;
using Shouldly;
using Xunit;

namespace Encina.EntityFrameworkCore.Tests.Inbox;

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
            MessageId = "msg-1",
            RequestType = "TestCommand",
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
            MessageId = "msg-1",
            RequestType = "TestCommand",
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
            MessageId = "msg-1",
            RequestType = "TestCommand",
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
            MessageId = "msg-1",
            RequestType = "TestCommand",
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
            MessageId = "msg-1",
            RequestType = "TestCommand",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        // Act & Assert
        message.IsExpired().ShouldBeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresNow_ReturnsTrue()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "msg-1",
            RequestType = "TestCommand",
            ExpiresAtUtc = DateTime.UtcNow
        };

        // Act & Assert - At or before now is expired
        message.IsExpired().ShouldBeTrue();
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
        var processedAt = DateTime.UtcNow.AddMinutes(5);
        var nextRetry = DateTime.UtcNow.AddMinutes(10);

        // Act
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "CreateOrderCommand",
            ReceivedAtUtc = receivedAt,
            ExpiresAtUtc = expiresAt,
            ProcessedAtUtc = processedAt,
            Response = "{\"success\":true}",
            ErrorMessage = null,
            RetryCount = 2,
            NextRetryAtUtc = nextRetry,
            Metadata = "{\"source\":\"api\"}"
        };

        // Assert
        message.MessageId.ShouldBe(messageId);
        message.RequestType.ShouldBe("CreateOrderCommand");
        message.ReceivedAtUtc.ShouldBe(receivedAt);
        message.ExpiresAtUtc.ShouldBe(expiresAt);
        message.ProcessedAtUtc.ShouldBe(processedAt);
        message.Response.ShouldBe("{\"success\":true}");
        message.ErrorMessage.ShouldBeNull();
        message.RetryCount.ShouldBe(2);
        message.NextRetryAtUtc.ShouldBe(nextRetry);
        message.Metadata.ShouldBe("{\"source\":\"api\"}");
    }

    [Fact]
    public void NullableProperties_CanBeSetToNull()
    {
        // Arrange & Act
        var message = new InboxMessage
        {
            MessageId = "msg-1",
            RequestType = "Test",
            ProcessedAtUtc = null,
            Response = null,
            ErrorMessage = null,
            NextRetryAtUtc = null,
            Metadata = null
        };

        // Assert
        message.ProcessedAtUtc.ShouldBeNull();
        message.Response.ShouldBeNull();
        message.ErrorMessage.ShouldBeNull();
        message.NextRetryAtUtc.ShouldBeNull();
        message.Metadata.ShouldBeNull();
    }

    #endregion
}
