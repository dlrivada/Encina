using Encina.Messaging.Transactions;
using Encina.MongoDB;
using Encina.MongoDB.Inbox;
using Encina.MongoDB.Sharding;
using Encina.MongoDB.Sharding.Migrations;
using Encina.MongoDB.Sharding.ReferenceTables;
using Encina.MongoDB.SoftDelete;
using Encina.MongoDB.Tenancy;
using Encina.MongoDB.Transactions;
using Encina.Sharding;
using Encina.Sharding.Migrations;
using Encina.Sharding.ReferenceTables;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.MongoDB;

/// <summary>
/// Runs every reachable <c>EncinaErrors.Create</c> producer of <c>Encina.MongoDB</c> and reads the declared
/// category back with <c>GetCategory()</c> (#3730, ADR-062 R4: wiring, topology and store failures are
/// <c>Internal</c>). The same codes are <c>Internal</c> in the ADO.NET, Dapper and core producers.
/// </summary>
/// <remarks>
/// <para>
/// The three inbox "not found" codes are held at <c>Internal</c> pending the maintainer's decision (D10 of the
/// register of #3730): the tests pin today's category, and switching is one argument on two calls.
/// </para>
/// <para>
/// Not reachable through the public API, so only the completeness static scan guards it:
/// <c>MissingColumnMappings</c> in <c>TenantEntityMappingBuilder.Build</c> (<c>HasId</c> always adds a field
/// mapping, and the identifier is checked first).
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Provider", "MongoDB")]
public sealed class MongoErrorCategoryTests
{
    #region Helpers

    private static EncinaError LeftOf<T>(Either<EncinaError, T> result)
    {
        result.IsLeft.ShouldBeTrue();
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        return error;
    }

    private static T RightOf<T>(Either<EncinaError, T> result)
    {
        result.IsRight.ShouldBeTrue();
        T value = default!;
        _ = result.IfRight(v => value = v);
        return value;
    }

    private static void AssertInternal<T>(Either<EncinaError, T> result, string expectedCode)
        => AssertInternal(LeftOf(result), expectedCode);

