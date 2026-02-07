using Encina.MongoDB;
using Encina.MongoDB.Scheduling;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Stores;

/// <summary>
/// Integration tests for <see cref="ScheduledMessageStoreMongoDB"/> using real MongoDB via Testcontainers.
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class ScheduledMessageStoreMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly IOptions<EncinaMongoDbOptions> _options;

    public ScheduledMessageStoreMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _options = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = MongoDbFixture.DatabaseName,
            UseScheduling = true
        });
    }

    public async Task InitializeAsync()
    {
        if (_fixture.IsAvailable)
        {
            var collection = _fixture.Database!.GetCollection<ScheduledMessage>(_options.Value.Collections.ScheduledMessages);
            await collection.DeleteManyAsync(Builders<ScheduledMessage>.Filter.Empty);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {

        // Arrange
        var store = CreateStore();
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "SendReminderCommand",
            Content = "{\"userId\":123}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        // Act
        await store.AddAsync(message);

        // Assert
        var collection = GetCollection();
        var stored = await collection.Find(m => m.Id == message.Id).FirstOrDefaultAsync();
        stored.ShouldNotBeNull();
        stored.RequestType.ShouldBe("SendReminderCommand");
        stored.Content.ShouldBe("{\"userId\":123}");
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldReturnOnlyDueMessages()
    {

        // Arrange
        var collection = GetCollection();

        var dueMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Due",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 0
        };

        var futureMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Future",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await collection.InsertManyAsync([dueMessage, futureMessage]);

        var store = CreateStore();

        // Act
        var messages = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        var messageList = messages.ToList();
        messageList.ShouldHaveSingleItem();
        messageList.First().Id.ShouldBe(dueMessage.Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldExcludeProcessedMessages()
    {

        // Arrange
        var collection = GetCollection();

        var processedMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Processed",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            ProcessedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await collection.InsertOneAsync(processedMessage);

        var store = CreateStore();

        // Act
        var messages = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldRespectBatchSize()
    {

        // Arrange
        var collection = GetCollection();
        var messages = Enumerable.Range(0, 10).Select(i => new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = $"Message{i}",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-i - 1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 0
        }).ToList();

        await collection.InsertManyAsync(messages);

        var store = CreateStore();

        // Act
        var dueMessages = await store.GetDueMessagesAsync(batchSize: 5, maxRetries: 3);

        // Assert
        dueMessages.Count().ShouldBe(5);
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldRespectMaxRetries()
    {

        // Arrange
        var collection = GetCollection();

        var maxRetriedMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "MaxRetried",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 5
        };

        await collection.InsertOneAsync(maxRetriedMessage);

        var store = CreateStore();

        // Act
        var messages = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateTimestamps()
    {

        // Arrange
        var collection = GetCollection();
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 0
        };

        await collection.InsertOneAsync(message);

        var store = CreateStore();

        // Act
        await store.MarkAsProcessedAsync(messageId);

        // Assert
        var updated = await collection.Find(m => m.Id == messageId).FirstOrDefaultAsync();
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.LastExecutedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateErrorAndRetryInfo()
    {

        // Arrange
        var collection = GetCollection();
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 0
        };

        await collection.InsertOneAsync(message);

        var nextRetry = DateTime.UtcNow.AddMinutes(5);
        var store = CreateStore();

        // Act
        await store.MarkAsFailedAsync(messageId, "Test error", nextRetry);

        // Assert
        var updated = await collection.Find(m => m.Id == messageId).FirstOrDefaultAsync();
        updated!.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldNotBeNull();
        updated.NextRetryAtUtc!.Value.ShouldBe(nextRetry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_ShouldResetForNextExecution()
    {

        // Arrange
        var collection = GetCollection();
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "RecurringTask",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            ProcessedAtUtc = DateTime.UtcNow,
            IsRecurring = true,
            CronExpression = "0 9 * * *",
            RetryCount = 2,
            ErrorMessage = "Previous error"
        };

        await collection.InsertOneAsync(message);

        var nextSchedule = DateTime.UtcNow.AddDays(1);
        var store = CreateStore();

        // Act
        await store.RescheduleRecurringMessageAsync(messageId, nextSchedule);

        // Assert
        var updated = await collection.Find(m => m.Id == messageId).FirstOrDefaultAsync();
        updated!.ScheduledAtUtc.ShouldBe(nextSchedule, TimeSpan.FromSeconds(1));
        updated.ProcessedAtUtc.ShouldBeNull();
        updated.RetryCount.ShouldBe(0);
        updated.ErrorMessage.ShouldBeNull();
        updated.NextRetryAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task CancelAsync_ShouldDeleteMessage()
    {

        // Arrange
        var collection = GetCollection();
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "ToCancel",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await collection.InsertOneAsync(message);

        var store = CreateStore();

        // Act
        await store.CancelAsync(messageId);

        // Assert
        var cancelled = await collection.Find(m => m.Id == messageId).FirstOrDefaultAsync();
        cancelled.ShouldBeNull();
    }

    [Fact]
    public async Task RecurringMessage_IsRecurringFlag_ShouldBePersisted()
    {

        // Arrange
        var store = CreateStore();
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DailyReport",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
            IsRecurring = true,
            CronExpression = "0 9 * * *",
            RetryCount = 0
        };

        // Act
        await store.AddAsync(message);

        // Assert
        var collection = GetCollection();
        var stored = await collection.Find(m => m.Id == message.Id).FirstOrDefaultAsync();
        stored!.IsRecurring.ShouldBeTrue();
        stored.CronExpression.ShouldBe("0 9 * * *");
    }

    [Fact]
    public async Task GetDueMessagesAsync_WithNextRetryInFuture_ShouldExcludeMessage()
    {

        // Arrange
        var collection = GetCollection();

        var futureRetry = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "FutureRetry",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 1,
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(10)
        };

        await collection.InsertOneAsync(futureRetry);

        var store = CreateStore();

        // Act
        var messages = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDueMessagesAsync_WithNextRetryInPast_ShouldIncludeMessage()
    {

        // Arrange
        var collection = GetCollection();

        var pastRetry = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "PastRetry",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 1,
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(-5)
        };

        await collection.InsertOneAsync(pastRetry);

        var store = CreateStore();

        // Act
        var messages = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldHaveSingleItem();
        messages.First().Id.ShouldBe(pastRetry.Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldOrderByScheduledAtUtc()
    {

        // Arrange
        var collection = GetCollection();

        var older = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Older",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-2),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
            RetryCount = 0
        };

        var newer = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Newer",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0
        };

        // Add in reverse order to verify sorting
        await collection.InsertManyAsync([newer, older]);

        var store = CreateStore();

        // Act
        var messages = (await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ToList();

        // Assert
        messages[0].Id.ShouldBe(older.Id);
        messages[1].Id.ShouldBe(newer.Id);
    }

    [Fact]
    public async Task ConcurrentScheduledMessageCreation_ShouldNotCorruptData()
    {

        // Arrange & Act
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var store = CreateStore();
            var messageId = Guid.NewGuid();

            var message = new ScheduledMessage
            {
                Id = messageId,
                RequestType = $"Concurrent{i}",
                Content = $"{{\"index\":{i}}}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(i + 1),
                CreatedAtUtc = DateTime.UtcNow,
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
            var stored = await collection.Find(m => m.Id == id).FirstOrDefaultAsync();
            stored.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task TimeBoundaryCondition_ExactlyDueMessage_ShouldBeIncluded()
    {

        // Arrange
        var collection = GetCollection();
        var now = DateTime.UtcNow;

        var exactlyDue = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "ExactlyDue",
            Content = "{}",
            ScheduledAtUtc = now,
            CreatedAtUtc = now.AddHours(-1),
            RetryCount = 0
        };

        await collection.InsertOneAsync(exactlyDue);

        var store = CreateStore();

        // Act - Wait a tiny bit to ensure "now" in the store is >= scheduledAtUtc
        await Task.Delay(10);
        var messages = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldHaveSingleItem();
        messages.First().Id.ShouldBe(exactlyDue.Id);
    }

    private ScheduledMessageStoreMongoDB CreateStore()
    {
        return new ScheduledMessageStoreMongoDB(
            _fixture.Client!,
            _options,
            NullLogger<ScheduledMessageStoreMongoDB>.Instance);
    }

    private IMongoCollection<ScheduledMessage> GetCollection()
    {
        return _fixture.Database!.GetCollection<ScheduledMessage>(_options.Value.Collections.ScheduledMessages);
    }
}
