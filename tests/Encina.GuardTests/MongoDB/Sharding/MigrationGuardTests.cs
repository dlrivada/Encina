using Encina.MongoDB.Sharding;
using Encina.MongoDB.Sharding.Migrations;
using Encina.Sharding;
using Encina.Sharding.Migrations;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoMigrationExtensions = Encina.MongoDB.Sharding.Migrations.MigrationServiceCollectionExtensions;

namespace Encina.GuardTests.MongoDB.Sharding;

public class MigrationGuardTests
{
    private static readonly IShardedMongoCollectionFactory Factory = Substitute.For<IShardedMongoCollectionFactory>();

    #region MongoMigrationExecutor

    [Fact]
    public void MigrationExecutor_NullFactory_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MongoMigrationExecutor(null!));

    [Fact]
    public async Task MigrationExecutor_ExecuteSqlAsync_NullShardInfo_Throws()
    {
        var executor = new MongoMigrationExecutor(Factory);
        await Should.ThrowAsync<ArgumentNullException>(
            () => executor.ExecuteSqlAsync(null!, "{}"));
    }

    [Fact]
    public async Task MigrationExecutor_ExecuteSqlAsync_NullSql_Throws()
    {
        var executor = new MongoMigrationExecutor(Factory);
        var shard = new ShardInfo("shard-0", "mongodb://x:27017/db");
        await Should.ThrowAsync<ArgumentException>(
            () => executor.ExecuteSqlAsync(shard, null!));
    }

    [Fact]
    public async Task MigrationExecutor_ExecuteSqlAsync_EmptySql_Throws()
    {
        var executor = new MongoMigrationExecutor(Factory);
        var shard = new ShardInfo("shard-0", "mongodb://x:27017/db");
        await Should.ThrowAsync<ArgumentException>(
            () => executor.ExecuteSqlAsync(shard, ""));
    }

    [Fact]
    public async Task MigrationExecutor_ExecuteSqlAsync_WhitespaceSql_Throws()
    {
        var executor = new MongoMigrationExecutor(Factory);
        var shard = new ShardInfo("shard-0", "mongodb://x:27017/db");
        await Should.ThrowAsync<ArgumentException>(
            () => executor.ExecuteSqlAsync(shard, "   "));
    }

    #endregion

    #region MongoMigrationHistoryStore

    [Fact]
    public void MigrationHistoryStore_NullFactory_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MongoMigrationHistoryStore(null!));

    [Fact]
    public async Task MigrationHistoryStore_GetAppliedAsync_NullShardId_Throws()
    {
        var store = new MongoMigrationHistoryStore(Factory);
        await Should.ThrowAsync<ArgumentException>(
            () => store.GetAppliedAsync(null!));
    }

    [Fact]
    public async Task MigrationHistoryStore_GetAppliedAsync_EmptyShardId_Throws()
    {
        var store = new MongoMigrationHistoryStore(Factory);
        await Should.ThrowAsync<ArgumentException>(
            () => store.GetAppliedAsync(""));
    }

    [Fact]
    public async Task MigrationHistoryStore_RecordAppliedAsync_NullShardId_Throws()
    {
        var store = new MongoMigrationHistoryStore(Factory);
        var script = new MigrationScript("m1", "{}", "{}", "desc", "hash");
        await Should.ThrowAsync<ArgumentException>(
            () => store.RecordAppliedAsync(null!, script, TimeSpan.Zero));
    }

    [Fact]
    public async Task MigrationHistoryStore_RecordAppliedAsync_NullScript_Throws()
    {
        var store = new MongoMigrationHistoryStore(Factory);
        await Should.ThrowAsync<ArgumentNullException>(
            () => store.RecordAppliedAsync("shard-0", null!, TimeSpan.Zero));
    }

    [Fact]
    public async Task MigrationHistoryStore_RecordRolledBackAsync_NullShardId_Throws()
    {
        var store = new MongoMigrationHistoryStore(Factory);
        await Should.ThrowAsync<ArgumentException>(
            () => store.RecordRolledBackAsync(null!, "m1"));
    }

    [Fact]
    public async Task MigrationHistoryStore_RecordRolledBackAsync_NullMigrationId_Throws()
    {
        var store = new MongoMigrationHistoryStore(Factory);
        await Should.ThrowAsync<ArgumentException>(
            () => store.RecordRolledBackAsync("shard-0", null!));
    }

    [Fact]
    public async Task MigrationHistoryStore_RecordRolledBackAsync_EmptyMigrationId_Throws()
    {
        var store = new MongoMigrationHistoryStore(Factory);
        await Should.ThrowAsync<ArgumentException>(
            () => store.RecordRolledBackAsync("shard-0", ""));
    }

    [Fact]
    public async Task MigrationHistoryStore_EnsureHistoryTableExistsAsync_NullShardInfo_Throws()
    {
        var store = new MongoMigrationHistoryStore(Factory);
        await Should.ThrowAsync<ArgumentNullException>(
            () => store.EnsureHistoryTableExistsAsync(null!));
    }

    [Fact]
    public async Task MigrationHistoryStore_ApplyHistoricalAsync_NullShardId_Throws()
    {
        var store = new MongoMigrationHistoryStore(Factory);
        await Should.ThrowAsync<ArgumentException>(
            () => store.ApplyHistoricalMigrationsAsync(null!, []));
    }

    [Fact]
    public async Task MigrationHistoryStore_ApplyHistoricalAsync_NullScripts_Throws()
    {
        var store = new MongoMigrationHistoryStore(Factory);
        await Should.ThrowAsync<ArgumentNullException>(
            () => store.ApplyHistoricalMigrationsAsync("shard-0", null!));
    }

    #endregion

    #region MongoSchemaIntrospector

    [Fact]
    public void SchemaIntrospector_NullFactory_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MongoSchemaIntrospector(null!));

    [Fact]
    public async Task SchemaIntrospector_CompareAsync_NullShard_Throws()
    {
        var introspector = new MongoSchemaIntrospector(Factory);
        var baseline = new ShardInfo("base", "mongodb://x:27017/db");
        await Should.ThrowAsync<ArgumentNullException>(
            () => introspector.CompareAsync(null!, baseline, false));
    }

    [Fact]
    public async Task SchemaIntrospector_CompareAsync_NullBaseline_Throws()
    {
        var introspector = new MongoSchemaIntrospector(Factory);
        var shard = new ShardInfo("shard-0", "mongodb://x:27017/db");
        await Should.ThrowAsync<ArgumentNullException>(
            () => introspector.CompareAsync(shard, null!, false));
    }

    #endregion

    #region MigrationServiceCollectionExtensions

    [Fact]
    public void AddEncinaMongoShardMigration_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            MongoMigrationExtensions.AddEncinaMongoShardMigration(null!));

    #endregion
}
