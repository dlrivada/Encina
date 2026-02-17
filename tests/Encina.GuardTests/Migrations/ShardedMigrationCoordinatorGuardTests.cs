using Encina.Sharding;
using Encina.Sharding.Migrations;

namespace Encina.GuardTests.Migrations;

/// <summary>
/// Guard clause tests for <see cref="ShardedMigrationCoordinator"/> (internal).
/// Constructor parameters are validated with <see cref="ArgumentNullException.ThrowIfNull"/>.
/// Method parameters are validated with <see cref="ArgumentNullException.ThrowIfNull"/> or
/// <see cref="ArgumentException.ThrowIfNullOrWhiteSpace"/>.
/// </summary>
public sealed class ShardedMigrationCoordinatorGuardTests
{
    private readonly ShardTopology _topology = new([new ShardInfo("shard-0", "Server=test;Database=db0")]);
    private readonly IMigrationExecutor _executor = Substitute.For<IMigrationExecutor>();
    private readonly ISchemaIntrospector _introspector = Substitute.For<ISchemaIntrospector>();
    private readonly IMigrationHistoryStore _historyStore = Substitute.For<IMigrationHistoryStore>();
    private readonly ILogger<ShardedMigrationCoordinator> _logger = NullLogger<ShardedMigrationCoordinator>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        var act = () => new ShardedMigrationCoordinator(null!, _executor, _introspector, _historyStore, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("topology");
    }

    [Fact]
    public void Constructor_NullExecutor_ThrowsArgumentNullException()
    {
        var act = () => new ShardedMigrationCoordinator(_topology, null!, _introspector, _historyStore, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("executor");
    }

    [Fact]
    public void Constructor_NullIntrospector_ThrowsArgumentNullException()
    {
        var act = () => new ShardedMigrationCoordinator(_topology, _executor, null!, _historyStore, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("introspector");
    }

    [Fact]
    public void Constructor_NullHistoryStore_ThrowsArgumentNullException()
    {
        var act = () => new ShardedMigrationCoordinator(_topology, _executor, _introspector, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("historyStore");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ShardedMigrationCoordinator(_topology, _executor, _introspector, _historyStore, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region ApplyToAllShardsAsync Guards

    [Fact]
    public async Task ApplyToAllShardsAsync_NullScript_ThrowsArgumentNullException()
    {
        var coordinator = CreateCoordinator();
        var options = new MigrationOptions();

        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            coordinator.ApplyToAllShardsAsync(null!, options));
        ex.ParamName.ShouldBe("script");
    }

    [Fact]
    public async Task ApplyToAllShardsAsync_NullOptions_ThrowsArgumentNullException()
    {
        var coordinator = CreateCoordinator();
        var script = CreateValidScript();

        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            coordinator.ApplyToAllShardsAsync(script, null!));
        ex.ParamName.ShouldBe("options");
    }

    #endregion

    #region RollbackAsync Guards

    [Fact]
    public async Task RollbackAsync_NullResult_ThrowsArgumentNullException()
    {
        var coordinator = CreateCoordinator();

        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            coordinator.RollbackAsync(null!));
        ex.ParamName.ShouldBe("result");
    }

    #endregion

    #region GetAppliedMigrationsAsync Guards

    [Fact]
    public async Task GetAppliedMigrationsAsync_NullShardId_ThrowsArgumentException()
    {
        var coordinator = CreateCoordinator();

        await Should.ThrowAsync<ArgumentException>(() =>
            coordinator.GetAppliedMigrationsAsync(null!));
    }

    [Fact]
    public async Task GetAppliedMigrationsAsync_EmptyShardId_ThrowsArgumentException()
    {
        var coordinator = CreateCoordinator();

        await Should.ThrowAsync<ArgumentException>(() =>
            coordinator.GetAppliedMigrationsAsync(string.Empty));
    }

    #endregion

    #region Helpers

    private ShardedMigrationCoordinator CreateCoordinator() =>
        new(_topology, _executor, _introspector, _historyStore, _logger);

    private static MigrationScript CreateValidScript() =>
        new("20260216_test", "CREATE TABLE t (id INT);", "DROP TABLE t;", "Test migration", "sha256:abc123");

    #endregion
}
