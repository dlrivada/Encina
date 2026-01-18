using Encina.MongoDB.Inbox;

namespace Encina.UnitTests.MongoDB.Inbox;

public sealed class InboxMessageFactoryTests
{
    private readonly InboxMessageFactory _factory = new();

    [Fact]
    public void Create_ReturnsInboxMessageWithCorrectProperties()
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
    }

    [Fact]
    public void Create_ReturnsConcreteType()
    {
        var result = _factory.Create("msg-123", "Type", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), null);

        result.ShouldBeOfType<InboxMessage>();
    }
}
