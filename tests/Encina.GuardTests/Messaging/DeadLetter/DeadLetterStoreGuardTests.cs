using Encina.Messaging.DeadLetter;
using Encina.Messaging.Health;
using Encina.MongoDB;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using MongoDB.Driver;

using MySqlConnector;

using Npgsql;

using NSubstitute;

using Shouldly;

using AdoMySqlFactory = Encina.ADO.MySQL.DeadLetter.DeadLetterMessageFactory;
using AdoMySqlStore = Encina.ADO.MySQL.DeadLetter.DeadLetterStoreADO;
using AdoPostgreSqlFactory = Encina.ADO.PostgreSQL.DeadLetter.DeadLetterMessageFactory;
using AdoPostgreSqlStore = Encina.ADO.PostgreSQL.DeadLetter.DeadLetterStoreADO;
using AdoSqlServerFactory = Encina.ADO.SqlServer.DeadLetter.DeadLetterMessageFactory;
using AdoSqlServerStore = Encina.ADO.SqlServer.DeadLetter.DeadLetterStoreADO;
using DapperMySqlFactory = Encina.Dapper.MySQL.DeadLetter.DeadLetterMessageFactory;
using DapperMySqlStore = Encina.Dapper.MySQL.DeadLetter.DeadLetterStoreDapper;
using DapperPostgreSqlFactory = Encina.Dapper.PostgreSQL.DeadLetter.DeadLetterMessageFactory;
using DapperPostgreSqlStore = Encina.Dapper.PostgreSQL.DeadLetter.DeadLetterStoreDapper;
using DapperSqlServerFactory = Encina.Dapper.SqlServer.DeadLetter.DeadLetterMessageFactory;
using DapperSqlServerStore = Encina.Dapper.SqlServer.DeadLetter.DeadLetterStoreDapper;
using EfConfiguration = Encina.EntityFrameworkCore.DeadLetter.DeadLetterMessageConfiguration;
using EfFactory = Encina.EntityFrameworkCore.DeadLetter.DeadLetterMessageFactory;
using EfStore = Encina.EntityFrameworkCore.DeadLetter.DeadLetterStoreEF;
using MongoFactory = Encina.MongoDB.DeadLetter.DeadLetterMessageFactory;
using MongoStore = Encina.MongoDB.DeadLetter.DeadLetterStoreMongoDB;

namespace Encina.GuardTests.Messaging.DeadLetter;

/// <summary>
/// Argument rules every persistent <see cref="IDeadLetterStore"/> enforces before any I/O. The stores are
/// built over unopened connections, so a violation that reached the database would fail differently.
/// </summary>
public abstract class DeadLetterStoreGuardTests
{
    /// <summary>Creates the store under test; it must never touch its database in these tests.</summary>
    protected abstract IDeadLetterStore CreateStore();

    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
        => await Should.ThrowAsync<ArgumentNullException>(() => CreateStore().AddAsync(null!));

    [Fact]
    public async Task GetAsync_EmptyId_ThrowsArgumentException()
        => await Should.ThrowAsync<ArgumentException>(() => CreateStore().GetAsync(Guid.Empty));

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    [InlineData(0, -5)]
    [InlineData(0, DeadLetterStoreLimits.MaxPageSize + 1)]
    public async Task GetMessagesAsync_SkipOrTakeOutOfRange_ThrowsArgumentOutOfRangeException(int skip, int take)
        => await Should.ThrowAsync<ArgumentOutOfRangeException>(() => CreateStore().GetMessagesAsync(null, skip, take));

    [Fact]
    public async Task TryClaimForReplayAsync_EmptyId_ThrowsArgumentException()
        => await Should.ThrowAsync<ArgumentException>(() => CreateStore().TryClaimForReplayAsync(Guid.Empty, DateTime.UtcNow));

