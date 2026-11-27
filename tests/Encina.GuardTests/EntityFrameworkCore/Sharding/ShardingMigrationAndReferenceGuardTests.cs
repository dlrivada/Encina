using Encina.EntityFrameworkCore.Sharding;
using Encina.EntityFrameworkCore.Sharding.Migrations;
using Encina.EntityFrameworkCore.Sharding.ReferenceTables;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using Encina.Sharding.ReplicaSelection;
using Microsoft.EntityFrameworkCore;

namespace Encina.GuardTests.EntityFrameworkCore.Sharding;

/// <summary>
/// Guard clause tests for the EF Core sharding migration executor, history store, schema introspector,
/// reference table store and the sharded read/write context factory (#3726).
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class ShardingMigrationAndReferenceGuardTests
{
    private static readonly ShardInfo Shard = new("shard-0", "Server=test;Database=shard0");

    private static IShardedDbContextFactory<TestShardDbContext> Factory()
        => Substitute.For<IShardedDbContextFactory<TestShardDbContext>>();

    private static MigrationScript Script() => new("m1", "SELECT 1", "SELECT 2", "test", "abc");

    #region Executor

    [Fact]
    public void Executor_NullFactory_Throws()
        => Should.Throw<ArgumentNullException>(() => new EfCoreMigrationExecutor<TestShardDbContext>(null!));

    [Fact]
    public async Task Executor_ExecuteSqlAsync_NullShard_Throws()
    {
        var executor = new EfCoreMigrationExecutor<TestShardDbContext>(Factory());

        await Should.ThrowAsync<ArgumentNullException>(() => executor.ExecuteSqlAsync(null!, "SELECT 1"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Executor_ExecuteSqlAsync_InvalidSql_Throws(string? sql)
    {
        var executor = new EfCoreMigrationExecutor<TestShardDbContext>(Factory());

        await Should.ThrowAsync<ArgumentException>(() => executor.ExecuteSqlAsync(Shard, sql!));
    }

    #endregion

    #region History store

    [Fact]
    public void HistoryStore_NullFactory_Throws()
        => Should.Throw<ArgumentNullException>(() => new EfCoreMigrationHistoryStore<TestShardDbContext>(null!));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task HistoryStore_InvalidShardId_Throws(string? shardId)
    {
        var store = new EfCoreMigrationHistoryStore<TestShardDbContext>(Factory());

        await Should.ThrowAsync<ArgumentException>(() => store.GetAppliedAsync(shardId!));
        await Should.ThrowAsync<ArgumentException>(
            () => store.RecordAppliedAsync(shardId!, Script(), TimeSpan.Zero));
        await Should.ThrowAsync<ArgumentException>(() => store.RecordRolledBackAsync(shardId!, "m1"));
        await Should.ThrowAsync<ArgumentException>(() => store.ApplyHistoricalMigrationsAsync(shardId!, []));
    }

    [Fact]
    public async Task HistoryStore_NullArguments_Throw()
    {
        var store = new EfCoreMigrationHistoryStore<TestShardDbContext>(Factory());

        await Should.ThrowAsync<ArgumentNullException>(
            () => store.RecordAppliedAsync("shard-0", null!, TimeSpan.Zero));
        await Should.ThrowAsync<ArgumentException>(() => store.RecordRolledBackAsync("shard-0", " "));
        await Should.ThrowAsync<ArgumentNullException>(() => store.EnsureHistoryTableExistsAsync(null!));
        await Should.ThrowAsync<ArgumentNullException>(() => store.ApplyHistoricalMigrationsAsync("shard-0", null!));
    }

    #endregion

    #region Schema introspector

    [Fact]
    public void Introspector_NullFactory_Throws()
        => Should.Throw<ArgumentNullException>(() => new EfCoreSchemaIntrospector<TestShardDbContext>(null!));

    [Fact]
    public async Task Introspector_CompareAsync_NullShards_Throw()
    {
        var introspector = new EfCoreSchemaIntrospector<TestShardDbContext>(Factory());

        await Should.ThrowAsync<ArgumentNullException>(() => introspector.CompareAsync(null!, Shard, true));
        await Should.ThrowAsync<ArgumentNullException>(() => introspector.CompareAsync(Shard, null!, true));
    }

    #endregion

    #region Reference table store

    [Fact]
    public void ReferenceTableStore_NullContext_Throws()
        => Should.Throw<ArgumentNullException>(() => new ReferenceTableStoreEF(null!));

    [Fact]
    public async Task ReferenceTableStore_UpsertAsync_NullEntities_Throws()
    {
        using var context = new TestShardDbContext(
            new DbContextOptionsBuilder<TestShardDbContext>().UseInMemoryDatabase("guard-ref").Options);
        var store = new ReferenceTableStoreEF(context);

        await Should.ThrowAsync<ArgumentNullException>(() => store.UpsertAsync<object>(null!));
    }

    #endregion

    #region Sharded read/write factory

    [Fact]
    public void ReadWriteFactory_NullArguments_Throw()
    {
        var topology = new ShardTopology([Shard]);
        var options = new ShardedReadWriteOptions();
        var tracker = Substitute.For<IReplicaHealthTracker>();
        var provider = Substitute.For<IServiceProvider>();
        Action<DbContextOptionsBuilder<TestShardDbContext>, string> configure = (_, _) => { };

        Should.Throw<ArgumentNullException>(() => new ShardedReadWriteDbContextFactory<TestShardDbContext>(
            null!, options, tracker, provider, configure));
        Should.Throw<ArgumentNullException>(() => new ShardedReadWriteDbContextFactory<TestShardDbContext>(
            topology, null!, tracker, provider, configure));
        Should.Throw<ArgumentNullException>(() => new ShardedReadWriteDbContextFactory<TestShardDbContext>(
            topology, options, null!, provider, configure));
        Should.Throw<ArgumentNullException>(() => new ShardedReadWriteDbContextFactory<TestShardDbContext>(
            topology, options, tracker, null!, configure));
        Should.Throw<ArgumentNullException>(() => new ShardedReadWriteDbContextFactory<TestShardDbContext>(
            topology, options, tracker, provider, null!));
    }

    #endregion

    public sealed class TestShardDbContext(DbContextOptions<TestShardDbContext> options) : DbContext(options);
}
