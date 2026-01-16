using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Outbox;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxMessageFactory"/>.
/// </summary>
public sealed class OutboxMessageFactoryTests
{
    [Fact]
    public void Create_ValidParameters_ReturnsOutboxMessage()
    {
        // Arrange
        var factory = new OutboxMessageFactory();
        var id = Guid.NewGuid();
        var notificationType = "TestNotification";
        var content = "{\"value\":123}";
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var result = factory.Create(id, notificationType, content, createdAtUtc);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<OutboxMessage>();
    }

    [Fact]
    public void Create_SetsIdCorrectly()
    {
        // Arrange
        var factory = new OutboxMessageFactory();
        var id = Guid.NewGuid();

        // Act
        var result = factory.Create(id, "Type", "{}", DateTime.UtcNow);

        // Assert
        result.Id.ShouldBe(id);
    }

    [Fact]
    public void Create_SetsNotificationTypeCorrectly()
    {
        // Arrange
        var factory = new OutboxMessageFactory();
        var notificationType = "TestNotification, TestAssembly";

        // Act
        var result = factory.Create(Guid.NewGuid(), notificationType, "{}", DateTime.UtcNow);

        // Assert
        result.NotificationType.ShouldBe(notificationType);
    }

    [Fact]
    public void Create_SetsContentCorrectly()
    {
        // Arrange
        var factory = new OutboxMessageFactory();
        var content = "{\"orderId\":123,\"amount\":99.99}";

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", content, DateTime.UtcNow);

        // Assert
        result.Content.ShouldBe(content);
    }

    [Fact]
    public void Create_SetsCreatedAtUtcCorrectly()
    {
        // Arrange
        var factory = new OutboxMessageFactory();
        var createdAtUtc = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", createdAtUtc);

        // Assert
        result.CreatedAtUtc.ShouldBe(createdAtUtc);
    }

    [Fact]
    public void Create_SetsRetryCountToZero()
    {
        // Arrange
        var factory = new OutboxMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow);

        // Assert
        result.RetryCount.ShouldBe(0);
    }

    [Fact]
    public void Create_LeavesProcessedAtUtcNull()
    {
        // Arrange
        var factory = new OutboxMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow);

        // Assert
        var message = result as OutboxMessage;
        message.ShouldNotBeNull();
        message.ProcessedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Create_LeavesErrorMessageNull()
    {
        // Arrange
        var factory = new OutboxMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow);

        // Assert
        var message = result as OutboxMessage;
        message.ShouldNotBeNull();
        message.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Create_LeavesNextRetryAtUtcNull()
    {
        // Arrange
        var factory = new OutboxMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow);

        // Assert
        var message = result as OutboxMessage;
        message.ShouldNotBeNull();
        message.NextRetryAtUtc.ShouldBeNull();
    }
}
