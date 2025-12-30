using Shouldly;
using Xunit;

namespace Encina.Testing.Bogus.Tests;

/// <summary>
/// Unit tests for <see cref="InboxMessageFaker"/>.
/// </summary>
public sealed class InboxMessageFakerTests
{
    [Fact]
    public void Generate_ShouldCreateValidInboxMessage()
    {
        // Arrange
        var faker = new InboxMessageFaker();

        // Act
        var message = faker.Generate();

        // Assert
        message.ShouldNotBeNull();
        message.MessageId.ShouldNotBeNullOrEmpty();
        message.RequestType.ShouldNotBeNullOrEmpty();
        message.Response.ShouldBeNull();
        message.ErrorMessage.ShouldBeNull();
        message.ReceivedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        message.ProcessedAtUtc.ShouldBeNull();
        message.ExpiresAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        message.RetryCount.ShouldBe(0);
        message.NextRetryAtUtc.ShouldBeNull();
        message.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void Generate_ShouldBeReproducible()
    {
        // Arrange
        var faker1 = new InboxMessageFaker();
        var faker2 = new InboxMessageFaker();

        // Act
        var message1 = faker1.Generate();
        var message2 = faker2.Generate();

        // Assert
        message1.MessageId.ShouldBe(message2.MessageId);
        message1.RequestType.ShouldBe(message2.RequestType);
    }

    [Fact]
    public void AsProcessed_ShouldSetProcessedTimestampAndResponse()
    {
        // Arrange
        var faker = new InboxMessageFaker().AsProcessed();

        // Act
        var message = faker.Generate();

        // Assert
        message.ProcessedAtUtc.ShouldNotBeNull();
        message.ProcessedAtUtc!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        message.Response.ShouldNotBeNullOrEmpty();
        message.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public void AsProcessed_WithCustomResponse_ShouldSetResponse()
    {
        // Arrange
        var customResponse = "{\"result\": \"success\"}";
        var faker = new InboxMessageFaker().AsProcessed(customResponse);

        // Act
        var message = faker.Generate();

        // Assert
        message.Response.ShouldBe(customResponse);
    }

    [Fact]
    public void AsFailed_ShouldSetErrorAndRetryInfo()
    {
        // Arrange
        var faker = new InboxMessageFaker().AsFailed(retryCount: 4);

        // Act
        var message = faker.Generate();

        // Assert
        message.ErrorMessage.ShouldNotBeNullOrEmpty();
        message.RetryCount.ShouldBe(4);
        message.NextRetryAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void AsFailed_ShouldUseDefaultRetryCount()
    {
        // Arrange
        var faker = new InboxMessageFaker().AsFailed();

        // Act
        var message = faker.Generate();

        // Assert
        message.RetryCount.ShouldBe(2);
    }

    [Fact]
    public void AsExpired_ShouldSetPastExpirationDate()
    {
        // Arrange
        var faker = new InboxMessageFaker().AsExpired();

        // Act
        var beforeGeneration = DateTime.UtcNow;
        var message = faker.Generate();

        // Assert - verify ExpiresAtUtc is in the past using captured timestamp
        // This is deterministic because we compare against a fixed point in time
        message.ExpiresAtUtc.ShouldBeLessThan(beforeGeneration);
    }

    [Fact]
    public void WithMessageId_ShouldSetSpecificId()
    {
        // Arrange
        var messageId = "custom-message-id-123";
        var faker = new InboxMessageFaker().WithMessageId(messageId);

        // Act
        var message = faker.Generate();

        // Assert
        message.MessageId.ShouldBe(messageId);
    }

    [Fact]
    public void WithRequestType_ShouldSetSpecificType()
    {
        // Arrange
        var faker = new InboxMessageFaker().WithRequestType("CustomRequest");

        // Act
        var message = faker.Generate();

        // Assert
        message.RequestType.ShouldBe("CustomRequest");
    }

    [Fact]
    public void GenerateMessage_ShouldReturnAsInterface()
    {
        // Arrange
        var faker = new InboxMessageFaker();

        // Act
        var message = faker.GenerateMessage();

        // Assert
        message.ShouldNotBeNull();
        message.ShouldBeOfType<FakeInboxMessage>();
    }

    [Fact]
    public void GenerateMultiple_ShouldCreateUniqueMessages()
    {
        // Arrange
        var faker = new InboxMessageFaker();

        // Act
        var messages = faker.Generate(5);

        // Assert
        messages.Count.ShouldBe(5);
        messages.DistinctBy(m => m.MessageId).Count().ShouldBe(5);
    }

    [Fact]
    public void DefaultMessage_ShouldHaveFutureExpiration()
    {
        // Arrange
        var faker = new InboxMessageFaker();

        // Act
        var message = faker.Generate();
        var afterGeneration = DateTime.UtcNow;

        // Assert - verify ExpiresAtUtc is in the future using captured timestamp
        // This is deterministic because we compare against a fixed point in time
        message.ExpiresAtUtc.ShouldBeGreaterThan(afterGeneration);
    }

    [Fact]
    public void MethodChaining_ShouldWork()
    {
        // Arrange & Act
        var message = new InboxMessageFaker()
            .WithMessageId("test-id")
            .WithRequestType("TestRequest")
            .AsFailed(3)
            .Generate();

        // Assert
        message.MessageId.ShouldBe("test-id");
        message.RequestType.ShouldBe("TestRequest");
        message.RetryCount.ShouldBe(3);
        message.ErrorMessage.ShouldNotBeNullOrEmpty();
    }
}