    private static void AssertInternal(EncinaError error, string expectedCode)
    {
        error.GetCode().IsSome.ShouldBeTrue();
        error.GetCode().IfSome(c => c.ShouldBe(expectedCode));
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    private static ShardInfo Shard(string id = "shard-1") => new(id, "mongodb://bogus");

    private static MigrationScript Script(string id = "m-1")
        => new(id, "{ \"ping\": 1 }", "{ \"ping\": 1 }", "description", "sha256:abc");

    public sealed class Widget
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string TenantId { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }

    private sealed class NoKeyEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Collection factory whose every lookup throws, driving the catch block of the stores.</summary>
    private sealed class ThrowingCollectionFactory : IShardedMongoCollectionFactory
    {
        public Either<EncinaError, IMongoCollection<TEntity>> GetCollectionForShard<TEntity>(string shardId, string collectionName)
            where TEntity : class
            => throw new InvalidOperationException("boom");

        public Either<EncinaError, IMongoCollection<TEntity>> GetDefaultCollection<TEntity>(string collectionName)
            where TEntity : class
            => throw new InvalidOperationException("boom");

        public Either<EncinaError, IReadOnlyDictionary<string, IMongoCollection<TEntity>>> GetAllCollections<TEntity>(string collectionName)
            where TEntity : class
            => throw new InvalidOperationException("boom");
    }

    #endregion

    #region Entity mapping builders

    [Fact]
    public void SoftDeleteBuilder_NoId_IsMissingPrimaryKeyInternal()
    {
        var builder = new SoftDeleteEntityMappingBuilder<Widget, Guid>();
        builder.HasSoftDelete(e => e.IsDeleted, "isDeleted");

        AssertInternal(builder.Build(), "mongodb.mapping.missing_primary_key");
    }

    [Fact]
    public void SoftDeleteBuilder_NoSoftDelete_IsMissingColumnMappingsInternal()
    {
        var builder = new SoftDeleteEntityMappingBuilder<Widget, Guid>();
        builder.HasId<Guid>(e => e.Id);

        AssertInternal(builder.Build(), "mongodb.mapping.missing_property_mappings");
    }

    [Fact]
    public void SoftDeleteBuilder_FullyConfigured_BuildsMapping()
    {
        var builder = new SoftDeleteEntityMappingBuilder<Widget, Guid>();
        builder.HasId<Guid>(e => e.Id);
        builder.HasSoftDelete(e => e.IsDeleted, "isDeleted");

        RightOf(builder.Build()).ShouldNotBeNull();
    }

    [Fact]
    public void TenantBuilder_NoCollection_IsMissingTableNameInternal()
    {
        var builder = new TenantEntityMappingBuilder<Widget, Guid>();

        AssertInternal(builder.Build(), "mongodb.mapping.missing_collection_name");
    }

    [Fact]
    public void TenantBuilder_NoId_IsMissingPrimaryKeyInternal()
    {
        var builder = new TenantEntityMappingBuilder<Widget, Guid>().ToCollection("widgets");

        AssertInternal(builder.Build(), "mongodb.mapping.missing_primary_key");
    }

    [Fact]
    public void TenantBuilder_FullyConfigured_BuildsMapping()
    {
        var builder = new TenantEntityMappingBuilder<Widget, Guid>()
            .ToCollection("widgets")
            .HasId(e => e.Id)
            .HasTenantId(e => e.TenantId)
            .MapField(e => e.Name);

        RightOf(builder.Build()).CollectionName.ShouldBe("widgets");
    }

    #endregion

    #region Sharding migrations

    [Fact]
    public async Task Executor_MalformedCommand_IsMigrationFailedInternal()
    {
        var executor = new MongoMigrationExecutor(Substitute.For<IShardedMongoCollectionFactory>());

        var result = await executor.ExecuteSqlAsync(Shard(), "this is not json");

        AssertInternal(result, MigrationErrorCodes.MigrationFailed);
    }

    [Fact]
    public async Task HistoryStore_GetApplied_FactoryThrows_IsMigrationFailedInternal()
    {
        var store = new MongoMigrationHistoryStore(new ThrowingCollectionFactory());

        AssertInternal(await store.GetAppliedAsync("shard-1"), MigrationErrorCodes.MigrationFailed);
    }

    [Fact]
    public async Task HistoryStore_RecordApplied_FactoryThrows_IsMigrationFailedInternal()
    {
        var store = new MongoMigrationHistoryStore(new ThrowingCollectionFactory());

        AssertInternal(
            await store.RecordAppliedAsync("shard-1", Script(), TimeSpan.FromSeconds(1)),
            MigrationErrorCodes.MigrationFailed);
    }

    [Fact]
    public async Task HistoryStore_RecordRolledBack_FactoryThrows_IsRollbackFailedInternal()
    {
        var store = new MongoMigrationHistoryStore(new ThrowingCollectionFactory());

        AssertInternal(await store.RecordRolledBackAsync("shard-1", "m-1"), MigrationErrorCodes.RollbackFailed);
    }

    [Fact]
    public async Task HistoryStore_EnsureHistoryTable_FactoryThrows_IsMigrationFailedInternal()
    {
        var store = new MongoMigrationHistoryStore(new ThrowingCollectionFactory());

        AssertInternal(await store.EnsureHistoryTableExistsAsync(Shard()), MigrationErrorCodes.MigrationFailed);
    }

    [Fact]
    public async Task HistoryStore_ApplyHistoricalMigrations_FactoryThrows_IsMigrationFailedInternal()
    {
        var store = new MongoMigrationHistoryStore(new ThrowingCollectionFactory());

        AssertInternal(
            await store.ApplyHistoricalMigrationsAsync("shard-1", [Script()]),
            MigrationErrorCodes.MigrationFailed);
    }

    [Fact]
    public async Task Introspector_Compare_FactoryThrows_IsSchemaComparisonFailedInternal()
    {
        var introspector = new MongoSchemaIntrospector(new ThrowingCollectionFactory());

        var result = await introspector.CompareAsync(Shard("a"), Shard("b"), includeColumnDiffs: false);

        AssertInternal(result, MigrationErrorCodes.SchemaComparisonFailed);
    }

    #endregion

    #region Reference tables

    [Fact]
    public async Task ReferenceTable_Upsert_EntityWithoutKey_IsUpsertFailedInternal()
    {
        var store = new ReferenceTableStoreMongoDB(Substitute.For<IMongoDatabase>());

        var result = await store.UpsertAsync([new NoKeyEntity()]);

        AssertInternal(result, ReferenceTableErrorCodes.UpsertFailed);
    }

    [Fact]
    public async Task ReferenceTable_GetAll_EntityWithoutKey_IsGetAllFailedInternal()
    {
        var store = new ReferenceTableStoreMongoDB(Substitute.For<IMongoDatabase>());

        var result = await store.GetAllAsync<NoKeyEntity>();

        AssertInternal(result, ReferenceTableErrorCodes.GetAllFailed);
    }

    [Fact]
    public async Task ReferenceTable_Upsert_EmptyList_ReturnsZero()
    {
        var store = new ReferenceTableStoreMongoDB(Substitute.For<IMongoDatabase>());

        RightOf(await store.UpsertAsync(Array.Empty<Widget>())).ShouldBe(0);
    }

    [Fact]
    public async Task ReferenceTable_Upsert_Entities_ReturnsModifiedCount()
    {
        var database = Substitute.For<IMongoDatabase>();
        var collection = Substitute.For<IMongoCollection<Widget>>();
        database.GetCollection<Widget>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(collection);
        collection.BulkWriteAsync(
                Arg.Any<IEnumerable<WriteModel<Widget>>>(),
                Arg.Any<BulkWriteOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new BulkWriteResult<Widget>.Acknowledged(
                requestCount: 2,
                matchedCount: 1,
                deletedCount: 0,
                insertedCount: 0,
                modifiedCount: 2,
                processedRequests: [],
                upserts: []));
        var store = new ReferenceTableStoreMongoDB(database);

        var result = await store.UpsertAsync([new Widget { Id = Guid.NewGuid() }, new Widget { Id = Guid.NewGuid() }]);

        RightOf(result).ShouldBe(2);
    }

    #endregion

    #region Transactions

    [Fact]
    public void StandaloneNotSupported_IsTransactionNotSupportedInternal()
        => AssertInternal(MongoSessionAccessor.StandaloneNotSupported(), TransactionErrors.NotSupportedCode);

    #endregion

    #region Inbox (D10: held at Internal pending the maintainer's decision)

    private static (InboxStoreMongoDB Store, IMongoCollection<InboxMessage> Collection) Inbox()
    {
        var client = Substitute.For<IMongoClient>();
        var database = Substitute.For<IMongoDatabase>();
        var collection = Substitute.For<IMongoCollection<InboxMessage>>();
        client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(database);
        database.GetCollection<InboxMessage>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(collection);
        collection.UpdateOneAsync(
                Arg.Any<FilterDefinition<InboxMessage>>(),
                Arg.Any<UpdateDefinition<InboxMessage>>(),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new UpdateResult.Acknowledged(0, 0, null));

        var accessor = Substitute.For<IMongoSessionAccessor>();
        accessor.CurrentSession.Returns((IClientSessionHandle?)null);
        var options = Options.Create(new EncinaMongoDbOptions { DatabaseName = "db" });

        return (new InboxStoreMongoDB(client, options, NullLogger<InboxStoreMongoDB>.Instance, null, accessor), collection);
    }

    [Fact]
    public async Task Inbox_MarkAsProcessed_NoMatch_IsNotFoundCodeHeldInternal()
    {
        var (store, _) = Inbox();

        AssertInternal(await store.MarkAsProcessedAsync("missing", "r"), InboxStoreMongoDB.MarkProcessedNotFoundCode);
    }

    [Fact]
    public async Task Inbox_CacheHandlerError_NoMatch_IsNotFoundCodeHeldInternal()
    {
        var (store, _) = Inbox();

        AssertInternal(await store.CacheHandlerErrorAsync("missing", "r"), InboxStoreMongoDB.CacheNotFoundCode);
    }

    [Fact]
    public async Task Inbox_MarkAsFailed_NoMatch_IsNotFoundCodeHeldInternal()
    {
        var (store, _) = Inbox();

        AssertInternal(await store.MarkAsFailedAsync("missing", "err", null), InboxStoreMongoDB.FailedNotFoundCode);
    }

    #endregion
}
