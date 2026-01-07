using Encina.ADO.Sqlite.Outbox;

namespace Encina.ADO.Sqlite.Tests.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxMessageFactory"/>.
/// </summary>
public sealed class OutboxMessageFactoryTests
{
    private readonly OutboxMessageFactory _factory = new();

    [Fact]
    public void Create_ReturnsOutboxMessageWithCorrectProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var notificationType = "OrderCreatedNotification";
        var content = """{"orderId": 123}""";
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var result = _factory.Create(id, notificationType, content, createdAtUtc);

        // Assert
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
        // Act
        var result = _factory.Create(
            Guid.NewGuid(),
            "Type",
            "{}",
            DateTime.UtcNow);

        // Assert
        result.ShouldBeOfType<OutboxMessage>();
    }
}
