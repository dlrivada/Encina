using Encina.Dapper.SqlServer.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.UnitTests.Dapper.SqlServer.Inbox;

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
        var expiresAtUtc = DateTime.UtcNow.AddHours(24);

        // Act
        var result = _factory.Create(messageId, requestType, receivedAtUtc, expiresAtUtc, null);

        // Assert
        result.ShouldNotBeNull();
        result.MessageId.ShouldBe(messageId);
        result.RequestType.ShouldBe(requestType);
        result.ReceivedAtUtc.ShouldBe(receivedAtUtc);
        result.ExpiresAtUtc.ShouldBe(expiresAtUtc);
        result.ProcessedAtUtc.ShouldBeNull();
        result.RetryCount.ShouldBe(0);
    }

    [Fact]
    public void Create_WithMetadata_SerializesMetadata()
    {
        // Arrange
        var metadata = new InboxMetadata { CorrelationId = "corr-123" };

        // Act
        var result = _factory.Create("msg-1", "Type", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), metadata);

        // Assert
        result.ShouldNotBeNull();
        var inboxMessage = result.ShouldBeOfType<InboxMessage>();
        inboxMessage.Metadata.ShouldNotBeNull();
        inboxMessage.Metadata.ShouldContain("corr-123");
    }

    [Fact]
    public void Create_ReturnsConcreteInboxMessageType()
    {
        // Act
        var result = _factory.Create("msg-456", "Type", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), null);

        // Assert
        result.ShouldBeOfType<InboxMessage>();
    }
}
