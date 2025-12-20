using FluentAssertions;
using SimpleMediator.EntityFrameworkCore.Inbox;
using Xunit;

namespace SimpleMediator.EntityFrameworkCore.IntegrationTests.Inbox;

/// <summary>
/// Integration tests for InboxStoreEF using real SQL Server via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public sealed class InboxStoreEFIntegrationTests : IClassFixture<EFCoreFixture>
{
    private readonly EFCoreFixture _fixture;

    public InboxStoreEFIntegrationTests(EFCoreFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearAllDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new InboxStoreEF(context);

        var message = new InboxMessage
        {
            MessageId = "test-message-1",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        // Act
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var stored = await verifyContext.InboxMessages.FindAsync(message.MessageId);
        stored.Should().NotBeNull();
        stored!.RequestType.Should().Be("TestRequest");
    }

    [Fact]
    public async Task ExistsAsync_WithExistingMessage_ShouldReturnTrue()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new InboxStoreEF(context);

        var message = new InboxMessage
        {
            MessageId = "existing-message",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        context.InboxMessages.Add(message);
        await context.SaveChangesAsync();

        // Act
        var exists = await store.ExistsAsync(message.MessageId);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentMessage_ShouldReturnFalse()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new InboxStoreEF(context);

        // Act
        var exists = await store.ExistsAsync("non-existent-message");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldReturnUnprocessedMessages()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new InboxStoreEF(context);

        var pending1 = new InboxMessage
        {
            MessageId = "pending-1",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        var pending2 = new InboxMessage
        {
            MessageId = "pending-2",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        var processed = new InboxMessage
        {
            MessageId = "processed-1",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddMinutes(-15),
            ProcessedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        context.InboxMessages.AddRange(pending1, pending2, processed);
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        var messageList = messages.ToList();
        messageList.Should().HaveCount(2);
        messageList.Should().Contain(m => m.MessageId == pending1.MessageId);
        messageList.Should().Contain(m => m.MessageId == pending2.MessageId);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateMessage()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new InboxStoreEF(context);

        var message = new InboxMessage
        {
            MessageId = "test-message",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        context.InboxMessages.Add(message);
        await context.SaveChangesAsync();

        // Act
        await store.MarkAsProcessedAsync(message.MessageId, "{\"result\":\"success\"}");
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.InboxMessages.FindAsync(message.MessageId);
        updated!.ProcessedAtUtc.Should().NotBeNull();
        updated.Response.Should().Be("{\"result\":\"success\"}");
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateErrorInfo()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new InboxStoreEF(context);

        var message = new InboxMessage
        {
            MessageId = "test-message",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        context.InboxMessages.Add(message);
        await context.SaveChangesAsync();

        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        await store.MarkAsFailedAsync(message.MessageId, "Test error", nextRetry);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.InboxMessages.FindAsync(message.MessageId);
        updated!.ErrorMessage.Should().Be("Test error");
        updated.RetryCount.Should().Be(1);
        updated.NextRetryAtUtc.Should().BeCloseTo(nextRetry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task DeleteExpiredMessagesAsync_ShouldRemoveExpiredMessages()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new InboxStoreEF(context);

        var expired = new InboxMessage
        {
            MessageId = "expired-1",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), // Expired
            RetryCount = 0
        };

        var valid = new InboxMessage
        {
            MessageId = "valid-1",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7), // Not expired
            RetryCount = 0
        };

        context.InboxMessages.AddRange(expired, valid);
        await context.SaveChangesAsync();

        // Act
        var deletedCount = await store.DeleteExpiredMessagesAsync();
        await store.SaveChangesAsync();

        // Assert
        deletedCount.Should().Be(1);

        using var verifyContext = _fixture.CreateDbContext();
        var remaining = await verifyContext.InboxMessages.FindAsync(valid.MessageId);
        remaining.Should().NotBeNull();

        var deleted = await verifyContext.InboxMessages.FindAsync(expired.MessageId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentWrites_ShouldNotCorruptData()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            using var context = _fixture.CreateDbContext();
            var store = new InboxStoreEF(context);

            var message = new InboxMessage
            {
                MessageId = $"concurrent-{i}",
                RequestType = "TestRequest",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                RetryCount = 0
            };

            await store.AddAsync(message);
            await store.SaveChangesAsync();
            return message.MessageId;
        });

        // Act
        var messageIds = await Task.WhenAll(tasks);

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        foreach (var id in messageIds)
        {
            var stored = await verifyContext.InboxMessages.FindAsync(id);
            stored.Should().NotBeNull();
        }
    }
}
