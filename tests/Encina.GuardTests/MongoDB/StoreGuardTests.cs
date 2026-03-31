using Encina.MongoDB;
using Encina.MongoDB.Inbox;
using Encina.MongoDB.Outbox;
using Encina.MongoDB.Sagas;
using Encina.MongoDB.Scheduling;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB;

public class StoreGuardTests
{
    private static readonly IOptions<EncinaMongoDbOptions> Opts = Options.Create(new EncinaMongoDbOptions { DatabaseName = "test" });

    #region InboxStoreMongoDB

    [Fact]
    public void InboxStore_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new InboxStoreMongoDB(null!, Opts, NullLogger<InboxStoreMongoDB>.Instance));

    [Fact]
    public void InboxStore_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new InboxStoreMongoDB(Substitute.For<IMongoClient>(), null!, NullLogger<InboxStoreMongoDB>.Instance));

    [Fact]
    public void InboxStore_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new InboxStoreMongoDB(Substitute.For<IMongoClient>(), Opts, null!));

    #endregion

    #region OutboxStoreMongoDB

    [Fact]
    public void OutboxStore_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new OutboxStoreMongoDB(null!, Opts, NullLogger<OutboxStoreMongoDB>.Instance));

    [Fact]
    public void OutboxStore_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new OutboxStoreMongoDB(Substitute.For<IMongoClient>(), null!, NullLogger<OutboxStoreMongoDB>.Instance));

    [Fact]
    public void OutboxStore_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new OutboxStoreMongoDB(Substitute.For<IMongoClient>(), Opts, null!));

    #endregion

    #region SagaStoreMongoDB

    [Fact]
    public void SagaStore_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SagaStoreMongoDB(null!, Opts, NullLogger<SagaStoreMongoDB>.Instance));

    [Fact]
    public void SagaStore_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SagaStoreMongoDB(Substitute.For<IMongoClient>(), null!, NullLogger<SagaStoreMongoDB>.Instance));

    [Fact]
    public void SagaStore_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SagaStoreMongoDB(Substitute.For<IMongoClient>(), Opts, null!));

    #endregion

    #region ScheduledMessageStoreMongoDB

    [Fact]
    public void ScheduledStore_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ScheduledMessageStoreMongoDB(null!, Opts, NullLogger<ScheduledMessageStoreMongoDB>.Instance));

    [Fact]
    public void ScheduledStore_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ScheduledMessageStoreMongoDB(Substitute.For<IMongoClient>(), null!, NullLogger<ScheduledMessageStoreMongoDB>.Instance));

    [Fact]
    public void ScheduledStore_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ScheduledMessageStoreMongoDB(Substitute.For<IMongoClient>(), Opts, null!));

    #endregion

    #region Method Guards — AddAsync/UpdateAsync null checks

    [Fact]
    public async Task InboxStore_AddAsync_NullMessage_Throws()
    {
        var client = CreateMockClient();
        var store = new InboxStoreMongoDB(client, Opts, NullLogger<InboxStoreMongoDB>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.AddAsync(null!));
    }

    [Fact]
    public async Task OutboxStore_AddAsync_NullMessage_Throws()
    {
        var client = CreateMockClient();
        var store = new OutboxStoreMongoDB(client, Opts, NullLogger<OutboxStoreMongoDB>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.AddAsync(null!));
    }

    [Fact]
    public async Task SagaStore_AddAsync_NullState_Throws()
    {
        var client = CreateMockClient();
        var store = new SagaStoreMongoDB(client, Opts, NullLogger<SagaStoreMongoDB>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.AddAsync(null!));
    }

    [Fact]
    public async Task SagaStore_UpdateAsync_NullState_Throws()
    {
        var client = CreateMockClient();
        var store = new SagaStoreMongoDB(client, Opts, NullLogger<SagaStoreMongoDB>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.UpdateAsync(null!));
    }

    [Fact]
    public async Task ScheduledStore_AddAsync_NullMessage_Throws()
    {
        var client = CreateMockClient();
        var store = new ScheduledMessageStoreMongoDB(client, Opts, NullLogger<ScheduledMessageStoreMongoDB>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.AddAsync(null!));
    }

    private static IMongoClient CreateMockClient()
    {
        var client = Substitute.For<IMongoClient>();
        var db = Substitute.For<IMongoDatabase>();
        client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(db);
        db.GetCollection<global::Encina.MongoDB.Inbox.InboxMessage>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(Substitute.For<IMongoCollection<global::Encina.MongoDB.Inbox.InboxMessage>>());
        db.GetCollection<global::Encina.MongoDB.Outbox.OutboxMessage>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(Substitute.For<IMongoCollection<global::Encina.MongoDB.Outbox.OutboxMessage>>());
        db.GetCollection<global::Encina.MongoDB.Sagas.SagaState>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(Substitute.For<IMongoCollection<global::Encina.MongoDB.Sagas.SagaState>>());
        db.GetCollection<global::Encina.MongoDB.Scheduling.ScheduledMessage>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(Substitute.For<IMongoCollection<global::Encina.MongoDB.Scheduling.ScheduledMessage>>());
        return client;
    }

    #endregion
}