    [Fact]
    public async Task MarkAsReplayedAsync_EmptyId_ThrowsArgumentException()
        => await Should.ThrowAsync<ArgumentException>(() => CreateStore().MarkAsReplayedAsync(Guid.Empty, "replay.succeeded"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MarkAsReplayedAsync_BlankOutcome_ThrowsArgumentException(string? replayResult)
        => await Should.ThrowAsync<ArgumentException>(() => CreateStore().MarkAsReplayedAsync(Guid.NewGuid(), replayResult!));

    [Fact]
    public async Task DeleteAsync_EmptyId_ThrowsArgumentException()
        => await Should.ThrowAsync<ArgumentException>(() => CreateStore().DeleteAsync(Guid.Empty));

    [Fact]
    public async Task DeleteManyAsync_NullFilter_ThrowsArgumentNullException()
        => await Should.ThrowAsync<ArgumentNullException>(() => CreateStore().DeleteManyAsync(null!));

    internal static DeadLetterData SampleData() => new(
        Id: Guid.NewGuid(),
        RequestType: "T",
        RequestContent: "{}",
        ErrorCode: "e",
        SourcePattern: "Outbox",
        SourceMessageId: "s",
        TotalRetryAttempts: 1,
        FirstFailedAtUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DeadLetteredAtUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ExpiresAtUtc: null);
}

public sealed class AdoSqlServerDeadLetterStoreGuardTests : DeadLetterStoreGuardTests
{
    protected override IDeadLetterStore CreateStore() => new AdoSqlServerStore(new SqlConnection());

    [Fact]
    public void Constructor_NullConnection_Throws() => Should.Throw<ArgumentNullException>(() => new AdoSqlServerStore(null!));

    [Fact]
    public void Constructor_NonDbConnection_Throws() => Should.Throw<ArgumentException>(() => new AdoSqlServerStore(Substitute.For<System.Data.IDbConnection>()));

    [Fact]
    public void Constructor_InvalidTableName_Throws() => Should.Throw<ArgumentException>(() => new AdoSqlServerStore(new SqlConnection(), "bad name; drop"));
}

public sealed class AdoPostgreSqlDeadLetterStoreGuardTests : DeadLetterStoreGuardTests
{
    protected override IDeadLetterStore CreateStore() => new AdoPostgreSqlStore(new NpgsqlConnection());

    [Fact]
    public void Constructor_NullConnection_Throws() => Should.Throw<ArgumentNullException>(() => new AdoPostgreSqlStore(null!));

    [Fact]
    public void Constructor_NonDbConnection_Throws() => Should.Throw<ArgumentException>(() => new AdoPostgreSqlStore(Substitute.For<System.Data.IDbConnection>()));

    [Fact]
    public void Constructor_InvalidTableName_Throws() => Should.Throw<ArgumentException>(() => new AdoPostgreSqlStore(new NpgsqlConnection(), "bad name; drop"));
}

public sealed class AdoMySqlDeadLetterStoreGuardTests : DeadLetterStoreGuardTests
{
    protected override IDeadLetterStore CreateStore() => new AdoMySqlStore(new MySqlConnection());

    [Fact]
    public void Constructor_NullConnection_Throws() => Should.Throw<ArgumentNullException>(() => new AdoMySqlStore(null!));

    [Fact]
    public void Constructor_NonDbConnection_Throws() => Should.Throw<ArgumentException>(() => new AdoMySqlStore(Substitute.For<System.Data.IDbConnection>()));

    [Fact]
    public void Constructor_InvalidTableName_Throws() => Should.Throw<ArgumentException>(() => new AdoMySqlStore(new MySqlConnection(), "bad name; drop"));
}

public sealed class DapperSqlServerDeadLetterStoreGuardTests : DeadLetterStoreGuardTests
{
    protected override IDeadLetterStore CreateStore() => new DapperSqlServerStore(new SqlConnection());

    [Fact]
    public void Constructor_NullConnection_Throws() => Should.Throw<ArgumentNullException>(() => new DapperSqlServerStore(null!));

    [Fact]
    public void Constructor_NonDbConnection_Throws() => Should.Throw<ArgumentException>(() => new DapperSqlServerStore(Substitute.For<System.Data.IDbConnection>()));

    [Fact]
    public void Constructor_InvalidTableName_Throws() => Should.Throw<ArgumentException>(() => new DapperSqlServerStore(new SqlConnection(), "bad name; drop"));
}

public sealed class DapperPostgreSqlDeadLetterStoreGuardTests : DeadLetterStoreGuardTests
{
    protected override IDeadLetterStore CreateStore() => new DapperPostgreSqlStore(new NpgsqlConnection());

    [Fact]
    public void Constructor_NullConnection_Throws() => Should.Throw<ArgumentNullException>(() => new DapperPostgreSqlStore(null!));

    [Fact]
    public void Constructor_NonDbConnection_Throws() => Should.Throw<ArgumentException>(() => new DapperPostgreSqlStore(Substitute.For<System.Data.IDbConnection>()));

    [Fact]
    public void Constructor_InvalidTableName_Throws() => Should.Throw<ArgumentException>(() => new DapperPostgreSqlStore(new NpgsqlConnection(), "bad name; drop"));
}

public sealed class DapperMySqlDeadLetterStoreGuardTests : DeadLetterStoreGuardTests
{
    protected override IDeadLetterStore CreateStore() => new DapperMySqlStore(new MySqlConnection());

    [Fact]
    public void Constructor_NullConnection_Throws() => Should.Throw<ArgumentNullException>(() => new DapperMySqlStore(null!));

    [Fact]
    public void Constructor_NonDbConnection_Throws() => Should.Throw<ArgumentException>(() => new DapperMySqlStore(Substitute.For<System.Data.IDbConnection>()));

    [Fact]
    public void Constructor_InvalidTableName_Throws() => Should.Throw<ArgumentException>(() => new DapperMySqlStore(new MySqlConnection(), "bad name; drop"));
}

public sealed class EntityFrameworkDeadLetterStoreGuardTests : DeadLetterStoreGuardTests
{
    protected override IDeadLetterStore CreateStore() => new EfStore(new DbContext(new DbContextOptions<DbContext>()));

    [Fact]
    public void Constructor_NullDbContext_Throws() => Should.Throw<ArgumentNullException>(() => new EfStore(null!));

    [Fact]
    public void Configuration_NullBuilder_Throws() => Should.Throw<ArgumentNullException>(() => new EfConfiguration(null).Configure(null!));
}

public sealed class MongoDbDeadLetterStoreGuardTests : DeadLetterStoreGuardTests
{
    protected override IDeadLetterStore CreateStore()
    {
        var client = Substitute.For<IMongoClient>();
        client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(Substitute.For<IMongoDatabase>());
        return new MongoStore(client, Options.Create(new EncinaMongoDbOptions()), NullLogger<MongoStore>.Instance);
    }

    [Fact]
    public void Constructor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() => new MongoStore(null!, Options.Create(new EncinaMongoDbOptions()), NullLogger<MongoStore>.Instance));

    [Fact]
    public void Constructor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() => new MongoStore(Substitute.For<IMongoClient>(), null!, NullLogger<MongoStore>.Instance));

