using Encina.Dapper.Sqlite.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.Dapper.Sqlite.Tests.Inbox;

/// <summary>
/// Unit tests for <see cref="InboxMessageFactory"/>.
/// </summary>
public sealed class InboxMessageFactoryTests
{
    private readonly InboxMessageFactory _factory = new();

    [Fact]
    public void Create_ReturnsInboxMessageWithCorrectProperties()
    {
        // Arrange
        var messageId = "msg-123";
        var requestType = "TestRequest";
        var receivedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = DateTime.UtcNow.AddHours(1);

        // Act
        var result = _factory.Create(messageId, requestType, receivedAtUtc, expiresAtUtc, null);

        // Assert
        result.ShouldNotBeNull();
        result.MessageId.ShouldBe(messageId);
        result.RequestType.ShouldBe(requestType);
        result.ReceivedAtUtc.ShouldBe(receivedAtUtc);
        result.ExpiresAtUtc.ShouldBe(expiresAtUtc);
        result.RetryCount.ShouldBe(0);
    }

    [Fact]
    public void Create_WithMetadata_SerializesMetadata()
    {
        // Arrange
        var messageId = "msg-456";
        var requestType = "TestRequest";
        var receivedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = DateTime.UtcNow.AddHours(1);
        var metadata = new InboxMetadata { CorrelationId = "corr-123" };

        // Act
        var result = _factory.Create(messageId, requestType, receivedAtUtc, expiresAtUtc, metadata);

        // Assert
        result.ShouldNotBeNull();
        var inboxMessage = result.ShouldBeOfType<InboxMessage>();
        inboxMessage.Metadata.ShouldNotBeNull();
        inboxMessage.Metadata.ShouldContain("corr-123");
    }

    [Fact]
    public void Create_WithNullMetadata_SetsMetadataToNull()
    {
        // Arrange
        var messageId = "msg-789";
        var requestType = "TestRequest";
        var receivedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = DateTime.UtcNow.AddHours(1);

        // Act
        var result = _factory.Create(messageId, requestType, receivedAtUtc, expiresAtUtc, null);

        // Assert
        var inboxMessage = result.ShouldBeOfType<InboxMessage>();
        inboxMessage.Metadata.ShouldBeNull();
    }
}
