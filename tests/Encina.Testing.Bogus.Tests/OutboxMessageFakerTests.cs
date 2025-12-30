using Shouldly;
using Xunit;

namespace Encina.Testing.Bogus.Tests;

/// <summary>
/// Unit tests for <see cref="OutboxMessageFaker"/>.
/// </summary>
public sealed class OutboxMessageFakerTests
{
    [Fact]
    public void Generate_ShouldCreateValidOutboxMessage()
    {
        // Arrange
        var faker = new OutboxMessageFaker();

        // Act
        var message = faker.Generate();

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldNotBe(Guid.Empty);
        message.NotificationType.ShouldNotBeNullOrEmpty();
        message.Content.ShouldNotBeNullOrEmpty();
        message.CreatedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        message.ProcessedAtUtc.ShouldBeNull();
        message.ErrorMessage.ShouldBeNull();
        message.RetryCount.ShouldBe(0);
        message.NextRetryAtUtc.ShouldBeNull();
        message.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void Generate_ShouldBeReproducible()
    {
        // Arrange
        var faker1 = new OutboxMessageFaker();
        var faker2 = new OutboxMessageFaker();

        // Act
        var message1 = faker1.Generate();
        var message2 = faker2.Generate();

        // Assert - Same seed should produce same IDs
        message1.Id.ShouldBe(message2.Id);
        message1.NotificationType.ShouldBe(message2.NotificationType);
    }

    [Fact]
    public void AsProcessed_ShouldSetProcessedTimestamp()
    {
        // Arrange
        var faker = new OutboxMessageFaker().AsProcessed();

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
        var faker = new OutboxMessageFaker().AsFailed(retryCount: 5);

        // Act
        var message = faker.Generate();

        // Assert
        message.ErrorMessage.ShouldNotBeNullOrEmpty();
        message.RetryCount.ShouldBe(5);
        message.NextRetryAtUtc.ShouldNotBeNull();
        message.NextRetryAtUtc!.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void AsFailed_ShouldUseDefaultRetryCount()
    {
        // Arrange
        var faker = new OutboxMessageFaker().AsFailed();

        // Act
        var message = faker.Generate();

        // Assert
        message.RetryCount.ShouldBe(3);
    }

    [Fact]
    public void WithNotificationType_ShouldSetSpecificType()
    {
        // Arrange
        var faker = new OutboxMessageFaker().WithNotificationType("CustomEvent");

        // Act
        var message = faker.Generate();

        // Assert
        message.NotificationType.ShouldBe("CustomEvent");
    }

    [Fact]
    public void WithContent_ShouldSetSpecificContent()
    {
        // Arrange
        var content = "{\"test\": \"value\"}";
        var faker = new OutboxMessageFaker().WithContent(content);

        // Act
        var message = faker.Generate();

        // Assert
        message.Content.ShouldBe(content);
    }

    [Fact]
    public void GenerateMessage_ShouldReturnAsInterface()
    {
        // Arrange
        var faker = new OutboxMessageFaker();

        // Act
        var message = faker.GenerateMessage();

        // Assert
        message.ShouldNotBeNull();
        message.ShouldBeOfType<FakeOutboxMessage>();
    }

    [Fact]
    public void GenerateMultiple_ShouldCreateUniqueMessages()
    {
        // Arrange
        var faker = new OutboxMessageFaker();

        // Act
        var messages = faker.Generate(5);

        // Assert
        messages.Count.ShouldBe(5);
        messages.DistinctBy(m => m.Id).Count().ShouldBe(5);
    }

    [Fact]
    public void IsDeadLettered_ShouldReturnTrueWhenExceedsMaxRetries()
    {
        // Arrange
        var faker = new OutboxMessageFaker().AsFailed(retryCount: 5);
        var message = faker.Generate();

        // Act & Assert
        message.IsDeadLettered(3).ShouldBeTrue();
        message.IsDeadLettered(5).ShouldBeTrue();
        message.IsDeadLettered(10).ShouldBeFalse();
    }

    [Fact]
    public void MethodChaining_ShouldWork()
    {
        // Arrange & Act
        var message = new OutboxMessageFaker()
            .WithNotificationType("TestEvent")
            .WithContent("{\"key\": \"value\"}")
            .AsFailed(2)
            .Generate();

        // Assert
        message.NotificationType.ShouldBe("TestEvent");
        message.Content.ShouldBe("{\"key\": \"value\"}");
        message.RetryCount.ShouldBe(2);
        message.ErrorMessage.ShouldNotBeNullOrEmpty();
    }
}
