using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Inbox;
using Xunit;

namespace Encina.EntityFrameworkCore.Tests.Inbox;

public class InboxStoreEFTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly InboxStoreEF _store;
    private static readonly string[] RemoveTestIds = ["remove-1", "remove-2"];

    public InboxStoreEFTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new InboxStoreEF(_dbContext);
    }

    [Fact]
    public async Task GetMessageAsync_ShouldReturnExistingMessage()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "test-message-id",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        await _dbContext.InboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _store.GetMessageAsync("test-message-id");

        // Assert
        result.Should().NotBeNull();
        result!.MessageId.Should().Be("test-message-id");
    }

    [Fact]
    public async Task GetMessageAsync_ShouldReturnNullForNonExistentMessage()
    {
        // Act
        var result = await _store.GetMessageAsync("non-existent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddMessageToStore()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "new-message-id",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        // Act
        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Assert
        var stored = await _dbContext.InboxMessages.FindAsync("new-message-id");
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateMessage()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "process-test-id",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        await _dbContext.InboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        await _store.MarkAsProcessedAsync("process-test-id", "{\"result\":\"success\"}");
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.InboxMessages.FindAsync("process-test-id");
        updated!.Response.Should().Be("{\"result\":\"success\"}");
        updated.ProcessedAtUtc.Should().NotBeNull();
        updated.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateMessageWithError()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "fail-test-id",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        await _dbContext.InboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        await _store.MarkAsFailedAsync("fail-test-id", "Test error", nextRetry);
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.InboxMessages.FindAsync("fail-test-id");
        updated!.ErrorMessage.Should().Be("Test error");
        updated.RetryCount.Should().Be(1);
        updated.NextRetryAtUtc.Should().BeCloseTo(nextRetry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_ShouldReturnExpiredMessages()
    {
        // Arrange
        var expired = new InboxMessage
        {
            MessageId = "expired-id",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
            ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-5),
            RetryCount = 0
        };

        var notExpired = new InboxMessage
        {
            MessageId = "not-expired-id",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        await _dbContext.InboxMessages.AddRangeAsync(expired, notExpired);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetExpiredMessagesAsync(batchSize: 10);

        // Assert
        messages.Should().HaveCount(1);
        messages.First().MessageId.Should().Be("expired-id");
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_ShouldDeleteMessages()
    {
        // Arrange
        var message1 = new InboxMessage
        {
            MessageId = "remove-1",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-5),
            RetryCount = 0
        };

        var message2 = new InboxMessage
        {
            MessageId = "remove-2",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-5),
            RetryCount = 0
        };

        await _dbContext.InboxMessages.AddRangeAsync(message1, message2);
        await _dbContext.SaveChangesAsync();

        // Act
        await _store.RemoveExpiredMessagesAsync(RemoveTestIds);
        await _store.SaveChangesAsync();

        // Assert
        var remaining = await _dbContext.InboxMessages.ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task IsProcessed_ShouldReturnTrueWhenProcessedWithoutError()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "processed-id",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        await _dbContext.InboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        message.IsProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task IsProcessed_ShouldReturnFalseWhenHasError()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "error-id",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorMessage = "Some error",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 1
        };

        await _dbContext.InboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        message.IsProcessed.Should().BeFalse();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
