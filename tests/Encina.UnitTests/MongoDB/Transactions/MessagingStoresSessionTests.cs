using Encina.MongoDB;
using Encina.MongoDB.Inbox;
using Encina.MongoDB.Outbox;
using Encina.MongoDB.Sagas;
using Encina.MongoDB.Scheduling;
using Encina.MongoDB.Transactions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.MongoDB.Transactions;

/// <summary>
/// The four messaging stores run every operation in the scope's client session when the accessor has one,
/// and without a session otherwise; the inbox follows the ADR-048 split.
/// </summary>
[Trait("Category", "Unit")]
public sealed class MessagingStoresSessionTests
{
    private static readonly IOptions<EncinaMongoDbOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new EncinaMongoDbOptions { DatabaseName = "db" });

    private readonly IClientSessionHandle _session = Substitute.For<IClientSessionHandle>();
    private readonly IMongoClient _client = Substitute.For<IMongoClient>();
    private readonly IMongoSessionAccessor _accessor = Substitute.For<IMongoSessionAccessor>();

    private IMongoCollection<T> Collection<T>()
    {
        var database = Substitute.For<IMongoDatabase>();
        var collection = Substitute.For<IMongoCollection<T>>();
        _client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(database);
        database.GetCollection<T>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(collection);
        collection.UpdateOneAsync(Arg.Any<FilterDefinition<T>>(), Arg.Any<UpdateDefinition<T>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateResult.Acknowledged(1, 1, null));
        collection.UpdateOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<T>>(), Arg.Any<UpdateDefinition<T>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateResult.Acknowledged(1, 1, null));
        collection.UpdateManyAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<T>>(), Arg.Any<UpdateDefinition<T>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateResult.Acknowledged(2, 2, null));
        collection.DeleteOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<T>>(), Arg.Any<DeleteOptions>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult.Acknowledged(1));
        collection.DeleteManyAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<T>>(), Arg.Any<DeleteOptions>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult.Acknowledged(1));
        collection.CountDocumentsAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<T>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>())
            .Returns(3L);
        var cursor = Substitute.For<IAsyncCursor<T>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);
        collection.FindAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<T>>(), Arg.Any<FindOptions<T, T>>(), Arg.Any<CancellationToken>())
            .Returns(cursor);
        collection.FindAsync(Arg.Any<FilterDefinition<T>>(), Arg.Any<FindOptions<T, T>>(), Arg.Any<CancellationToken>())
            .Returns(cursor);
        return collection;
    }

    private void WithSession(bool active) => _accessor.CurrentSession.Returns(active ? _session : null);

    private OutboxStoreMongoDB Outbox() => new(_client, Options, NullLogger<OutboxStoreMongoDB>.Instance, null, _accessor);

    private InboxStoreMongoDB Inbox() => new(_client, Options, NullLogger<InboxStoreMongoDB>.Instance, null, _accessor);

    private SagaStoreMongoDB Saga() => new(_client, Options, NullLogger<SagaStoreMongoDB>.Instance, null, _accessor);

    private ScheduledMessageStoreMongoDB Scheduled() => new(_client, Options, NullLogger<ScheduledMessageStoreMongoDB>.Instance, null, _accessor);

    // ---- Outbox ----

    [Fact]
    public async Task Outbox_AddAsync_WithSession_InsertsInSession()
    {
        var collection = Collection<OutboxMessage>();
        WithSession(true);

        var result = await Outbox().AddAsync(new OutboxMessage { Id = Guid.NewGuid(), NotificationType = "t", Content = "{}" });

        result.IsRight.ShouldBeTrue();
        await collection.Received(1).InsertOneAsync(_session, Arg.Any<OutboxMessage>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Outbox_AddAsync_WithoutSession_InsertsWithoutSession()
    {
        var collection = Collection<OutboxMessage>();
        WithSession(false);

        await Outbox().AddAsync(new OutboxMessage { Id = Guid.NewGuid(), NotificationType = "t", Content = "{}" });

        await collection.Received(1).InsertOneAsync(Arg.Any<OutboxMessage>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
        await collection.DidNotReceive().InsertOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<OutboxMessage>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Outbox_ReadsAndUpdates_RunInSession()
    {
        var collection = Collection<OutboxMessage>();
        WithSession(true);
        var store = Outbox();
        var id = Guid.NewGuid();

        (await store.GetPendingMessagesAsync(10, 3)).IsRight.ShouldBeTrue();
        (await store.MarkAsProcessedAsync(id)).IsRight.ShouldBeTrue();
        (await store.MarkAsFailedAsync(id, "type", null)).IsRight.ShouldBeTrue();
        (await store.MarkAsExhaustedAsync(id, "type", 3)).IsRight.ShouldBeTrue();
        (await store.GetPendingCountAsync(3)).IsRight.ShouldBeTrue();
        (await store.GetExhaustedCountAsync(3)).IsRight.ShouldBeTrue();
        (await store.RequeueExhaustedAsync(3, null)).IsRight.ShouldBeTrue();

        await collection.Received(1).FindAsync(_session, Arg.Any<FilterDefinition<OutboxMessage>>(), Arg.Any<FindOptions<OutboxMessage, OutboxMessage>>(), Arg.Any<CancellationToken>());
        await collection.Received(3).UpdateOneAsync(_session, Arg.Any<FilterDefinition<OutboxMessage>>(), Arg.Any<UpdateDefinition<OutboxMessage>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
        await collection.Received(2).CountDocumentsAsync(_session, Arg.Any<FilterDefinition<OutboxMessage>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
        await collection.Received(1).UpdateManyAsync(_session, Arg.Any<FilterDefinition<OutboxMessage>>(), Arg.Any<UpdateDefinition<OutboxMessage>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Outbox_NoAccessor_RunsWithoutSession()
    {
        var collection = Collection<OutboxMessage>();
        var store = new OutboxStoreMongoDB(_client, Options, NullLogger<OutboxStoreMongoDB>.Instance);

        (await store.MarkAsProcessedAsync(Guid.NewGuid())).IsRight.ShouldBeTrue();
        (await store.GetPendingMessagesAsync(1, 1)).IsRight.ShouldBeTrue();

        await collection.DidNotReceive().UpdateOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<OutboxMessage>>(), Arg.Any<UpdateDefinition<OutboxMessage>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
        await collection.DidNotReceive().FindAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<OutboxMessage>>(), Arg.Any<FindOptions<OutboxMessage, OutboxMessage>>(), Arg.Any<CancellationToken>());
    }

    // ---- Inbox (ADR-048 split) ----

    [Fact]
    public async Task Inbox_EnlistedOperations_RunInSession()
    {
        var collection = Collection<InboxMessage>();
        WithSession(true);
        var store = Inbox();

        (await store.GetMessageAsync("m")).IsRight.ShouldBeTrue();
        (await store.MarkAsProcessedAsync("m", "r")).IsRight.ShouldBeTrue();
        (await store.GetExpiredMessagesAsync(5)).IsRight.ShouldBeTrue();
        (await store.RemoveExpiredMessagesAsync(["m"])).IsRight.ShouldBeTrue();

        await collection.Received(2).FindAsync(_session, Arg.Any<FilterDefinition<InboxMessage>>(), Arg.Any<FindOptions<InboxMessage, InboxMessage>>(), Arg.Any<CancellationToken>());
        await collection.Received(1).UpdateOneAsync(_session, Arg.Any<FilterDefinition<InboxMessage>>(), Arg.Any<UpdateDefinition<InboxMessage>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
        await collection.Received(1).DeleteManyAsync(_session, Arg.Any<FilterDefinition<InboxMessage>>(), Arg.Any<DeleteOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Inbox_IndependentOperations_NeverUseTheSession()
    {
        var collection = Collection<InboxMessage>();
        WithSession(true);
        var store = Inbox();

        (await store.AddAsync(new InboxMessage { MessageId = "m", RequestType = "t" })).IsRight.ShouldBeTrue();
        (await store.MarkAsFailedAsync("m", "type", null)).IsRight.ShouldBeTrue();
        (await store.CacheHandlerErrorAsync("m", "r")).IsRight.ShouldBeTrue();

        await collection.DidNotReceive().InsertOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<InboxMessage>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
        await collection.DidNotReceive().UpdateOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<InboxMessage>>(), Arg.Any<UpdateDefinition<InboxMessage>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
        await collection.Received(1).InsertOneAsync(Arg.Any<InboxMessage>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
        await collection.Received(2).UpdateOneAsync(Arg.Any<FilterDefinition<InboxMessage>>(), Arg.Any<UpdateDefinition<InboxMessage>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Inbox_MarkAsProcessed_MatchingNoDocument_ReturnsLeft()
    {
        var collection = Collection<InboxMessage>();
        WithSession(true);
        collection.UpdateOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<InboxMessage>>(), Arg.Any<UpdateDefinition<InboxMessage>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateResult.Acknowledged(0, 0, null));

        var result = await Inbox().MarkAsProcessedAsync("missing", "r");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(InboxStoreMongoDB.MarkProcessedNotFoundCode));
    }

    [Fact]
    public async Task Inbox_MarkAsProcessed_StoreThrows_ReturnsLeft()
    {
        var collection = Collection<InboxMessage>();
        WithSession(true);
        collection.UpdateOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<InboxMessage>>(), Arg.Any<UpdateDefinition<InboxMessage>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns<UpdateResult>(_ => throw new MongoException("x"));

        (await Inbox().MarkAsProcessedAsync("m", "r")).IsLeft.ShouldBeTrue();
    }

    // ---- Saga ----

    [Fact]
    public async Task Saga_Operations_RunInSession()
    {
        var collection = Collection<SagaState>();
        WithSession(true);
        var store = Saga();
        var saga = new SagaState { SagaId = Guid.NewGuid(), SagaType = "s", Data = "{}", Status = "Running" };

        (await store.AddAsync(saga)).IsRight.ShouldBeTrue();
        (await store.GetAsync(saga.SagaId)).IsRight.ShouldBeTrue();
        (await store.UpdateAsync(saga)).IsRight.ShouldBeTrue();
        (await store.GetStuckSagasAsync(TimeSpan.FromMinutes(1), 5)).IsRight.ShouldBeTrue();
        (await store.GetExpiredSagasAsync(5)).IsRight.ShouldBeTrue();

        await collection.Received(1).InsertOneAsync(_session, Arg.Any<SagaState>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
        await collection.Received(3).FindAsync(_session, Arg.Any<FilterDefinition<SagaState>>(), Arg.Any<FindOptions<SagaState, SagaState>>(), Arg.Any<CancellationToken>());
        await collection.Received(1).UpdateOneAsync(_session, Arg.Any<FilterDefinition<SagaState>>(), Arg.Any<UpdateDefinition<SagaState>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
    }

    // ---- Scheduled messages ----

    [Fact]
    public async Task Scheduled_Operations_RunInSession()
    {
        var collection = Collection<ScheduledMessage>();
        WithSession(true);
        var store = Scheduled();
        var id = Guid.NewGuid();

        (await store.AddAsync(new ScheduledMessage { Id = id, RequestType = "t", Content = "{}" })).IsRight.ShouldBeTrue();
        (await store.GetDueMessagesAsync(5, 3)).IsRight.ShouldBeTrue();
        (await store.MarkAsProcessedAsync(id)).IsRight.ShouldBeTrue();
        (await store.MarkAsFailedAsync(id, "type", null)).IsRight.ShouldBeTrue();
        (await store.RescheduleRecurringMessageAsync(id, DateTime.UtcNow)).IsRight.ShouldBeTrue();
        (await store.CancelAsync(id)).IsRight.ShouldBeTrue();

        await collection.Received(1).InsertOneAsync(_session, Arg.Any<ScheduledMessage>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
        await collection.Received(1).FindAsync(_session, Arg.Any<FilterDefinition<ScheduledMessage>>(), Arg.Any<FindOptions<ScheduledMessage, ScheduledMessage>>(), Arg.Any<CancellationToken>());
        await collection.Received(3).UpdateOneAsync(_session, Arg.Any<FilterDefinition<ScheduledMessage>>(), Arg.Any<UpdateDefinition<ScheduledMessage>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
        await collection.Received(1).DeleteOneAsync(_session, Arg.Any<FilterDefinition<ScheduledMessage>>(), Arg.Any<DeleteOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scheduled_WithoutSession_CancelDeletesWithoutSession()
    {
        var collection = Collection<ScheduledMessage>();
        collection.DeleteOneAsync(Arg.Any<FilterDefinition<ScheduledMessage>>(), Arg.Any<CancellationToken>()).Returns(new DeleteResult.Acknowledged(1));
        WithSession(false);

        (await Scheduled().CancelAsync(Guid.NewGuid())).IsRight.ShouldBeTrue();

        await collection.DidNotReceive().DeleteOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<ScheduledMessage>>(), Arg.Any<DeleteOptions>(), Arg.Any<CancellationToken>());
    }
}
