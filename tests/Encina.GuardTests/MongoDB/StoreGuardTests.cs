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
}
