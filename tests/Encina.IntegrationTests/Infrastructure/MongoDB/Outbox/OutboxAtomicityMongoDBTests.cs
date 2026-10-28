using Encina.IntegrationTests.Infrastructure.MongoDB.ReadWriteSeparation;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using Encina.Messaging.Transactions;
using Encina.MongoDB;
using Encina.MongoDB.Outbox;
using Encina.MongoDB.Transactions;
using Microsoft.Extensions.Logging.Abstractions;
using Encina.TestInfrastructure.Fixtures;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Xunit;
using static Encina.IntegrationTests.Infrastructure.MongoDB.Outbox.OutboxAtomicityMongoDB;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Outbox;

/// <summary>
/// Proves the transactional outbox of MongoDB (#718) on a real single-node replica set: the outbox documents of a
/// request's notifications and its business document are written in the request's single client-session
/// transaction and commit or abort together; a failed outbox write or a rollback-only participant fails the
/// request.
/// </summary>
[Collection(MongoDbReplicaSetCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class OutboxAtomicityMongoDBTests : IAsyncLifetime
{
    private readonly MongoDbReplicaSetFixture _fixture;

    public OutboxAtomicityMongoDBTests(MongoDbReplicaSetFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task InboxAndOutbox_FirstTimeMessage_ShareTheOutboxTransaction()
    {
        // GetMessage (plain read) -> AddAsync (independent) -> handler writes (the snapshot starts here) ->
        // MarkAsProcessedAsync (enlisted) inside the outbox transaction. See the probe test below for why
        // GetMessage must not be in the session.
        await using var provider = Build(inbox: true);
        var key = Guid.NewGuid().ToString();

        var result = await SendIdempotentAsync(provider, Mode.Commit, key);

        result.ShouldBeRight();
        (await CountAsync()).ShouldBe((1L, 2L));
        var row = await ReadInboxAsync(key);
        row.ShouldNotBeNull();
        row.ProcessedAtUtc.ShouldNotBeNull();
        row.Response.ShouldNotBeNull();

        // A second delivery of the same id is answered from the inbox: the handler does not run again. (The outer
        // outbox behavior still writes the request's notifications for it; that is a finding filed as a follow-up
        // issue and is not asserted here.)
        (await SendIdempotentAsync(provider, Mode.Commit, key)).ShouldBeRight();
        (await CountAsync()).Business.ShouldBe(1L);
    }

    [Fact]
    public async Task InboxAndOutbox_FailingHandler_KeepsTheFailureRecordAndRollsTheRestBack()
    {
        await using var provider = Build(inbox: true);

        var leftKey = Guid.NewGuid().ToString();
        (await SendIdempotentAsync(provider, Mode.ReturnLeft, leftKey)).IsLeft.ShouldBeTrue();
        var leftRow = await ReadInboxAsync(leftKey);
        leftRow.ShouldNotBeNull();
        leftRow.Response.ShouldNotBeNull();

        var throwKey = Guid.NewGuid().ToString();
        // The inbox behavior turns a handler exception into a Left after recording the failure.
        (await SendIdempotentAsync(provider, Mode.Throw, throwKey)).IsLeft.ShouldBeTrue();
        var throwRow = await ReadInboxAsync(throwKey);
        throwRow.ShouldNotBeNull();
        throwRow.RetryCount.ShouldBe(1);
        throwRow.ProcessedAtUtc.ShouldBeNull();

        // The failure records are independent of the transaction; the business and outbox writes rolled back.
        (await CountAsync()).ShouldBe((0L, 0L));
    }

    [Fact]
    public async Task UnitOfWorkRollbackInsideTheRequest_FailsWithRollbackOnlyAndLeavesNoDocument()
    {
        await using var provider = Build();

        var result = await SendOrderAsync(provider, 2, Mode.UnitOfWorkRollback);

        result.ShouldBeErrorWithCode(TransactionErrors.RollbackOnlyCode);
        (await CountAsync()).ShouldBe((0L, 0L));
    }

    [Fact]
    public async Task DisposingTheAccessor_AbortsAnOpenTransaction()
    {
        var accessor = NewAccessor();
        var lease = (await accessor.BeginOrJoinAsync()).RequireLease();
        await _fixture.Database!.GetCollection<BsonDocument>(BusinessCollection)
            .InsertOneAsync(accessor.CurrentSession!, new BsonDocument("_id", "abandoned"));

        accessor.Dispose();

        await lease.DisposeAsync();
        (await CountAsync()).Business.ShouldBe(0L);
    }

    [Fact]
    public async Task AbortAfterAFailedCommit_DoesNotThrow()
    {
        var accessor = NewAccessor();
        await using var lease = (await accessor.BeginOrJoinAsync()).RequireLease();
        await _fixture.Database!.GetCollection<BsonDocument>(BusinessCollection)
            .InsertOneAsync(accessor.CurrentSession!, new BsonDocument("_id", "doomed"));
        // The transaction ends behind the lease's back, so the commit fails.
        await accessor.CurrentSession!.AbortTransactionAsync();

        var commit = await lease.CommitAsync();

        commit.ShouldBeErrorWithCode(TransactionErrors.CommitFailedCode);
        await Should.NotThrowAsync(async () => await lease.RollbackAsync());
        (await CountAsync()).Business.ShouldBe(0L);
    }

    [Fact]
    public async Task ReadWriteSeparationBehavior_CoexistsWithTheSessionTransaction()
    {
        await using var provider = Build(customize: null, readWriteSeparation: true);

        var result = await SendOrderAsync(provider, 2, Mode.Commit);

        result.ShouldBeRight();
        (await CountAsync()).ShouldBe((1L, 2L));
    }

    [Fact]
    public async Task SagaAndScheduledMessageStores_EnlistInTheSessionTransaction()
    {
        var accessor = NewAccessor();
        var options = Microsoft.Extensions.Options.Options.Create(new EncinaMongoDbOptions { DatabaseName = MongoDbReplicaSetFixture.DatabaseName });
        var sagas = new global::Encina.MongoDB.Sagas.SagaStoreMongoDB(
            _fixture.Client!, options, NullLogger<global::Encina.MongoDB.Sagas.SagaStoreMongoDB>.Instance, null, accessor);
        var scheduled = new global::Encina.MongoDB.Scheduling.ScheduledMessageStoreMongoDB(
            _fixture.Client!, options, NullLogger<global::Encina.MongoDB.Scheduling.ScheduledMessageStoreMongoDB>.Instance, null, accessor);
        var sagaId = Guid.NewGuid();
        var scheduledId = Guid.NewGuid();

        await using (var lease = (await accessor.BeginOrJoinAsync()).RequireLease())
        {
            (await sagas.AddAsync(new global::Encina.MongoDB.Sagas.SagaState
            {
                SagaId = sagaId, SagaType = "S", Data = "{}", Status = "Running", StartedAtUtc = DateTime.UtcNow, LastUpdatedAtUtc = DateTime.UtcNow
            })).ShouldBeRight();
            (await scheduled.AddAsync(new global::Encina.MongoDB.Scheduling.ScheduledMessage
            {
                Id = scheduledId, RequestType = "R", Content = "{}", ScheduledAtUtc = DateTime.UtcNow.AddHours(1), CreatedAtUtc = DateTime.UtcNow
            })).ShouldBeRight();
            // Read-your-writes inside the session.
            (await sagas.GetAsync(sagaId)).ShouldBeRight().IsSome.ShouldBeTrue();
            await lease.RollbackAsync();
        }

        var names = new MongoDbCollectionNames();
        (await _fixture.Database!.GetCollection<BsonDocument>(names.Sagas).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldBe(0L);
        (await _fixture.Database!.GetCollection<BsonDocument>(names.ScheduledMessages).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldBe(0L);
    }

    private MongoSessionAccessor NewAccessor()
        => new(_fixture.Client!, NullLogger<MongoSessionAccessor>.Instance);

    [Fact]
    public async Task ServerBehavior_AnIndependentInsertAfterTheFirstTransactionOperationIsInvisibleToTheTransaction()
    {
        // Raw-driver evidence for the D15 finding, with no Encina code involved.
        var documents = _fixture.Database!.GetCollection<BsonDocument>("snapshot_probe");
        using var session = await _fixture.Client!.StartSessionAsync();
        session.StartTransaction();
        (await documents.Find(session, new BsonDocument("_id", "first")).FirstOrDefaultAsync()).ShouldBeNull();

        await documents.InsertOneAsync(new BsonDocument { { "_id", "late" }, { "v", 0 } });
        var update = await documents.UpdateOneAsync(
            session, new BsonDocument("_id", "late"), new BsonDocument("$set", new BsonDocument("v", 1)));

        update.MatchedCount.ShouldBe(0, "the insert was committed after the transaction's snapshot");
        await session.AbortTransactionAsync();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(0)]
    public async Task Commit_WritesTheBusinessDocumentAndOneOutboxDocumentPerNotification(int notifications)
    {
        await using var provider = Build();

        var result = await SendOrderAsync(provider, notifications, Mode.Commit);

        result.ShouldBeRight();
        (await CountAsync()).ShouldBe((1L, (long)notifications));
    }

    [Fact]
    public async Task HandlerLeft_LeavesNoBusinessAndNoOutboxDocument()
    {
        await using var provider = Build();

        var result = await SendOrderAsync(provider, 3, Mode.ReturnLeft);

        result.ShouldBeErrorWithCode("biz.rule");
        (await CountAsync()).ShouldBe((0L, 0L));
    }

    [Fact]
    public async Task HandlerException_RethrowsAndLeavesNoDocument()
    {
        await using var provider = Build();

        var thrown = await Should.ThrowAsync<InvalidOperationException>(async () => await SendOrderAsync(provider, 2, Mode.Throw));

        thrown.Message.ShouldBe("boom");
        (await CountAsync()).ShouldBe((0L, 0L));
    }

    [Fact]
    public async Task FailedOutboxWrite_FailsTheRequestAndRollsTheBusinessDocumentBack()
    {
        var fixedId = Guid.NewGuid();
        await using var provider = Build(customize: services => services.AddScoped<IOutboxMessageFactory>(
            _ => new FixedIdOutboxMessageFactory(new OutboxMessageFactory(), fixedId)));

        var result = await SendOrderAsync(provider, 2, Mode.Commit);

        result.IsLeft.ShouldBeTrue();
        (await CountAsync()).ShouldBe((0L, 0L));
    }

    private ServiceProvider Build(bool inbox = false, Action<IServiceCollection>? customize = null, bool readWriteSeparation = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddScoped<IRequestHandler<PlaceOrder, int>, PlaceOrderHandler>();
        services.AddScoped<IRequestHandler<PlaceIdempotentOrder, int>, PlaceIdempotentOrderHandler>();
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseOutbox = true;
            options.UseInbox = inbox;
            options.UseReadWriteSeparation = readWriteSeparation;
        });
        customize?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static async Task<Either<EncinaError, int>> SendOrderAsync(ServiceProvider provider, int notifications, Mode mode)
    {
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        return await encina.Send(new PlaceOrder(Guid.NewGuid(), notifications, mode));
    }

    private static async Task<Either<EncinaError, int>> SendIdempotentAsync(ServiceProvider provider, Mode mode, string idempotencyKey)
    {
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        return await encina.Send(
            new PlaceIdempotentOrder(Guid.NewGuid(), 2, mode),
            RequestContext.CreateForTest(idempotencyKey: idempotencyKey));
    }

    private async Task<(long Business, long Outbox)> CountAsync()
    {
        var database = _fixture.Database!;
        var business = await database.GetCollection<BsonDocument>(BusinessCollection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        var outbox = await database.GetCollection<BsonDocument>(new MongoDbCollectionNames().Outbox).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        return (business, outbox);
    }

    private async Task<global::Encina.MongoDB.Inbox.InboxMessage?> ReadInboxAsync(string messageId)
        => await _fixture.Database!
            .GetCollection<global::Encina.MongoDB.Inbox.InboxMessage>(new MongoDbCollectionNames().Inbox)
            .Find(m => m.MessageId == messageId)
            .FirstOrDefaultAsync();
}
