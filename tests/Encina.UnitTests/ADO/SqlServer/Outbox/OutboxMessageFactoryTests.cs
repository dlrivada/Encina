using Encina.ADO.SqlServer.Outbox;

namespace Encina.UnitTests.ADO.SqlServer.Outbox;

public sealed class OutboxMessageFactoryTests
{
    private readonly OutboxMessageFactory _factory = new();

    [Fact]
    public void Create_ReturnsOutboxMessageWithCorrectProperties()
    {
        var id = Guid.NewGuid();
        var notificationType = "OrderCreatedNotification";
        var content = """{"orderId": 123}""";
        var createdAtUtc = DateTime.UtcNow;

        var result = _factory.Create(id, notificationType, content, createdAtUtc);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(id);
        result.NotificationType.ShouldBe(notificationType);
        result.Content.ShouldBe(content);
        result.CreatedAtUtc.ShouldBe(createdAtUtc);
        result.RetryCount.ShouldBe(0);
    }

    [Fact]
    public void Create_ReturnsConcreteType()
    {
        var result = _factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow);

        result.ShouldBeOfType<OutboxMessage>();
    }
}
