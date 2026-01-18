using Encina.Dapper.SqlServer.Scheduling;

namespace Encina.UnitTests.Dapper.SqlServer.Scheduling;

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
        var content = """{"to": "test@example.com"}""";
        var scheduledAtUtc = DateTime.UtcNow.AddHours(1);
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var result = _factory.Create(id, requestType, content, scheduledAtUtc, createdAtUtc, false, null);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(id);
        result.RequestType.ShouldBe(requestType);
        result.Content.ShouldBe(content);
        result.ScheduledAtUtc.ShouldBe(scheduledAtUtc);
        result.CreatedAtUtc.ShouldBe(createdAtUtc);
        result.ProcessedAtUtc.ShouldBeNull();
        result.IsRecurring.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithRecurring_SetsIsRecurringAndCron()
    {
        // Arrange
        var cronExpression = "0 9 * * *";

        // Act
        var result = _factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, true, cronExpression);

        // Assert
        result.IsRecurring.ShouldBeTrue();
        result.CronExpression.ShouldBe(cronExpression);
    }

    [Fact]
    public void Create_ReturnsConcreteType()
    {
        // Act
        var result = _factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        // Assert
        result.ShouldBeOfType<ScheduledMessage>();
    }
}
