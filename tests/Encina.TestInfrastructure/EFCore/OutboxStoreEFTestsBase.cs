using Encina.EntityFrameworkCore.Outbox;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.TestInfrastructure.EFCore;

/// <summary>
/// Abstract base class for OutboxStoreEF integration tests.
/// Contains test methods that run against any database provider.
/// </summary>
/// <typeparam name="TFixture">The type of EF Core fixture to use.</typeparam>
/// <typeparam name="TContext">The type of DbContext to use.</typeparam>
public abstract class OutboxStoreEFTestsBase<TFixture, TContext> : EFCoreTestBase<TFixture>
    where TFixture : class, IEFCoreFixture
    where TContext : DbContext
{
    /// <summary>
    /// Gets the OutboxMessages DbSet from the context.
    /// </summary>
    protected abstract DbSet<OutboxMessage> GetOutboxMessages(TContext context);

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
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
        await using var verifyContext = CreateDbContext<TContext>();
        var stored = await GetOutboxMessages(verifyContext).FindAsync(message.Id);
        stored.ShouldNotBeNull();
        stored!.NotificationType.ShouldBe("TestNotification");
        stored.Content.ShouldBe("{\"test\":\"data\"}");
        stored.RetryCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_WithMultipleMessages_ShouldReturnUnprocessedOnly()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
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

        GetOutboxMessages(context).AddRange(pending1, pending2, processed);
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
        await using var context = CreateDbContext<TContext>();
        var store = new OutboxStoreEF(context);

        for (int i = 0; i < 10; i++)
        {
            GetOutboxMessages(context).Add(new OutboxMessage
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
        messages.Count().ShouldBe(5);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldExcludeMaxRetriedMessages()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new OutboxStoreEF(context);

        var maxRetried = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "MaxRetried",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 5
        };

        GetOutboxMessages(context).Add(maxRetried);
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateTimestampAndClearError()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
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

        GetOutboxMessages(context).Add(message);
        await context.SaveChangesAsync();

        // Act
        await store.MarkAsProcessedAsync(message.Id);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var updated = await GetOutboxMessages(verifyContext).FindAsync(message.Id);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateErrorAndRetryInfo()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new OutboxStoreEF(context);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        GetOutboxMessages(context).Add(message);
        await context.SaveChangesAsync();

        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        await store.MarkAsFailedAsync(message.Id, "Test error", nextRetry);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var updated = await GetOutboxMessages(verifyContext).FindAsync(message.Id);
        updated!.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldNotBeNull();
        updated.NextRetryAtUtc!.Value.ShouldBe(nextRetry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ConcurrentWrites_ShouldNotCorruptData()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            await using var context = CreateDbContext<TContext>();
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
        await using var verifyContext = CreateDbContext<TContext>();
        foreach (var id in messageIds)
        {
            var stored = await GetOutboxMessages(verifyContext).FindAsync(id);
            stored.ShouldNotBeNull();
        }
    }
}