    [Fact]
    public void Constructor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() => new MongoStore(Substitute.For<IMongoClient>(), Options.Create(new EncinaMongoDbOptions()), null!));
}

/// <summary>The dead letter health check rejects a null store and answers for a healthy empty queue.</summary>
public sealed class DeadLetterHealthCheckGuardTests
{
    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
        => Should.Throw<ArgumentNullException>(() => new DeadLetterHealthCheck(null!));

    [Fact]
    public async Task CheckHealthAsync_EmptyQueue_IsHealthy()
    {
        var store = Substitute.For<IDeadLetterStore>();
        store.GetCountAsync(Arg.Any<DeadLetterFilter?>(), Arg.Any<CancellationToken>())
            .Returns(LanguageExt.Prelude.Right<EncinaError, int>(0));
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(LanguageExt.Prelude.Right<EncinaError, IEnumerable<IDeadLetterMessage>>(Array.Empty<IDeadLetterMessage>()));
        var check = new DeadLetterHealthCheck(store);

        var result = await check.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Healthy);
    }
}

/// <summary>Every provider factory rejects a null <see cref="DeadLetterData"/>.</summary>
public sealed class DeadLetterMessageFactoryGuardTests
{
    public static TheoryData<string, IDeadLetterMessageFactory> Factories => new()
    {
        { "ADO.SqlServer", new AdoSqlServerFactory() },
        { "ADO.PostgreSQL", new AdoPostgreSqlFactory() },
        { "ADO.MySQL", new AdoMySqlFactory() },
        { "Dapper.SqlServer", new DapperSqlServerFactory() },
        { "Dapper.PostgreSQL", new DapperPostgreSqlFactory() },
        { "Dapper.MySQL", new DapperMySqlFactory() },
        { "EntityFrameworkCore", new EfFactory() },
        { "MongoDB", new MongoFactory() }
    };

    [Theory]
    [MemberData(nameof(Factories))]
    public void Create_NullData_ThrowsArgumentNullException(string provider, IDeadLetterMessageFactory factory)
    {
        provider.ShouldNotBeNullOrEmpty();
        Should.Throw<ArgumentNullException>(() => factory.Create(null!));
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void Create_ValidData_BuildsAMessageWithTheSameIdentity(string provider, IDeadLetterMessageFactory factory)
    {
        provider.ShouldNotBeNullOrEmpty();
        var data = DeadLetterStoreGuardTests.SampleData();

        var message = factory.Create(data);

        message.Id.ShouldBe(data.Id);
        message.SourceMessageId.ShouldBe(data.SourceMessageId);
        message.IsReplayed.ShouldBeFalse();
    }
}
