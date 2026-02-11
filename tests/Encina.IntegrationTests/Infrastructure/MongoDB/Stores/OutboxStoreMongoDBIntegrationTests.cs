using Encina.MongoDB;
using Encina.MongoDB.Outbox;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Stores;

/// <summary>
/// Integration tests for <see cref="OutboxStoreMongoDB"/> using real MongoDB via Testcontainers.
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class OutboxStoreMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly IOptions<EncinaMongoDbOptions> _options;

    public OutboxStoreMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _options = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = MongoDbFixture.DatabaseName,
            UseOutbox = true
        });
    }

    public async ValueTask InitializeAsync()
    {
        if (_fixture.IsAvailable)
        {
            var collection = _fixture.Database!.GetCollection<OutboxMessage>(_options.Value.Collections.Outbox);
            await collection.DeleteManyAsync(Builders<OutboxMessage>.Filter.Empty);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {

        // Arrange
        var store = CreateStore();
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

        // Assert
        var collection = GetCollection();
        var stored = await collection.Find(m => m.Id == message.Id).FirstOrDefaultAsync();
        stored.ShouldNotBeNull();
        stored.NotificationType.ShouldBe("TestNotification");
        stored.Content.ShouldBe("{\"test\":\"data\"}");
        stored.RetryCount.ShouldBe(0);
    }

    [Fact]
    public async Task AddAsync_MultipleMessages_ShouldPersistAll()
    {

        // Arrange
        var store = CreateStore();
        var messages = Enumerable.Range(0, 5).Select(i => new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = $"Notification{i}",
            Content = $"{{\"index\":{i}}}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        }).ToList();

        // Act
        foreach (var message in messages)
        {
            await store.AddAsync(message);
        }

        // Assert
        var collection = GetCollection();
        var count = await collection.CountDocumentsAsync(Builders<OutboxMessage>.Filter.Empty);
        count.ShouldBe(5);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldReturnUnprocessedOnly()
    {

        // Arrange
        var collection = GetCollection();

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

        await collection.InsertManyAsync([pending1, pending2, processed]);

        var store = CreateStore();

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
        var collection = GetCollection();
        var messages = Enumerable.Range(0, 10).Select(i => new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = $"Notification{i}",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        }).ToList();

        await collection.InsertManyAsync(messages);

        var store = CreateStore();

        // Act
        var pending = await store.GetPendingMessagesAsync(batchSize: 5, maxRetries: 3);

        // Assert
        pending.Count().ShouldBe(5);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldExcludeMaxRetriedMessages()
    {

        // Arrange
        var collection = GetCollection();

        var maxRetried = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "MaxRetried",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 5
        };

        await collection.InsertOneAsync(maxRetried);

        var store = CreateStore();

        // Act
        var messages = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldOrderByCreatedAtUtc()
    {

        // Arrange
        var collection = GetCollection();

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
        await collection.InsertManyAsync([newer, older]);

        var store = CreateStore();

        // Act
        var messages = (await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3)).ToList();

        // Assert
        messages[0].Id.ShouldBe(older.Id);
        messages[1].Id.ShouldBe(newer.Id);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_WithNextRetryInFuture_ShouldExcludeMessage()
    {

        // Arrange
        var collection = GetCollection();

        var futureRetry = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "FutureRetry",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 1,
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(10)
        };

        await collection.InsertOneAsync(futureRetry);

        var store = CreateStore();

        // Act
        var messages = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_WithNextRetryInPast_ShouldIncludeMessage()
    {

        // Arrange
        var collection = GetCollection();

        var pastRetry = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "PastRetry",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 1,
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(-5)
        };

        await collection.InsertOneAsync(pastRetry);

        var store = CreateStore();

        // Act
        var messages = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldHaveSingleItem();
        messages.First().Id.ShouldBe(pastRetry.Id);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateTimestamp()
    {

        // Arrange
        var collection = GetCollection();

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            ErrorMessage = "Previous error",
            RetryCount = 1
        };

        await collection.InsertOneAsync(message);

        var store = CreateStore();

        // Act
        await store.MarkAsProcessedAsync(message.Id);

        // Assert
        var updated = await collection.Find(m => m.Id == message.Id).FirstOrDefaultAsync();
        updated!.ProcessedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateErrorAndRetryInfo()
    {

        // Arrange
        var collection = GetCollection();

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await collection.InsertOneAsync(message);

        var nextRetry = DateTime.UtcNow.AddMinutes(5);
        var store = CreateStore();

        // Act
        await store.MarkAsFailedAsync(message.Id, "Test error", nextRetry);

        // Assert
        var updated = await collection.Find(m => m.Id == message.Id).FirstOrDefaultAsync();
        updated!.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldNotBeNull();
        updated.NextRetryAtUtc!.Value.ShouldBe(nextRetry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ConcurrentWrites_ShouldNotCorruptData()
    {

        // Arrange & Act
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var store = CreateStore();

            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Concurrent{i}",
                Content = $"{{\"index\":{i}}}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            };

            await store.AddAsync(message);
            return message.Id;
        });

        var messageIds = await Task.WhenAll(tasks);

        // Assert
        var collection = GetCollection();
        foreach (var id in messageIds)
        {
            var stored = await collection.Find(m => m.Id == id).FirstOrDefaultAsync();
            stored.ShouldNotBeNull();
        }
    }

    private OutboxStoreMongoDB CreateStore()
    {
        return new OutboxStoreMongoDB(
            _fixture.Client!,
            _options,
            NullLogger<OutboxStoreMongoDB>.Instance);
    }

    private IMongoCollection<OutboxMessage> GetCollection()
    {
        return _fixture.Database!.GetCollection<OutboxMessage>(_options.Value.Collections.Outbox);
    }
}
