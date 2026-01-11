using Encina.EntityFrameworkCore.Inbox;
using Encina.Messaging.Inbox;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.EntityFrameworkCore.Tests.Inbox;

/// <summary>
/// Additional unit tests for <see cref="InboxStoreEF"/> focusing on constructor validation
/// and error handling paths.
/// </summary>
public class InboxStoreEFAdditionalTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly InboxStoreEF _store;

    public InboxStoreEFAdditionalTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new InboxStoreEF(_dbContext);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new InboxStoreEF(null!));
    }

    #endregion

    #region GetMessageAsync Tests

    [Fact]
    public async Task GetMessageAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.GetMessageAsync(null!));
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
        var wrongMessage = Substitute.For<IInboxMessage>();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            _store.AddAsync(wrongMessage));
        ex.Message.ShouldContain("InboxStoreEF requires messages of type InboxMessage");
    }

    #endregion

    #region MarkAsProcessedAsync Tests

    [Fact]
    public async Task MarkAsProcessedAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.MarkAsProcessedAsync(null!, "response"));
    }

    [Fact]
    public async Task MarkAsProcessedAsync_NullResponse_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.MarkAsProcessedAsync("msg-1", null!));
    }

    [Fact]
    public async Task MarkAsProcessedAsync_NonExistentMessage_DoesNotThrow()
    {
        // Act - Should not throw for non-existent message
        var exception = await Record.ExceptionAsync(() =>
            _store.MarkAsProcessedAsync("non-existent-id", "response"));

        // Assert
        exception.ShouldBeNull();
    }

    #endregion

    #region MarkAsFailedAsync Tests

    [Fact]
    public async Task MarkAsFailedAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.MarkAsFailedAsync(null!, "error", DateTime.UtcNow));
    }

    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.MarkAsFailedAsync("msg-1", null!, DateTime.UtcNow));
    }

    [Fact]
    public async Task MarkAsFailedAsync_NonExistentMessage_DoesNotThrow()
    {
        // Act - Should not throw for non-existent message
        var exception = await Record.ExceptionAsync(() =>
            _store.MarkAsFailedAsync("non-existent-id", "error", DateTime.UtcNow));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_WithNullNextRetry_SetsNullNextRetryAtUtc()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "null-retry-test",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        await _dbContext.InboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        await _store.MarkAsFailedAsync("null-retry-test", "Test error", null);
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.InboxMessages.FindAsync("null-retry-test");
        updated!.NextRetryAtUtc.ShouldBeNull();
        updated.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
    }

    #endregion

    #region RemoveExpiredMessagesAsync Tests

    [Fact]
    public async Task RemoveExpiredMessagesAsync_NullMessageIds_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.RemoveExpiredMessagesAsync(null!));
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_EmptyMessageIds_DoesNothing()
    {
        // Act - Should not throw
        var exception = await Record.ExceptionAsync(() =>
            _store.RemoveExpiredMessagesAsync([]));

        // Assert
        exception.ShouldBeNull();
    }

    #endregion

    #region IncrementRetryCountAsync Tests

    [Fact]
    public async Task IncrementRetryCountAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.IncrementRetryCountAsync(null!));
    }

    [Fact]
    public async Task IncrementRetryCountAsync_NonExistentMessage_DoesNotThrow()
    {
        // Act - Should not throw for non-existent message
        var exception = await Record.ExceptionAsync(() =>
            _store.IncrementRetryCountAsync("non-existent-id"));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public async Task IncrementRetryCountAsync_IncrementsRetryCount()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "increment-test",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 2
        };

        await _dbContext.InboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        await _store.IncrementRetryCountAsync("increment-test");
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.InboxMessages.FindAsync("increment-test");
        updated!.RetryCount.ShouldBe(3);
    }

    #endregion

    #region GetExpiredMessagesAsync Tests

    [Fact]
    public async Task GetExpiredMessagesAsync_RespectsBatchSize()
    {
        // Arrange
        for (var i = 0; i < 10; i++)
        {
            await _dbContext.InboxMessages.AddAsync(new InboxMessage
            {
                MessageId = $"expired-{i}",
                RequestType = "TestRequest",
                ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-5 - i),
                RetryCount = 0
            });
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetExpiredMessagesAsync(batchSize: 5);

        // Assert
        messages.Count().ShouldBe(5);
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_ReturnsEmptyWhenNoExpiredMessages()
    {
        // Arrange
        var notExpired = new InboxMessage
        {
            MessageId = "not-expired",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        await _dbContext.InboxMessages.AddAsync(notExpired);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetExpiredMessagesAsync(batchSize: 10);

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
