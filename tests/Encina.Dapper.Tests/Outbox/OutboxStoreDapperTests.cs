using Dapper;
using Encina.Dapper.Sqlite.Outbox;

namespace Encina.Dapper.Tests.Outbox;

[Trait("Category", "Integration")]
public class OutboxStoreDapperTests : IDisposable
{
    private readonly SqliteTestHelper _dbHelper;
    private readonly OutboxStoreDapper _store;

    public OutboxStoreDapperTests()
    {
        _dbHelper = new SqliteTestHelper();
        _dbHelper.CreateOutboxTable();
        _store = new OutboxStoreDapper(_dbHelper.Connection);
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

        // Assert
        var stored = await _dbHelper.Connection.QuerySingleOrDefaultAsync<OutboxMessage>(
            "SELECT * FROM OutboxMessages WHERE Id = @Id",
            new { message.Id });
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

        await _store.AddAsync(pending1);
        await _store.AddAsync(pending2);
        await _store.AddAsync(processed);

        // Act
        var messages = await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.Count.ShouldBe(2);
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
            await _store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Notification{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            });
        }

        // Act
        var messages = await _store.GetPendingMessagesAsync(batchSize: 5, maxRetries: 3);

        // Assert
        messages.Count.ShouldBe(5);
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

        await _store.AddAsync(maxRetried);

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

        await _store.AddAsync(message);

        // Act
        await _store.MarkAsProcessedAsync(message.Id);

        // Assert
        var updated = await _dbHelper.Connection.QuerySingleAsync<OutboxMessage>(
            "SELECT * FROM OutboxMessages WHERE Id = @Id",
            new { message.Id });
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

        await _store.AddAsync(message);

        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        await _store.MarkAsFailedAsync(message.Id, "Test error", nextRetry);

        // Assert
        var updated = await _dbHelper.Connection.QuerySingleAsync<OutboxMessage>(
            "SELECT * FROM OutboxMessages WHERE Id = @Id",
            new { message.Id });
        updated.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldBe(nextRetry, TimeSpan.FromSeconds(1));
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

        await _store.AddAsync(newer);
        await _store.AddAsync(older);

        // Act
        var messages = (await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3)).ToList();

        // Assert
        messages[0].Id.ShouldBe(older.Id);
        messages[1].Id.ShouldBe(newer.Id);
    }

    [Fact]
    public async Task IsProcessed_ShouldReturnTrueForProcessedMessage()
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

        await _store.AddAsync(message);
        await _store.MarkAsProcessedAsync(message.Id);

        // Act
        var updated = await _dbHelper.Connection.QuerySingleAsync<OutboxMessage>(
            "SELECT * FROM OutboxMessages WHERE Id = @Id",
            new { message.Id });

        // Assert
        updated.IsProcessed.ShouldBeTrue();
    }

    public void Dispose()
    {
        _dbHelper.Dispose();
        GC.SuppressFinalize(this);
    }
}
