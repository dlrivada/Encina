using Encina.ADO.SqlServer.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.UnitTests.ADO.SqlServer.Inbox;

public sealed class InboxMessageFactoryTests
{
    private readonly InboxMessageFactory _factory = new();

    [Fact]
    public void Create_WithoutMetadata_ReturnsInboxMessageWithCorrectProperties()
    {
        var messageId = "msg-12345";
        var requestType = "CreateOrderCommand";
        var receivedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = DateTime.UtcNow.AddHours(24);

        var result = _factory.Create(messageId, requestType, receivedAtUtc, expiresAtUtc, null);

        result.ShouldNotBeNull();
        result.MessageId.ShouldBe(messageId);
        result.RequestType.ShouldBe(requestType);
        result.ReceivedAtUtc.ShouldBe(receivedAtUtc);
        result.ExpiresAtUtc.ShouldBe(expiresAtUtc);
        result.RetryCount.ShouldBe(0);
        var inboxMessage = result.ShouldBeOfType<InboxMessage>();
        inboxMessage.Metadata.ShouldBeNull();
    }

    [Fact]
    public void Create_WithMetadata_SerializesMetadataToJson()
    {
        var metadata = new InboxMetadata
        {
            CorrelationId = "corr-123",
            UserId = "user-456"
        };

        var result = _factory.Create("msg-123", "TestCommand", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), metadata);

        var inboxMessage = result.ShouldBeOfType<InboxMessage>();
        inboxMessage.Metadata.ShouldNotBeNull();
        inboxMessage.Metadata.ShouldContain("corr-123");
        inboxMessage.Metadata.ShouldContain("user-456");
    }

    [Fact]
    public void Create_ReturnsConcreteType()
    {
        var result = _factory.Create("msg-123", "Type", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), null);

        result.ShouldBeOfType<InboxMessage>();
    }
}
