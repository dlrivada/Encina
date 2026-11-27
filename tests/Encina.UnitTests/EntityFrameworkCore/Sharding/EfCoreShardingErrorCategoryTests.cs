using Encina.EntityFrameworkCore.Sharding;
using Encina.EntityFrameworkCore.Sharding.Migrations;
using Encina.EntityFrameworkCore.Sharding.ReferenceTables;
using Encina.Messaging.ReadWriteSeparation;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using Encina.Sharding.ReferenceTables;
using Encina.Sharding.ReplicaSelection;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.Sharding;

/// <summary>
/// Runs every <c>EncinaErrors.Create</c> producer of <c>Encina.EntityFrameworkCore/Sharding</c> and reads the
/// declared category back with <c>GetCategory()</c> (#3726, ADR-062 R1 and R4: wiring, topology, store and
/// provider failures are <c>Internal</c>; decision D11 of #3728: <c>AllReplicasUnhealthy</c> is <c>Unavailable</c>).
/// The same codes have the same category in the ADO.NET, Dapper and core packages.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EfCoreShardingErrorCategoryTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider = new ServiceCollection().BuildServiceProvider();

    public void Dispose() => _serviceProvider.Dispose();

    #region Helpers

    private static EncinaError LeftOf<T>(Either<EncinaError, T> result)
    {
        result.IsLeft.ShouldBeTrue();
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        return error;
    }

    private static void AssertCategory(EncinaError error, string expectedCode, ErrorCategory expectedCategory)
    {
        error.GetCode().IsSome.ShouldBeTrue();
        error.GetCode().IfSome(c => c.ShouldBe(expectedCode));
        error.GetCategory().ShouldBe(expectedCategory);
    }

    private static void AssertInternal<T>(Either<EncinaError, T> result, string expectedCode)
        => AssertCategory(LeftOf(result), expectedCode, ErrorCategory.Internal);

    private static ShardInfo Shard(string id) => new(id, $"Server={id};Database=test;");

    private static IShardedDbContextFactory<ShardTestDbContext> InMemoryFactory()
    {
        var factory = Substitute.For<IShardedDbContextFactory<ShardTestDbContext>>();
        factory.CreateContextForShard(Arg.Any<string>())
            .Returns(_ => (Either<EncinaError, ShardTestDbContext>)NewContext());
        return factory;
    }

    private static IShardedDbContextFactory<ShardTestDbContext> ThrowingFactory()
    {
        var factory = Substitute.For<IShardedDbContextFactory<ShardTestDbContext>>();
        factory.CreateContextForShard(Arg.Any<string>()).Returns(_ => throw new InvalidOperationException("boom"));
        return factory;
    }

    private static ShardTestDbContext NewContext()
        => new(new DbContextOptionsBuilder<ShardTestDbContext>()
            .UseInMemoryDatabase($"EfShardCategory-{Guid.NewGuid()}")
            .Options);

    private static MigrationScript Script() => new("m1", "SELECT 1", "SELECT 2", "test", "abc");

    private ShardedReadWriteDbContextFactory<ShardTestDbContext> ReplicaFactory(
        ShardInfo shard, ShardedReadWriteOptions options, IReplicaHealthTracker tracker)
        => new(
            new ShardTopology([shard]),
            options,
            tracker,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));

    #endregion

    #region ShardedReadWriteDbContextFactory

    [Fact]
    public void CreateReadContextForShard_NoReplicasAndNoFallback_IsInternal()
    {
        var tracker = Substitute.For<IReplicaHealthTracker>();
        var factory = ReplicaFactory(
            Shard("shard-0"),
            new ShardedReadWriteOptions { FallbackToPrimaryWhenNoReplicas = false },
            tracker);

        var result = factory.CreateReadContextForShard("shard-0");

        AssertInternal(result, ShardingErrorCodes.NoReplicasConfigured);
    }

    [Fact]
    public void CreateReadContextForShard_AllReplicasUnhealthyAndNoFallback_IsUnavailable()
    {
        var tracker = Substitute.For<IReplicaHealthTracker>();
        tracker.GetAvailableReplicas(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(new List<string>());
        var shard = new ShardInfo(
            "shard-0",
            "Server=shard-0;Database=test;",
            ReplicaConnectionStrings: ["Server=replica-0;Database=test;"]);
        var factory = ReplicaFactory(
            shard,
            new ShardedReadWriteOptions { FallbackToPrimaryWhenNoReplicas = false },
            tracker);

        var result = factory.CreateReadContextForShard("shard-0");

        AssertCategory(LeftOf(result), ShardingErrorCodes.AllReplicasUnhealthy, ErrorCategory.Unavailable);
    }

    #endregion

    #region Migrations

    [Fact]
    public async Task ExecuteSqlAsync_ProviderFailure_IsInternal()
    {
        // The in-memory provider is not relational: ExecuteSqlRawAsync throws InvalidOperationException.
        var executor = new EfCoreMigrationExecutor<ShardTestDbContext>(InMemoryFactory());

        var result = await executor.ExecuteSqlAsync(Shard("shard-0"), "SELECT 1");

        AssertInternal(result, MigrationErrorCodes.MigrationFailed);
    }

    [Fact]
    public async Task ExecuteSqlAsync_FactoryThrows_IsInternal()
    {
        var executor = new EfCoreMigrationExecutor<ShardTestDbContext>(ThrowingFactory());

        var result = await executor.ExecuteSqlAsync(Shard("shard-0"), "SELECT 1");

        AssertInternal(result, MigrationErrorCodes.MigrationFailed);
    }

    [Fact]
    public async Task ExecuteSqlAsync_FactoryReturnsLeft_PassesTheLeftThrough()
    {
        var factory = Substitute.For<IShardedDbContextFactory<ShardTestDbContext>>();
        factory.CreateContextForShard(Arg.Any<string>())
            .Returns(Either<EncinaError, ShardTestDbContext>.Left(
                EncinaErrors.Create("test.factory", ErrorCategory.NotFound, "no shard")));
        var executor = new EfCoreMigrationExecutor<ShardTestDbContext>(factory);

        var result = await executor.ExecuteSqlAsync(Shard("shard-0"), "SELECT 1");

        AssertCategory(LeftOf(result), "test.factory", ErrorCategory.NotFound);
    }

    [Fact]
    public async Task GetAppliedAsync_ProviderFailure_IsInternal()
    {
        var store = new EfCoreMigrationHistoryStore<ShardTestDbContext>(InMemoryFactory());

        var result = await store.GetAppliedAsync("shard-0");

        AssertInternal(result, MigrationErrorCodes.MigrationFailed);
    }

    [Fact]
    public async Task RecordAppliedAsync_ProviderFailure_IsInternal()
    {
        var store = new EfCoreMigrationHistoryStore<ShardTestDbContext>(InMemoryFactory());

        var result = await store.RecordAppliedAsync("shard-0", Script(), TimeSpan.FromMilliseconds(5));

        AssertInternal(result, MigrationErrorCodes.MigrationFailed);
    }

    [Fact]
    public async Task RecordRolledBackAsync_ProviderFailure_IsInternal()
    {
        var store = new EfCoreMigrationHistoryStore<ShardTestDbContext>(InMemoryFactory());

        var result = await store.RecordRolledBackAsync("shard-0", "m1");

        AssertInternal(result, MigrationErrorCodes.RollbackFailed);
    }

    [Fact]
    public async Task EnsureHistoryTableExistsAsync_ProviderFailure_IsInternal()
    {
        var store = new EfCoreMigrationHistoryStore<ShardTestDbContext>(InMemoryFactory());

        var result = await store.EnsureHistoryTableExistsAsync(Shard("shard-0"));

        AssertInternal(result, MigrationErrorCodes.MigrationFailed);
    }

    [Fact]
    public async Task ApplyHistoricalMigrationsAsync_ProviderFailure_IsInternal()
    {
        var store = new EfCoreMigrationHistoryStore<ShardTestDbContext>(InMemoryFactory());

        var result = await store.ApplyHistoricalMigrationsAsync("shard-0", [Script()]);

        AssertInternal(result, MigrationErrorCodes.MigrationFailed);
    }

    [Fact]
    public async Task CompareAsync_FactoryThrows_IsInternal()
    {
        var introspector = new EfCoreSchemaIntrospector<ShardTestDbContext>(ThrowingFactory());

        var result = await introspector.CompareAsync(Shard("shard-0"), Shard("shard-1"), includeColumnDiffs: true);

        AssertInternal(result, MigrationErrorCodes.SchemaComparisonFailed);
    }

    [Fact]
    public async Task CompareAsync_NonRelationalModel_IsInternal()
    {
        // The in-memory provider has no relational model: reading the table name throws.
        var introspector = new EfCoreSchemaIntrospector<ShardTestDbContext>(InMemoryFactory());

        var result = await introspector.CompareAsync(Shard("shard-0"), Shard("shard-1"), includeColumnDiffs: true);

        AssertInternal(result, MigrationErrorCodes.SchemaComparisonFailed);
    }

    [Fact]
    public async Task CompareAsync_RelationalModel_ReturnsDiff()
    {
        var factory = Substitute.For<IShardedDbContextFactory<ShardTestDbContext>>();
        factory.CreateContextForShard(Arg.Any<string>())
            .Returns(_ => (Either<EncinaError, ShardTestDbContext>)new ShardTestDbContext(
                new DbContextOptionsBuilder<ShardTestDbContext>().UseSqlite("DataSource=:memory:").Options));
        var introspector = new EfCoreSchemaIntrospector<ShardTestDbContext>(factory);

        var result = await introspector.CompareAsync(Shard("shard-0"), Shard("shard-1"), includeColumnDiffs: true);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region ReferenceTableStoreEF

    [Fact]
    public async Task UpsertAsync_DisposedContext_IsInternal()
    {
        var context = NewContext();
        var store = new ReferenceTableStoreEF(context);
        await context.DisposeAsync();

        var result = await store.UpsertAsync([new ShardRef { Id = 1, Name = "a" }]);

        AssertInternal(result, ReferenceTableErrorCodes.UpsertFailed);
    }

    [Fact]
    public async Task GetAllAsync_DisposedContext_IsInternal()
    {
        var context = NewContext();
        var store = new ReferenceTableStoreEF(context);
        await context.DisposeAsync();

        var result = await store.GetAllAsync<ShardRef>();

        AssertInternal(result, ReferenceTableErrorCodes.GetAllFailed);
    }

    #endregion

    #region Test types

    public sealed class ShardRef
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class ShardTestDbContext(DbContextOptions<ShardTestDbContext> options) : DbContext(options)
    {
        public DbSet<ShardRef> Refs => Set<ShardRef>();
    }

    #endregion
}
