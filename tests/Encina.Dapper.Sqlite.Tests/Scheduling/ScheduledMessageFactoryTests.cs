using Encina.Dapper.Sqlite.Scheduling;

namespace Encina.Dapper.Sqlite.Tests.Scheduling;

/// <summary>
/// Unit tests for <see cref="ScheduledMessageFactory"/>.
/// </summary>
public sealed class ScheduledMessageFactoryTests
{
    private readonly ScheduledMessageFactory _factory = new();

    [Fact]
    public void Create_ReturnsScheduledMessageWithCorrectProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var requestType = "SendEmailCommand";
        var content = """{"email": "test@example.com"}""";
        var scheduledAtUtc = DateTime.UtcNow.AddHours(1);
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var result = _factory.Create(
            id,
            requestType,
            content,
            scheduledAtUtc,
            createdAtUtc,
            isRecurring: false,
            cronExpression: null);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(id);
        result.RequestType.ShouldBe(requestType);
        result.Content.ShouldBe(content);
        result.ScheduledAtUtc.ShouldBe(scheduledAtUtc);
        result.CreatedAtUtc.ShouldBe(createdAtUtc);
        result.IsRecurring.ShouldBeFalse();
        result.CronExpression.ShouldBeNull();
        result.RetryCount.ShouldBe(0);
    }

    [Fact]
    public void Create_WithRecurringMessage_SetsRecurringProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cronExpression = "0 0 * * *"; // Daily at midnight

        // Act
        var result = _factory.Create(
            id,
            "DailyReport",
            "{}",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow,
            isRecurring: true,
            cronExpression: cronExpression);

        // Assert
        result.ShouldNotBeNull();
        result.IsRecurring.ShouldBeTrue();
        result.CronExpression.ShouldBe(cronExpression);
    }

    [Fact]
    public void Create_ReturnsConcreteType()
    {
        // Act
        var result = _factory.Create(
            Guid.NewGuid(),
            "Type",
            "{}",
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);

        // Assert
        result.ShouldBeOfType<ScheduledMessage>();
    }
}
