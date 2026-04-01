using Encina.Sharding;
using Encina.Sharding.Migrations;

namespace Encina.GuardTests.Core.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardedMigrationCoordinator"/>.
/// Verifies null parameter handling for the constructor and public methods.
/// </summary>
public sealed class ShardedMigrationCoordinatorGuardTests
{
    private readonly ShardTopology _topology = CreateTestTopology();
    private readonly IMigrationExecutor _executor = Substitute.For<IMigrationExecutor>();
    private readonly ISchemaIntrospector _introspector = Substitute.For<ISchemaIntrospector>();
    private readonly IMigrationHistoryStore _historyStore = Substitute.For<IMigrationHistoryStore>();
    private readonly ILogger<ShardedMigrationCoordinator> _logger = NullLogger<ShardedMigrationCoordinator>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws when topology is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedMigrationCoordinator(null!, _executor, _introspector, _historyStore, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when executor is null.
    /// </summary>
    [Fact]
    public void Constructor_NullExecutor_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedMigrationCoordinator(_topology, null!, _introspector, _historyStore, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when introspector is null.
    /// </summary>
    [Fact]
    public void Constructor_NullIntrospector_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedMigrationCoordinator(_topology, _executor, null!, _historyStore, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when history store is null.
    /// </summary>
    [Fact]
    public void Constructor_NullHistoryStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedMigrationCoordinator(_topology, _executor, _introspector, null!, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedMigrationCoordinator(_topology, _executor, _introspector, _historyStore, null!));
    }

    /// <summary>
    /// Verifies that the constructor succeeds with all valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        var coordinator = new ShardedMigrationCoordinator(
            _topology, _executor, _introspector, _historyStore, _logger);

        coordinator.ShouldNotBeNull();
    }

    #endregion

    #region ApplyToAllShardsAsync Guards

    /// <summary>
    /// Verifies that ApplyToAllShardsAsync throws when script is null.
    /// </summary>
    [Fact]
    public async Task ApplyToAllShardsAsync_NullScript_ThrowsArgumentNullException()
    {
        var coordinator = CreateCoordinator();
        var options = new MigrationOptions();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            coordinator.ApplyToAllShardsAsync(null!, options));
    }

    /// <summary>
    /// Verifies that ApplyToAllShardsAsync throws when options is null.
    /// </summary>
    [Fact]
    public async Task ApplyToAllShardsAsync_NullOptions_ThrowsArgumentNullException()
    {
        var coordinator = CreateCoordinator();
        var script = CreateTestScript();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            coordinator.ApplyToAllShardsAsync(script, null!));
    }

    /// <summary>
    /// Verifies that ApplyToAllShardsAsync returns error when topology has no active shards.
    /// </summary>
    [Fact]
    public async Task ApplyToAllShardsAsync_NoActiveShards_ReturnsLeftError()
    {
        var emptyTopology = new ShardTopology(new[]
        {
            new ShardInfo("inactive-1", "Server=s1;Database=db;", IsActive: false),
        });
        var coordinator = new ShardedMigrationCoordinator(
            emptyTopology, _executor, _introspector, _historyStore, _logger);
        var script = CreateTestScript();
        var options = new MigrationOptions();

        var result = await coordinator.ApplyToAllShardsAsync(script, options);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region RollbackAsync Guards

    /// <summary>
    /// Verifies that RollbackAsync throws when result is null.
    /// </summary>
    [Fact]
    public async Task RollbackAsync_NullResult_ThrowsArgumentNullException()
    {
        var coordinator = CreateCoordinator();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            coordinator.RollbackAsync(null!));
    }

    /// <summary>
    /// Verifies that RollbackAsync returns error for unknown migration ID.
    /// </summary>
    [Fact]
    public async Task RollbackAsync_UnknownMigrationId_ReturnsLeftError()
    {
        var coordinator = CreateCoordinator();
        var result = new MigrationResult(
            Guid.NewGuid(),
            new Dictionary<string, ShardMigrationStatus>(),
            TimeSpan.Zero,
            DateTimeOffset.UtcNow);

        var rollbackResult = await coordinator.RollbackAsync(result);

        rollbackResult.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetAppliedMigrationsAsync Guards

    /// <summary>
    /// Verifies that GetAppliedMigrationsAsync throws when shard ID is null.
    /// </summary>
    [Fact]
    public async Task GetAppliedMigrationsAsync_NullShardId_ThrowsArgumentException()
    {
        var coordinator = CreateCoordinator();

        await Should.ThrowAsync<ArgumentException>(() =>
            coordinator.GetAppliedMigrationsAsync(null!));
    }

    /// <summary>
    /// Verifies that GetAppliedMigrationsAsync throws when shard ID is whitespace.
    /// </summary>
    [Fact]
    public async Task GetAppliedMigrationsAsync_WhitespaceShardId_ThrowsArgumentException()
    {
        var coordinator = CreateCoordinator();

        await Should.ThrowAsync<ArgumentException>(() =>
            coordinator.GetAppliedMigrationsAsync("   "));
    }

    /// <summary>
    /// Verifies that GetAppliedMigrationsAsync returns error for unknown shard.
    /// </summary>
    [Fact]
    public async Task GetAppliedMigrationsAsync_UnknownShardId_ReturnsLeftError()
    {
        var coordinator = CreateCoordinator();

        var result = await coordinator.GetAppliedMigrationsAsync("non-existent-shard");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetProgressAsync Guards

    /// <summary>
    /// Verifies that GetProgressAsync returns error for unknown migration ID.
    /// </summary>
    [Fact]
    public async Task GetProgressAsync_UnknownMigrationId_ReturnsLeftError()
    {
        var coordinator = CreateCoordinator();

        var result = await coordinator.GetProgressAsync(Guid.NewGuid());

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Test Helpers

    private ShardedMigrationCoordinator CreateCoordinator() =>
        new(_topology, _executor, _introspector, _historyStore, _logger);

    private static ShardTopology CreateTestTopology() =>
        new(new[]
        {
            new ShardInfo("shard-0", "Server=s0;Database=db;"),
            new ShardInfo("shard-1", "Server=s1;Database=db;"),
        });

    private static MigrationScript CreateTestScript() =>
        new("mig-001", "CREATE TABLE Test (Id INT);", "DROP TABLE Test;", "Test migration", "abc123");

    #endregion
}
