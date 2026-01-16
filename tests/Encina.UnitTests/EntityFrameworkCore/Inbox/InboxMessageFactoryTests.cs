using System.Text.Json;
using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Inbox;
using Encina.Messaging.Inbox;
using Shouldly;
using Xunit;

#pragma warning disable CA1869 // Cache JsonSerializerOptions - Acceptable for test code

namespace Encina.UnitTests.EntityFrameworkCore.Inbox;

/// <summary>
/// Unit tests for <see cref="InboxMessageFactory"/>.
/// </summary>
public sealed class InboxMessageFactoryTests
{
    [Fact]
    public void Create_ValidParameters_ReturnsInboxMessage()
    {
        // Arrange
        var factory = new InboxMessageFactory();
        var messageId = "msg-12345";
        var requestType = "CreateOrderCommand";
        var receivedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = DateTime.UtcNow.AddDays(7);

        // Act
        var result = factory.Create(messageId, requestType, receivedAtUtc, expiresAtUtc, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<InboxMessage>();
    }

    [Fact]
    public void Create_SetsMessageIdCorrectly()
    {
        // Arrange
        var factory = new InboxMessageFactory();
        var messageId = "unique-message-id-123";

        // Act
        var result = factory.Create(messageId, "Type", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null);

        // Assert
        result.MessageId.ShouldBe(messageId);
    }

    [Fact]
    public void Create_SetsRequestTypeCorrectly()
    {
        // Arrange
        var factory = new InboxMessageFactory();
        var requestType = "ProcessPaymentCommand, MyAssembly";

        // Act
        var result = factory.Create("msg-1", requestType, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null);

        // Assert
        result.RequestType.ShouldBe(requestType);
    }

    [Fact]
    public void Create_SetsReceivedAtUtcCorrectly()
    {
        // Arrange
        var factory = new InboxMessageFactory();
        var receivedAtUtc = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var result = factory.Create("msg-1", "Type", receivedAtUtc, DateTime.UtcNow.AddDays(1), null);

        // Assert
        result.ReceivedAtUtc.ShouldBe(receivedAtUtc);
    }

    [Fact]
    public void Create_SetsExpiresAtUtcCorrectly()
    {
        // Arrange
        var factory = new InboxMessageFactory();
        var expiresAtUtc = new DateTime(2025, 6, 22, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var result = factory.Create("msg-1", "Type", DateTime.UtcNow, expiresAtUtc, null);

        // Assert
        result.ExpiresAtUtc.ShouldBe(expiresAtUtc);
    }

    [Fact]
    public void Create_SetsRetryCountToZero()
    {
        // Arrange
        var factory = new InboxMessageFactory();

        // Act
        var result = factory.Create("msg-1", "Type", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null);

        // Assert
        result.RetryCount.ShouldBe(0);
    }

    [Fact]
    public void Create_WithNullMetadata_SetsMetadataToNull()
    {
        // Arrange
        var factory = new InboxMessageFactory();

        // Act
        var result = factory.Create("msg-1", "Type", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null);

        // Assert
        var message = result as InboxMessage;
        message.ShouldNotBeNull();
        message.Metadata.ShouldBeNull();
    }

    [Fact]
    public void Create_WithMetadata_SerializesMetadataToJson()
    {
        // Arrange
        var factory = new InboxMessageFactory();
        var metadata = new InboxMetadata
        {
            CorrelationId = "corr-123",
            UserId = "user-456"
        };

        // Act
        var result = factory.Create("msg-1", "Type", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), metadata);

        // Assert
        var message = result as InboxMessage;
        message.ShouldNotBeNull();
        message.Metadata.ShouldNotBeNull();

        // Verify JSON contains expected properties
        var deserializedMetadata = JsonSerializer.Deserialize<InboxMetadata>(
            message.Metadata,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        deserializedMetadata.ShouldNotBeNull();
        deserializedMetadata.CorrelationId.ShouldBe("corr-123");
        deserializedMetadata.UserId.ShouldBe("user-456");
    }

    [Fact]
    public void Create_LeavesProcessedAtUtcNull()
    {
        // Arrange
        var factory = new InboxMessageFactory();

        // Act
        var result = factory.Create("msg-1", "Type", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null);

        // Assert
        var message = result as InboxMessage;
        message.ShouldNotBeNull();
        message.ProcessedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Create_LeavesResponseNull()
    {
        // Arrange
        var factory = new InboxMessageFactory();

        // Act
        var result = factory.Create("msg-1", "Type", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null);

        // Assert
        var message = result as InboxMessage;
        message.ShouldNotBeNull();
        message.Response.ShouldBeNull();
    }

    [Fact]
    public void Create_LeavesErrorMessageNull()
    {
        // Arrange
        var factory = new InboxMessageFactory();

        // Act
        var result = factory.Create("msg-1", "Type", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null);

        // Assert
        var message = result as InboxMessage;
        message.ShouldNotBeNull();
        message.ErrorMessage.ShouldBeNull();
    }
}
