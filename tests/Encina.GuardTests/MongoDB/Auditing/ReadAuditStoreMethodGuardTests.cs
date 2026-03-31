using Encina.MongoDB;
using Encina.MongoDB.Auditing;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Auditing;

public class ReadAuditStoreMethodGuardTests
{
    private static ReadAuditStoreMongoDB CreateStore()
    {
        var client = Substitute.For<IMongoClient>();
        var db = Substitute.For<IMongoDatabase>();
        client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(db);
        db.GetCollection<ReadAuditEntryDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(Substitute.For<IMongoCollection<ReadAuditEntryDocument>>());
        return new ReadAuditStoreMongoDB(client,
            Options.Create(new EncinaMongoDbOptions { DatabaseName = "test" }),
            NullLogger<ReadAuditStoreMongoDB>.Instance);
    }

    [Fact]
    public async Task LogReadAsync_NullEntry_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.LogReadAsync(null!));
    }

    [Fact]
    public async Task GetAccessHistoryAsync_NullEntityType_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () => await store.GetAccessHistoryAsync(null!, "id"));
    }

    [Fact]
    public async Task GetAccessHistoryAsync_NullEntityId_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () => await store.GetAccessHistoryAsync("type", null!));
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_NullUserId_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetUserAccessHistoryAsync(null!, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task QueryAsync_NullQuery_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.QueryAsync(null!));
    }
}
