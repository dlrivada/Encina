using Encina.Dapper.Sqlite;
using Encina.Dapper.Sqlite.Outbox;

namespace Encina.UnitTests.Dapper.Sqlite.Outbox;

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
        var notificationType = "TestNotification";
        var content = """{"key": "value"}""";
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
        result.ProcessedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Create_ReturnsConcreteOutboxMessageType()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = _factory.Create(id, "Type", "Content", DateTime.UtcNow);

        // Assert
        result.ShouldBeOfType<OutboxMessage>();
    }
}
