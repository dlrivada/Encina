using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Outbox;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Encina.EntityFrameworkCore.Tests.Outbox;

/// <summary>
/// Additional unit tests for <see cref="OutboxStoreEF"/> focusing on constructor validation
/// and error handling paths.
/// </summary>
public class OutboxStoreEFAdditionalTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly OutboxStoreEF _store;

    public OutboxStoreEFAdditionalTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new OutboxStoreEF(_dbContext);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new OutboxStoreEF(null!));
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_WrongMessageType_ThrowsInvalidOperationException()
    {
        // Arrange
        var wrongMessage = Substitute.For<IOutboxMessage>();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            _store.AddAsync(wrongMessage));
        ex.Message.ShouldContain("OutboxStoreEF requires messages of type OutboxMessage");
    }

    #endregion

    #region MarkAsProcessedAsync Tests

    [Fact]
    public async Task MarkAsProcessedAsync_NonExistentMessage_DoesNotThrow()
    {
        // Act & Assert - Should not throw for non-existent message
        await _store.MarkAsProcessedAsync(Guid.NewGuid());
    }

    #endregion

    #region MarkAsFailedAsync Tests

    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.MarkAsFailedAsync(Guid.NewGuid(), null!, DateTime.UtcNow));
    }

    [Fact]
    public async Task MarkAsFailedAsync_NonExistentMessage_DoesNotThrow()
    {
        // Act & Assert - Should not throw for non-existent message
        await _store.MarkAsFailedAsync(Guid.NewGuid(), "Error", DateTime.UtcNow);
    }

    [Fact]
    public async Task MarkAsFailedAsync_WithNullNextRetry_SetsNullNextRetryAtUtc()
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
        await _store.MarkAsFailedAsync(message.Id, "Test error", null);
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.OutboxMessages.FindAsync(message.Id);
        updated!.NextRetryAtUtc.ShouldBeNull();
        updated.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
    }

    #endregion

    #region GetPendingMessagesAsync Tests

    [Fact]
    public async Task GetPendingMessagesAsync_WithNextRetryInFuture_ExcludesMessage()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "FutureRetryNotification",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 1,
            NextRetryAtUtc = DateTime.UtcNow.AddHours(1) // Future retry
        };

        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_WithNextRetryInPast_IncludesMessage()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "PastRetryNotification",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 1,
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(-5) // Past retry
        };

        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.Count().ShouldBe(1);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ReturnsEmptyWhenNoMessages()
    {
        // Act
        var messages = await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldBeEmpty();
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
