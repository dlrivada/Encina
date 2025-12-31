using Shouldly;
using Encina.EntityFrameworkCore.Outbox;
using Xunit;

namespace Encina.EntityFrameworkCore.IntegrationTests.Outbox;

/// <summary>
/// Integration tests for OutboxStoreEF using real SQL Server via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public sealed class OutboxStoreEFIntegrationTests : IClassFixture<EFCoreFixture>
{
    private readonly EFCoreFixture _fixture;

    public OutboxStoreEFIntegrationTests(EFCoreFixture fixture)
    {
        _fixture = fixture;
        // Clear data before each test to ensure isolation
        _fixture.ClearAllDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new OutboxStoreEF(context);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{\"test\":\"data\"}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        // Act
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var stored = await verifyContext.OutboxMessages.FindAsync(message.Id);
        stored.ShouldNotBeNull();
        stored!.NotificationType.ShouldBe("TestNotification");
        stored.Content.ShouldBe("{\"test\":\"data\"}");
        stored.RetryCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_WithMultipleMessages_ShouldReturnUnprocessedOnly()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new OutboxStoreEF(context);

        var pending1 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Pending1",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            RetryCount = 0
        };

        var pending2 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Pending2",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            RetryCount = 0
        };

        var processed = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Processed",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-15),
            ProcessedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        context.OutboxMessages.AddRange(pending1, pending2, processed);
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        var messageList = messages.ToList();
        messageList.Count.ShouldBe(2);
        messageList.ShouldContain(m => m.Id == pending1.Id);
        messageList.ShouldContain(m => m.Id == pending2.Id);
        messageList.ShouldNotContain(m => m.Id == processed.Id);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldRespectBatchSize()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new OutboxStoreEF(context);

        for (int i = 0; i < 10; i++)
        {
            context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Notification{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            });
        }
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetPendingMessagesAsync(batchSize: 5, maxRetries: 3);

        // Assert
        messages.Count.ShouldBe(5);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldExcludeMaxRetriedMessages()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new OutboxStoreEF(context);

        var maxRetried = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "MaxRetried",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 5
        };

        context.OutboxMessages.Add(maxRetried);
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldOrderByCreatedAtUtc()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new OutboxStoreEF(context);

        var older = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Older",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0
        };

        var newer = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Newer",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 0
        };

        // Add in reverse order to verify sorting
        context.OutboxMessages.AddRange(newer, older);
        await context.SaveChangesAsync();

        // Act
        var messages = (await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3)).ToList();

        // Assert
        messages[0].Id.ShouldBe(older.Id);
        messages[1].Id.ShouldBe(newer.Id);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateTimestampAndClearError()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new OutboxStoreEF(context);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            ErrorMessage = "Previous error",
            RetryCount = 1
        };

        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        // Act
        await store.MarkAsProcessedAsync(message.Id);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.OutboxMessages.FindAsync(message.Id);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateErrorAndRetryInfo()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new OutboxStoreEF(context);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        await store.MarkAsFailedAsync(message.Id, "Test error", nextRetry);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.OutboxMessages.FindAsync(message.Id);
        updated!.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldBe(nextRetry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetPendingMessagesAsync_WithNextRetryInFuture_ShouldExcludeMessage()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new OutboxStoreEF(context);

        var futureRetry = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "FutureRetry",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 1,
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(10) // In future
        };

        context.OutboxMessages.Add(futureRetry);
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_WithNextRetryInPast_ShouldIncludeMessage()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new OutboxStoreEF(context);

        var pastRetry = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "PastRetry",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 1,
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(-5) // In past
        };

        context.OutboxMessages.Add(pastRetry);
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldHaveSingleItem();
        messages.First().Id.ShouldBe(pastRetry.Id);
    }

    [Fact]
    public async Task ConcurrentWrites_ShouldNotCorruptData()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            using var context = _fixture.CreateDbContext();
            var store = new OutboxStoreEF(context);

            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Concurrent{i}",
                Content = $"{{\"index\":{i}}}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            };

            await store.AddAsync(message);
            await store.SaveChangesAsync();
            return message.Id;
        });

        // Act
        var messageIds = await Task.WhenAll(tasks);

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        foreach (var id in messageIds)
        {
            var stored = await verifyContext.OutboxMessages.FindAsync(id);
            stored.ShouldNotBeNull();
        }
    }
}
