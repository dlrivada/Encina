using Encina.MongoDB;
using Encina.MongoDB.Outbox;
using Encina.Testing.Fakes.Stores;
using MongoDB.Driver;
using AdoMySqlOutbox = Encina.ADO.MySQL.Outbox;
using AdoPostgreSqlOutbox = Encina.ADO.PostgreSQL.Outbox;
using AdoSqlServerOutbox = Encina.ADO.SqlServer.Outbox;
using DapperMySqlOutbox = Encina.Dapper.MySQL.Outbox;
using DapperPostgreSqlOutbox = Encina.Dapper.PostgreSQL.Outbox;
using DapperSqlServerOutbox = Encina.Dapper.SqlServer.Outbox;
using EfOutbox = Encina.EntityFrameworkCore.Outbox;

namespace Encina.GuardTests.Messaging.Outbox;

/// <summary>
/// Guard tests for the exhausted-message members of <see cref="IOutboxStore"/> (#1150) on every store:
/// a negative retry limit is rejected before any database access, and an empty identifier list requeues
/// nothing without touching the database.
/// </summary>
[Trait("Category", "Guard")]
public sealed class OutboxStoreExhaustionGuardTests
{
    public static TheoryData<string> StoreNames() =>
    [
        "ADO.SqlServer",
        "ADO.PostgreSQL",
        "ADO.MySQL",
        "Dapper.SqlServer",
        "Dapper.PostgreSQL",
        "Dapper.MySQL",
        "EntityFrameworkCore",
        "MongoDB",
        "Fake"
    ];

    [Theory]
    [MemberData(nameof(StoreNames))]
    public async Task GetPendingCountAsync_NegativeMaxRetries_ThrowsArgumentOutOfRangeException(string storeName)
    {
        var store = CreateStore(storeName);

        var ex = await Should.ThrowAsync<ArgumentOutOfRangeException>(() => store.GetPendingCountAsync(-1));

        ex.ParamName.ShouldBe("maxRetries");
    }

    [Theory]
    [MemberData(nameof(StoreNames))]
    public async Task GetExhaustedCountAsync_NegativeMaxRetries_ThrowsArgumentOutOfRangeException(string storeName)
    {
        var store = CreateStore(storeName);

        var ex = await Should.ThrowAsync<ArgumentOutOfRangeException>(() => store.GetExhaustedCountAsync(-1));

        ex.ParamName.ShouldBe("maxRetries");
    }

    [Theory]
    [MemberData(nameof(StoreNames))]
    public async Task RequeueExhaustedAsync_NegativeMaxRetries_ThrowsArgumentOutOfRangeException(string storeName)
    {
        var store = CreateStore(storeName);

        var ex = await Should.ThrowAsync<ArgumentOutOfRangeException>(() => store.RequeueExhaustedAsync(-1, null));

        ex.ParamName.ShouldBe("maxRetries");
    }

    [Theory]
    [MemberData(nameof(StoreNames))]
    public async Task RequeueExhaustedAsync_EmptyMessageIds_ReturnsZero(string storeName)
    {
        var store = CreateStore(storeName);

        var result = await store.RequeueExhaustedAsync(3, []);

        result.ShouldBeRight().ShouldBe(0);
    }

    private static IOutboxStore CreateStore(string storeName) => storeName switch
    {
        "ADO.SqlServer" => new AdoSqlServerOutbox.OutboxStoreADO(Substitute.For<IDbConnection>()),
        "ADO.PostgreSQL" => new AdoPostgreSqlOutbox.OutboxStoreADO(Substitute.For<IDbConnection>()),
        "ADO.MySQL" => new AdoMySqlOutbox.OutboxStoreADO(Substitute.For<IDbConnection>()),
        "Dapper.SqlServer" => new DapperSqlServerOutbox.OutboxStoreDapper(Substitute.For<IDbConnection>()),
        "Dapper.PostgreSQL" => new DapperPostgreSqlOutbox.OutboxStoreDapper(Substitute.For<IDbConnection>()),
        "Dapper.MySQL" => new DapperMySqlOutbox.OutboxStoreDapper(Substitute.For<IDbConnection>()),
        "EntityFrameworkCore" => new EfOutbox.OutboxStoreEF(new ExhaustionGuardDbContext(
            new DbContextOptionsBuilder<ExhaustionGuardDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options)),
        "MongoDB" => new OutboxStoreMongoDB(
            Substitute.For<IMongoClient>(),
            Options.Create(new EncinaMongoDbOptions { DatabaseName = "test" }),
            NullLogger<OutboxStoreMongoDB>.Instance),
        "Fake" => new FakeOutboxStore(),
        _ => throw new ArgumentOutOfRangeException(nameof(storeName), storeName, "Unknown store.")
    };

    private sealed class ExhaustionGuardDbContext(DbContextOptions<ExhaustionGuardDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<EfOutbox.OutboxMessage>(entity => entity.HasKey(e => e.Id));
    }
}
