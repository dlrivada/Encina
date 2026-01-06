using Shouldly;
using Encina.EntityFrameworkCore.Inbox;
using Xunit;

namespace Encina.EntityFrameworkCore.IntegrationTests.Inbox;

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
        stored.ShouldNotBeNull();
        stored!.RequestType.ShouldBe("TestRequest");
    }

    [Fact]
    public async Task GetMessageAsync_WithExistingMessage_ShouldReturnMessage()
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
        var retrieved = await store.GetMessageAsync(message.MessageId);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved!.MessageId.ShouldBe("existing-message");
    }

    [Fact]
    public async Task GetMessageAsync_WithNonExistentMessage_ShouldReturnNull()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new InboxStoreEF(context);

        // Act
        var retrieved = await store.GetMessageAsync("non-existent-message");

        // Assert
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_ShouldReturnExpiredMessages()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new InboxStoreEF(context);

        var expired1 = new InboxMessage
        {
            MessageId = "expired-1",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10),
            RetryCount = 0
        };

        var expired2 = new InboxMessage
        {
            MessageId = "expired-2",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-5),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5),
            RetryCount = 0
        };

        var valid = new InboxMessage
        {
            MessageId = "valid-1",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        context.InboxMessages.AddRange(expired1, expired2, valid);
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetExpiredMessagesAsync(batchSize: 10);

        // Assert
        var messageList = messages.ToList();
        messageList.Count.ShouldBe(2);
        messageList.ShouldContain(m => m.MessageId == expired1.MessageId);
        messageList.ShouldContain(m => m.MessageId == expired2.MessageId);
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
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.Response.ShouldBe("{\"result\":\"success\"}");
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
        updated!.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldNotBeNull();
        updated.NextRetryAtUtc.Value.ShouldBe(nextRetry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_ShouldDeleteExpiredMessages()
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
        await store.RemoveExpiredMessagesAsync(new[] { expired.MessageId });
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var remaining = await verifyContext.InboxMessages.FindAsync(valid.MessageId);
        remaining.ShouldNotBeNull();

        var deleted = await verifyContext.InboxMessages.FindAsync(expired.MessageId);
        deleted.ShouldBeNull();
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
            stored.ShouldNotBeNull();
        }
    }
}
