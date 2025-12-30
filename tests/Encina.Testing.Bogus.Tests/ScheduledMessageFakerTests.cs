using Shouldly;
using Xunit;

namespace Encina.Testing.Bogus.Tests;

/// <summary>
/// Unit tests for <see cref="ScheduledMessageFaker"/>.
/// </summary>
public sealed class ScheduledMessageFakerTests
{
    [Fact]
    public void Generate_ShouldCreateValidScheduledMessage()
    {
        // Arrange
        var faker = new ScheduledMessageFaker();

        // Act
        var message = faker.Generate();

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldNotBe(Guid.Empty);
        message.RequestType.ShouldNotBeNullOrEmpty();
        message.Content.ShouldNotBeNullOrEmpty();
        message.ScheduledAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        message.CreatedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        message.ProcessedAtUtc.ShouldBeNull();
        message.ErrorMessage.ShouldBeNull();
        message.RetryCount.ShouldBe(0);
        message.NextRetryAtUtc.ShouldBeNull();
        message.IsRecurring.ShouldBeFalse();
        message.CronExpression.ShouldBeNull();
        message.LastExecutedAtUtc.ShouldBeNull();
        message.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void Generate_ShouldBeReproducible()
    {
        // Arrange
        var faker1 = new ScheduledMessageFaker();
        var faker2 = new ScheduledMessageFaker();

        // Act
        var message1 = faker1.Generate();
        var message2 = faker2.Generate();

        // Assert
        message1.Id.ShouldBe(message2.Id);
        message1.RequestType.ShouldBe(message2.RequestType);
    }

    [Fact]
    public void AsProcessed_ShouldSetProcessedTimestamp()
    {
        // Arrange
        var faker = new ScheduledMessageFaker().AsProcessed();

        // Act
        var message = faker.Generate();

        // Assert
        message.ProcessedAtUtc.ShouldNotBeNull();
        message.ProcessedAtUtc!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        message.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public void AsFailed_ShouldSetErrorAndRetryInfo()
    {
        // Arrange
        var faker = new ScheduledMessageFaker().AsFailed(retryCount: 3);

        // Act
        var message = faker.Generate();

        // Assert
        message.ErrorMessage.ShouldNotBeNullOrEmpty();
        message.RetryCount.ShouldBe(3);
        message.NextRetryAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void AsFailed_ShouldUseDefaultRetryCount()
    {
        // Arrange
        var faker = new ScheduledMessageFaker().AsFailed();

        // Act
        var message = faker.Generate();

        // Assert
        message.RetryCount.ShouldBe(2);
    }

    [Fact]
    public void AsDue_ShouldSetPastScheduledTime()
    {
        // Arrange
        var faker = new ScheduledMessageFaker().AsDue();

        // Act
        var message = faker.Generate();

        // Assert
        message.ScheduledAtUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        message.IsDue().ShouldBeTrue();
    }

    [Fact]
    public void AsRecurring_ShouldSetRecurringFlag()
    {
        // Arrange
        var faker = new ScheduledMessageFaker().AsRecurring();

        // Act
        var message = faker.Generate();

        // Assert
        message.IsRecurring.ShouldBeTrue();
        message.CronExpression.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void AsRecurring_WithCustomCron_ShouldSetSpecificExpression()
    {
        // Arrange
        var cron = "0 0 12 * * ?";
        var faker = new ScheduledMessageFaker().AsRecurring(cron);

        // Act
        var message = faker.Generate();

        // Assert
        message.IsRecurring.ShouldBeTrue();
        message.CronExpression.ShouldBe(cron);
    }

    [Fact]
    public void AsRecurringExecuted_ShouldSetLastExecutedTime()
    {
        // Arrange
        var faker = new ScheduledMessageFaker().AsRecurringExecuted();

        // Act
        var message = faker.Generate();

        // Assert
        message.IsRecurring.ShouldBeTrue();
        message.CronExpression.ShouldNotBeNullOrEmpty();
        message.LastExecutedAtUtc.ShouldNotBeNull();
        message.LastExecutedAtUtc!.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void WithRequestType_ShouldSetSpecificType()
    {
        // Arrange
        var faker = new ScheduledMessageFaker().WithRequestType("SendReminder");

        // Act
        var message = faker.Generate();

        // Assert
        message.RequestType.ShouldBe("SendReminder");
    }

    [Fact]
    public void WithContent_ShouldSetSpecificContent()
    {
        // Arrange
        var content = "{\"userId\": \"123\"}";
        var faker = new ScheduledMessageFaker().WithContent(content);

        // Act
        var message = faker.Generate();

        // Assert
        message.Content.ShouldBe(content);
    }

    [Fact]
    public void ScheduledAt_ShouldSetSpecificTime()
    {
        // Arrange
        var scheduledTime = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var faker = new ScheduledMessageFaker().ScheduledAt(scheduledTime);

        // Act
        var message = faker.Generate();

        // Assert
        message.ScheduledAtUtc.ShouldBe(scheduledTime);
    }

    [Fact]
    public void GenerateMessage_ShouldReturnAsInterface()
    {
        // Arrange
        var faker = new ScheduledMessageFaker();

        // Act
        var message = faker.GenerateMessage();

        // Assert
        message.ShouldNotBeNull();
        message.ShouldBeOfType<FakeScheduledMessage>();
    }

    [Fact]
    public void GenerateMultiple_ShouldCreateUniqueMessages()
    {
        // Arrange
        var faker = new ScheduledMessageFaker();

        // Act
        var messages = faker.Generate(5);

        // Assert
        messages.Count.ShouldBe(5);
        messages.Select(m => m.Id).Distinct().Count().ShouldBe(5);
    }

    [Fact]
    public void IsDeadLettered_ShouldReturnTrueWhenExceedsMaxRetries()
    {
        // Arrange
        var faker = new ScheduledMessageFaker().AsFailed(retryCount: 5);
        var message = faker.Generate();

        // Act & Assert
        message.IsDeadLettered(3).ShouldBeTrue();
        message.IsDeadLettered(5).ShouldBeTrue();
        message.IsDeadLettered(10).ShouldBeFalse();
    }

    [Fact]
    public void IsDue_ShouldReturnFalseForFutureSchedule()
    {
        // Arrange
        var faker = new ScheduledMessageFaker();

        // Act
        var message = faker.Generate();

        // Assert
        // Default generates future scheduled time
        message.IsDue().ShouldBeFalse();
    }

    [Fact]
    public void IsProcessed_ShouldReturnFalseForRecurring()
    {
        // Arrange - Recurring messages are never "processed" even if they have a ProcessedAtUtc
        var faker = new ScheduledMessageFaker()
            .AsRecurring()
            .AsProcessed();

        // Act
        var message = faker.Generate();

        // Assert
        message.ProcessedAtUtc.ShouldNotBeNull();
        message.IsRecurring.ShouldBeTrue();
        message.IsProcessed.ShouldBeFalse(); // Recurring messages are never done
    }

    [Fact]
    public void MethodChaining_ShouldWork()
    {
        // Arrange & Act
        var message = new ScheduledMessageFaker()
            .WithRequestType("DailyReport")
            .WithContent("{\"type\": \"summary\"}")
            .AsRecurring("0 0 6 * * ?")
            .Generate();

        // Assert
        message.RequestType.ShouldBe("DailyReport");
        message.Content.ShouldBe("{\"type\": \"summary\"}");
        message.IsRecurring.ShouldBeTrue();
        message.CronExpression.ShouldBe("0 0 6 * * ?");
    }
}
