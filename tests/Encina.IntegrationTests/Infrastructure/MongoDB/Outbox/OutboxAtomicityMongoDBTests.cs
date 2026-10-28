using Encina.IntegrationTests.Infrastructure.MongoDB.ReadWriteSeparation;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using Encina.Messaging.Transactions;
using Encina.MongoDB;
using Encina.MongoDB.Outbox;
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

    [Fact(Skip = "D15 open design Dilemma (#718): an independent inbox insert is invisible to the enlisted processed mark; see ServerBehavior_AnIndependentInsertAfterTheFirstTransactionOperationIsInvisibleToTheTransaction. Remove the skip when the maintainer decides.")]
    public async Task InboxAndOutbox_FirstTimeMessage_ShareTheOutboxTransaction()
    {
        // The design risk of ADR-048 on MongoDB: GetMessage (enlisted) -> AddAsync (independent, no session) ->
        // handler writes -> MarkAsProcessedAsync (enlisted) inside the outbox transaction.
        await using var provider = Build(inbox: true);
        var key = Guid.NewGuid().ToString();

        var result = await SendIdempotentAsync(provider, Mode.Commit, key);

        result.ShouldBeRight();
        (await CountAsync()).ShouldBe((1L, 2L));
        var row = await ReadInboxAsync(key);
        row.ShouldNotBeNull();
        row.ProcessedAtUtc.ShouldNotBeNull();
    }

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

    private ServiceProvider Build(bool inbox = false, Action<IServiceCollection>? customize = null)
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
