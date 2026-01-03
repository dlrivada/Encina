using Encina.EntityFrameworkCore.Outbox;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.EntityFrameworkCore.Tests.Outbox;

/// <summary>
/// Integration tests for <see cref="OutboxStoreEF"/> using EF Core InMemoryDatabase.
/// These tests verify store behavior against a real (in-memory) database.
/// </summary>
[Trait("Category", "Integration")]
public class OutboxStoreEFTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly OutboxStoreEF _store;

    public OutboxStoreEFTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new OutboxStoreEF(_dbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldAddMessageToStore()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{\"test\":\"data\"}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        // Act
        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Assert
        var stored = await _dbContext.OutboxMessages.FindAsync(message.Id);
        stored.ShouldNotBeNull();
        stored!.NotificationType.ShouldBe("TestNotification");
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldReturnUnprocessedMessages()
    {
        // Arrange
        var pending1 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Notification1",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            RetryCount = 0
        };

        var pending2 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Notification2",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            RetryCount = 0
        };

        var processed = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Notification3",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-15),
            ProcessedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _dbContext.OutboxMessages.AddRangeAsync(pending1, pending2, processed);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.Count().ShouldBe(2);
        messages.ShouldContain(m => m.Id == pending1.Id);
        messages.ShouldContain(m => m.Id == pending2.Id);
        messages.ShouldNotContain(m => m.Id == processed.Id);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldRespectBatchSize()
    {
        // Arrange
        for (var i = 0; i < 10; i++)
        {
            await _dbContext.OutboxMessages.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Notification{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            });
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetPendingMessagesAsync(batchSize: 5, maxRetries: 3);

        // Assert
        messages.Count().ShouldBe(5);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldExcludeMaxRetriedMessages()
    {
        // Arrange
        var maxRetried = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "MaxRetriedNotification",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 5
        };

        await _dbContext.OutboxMessages.AddAsync(maxRetried);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateMessage()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        await _store.MarkAsProcessedAsync(message.Id);
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.OutboxMessages.FindAsync(message.Id);
        updated.ShouldNotBeNull();
        updated.ProcessedAtUtc.ShouldNotBeNull();
        updated.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateMessageWithError()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        await _store.MarkAsFailedAsync(message.Id, "Test error", nextRetry);
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.OutboxMessages.FindAsync(message.Id);
        updated.ShouldNotBeNull();
        updated.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldNotBeNull();
        updated.NextRetryAtUtc!.Value.ShouldBe(nextRetry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldOrderByCreatedAtUtc()
    {
        // Arrange
        var older = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "OlderNotification",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0
        };

        var newer = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "NewerNotification",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 0
        };

        await _dbContext.OutboxMessages.AddRangeAsync(newer, older);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = (await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3)).ToList();

        // Assert
        messages[0].Id.ShouldBe(older.Id);
        messages[1].Id.ShouldBe(newer.Id);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
