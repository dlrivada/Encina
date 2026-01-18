using Encina.MongoDB.Scheduling;

namespace Encina.UnitTests.MongoDB.Scheduling;

public sealed class ScheduledMessageFactoryTests
{
    private readonly ScheduledMessageFactory _factory = new();

    [Fact]
    public void Create_ReturnsScheduledMessageWithCorrectProperties()
    {
        var id = Guid.NewGuid();
        var requestType = "SendReminderCommand";
        var content = """{"userId": "user-123"}""";
        var scheduledAtUtc = DateTime.UtcNow.AddHours(1);
        var createdAtUtc = DateTime.UtcNow;

        var result = _factory.Create(id, requestType, content, scheduledAtUtc, createdAtUtc, false, null);

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
    public void Create_WithRecurring_SetsRecurringProperties()
    {
        var cronExpression = "0 0 * * *";

        var result = _factory.Create(
            Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, true, cronExpression);

        result.IsRecurring.ShouldBeTrue();
        result.CronExpression.ShouldBe(cronExpression);
    }

    [Fact]
    public void Create_ReturnsConcreteType()
    {
        var result = _factory.Create(Guid.NewGuid(), "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);

        result.ShouldBeOfType<ScheduledMessage>();
    }
}
