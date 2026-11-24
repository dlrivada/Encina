using System.Reflection;
using Encina.Messaging.Outbox;
using Encina.MongoDB;
using Encina.MongoDB.Outbox;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.MongoDB.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxStoreMongoDB"/> against substituted driver interfaces. Filters and updates are
/// rendered to BSON and compared as whole documents.
/// </summary>
[Trait("Category", "Unit")]
public sealed class OutboxStoreMongoDBTests
{
    private static readonly DateTimeOffset FakeNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IMongoClient _client = Substitute.For<IMongoClient>();
    private readonly IMongoDatabase _database = Substitute.For<IMongoDatabase>();
    private readonly IMongoCollection<OutboxMessage> _collection = Substitute.For<IMongoCollection<OutboxMessage>>();
    private readonly FakeTimeProvider _time = new(FakeNow);
    private readonly OutboxStoreMongoDB _store;

    public OutboxStoreMongoDBTests()
    {
        _client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(_database);
        _database.GetCollection<OutboxMessage>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(_collection);
        _store = new OutboxStoreMongoDB(
            _client,
            Options.Create(new EncinaMongoDbOptions { DatabaseName = "TestDb" }),
            Substitute.For<ILogger<OutboxStoreMongoDB>>(),
            _time);
    }

    #region Constructor

