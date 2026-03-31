using Encina.MongoDB;
using Encina.MongoDB.Auditing;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Auditing;

public class AuditStoreMethodGuardTests
{
    private static AuditStoreMongoDB CreateStore()
    {
        var client = Substitute.For<IMongoClient>();
        var db = Substitute.For<IMongoDatabase>();
        client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(db);
        db.GetCollection<AuditEntryDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(Substitute.For<IMongoCollection<AuditEntryDocument>>());
        return new AuditStoreMongoDB(client,
            Options.Create(new EncinaMongoDbOptions { DatabaseName = "test" }),
            NullLogger<AuditStoreMongoDB>.Instance);
    }

    [Fact]
    public async Task RecordAsync_NullEntry_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.RecordAsync(null!));
    }

    [Fact]
    public async Task GetByEntityAsync_NullEntityType_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () => await store.GetByEntityAsync(null!, null));
    }

    [Fact]
    public async Task GetByEntityAsync_EmptyEntityType_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () => await store.GetByEntityAsync("", null));
    }

    [Fact]
    public async Task GetByUserAsync_NullUserId_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () => await store.GetByUserAsync(null!, null, null));
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_NullId_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () => await store.GetByCorrelationIdAsync(null!));
    }

    [Fact]
    public async Task QueryAsync_NullQuery_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.QueryAsync(null!));
    }
}
