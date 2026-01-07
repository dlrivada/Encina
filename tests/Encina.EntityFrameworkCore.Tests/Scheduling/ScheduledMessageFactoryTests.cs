using Encina.EntityFrameworkCore.Scheduling;
using Shouldly;
using Xunit;

namespace Encina.EntityFrameworkCore.Tests.Scheduling;

/// <summary>
/// Unit tests for <see cref="ScheduledMessageFactory"/>.
/// </summary>
public sealed class ScheduledMessageFactoryTests
{
    [Fact]
    public void Create_ValidParameters_ReturnsScheduledMessage()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();
        var id = Guid.NewGuid();
        var requestType = "SendReminderCommand";
        var content = "{\"userId\":123}";
        var scheduledAtUtc = DateTime.UtcNow.AddHours(24);
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var result = factory.Create(id, requestType, content, scheduledAtUtc, createdAtUtc, false, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<ScheduledMessage>();
    }

    [Fact]
    public void Create_SetsIdCorrectly()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();
        var id = Guid.NewGuid();

        // Act
        var result = factory.Create(id, "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        result.Id.ShouldBe(id);
    }

    [Fact]
    public void Create_SetsRequestTypeCorrectly()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();
        var requestType = "ArchiveOrderCommand, MyAssembly";

        // Act
        var result = factory.Create(Guid.NewGuid(), requestType, "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        result.RequestType.ShouldBe(requestType);
    }

    [Fact]
    public void Create_SetsContentCorrectly()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();
        var content = "{\"orderId\":456,\"reason\":\"expiry\"}";

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", content, DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        result.Content.ShouldBe(content);
    }

    [Fact]
    public void Create_SetsScheduledAtUtcCorrectly()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();
        var scheduledAtUtc = new DateTime(2025, 6, 20, 14, 0, 0, DateTimeKind.Utc);

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", scheduledAtUtc, DateTime.UtcNow, false, null);

        // Assert
        result.ScheduledAtUtc.ShouldBe(scheduledAtUtc);
    }

    [Fact]
    public void Create_SetsCreatedAtUtcCorrectly()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();
        var createdAtUtc = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), createdAtUtc, false, null);

        // Assert
        result.CreatedAtUtc.ShouldBe(createdAtUtc);
    }

    [Fact]
    public void Create_SetsIsRecurringCorrectly_WhenFalse()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        result.IsRecurring.ShouldBeFalse();
    }

    [Fact]
    public void Create_SetsIsRecurringCorrectly_WhenTrue()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, true, "0 0 * * *");

        // Assert
        result.IsRecurring.ShouldBeTrue();
    }

    [Fact]
    public void Create_SetsCronExpressionCorrectly_WhenProvided()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();
        var cronExpression = "0 0 * * *"; // Daily at midnight

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, true, cronExpression);

        // Assert
        result.CronExpression.ShouldBe(cronExpression);
    }

    [Fact]
    public void Create_SetsCronExpressionCorrectly_WhenNull()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        result.CronExpression.ShouldBeNull();
    }

    [Fact]
    public void Create_SetsRetryCountToZero()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        result.RetryCount.ShouldBe(0);
    }

    [Fact]
    public void Create_LeavesProcessedAtUtcNull()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        var message = result as ScheduledMessage;
        message.ShouldNotBeNull();
        message.ProcessedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Create_LeavesErrorMessageNull()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        var message = result as ScheduledMessage;
        message.ShouldNotBeNull();
        message.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Create_LeavesNextRetryAtUtcNull()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        var message = result as ScheduledMessage;
        message.ShouldNotBeNull();
        message.NextRetryAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Create_LeavesCorrelationIdNull()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        var message = result as ScheduledMessage;
        message.ShouldNotBeNull();
        message.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public void Create_LeavesMetadataNull()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        var message = result as ScheduledMessage;
        message.ShouldNotBeNull();
        message.Metadata.ShouldBeNull();
    }

    [Fact]
    public void Create_LeavesLastExecutedAtUtcNull()
    {
        // Arrange
        var factory = new ScheduledMessageFactory();

        // Act
        var result = factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        var message = result as ScheduledMessage;
        message.ShouldNotBeNull();
        message.LastExecutedAtUtc.ShouldBeNull();
    }
}
