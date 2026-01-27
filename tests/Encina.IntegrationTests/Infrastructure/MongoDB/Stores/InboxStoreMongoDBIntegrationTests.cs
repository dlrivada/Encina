using Encina.MongoDB;
using Encina.MongoDB.Inbox;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Stores;

/// <summary>
/// Integration tests for <see cref="InboxStoreMongoDB"/> using real MongoDB via Testcontainers.
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class InboxStoreMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly IOptions<EncinaMongoDbOptions> _options;

    public InboxStoreMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _options = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = MongoDbFixture.DatabaseName,
            UseInbox = true
        });
    }

    public async Task InitializeAsync()
    {
        if (_fixture.IsAvailable)
        {
            var collection = _fixture.Database!.GetCollection<InboxMessage>(_options.Value.Collections.Inbox);
            await collection.DeleteManyAsync(Builders<InboxMessage>.Filter.Empty);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var store = CreateStore();
        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        // Act
        await store.AddAsync(message);

        // Assert
        var collection = GetCollection();
        var stored = await collection.Find(m => m.MessageId == message.MessageId).FirstOrDefaultAsync();
        stored.ShouldNotBeNull();
        stored.RequestType.ShouldBe("TestRequest");
        stored.RetryCount.ShouldBe(0);
    }

    [SkippableFact]
    public async Task GetMessageAsync_ExistingMessage_ShouldReturnMessage()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        };

        await collection.InsertOneAsync(message);

        var store = CreateStore();

        // Act
        var result = await store.GetMessageAsync(messageId);

        // Assert
        result.ShouldNotBeNull();
        result!.MessageId.ShouldBe(messageId);
        result.RequestType.ShouldBe("TestRequest");
    }

    [SkippableFact]
    public async Task GetMessageAsync_NonExistentMessage_ShouldReturnNull()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetMessageAsync("non-existent-id");

        // Assert
        result.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetExpiredMessagesAsync_ShouldReturnExpiredMessages()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();

        var expired = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "Expired",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        var notExpired = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "NotExpired",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        };

        await collection.InsertManyAsync([expired, notExpired]);

        var store = CreateStore();

        // Act
        var messages = await store.GetExpiredMessagesAsync(batchSize: 10);

        // Assert
        var messageList = messages.ToList();
        messageList.ShouldHaveSingleItem();
        messageList.First().MessageId.ShouldBe(expired.MessageId);
    }

    [SkippableFact]
    public async Task MarkAsProcessedAsync_ShouldUpdateTimestampAndResponse()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        };

        await collection.InsertOneAsync(message);

        var store = CreateStore();
        var response = "{\"status\":\"success\"}";

        // Act
        await store.MarkAsProcessedAsync(messageId, response);

        // Assert
        var updated = await collection.Find(m => m.MessageId == messageId).FirstOrDefaultAsync();
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.Response.ShouldBe(response);
    }

    [SkippableFact]
    public async Task MarkAsFailedAsync_ShouldUpdateErrorAndIncrementRetryCount()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        await collection.InsertOneAsync(message);

        var store = CreateStore();
        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        await store.MarkAsFailedAsync(messageId, "Test error", nextRetry);

        // Assert
        var updated = await collection.Find(m => m.MessageId == messageId).FirstOrDefaultAsync();
        updated!.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldNotBeNull();
        updated.NextRetryAtUtc!.Value.ShouldBe(nextRetry, TimeSpan.FromSeconds(1));
    }

    [SkippableFact]
    public async Task RemoveExpiredMessagesAsync_ShouldDeleteSpecifiedMessages()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();
        var messageId1 = Guid.NewGuid().ToString();
        var messageId2 = Guid.NewGuid().ToString();
        var messageId3 = Guid.NewGuid().ToString();

        var messages = new[]
        {
            new InboxMessage
            {
                MessageId = messageId1,
                RequestType = "ToDelete1",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
            },
            new InboxMessage
            {
                MessageId = messageId2,
                RequestType = "ToDelete2",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
            },
            new InboxMessage
            {
                MessageId = messageId3,
                RequestType = "ToKeep",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
            }
        };

        await collection.InsertManyAsync(messages);

        var store = CreateStore();

        // Act
        await store.RemoveExpiredMessagesAsync([messageId1, messageId2]);

        // Assert
        var remaining = await collection.CountDocumentsAsync(Builders<InboxMessage>.Filter.Empty);
        remaining.ShouldBe(1);

        var kept = await collection.Find(m => m.MessageId == messageId3).FirstOrDefaultAsync();
        kept.ShouldNotBeNull();
    }

    [SkippableFact]
    public async Task IncrementRetryCountAsync_ShouldIncrementCount()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var collection = GetCollection();
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 2
        };

        await collection.InsertOneAsync(message);

        var store = CreateStore();

        // Act
        await store.IncrementRetryCountAsync(messageId);

        // Assert
        var updated = await collection.Find(m => m.MessageId == messageId).FirstOrDefaultAsync();
        updated!.RetryCount.ShouldBe(3);
    }

    [SkippableFact]
    public async Task EdgeCase_NullProcessedAtUtc_ShouldBeStoredCorrectly()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var store = CreateStore();
        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            ProcessedAtUtc = null
        };

        // Act
        await store.AddAsync(message);

        // Assert
        var collection = GetCollection();
        var stored = await collection.Find(m => m.MessageId == message.MessageId).FirstOrDefaultAsync();
        stored!.ProcessedAtUtc.ShouldBeNull();
    }

    [SkippableFact]
    public async Task EdgeCase_NullErrorMessage_ShouldBeStoredCorrectly()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var store = CreateStore();
        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            ErrorMessage = null
        };

        // Act
        await store.AddAsync(message);

        // Assert
        var collection = GetCollection();
        var stored = await collection.Find(m => m.MessageId == message.MessageId).FirstOrDefaultAsync();
        stored!.ErrorMessage.ShouldBeNull();
    }

    [SkippableFact]
    public async Task EdgeCase_NullNextRetryAtUtc_ShouldBeStoredCorrectly()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var store = CreateStore();
        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            NextRetryAtUtc = null
        };

        // Act
        await store.AddAsync(message);

        // Assert
        var collection = GetCollection();
        var stored = await collection.Find(m => m.MessageId == message.MessageId).FirstOrDefaultAsync();
        stored!.NextRetryAtUtc.ShouldBeNull();
    }

    [SkippableFact]
    public async Task ConcurrentWrites_WithUniqueMessageIds_ShouldNotCorruptData()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange & Act
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var store = CreateStore();
            var messageId = Guid.NewGuid().ToString();

            var message = new InboxMessage
            {
                MessageId = messageId,
                RequestType = $"Concurrent{i}",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                RetryCount = 0
            };

            await store.AddAsync(message);
            return messageId;
        });

        var messageIds = await Task.WhenAll(tasks);

        // Assert
        var collection = GetCollection();
        foreach (var id in messageIds)
        {
            var stored = await collection.Find(m => m.MessageId == id).FirstOrDefaultAsync();
            stored.ShouldNotBeNull();
        }
    }

    private InboxStoreMongoDB CreateStore()
    {
        return new InboxStoreMongoDB(
            _fixture.Client!,
            _options,
            NullLogger<InboxStoreMongoDB>.Instance);
    }

    private IMongoCollection<InboxMessage> GetCollection()
    {
        return _fixture.Database!.GetCollection<InboxMessage>(_options.Value.Collections.Inbox);
    }
}