    [Fact]
    public void Constructor_NullMongoClient_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new OutboxStoreMongoDB(
            null!, Options.Create(new EncinaMongoDbOptions()), Substitute.For<ILogger<OutboxStoreMongoDB>>()))
            .ParamName.ShouldBe("mongoClient");
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new OutboxStoreMongoDB(
            _client, null!, Substitute.For<ILogger<OutboxStoreMongoDB>>()))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new OutboxStoreMongoDB(
            _client, Options.Create(new EncinaMongoDbOptions()), null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ResolvesConfiguredDatabaseAndCollection()
    {
        var options = new EncinaMongoDbOptions { DatabaseName = "Custom" };
        options.Collections.Outbox = "custom_outbox";

        _ = new OutboxStoreMongoDB(_client, Options.Create(options), Substitute.For<ILogger<OutboxStoreMongoDB>>());

        _client.Received(1).GetDatabase("Custom", Arg.Any<MongoDatabaseSettings>());
        _database.Received(1).GetCollection<OutboxMessage>("custom_outbox", Arg.Any<MongoCollectionSettings>());
    }

    [Fact]
    public void Constructor_WithoutTimeProvider_UsesTheSystemClock()
    {
        var store = new OutboxStoreMongoDB(
            _client, Options.Create(new EncinaMongoDbOptions()), Substitute.For<ILogger<OutboxStoreMongoDB>>());

        var field = typeof(OutboxStoreMongoDB).GetField("_timeProvider", BindingFlags.Instance | BindingFlags.NonPublic);

        field.ShouldNotBeNull();
        field.GetValue(store).ShouldBeSameAs(TimeProvider.System);
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_OutboxMessage_InsertsSameInstanceAndForwardsToken()
    {
        var message = NewMessage();
        OutboxMessage? inserted = null;
        using var cts = new CancellationTokenSource();
        _collection.InsertOneAsync(
                Arg.Do<OutboxMessage>(m => inserted = m), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _store.AddAsync(message, cts.Token);

        result.IsRight.ShouldBeTrue();
        inserted.ShouldBeSameAs(message);
        await _collection.Received(1).InsertOneAsync(message, Arg.Any<InsertOneOptions>(), cts.Token);
    }

    [Fact]
    public async Task AddAsync_ForeignMessage_InsertsCopyOfEveryField()
    {
        var source = NewMessage();
        source.ProcessedAtUtc = FakeNow.UtcDateTime.AddMinutes(1);
        source.ErrorMessage = "failure";
        source.RetryCount = 2;
        source.NextRetryAtUtc = FakeNow.UtcDateTime.AddMinutes(5);
        var foreign = Substitute.For<IOutboxMessage>();
        foreign.Id.Returns(source.Id);
        foreign.NotificationType.Returns(source.NotificationType);
        foreign.Content.Returns(source.Content);
        foreign.CreatedAtUtc.Returns(source.CreatedAtUtc);
        foreign.ProcessedAtUtc.Returns(source.ProcessedAtUtc);
        foreign.ErrorMessage.Returns(source.ErrorMessage);
        foreign.RetryCount.Returns(source.RetryCount);
        foreign.NextRetryAtUtc.Returns(source.NextRetryAtUtc);
        OutboxMessage? inserted = null;
        _collection.InsertOneAsync(
                Arg.Do<OutboxMessage>(m => inserted = m), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _store.AddAsync(foreign);

        result.IsRight.ShouldBeTrue();
        inserted.ShouldNotBeNull();
        inserted.ShouldNotBeSameAs(source);
        inserted.ToBsonDocument().ShouldBe(source.ToBsonDocument());
    }

    [Fact]
    public async Task AddAsync_DriverThrows_ReturnsLeftAddFailed()
    {
        _collection.InsertOneAsync(
                Arg.Any<OutboxMessage>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _store.AddAsync(NewMessage());

        CodeOf(result).ShouldBe("outbox.add_failed");
    }

    [Fact]
    public async Task AddAsync_Cancelled_Propagates()
    {
        _collection.InsertOneAsync(
                Arg.Any<OutboxMessage>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => _store.AddAsync(NewMessage()));
    }

    #endregion

    #region GetPendingMessagesAsync

    [Fact]
    public async Task GetPendingMessagesAsync_ReturnsDocumentsAndSendsFullFilterSortAndLimit()
    {
        var first = NewMessage();
        var second = NewMessage();
        using var cts = new CancellationTokenSource();
        var capture = ArrangeFind(first, second);

        var result = await _store.GetPendingMessagesAsync(batchSize: 25, maxRetries: 3, cts.Token);

        RightOf(result).ToList().ShouldBe([first, second]);
        Render(capture.Filter!).ShouldBe(BsonDocument.Parse($$"""
            {
              "processedAtUtc": null,
              "retryCount": { "$lt": 3 },
              "$or": [
                { "nextRetryAtUtc": null },
                { "nextRetryAtUtc": { "$lte": {{DateJson(FakeNow.UtcDateTime)}} } }
              ]
            }
            """));
        capture.Options!.Limit.ShouldBe(25);
        RenderSort(capture.Options.Sort!).ShouldBe(new BsonDocument("createdAtUtc", 1));
        await _collection.Received(1).FindAsync(
            Arg.Any<FilterDefinition<OutboxMessage>>(),
            Arg.Any<FindOptions<OutboxMessage, OutboxMessage>>(),
            cts.Token);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_NoDocuments_ReturnsEmpty()
    {
        ArrangeFind();

        var result = await _store.GetPendingMessagesAsync(10, 3);

        RightOf(result).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_AdvancedClock_UsesTheNewInstantAsRetryCutoff()
    {
        var capture = ArrangeFind();
        _time.Advance(TimeSpan.FromMinutes(90));

        await _store.GetPendingMessagesAsync(10, 3);

        Render(capture.Filter!)["$or"][1]["nextRetryAtUtc"]["$lte"].ToUniversalTime()
            .ShouldBe(FakeNow.UtcDateTime.AddMinutes(90));
    }

    [Fact]
    public async Task GetPendingMessagesAsync_DriverThrows_ReturnsLeftGetPendingFailed()
    {
        _collection.FindAsync(
                Arg.Any<FilterDefinition<OutboxMessage>>(),
                Arg.Any<FindOptions<OutboxMessage, OutboxMessage>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException());

        var result = await _store.GetPendingMessagesAsync(10, 3);

        CodeOf(result).ShouldBe("outbox.get_pending_failed");
    }

    [Fact]
    public async Task GetPendingMessagesAsync_Cancelled_Propagates()
    {
        _collection.FindAsync(
                Arg.Any<FilterDefinition<OutboxMessage>>(),
                Arg.Any<FindOptions<OutboxMessage, OutboxMessage>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => _store.GetPendingMessagesAsync(10, 3));
    }

    #endregion

    #region MarkAsProcessedAsync

    [Theory]
    [InlineData(1L)]
    [InlineData(0L)]
    public async Task MarkAsProcessedAsync_SetsProcessedInstantForTheMessage(long modified)
    {
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var capture = ArrangeUpdate(modified);

        var result = await _store.MarkAsProcessedAsync(id, cts.Token);

        result.IsRight.ShouldBeTrue();
        Render(capture.Filter!).ShouldBe(IdFilter(id));
        Render(capture.Update!).ShouldBe(BsonDocument.Parse($$"""
            { "$set": { "processedAtUtc": {{DateJson(FakeNow.UtcDateTime)}} } }
            """));
        await _collection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<OutboxMessage>>(),
            Arg.Any<UpdateDefinition<OutboxMessage>>(),
            Arg.Any<UpdateOptions>(),
            cts.Token);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_AdvancedClock_StampsTheNewInstant()
    {
        var capture = ArrangeUpdate(1);
        _time.Advance(TimeSpan.FromMinutes(10));

        await _store.MarkAsProcessedAsync(Guid.NewGuid());

        Render(capture.Update!)["$set"]["processedAtUtc"].ToUniversalTime().ShouldBe(FakeNow.UtcDateTime.AddMinutes(10));
    }

    [Fact]
    public async Task MarkAsProcessedAsync_DriverThrows_ReturnsLeftMarkProcessedFailed()
    {
        ArrangeUpdateThrows(new InvalidOperationException());

        var result = await _store.MarkAsProcessedAsync(Guid.NewGuid());

        CodeOf(result).ShouldBe("outbox.mark_processed_failed");
    }

    [Fact]
    public async Task MarkAsProcessedAsync_Cancelled_Propagates()
    {
        ArrangeUpdateThrows(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => _store.MarkAsProcessedAsync(Guid.NewGuid()));
    }

    #endregion

    #region MarkAsFailedAsync

    [Theory]
    [InlineData(1L)]
    [InlineData(0L)]
    public async Task MarkAsFailedAsync_SetsErrorNextRetryAndIncrementsRetryCount(long modified)
    {
        var id = Guid.NewGuid();
        var next = FakeNow.UtcDateTime.AddMinutes(5);
        using var cts = new CancellationTokenSource();
        var capture = ArrangeUpdate(modified);

        var result = await _store.MarkAsFailedAsync(id, "delivery failed", next, cts.Token);

        result.IsRight.ShouldBeTrue();
        Render(capture.Filter!).ShouldBe(IdFilter(id));
        Render(capture.Update!).ShouldBe(BsonDocument.Parse($$"""
            {
              "$set": { "errorMessage": "delivery failed", "nextRetryAtUtc": {{DateJson(next)}} },
              "$inc": { "retryCount": 1 }
            }
            """));
        await _collection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<OutboxMessage>>(),
            Arg.Any<UpdateDefinition<OutboxMessage>>(),
            Arg.Any<UpdateOptions>(),
            cts.Token);
    }

    [Fact]
    public async Task MarkAsFailedAsync_NoNextRetry_SetsNullNextRetry()
    {
        var capture = ArrangeUpdate(1);

        await _store.MarkAsFailedAsync(Guid.NewGuid(), "failure", null);

        Render(capture.Update!)["$set"]["nextRetryAtUtc"].ShouldBe(BsonNull.Value);
    }

    [Fact]
    public async Task MarkAsFailedAsync_DriverThrows_ReturnsLeftMarkFailedFailed()
    {
        ArrangeUpdateThrows(new InvalidOperationException());

        var result = await _store.MarkAsFailedAsync(Guid.NewGuid(), "failure", null);

        CodeOf(result).ShouldBe("outbox.mark_failed_failed");
    }

    [Fact]
    public async Task MarkAsFailedAsync_Cancelled_Propagates()
    {
        ArrangeUpdateThrows(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(
            () => _store.MarkAsFailedAsync(Guid.NewGuid(), "failure", null));
    }

    #endregion

    #region MarkAsExhaustedAsync

    [Theory]
    [InlineData(1L)]
    [InlineData(0L)]
    public async Task MarkAsExhaustedAsync_SendsOneAtomicPipelineUpdate(long modified)
    {
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var capture = ArrangeUpdate(modified);

        var result = await _store.MarkAsExhaustedAsync(id, "gave up", maxRetries: 5, cts.Token);

        result.IsRight.ShouldBeTrue();
        Render(capture.Filter!).ShouldBe(IdFilter(id));
        RenderPipeline(capture.Update!).ShouldBe(BsonArray.Create(new[]
        {
            BsonDocument.Parse("""
                {
                  "$set": {
                    "errorMessage": "gave up",
                    "nextRetryAtUtc": null,
                    "retryCount": { "$max": [ { "$add": [ "$retryCount", 1 ] }, 5 ] }
                  }
                }
                """)
        }));
        await _collection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<OutboxMessage>>(),
            Arg.Any<UpdateDefinition<OutboxMessage>>(),
            Arg.Any<UpdateOptions>(),
            cts.Token);
    }

    [Fact]
    public async Task MarkAsExhaustedAsync_DriverThrows_ReturnsLeftMarkExhaustedFailed()
    {
        ArrangeUpdateThrows(new InvalidOperationException());

        var result = await _store.MarkAsExhaustedAsync(Guid.NewGuid(), "gave up", 3);

        CodeOf(result).ShouldBe("outbox.mark_exhausted_failed");
    }

    [Fact]
    public async Task MarkAsExhaustedAsync_Cancelled_Propagates()
    {
        ArrangeUpdateThrows(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(
            () => _store.MarkAsExhaustedAsync(Guid.NewGuid(), "gave up", 3));
    }

    #endregion

    #region GetPendingCountAsync

    [Fact]
    public async Task GetPendingCountAsync_ReturnsDriverCountForUnprocessedMessagesBelowTheLimit()
    {
        using var cts = new CancellationTokenSource();
        var capture = ArrangeCount(7L);

        var result = await _store.GetPendingCountAsync(maxRetries: 4, cts.Token);

        RightOf(result).ShouldBe(7);
        Render(capture.Filter!).ShouldBe(BsonDocument.Parse("""
            { "processedAtUtc": null, "retryCount": { "$lt": 4 } }
            """));
        await _collection.Received(1).CountDocumentsAsync(
            Arg.Any<FilterDefinition<OutboxMessage>>(), Arg.Any<CountOptions>(), cts.Token);
    }

    [Fact]
    public async Task GetPendingCountAsync_CountAboveIntRange_ReturnsLeftCountFailed()
    {
        ArrangeCount((long)int.MaxValue + 1);

        var result = await _store.GetPendingCountAsync(3);

        CodeOf(result).ShouldBe("outbox.get_pending_count_failed");
    }

    [Fact]
    public async Task GetPendingCountAsync_DriverThrows_ReturnsLeftCountFailed()
    {
        ArrangeCountThrows(new InvalidOperationException());

        var result = await _store.GetPendingCountAsync(3);

        CodeOf(result).ShouldBe("outbox.get_pending_count_failed");
    }

    [Fact]
    public async Task GetPendingCountAsync_Cancelled_Propagates()
    {
        ArrangeCountThrows(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => _store.GetPendingCountAsync(3));
    }

    #endregion

    #region GetExhaustedCountAsync

    [Fact]
    public async Task GetExhaustedCountAsync_ReturnsDriverCountForUnprocessedMessagesAtOrAboveTheLimit()
    {
        using var cts = new CancellationTokenSource();
        var capture = ArrangeCount(2L);

        var result = await _store.GetExhaustedCountAsync(maxRetries: 4, cts.Token);

        RightOf(result).ShouldBe(2);
        Render(capture.Filter!).ShouldBe(ExhaustedFilterDocument(4));
        await _collection.Received(1).CountDocumentsAsync(
            Arg.Any<FilterDefinition<OutboxMessage>>(), Arg.Any<CountOptions>(), cts.Token);
    }

    [Fact]
    public async Task GetExhaustedCountAsync_CountAboveIntRange_ReturnsLeftCountFailed()
    {
        ArrangeCount((long)int.MaxValue + 1);

        var result = await _store.GetExhaustedCountAsync(3);

        CodeOf(result).ShouldBe("outbox.get_exhausted_count_failed");
    }

    [Fact]
    public async Task GetExhaustedCountAsync_DriverThrows_ReturnsLeftCountFailed()
    {
        ArrangeCountThrows(new InvalidOperationException());

        var result = await _store.GetExhaustedCountAsync(3);

        CodeOf(result).ShouldBe("outbox.get_exhausted_count_failed");
    }

    [Fact]
    public async Task GetExhaustedCountAsync_Cancelled_Propagates()
    {
        ArrangeCountThrows(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => _store.GetExhaustedCountAsync(3));
    }

    #endregion

    #region RequeueExhaustedAsync

    [Fact]
    public async Task RequeueExhaustedAsync_NullIds_RequeuesEveryExhaustedMessage()
    {
        using var cts = new CancellationTokenSource();
        var capture = ArrangeUpdateMany(modified: 4);

        var result = await _store.RequeueExhaustedAsync(maxRetries: 3, messageIds: null, cts.Token);

        RightOf(result).ShouldBe(4);
        Render(capture.Filter!).ShouldBe(ExhaustedFilterDocument(3));
        Render(capture.Update!).ShouldBe(BsonDocument.Parse("""
            { "$set": { "retryCount": 0, "nextRetryAtUtc": null, "errorMessage": null } }
            """));
        await _collection.Received(1).UpdateManyAsync(
            Arg.Any<FilterDefinition<OutboxMessage>>(),
            Arg.Any<UpdateDefinition<OutboxMessage>>(),
            Arg.Any<UpdateOptions>(),
            cts.Token);
    }

    [Fact]
    public async Task RequeueExhaustedAsync_WithIds_RestrictsToTheDistinctIds()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var capture = ArrangeUpdateMany(modified: 2);

        var result = await _store.RequeueExhaustedAsync(3, [first, second, first]);

        RightOf(result).ShouldBe(2);
        var filter = Render(capture.Filter!);
        filter.Contains("processedAtUtc").ShouldBeTrue();
        filter["retryCount"].ShouldBe(new BsonDocument("$gte", 3));
        var ids = filter["_id"]["$in"].AsBsonArray;
        ids.Count.ShouldBe(2);
        ids.ShouldBe(
            new[] { first, second }.Select(g => (BsonValue)g.ToString("D")).ToArray(),
            ignoreOrder: true);
    }

    [Fact]
    public async Task RequeueExhaustedAsync_EmptyIds_ReturnsZeroWithoutTouchingTheCollection()
    {
        var result = await _store.RequeueExhaustedAsync(3, []);

        RightOf(result).ShouldBe(0);
        _collection.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task RequeueExhaustedAsync_ModifiedCountAboveIntRange_ReturnsLeftRequeueFailed()
    {
        ArrangeUpdateMany(modified: (long)int.MaxValue + 1);

        var result = await _store.RequeueExhaustedAsync(3, null);

        CodeOf(result).ShouldBe("outbox.requeue_exhausted_failed");
    }

    [Fact]
    public async Task RequeueExhaustedAsync_DriverThrows_ReturnsLeftRequeueFailed()
    {
        _collection.UpdateManyAsync(
                Arg.Any<FilterDefinition<OutboxMessage>>(),
                Arg.Any<UpdateDefinition<OutboxMessage>>(),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        var result = await _store.RequeueExhaustedAsync(3, null);

        CodeOf(result).ShouldBe("outbox.requeue_exhausted_failed");
    }

    [Fact]
    public async Task RequeueExhaustedAsync_Cancelled_Propagates()
    {
        _collection.UpdateManyAsync(
                Arg.Any<FilterDefinition<OutboxMessage>>(),
                Arg.Any<UpdateDefinition<OutboxMessage>>(),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => _store.RequeueExhaustedAsync(3, null));
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_ReturnsUnitWithoutTouchingTheCollection()
    {
        var result = await _store.SaveChangesAsync();

        result.IsRight.ShouldBeTrue();
        _collection.ReceivedCalls().ShouldBeEmpty();
    }

    #endregion

    #region Helpers

    private sealed class FindCapture
    {
        public FilterDefinition<OutboxMessage>? Filter { get; set; }

        public FindOptions<OutboxMessage, OutboxMessage>? Options { get; set; }
    }

    private sealed class FilterUpdateCapture
    {
        public FilterDefinition<OutboxMessage>? Filter { get; set; }

        public UpdateDefinition<OutboxMessage>? Update { get; set; }
    }

    private sealed class FilterCapture
    {
        public FilterDefinition<OutboxMessage>? Filter { get; set; }
    }

    private FindCapture ArrangeFind(params OutboxMessage[] documents)
    {
        var capture = new FindCapture();
        var cursor = Substitute.For<IAsyncCursor<OutboxMessage>>();
        var consumed = false;
        cursor.Current.Returns(_ => documents);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            var hasMore = !consumed && documents.Length > 0;
            consumed = true;
            return hasMore;
        });
        _collection.FindAsync(
                Arg.Do<FilterDefinition<OutboxMessage>>(f => capture.Filter = f),
                Arg.Do<FindOptions<OutboxMessage, OutboxMessage>>(o => capture.Options = o),
                Arg.Any<CancellationToken>())
            .Returns(cursor);
        return capture;
    }

    private FilterUpdateCapture ArrangeUpdate(long modified)
    {
        var capture = new FilterUpdateCapture();
        _collection.UpdateOneAsync(
                Arg.Do<FilterDefinition<OutboxMessage>>(f => capture.Filter = f),
                Arg.Do<UpdateDefinition<OutboxMessage>>(u => capture.Update = u),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new UpdateResult.Acknowledged(modified, modified, null));
        return capture;
    }

    private void ArrangeUpdateThrows(Exception exception)
        => _collection.UpdateOneAsync(
                Arg.Any<FilterDefinition<OutboxMessage>>(),
                Arg.Any<UpdateDefinition<OutboxMessage>>(),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

    private FilterUpdateCapture ArrangeUpdateMany(long modified)
    {
        var capture = new FilterUpdateCapture();
        _collection.UpdateManyAsync(
                Arg.Do<FilterDefinition<OutboxMessage>>(f => capture.Filter = f),
                Arg.Do<UpdateDefinition<OutboxMessage>>(u => capture.Update = u),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new UpdateResult.Acknowledged(modified, modified, null));
        return capture;
    }

    private FilterCapture ArrangeCount(long count)
    {
        var capture = new FilterCapture();
        _collection.CountDocumentsAsync(
                Arg.Do<FilterDefinition<OutboxMessage>>(f => capture.Filter = f),
                Arg.Any<CountOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(count);
        return capture;
    }

    private void ArrangeCountThrows(Exception exception)
        => _collection.CountDocumentsAsync(
                Arg.Any<FilterDefinition<OutboxMessage>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

    private static OutboxMessage NewMessage() => new()
    {
        Id = Guid.NewGuid(),
        NotificationType = "Orders.Created",
        Content = "{}",
        CreatedAtUtc = FakeNow.UtcDateTime
    };

    private static BsonDocument ExhaustedFilterDocument(int maxRetries)
        => new()
        {
            { "processedAtUtc", BsonNull.Value },
            { "retryCount", new BsonDocument("$gte", maxRetries) }
        };

    private static BsonDocument IdFilter(Guid id)
        => new("_id", id.ToString("D"));

    private static string DateJson(DateTime utc)
        => $$"""{ "$date": {{new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeMilliseconds()}} }""";

    private static RenderArgs<OutboxMessage> Args()
    {
        var registry = BsonSerializer.SerializerRegistry;
        return new RenderArgs<OutboxMessage>(registry.GetSerializer<OutboxMessage>(), registry);
    }

    private static BsonDocument Render(FilterDefinition<OutboxMessage> filter) => filter.Render(Args());

    private static BsonDocument Render(UpdateDefinition<OutboxMessage> update) => update.Render(Args()).AsBsonDocument;

    private static BsonArray RenderPipeline(UpdateDefinition<OutboxMessage> update) => update.Render(Args()).AsBsonArray;

    private static BsonDocument RenderSort(SortDefinition<OutboxMessage> sort) => sort.Render(Args());

    private static T RightOf<T>(Either<EncinaError, T> result)
        => result.Match(Right: value => value, Left: error => throw new InvalidOperationException($"Expected Right, got {error.GetCode()}"));

    private static string CodeOf<T>(Either<EncinaError, T> result)
        => result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left, got Right"),
            Left: error => error.GetCode().IfNone(string.Empty));

    #endregion
}
