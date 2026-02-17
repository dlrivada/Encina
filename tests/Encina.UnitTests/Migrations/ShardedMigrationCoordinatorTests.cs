using Encina.Sharding;
using Encina.Sharding.Migrations;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Migrations;

/// <summary>
/// Unit tests for <see cref="ShardedMigrationCoordinator"/>.
/// Verifies migration coordination across shards, rollback behavior,
/// drift detection, progress tracking, and history delegation.
/// </summary>
public sealed class ShardedMigrationCoordinatorTests
{
    #region Test Helpers

    private static ShardInfo CreateShard(string shardId, bool isActive = true)
        => new(shardId, $"Server=test;Database={shardId}", IsActive: isActive);

    private static ShardTopology CreateTopology(params ShardInfo[] shards)
        => new(shards);

    private static ShardTopology CreateTopology(int activeShardCount)
    {
        var shards = Enumerable.Range(0, activeShardCount)
            .Select(i => CreateShard($"shard-{i}"))
            .ToArray();
        return new ShardTopology(shards);
    }

    private static MigrationScript CreateTestScript(
        string id = "20260216_test_migration",
        string upSql = "CREATE TABLE test (id INT);",
        string downSql = "DROP TABLE test;",
        string description = "Test migration",
        string checksum = "sha256:abc123")
        => new(id, upSql, downSql, description, checksum);

    private static MigrationOptions CreateOptions(
        MigrationStrategy strategy = MigrationStrategy.Sequential,
        bool stopOnFirstFailure = true)
        => new()
        {
            Strategy = strategy,
            StopOnFirstFailure = stopOnFirstFailure,
            MaxParallelism = 4,
            PerShardTimeout = TimeSpan.FromMinutes(5)
        };

    private static ShardedMigrationCoordinator CreateCoordinator(
        ShardTopology topology,
        IMigrationExecutor? executor = null,
        ISchemaIntrospector? introspector = null,
        IMigrationHistoryStore? historyStore = null,
        ILogger<ShardedMigrationCoordinator>? logger = null)
    {
        return new ShardedMigrationCoordinator(
            topology,
            executor ?? CreateSucceedingExecutor(),
            introspector ?? Substitute.For<ISchemaIntrospector>(),
            historyStore ?? CreateSucceedingHistoryStore(),
            logger ?? NullLogger<ShardedMigrationCoordinator>.Instance);
    }

    private static IMigrationExecutor CreateSucceedingExecutor()
    {
        var executor = Substitute.For<IMigrationExecutor>();
        executor.ExecuteSqlAsync(Arg.Any<ShardInfo>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));
        return executor;
    }

    private static IMigrationExecutor CreateFailingExecutor()
    {
        var executor = Substitute.For<IMigrationExecutor>();
        executor.ExecuteSqlAsync(Arg.Any<ShardInfo>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(
                EncinaErrors.Create(MigrationErrorCodes.MigrationFailed, "Simulated failure")));
        return executor;
    }

    private static IMigrationHistoryStore CreateSucceedingHistoryStore()
    {
        var store = Substitute.For<IMigrationHistoryStore>();
        store.EnsureHistoryTableExistsAsync(Arg.Any<ShardInfo>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));
        store.RecordAppliedAsync(Arg.Any<string>(), Arg.Any<MigrationScript>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));
        store.RecordRolledBackAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));
        store.GetAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<string>>.Right(
                (IReadOnlyList<string>)new List<string> { "migration-1", "migration-2" }));
        return store;
    }

    private static T ExtractRight<T>(Either<EncinaError, T> result)
    {
        result.IsRight.ShouldBeTrue("Expected Right but got Left: " +
            result.Match(Right: _ => "", Left: e => e.Message));
        return result.Match(Right: r => r, Left: _ => default!);
    }

    private static EncinaError ExtractLeft<T>(Either<EncinaError, T> result)
    {
        result.IsLeft.ShouldBeTrue("Expected Left but got Right");
        return result.Match(Right: _ => default!, Left: e => e);
    }

    #endregion

    #region ApplyToAllShardsAsync

    [Fact]
    public async Task ApplyToAllShardsAsync_WithNoActiveShards_ReturnsLeftError()
    {
        // Arrange
        var inactiveShard = CreateShard("shard-0", isActive: false);
        var topology = CreateTopology(inactiveShard);
        var coordinator = CreateCoordinator(topology);
        var script = CreateTestScript();
        var options = CreateOptions();

        // Act
        var result = await coordinator.ApplyToAllShardsAsync(script, options);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.Message.ShouldContain("No active shards");
    }

    [Fact]
    public async Task ApplyToAllShardsAsync_WithAllShardsSucceeding_ReturnsRightWithAllSucceeded()
    {
        // Arrange
        var topology = CreateTopology(3);
        var coordinator = CreateCoordinator(topology);
        var script = CreateTestScript();
        var options = CreateOptions();

        // Act
        var result = await coordinator.ApplyToAllShardsAsync(script, options);

        // Assert
        var migrationResult = ExtractRight(result);
        migrationResult.AllSucceeded.ShouldBeTrue();
        migrationResult.SucceededCount.ShouldBe(3);
        migrationResult.FailedCount.ShouldBe(0);
        migrationResult.PerShardStatus.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ApplyToAllShardsAsync_WithFirstShardFailing_StopOnFirstFailure_MarksPendingShards()
    {
        // Arrange
        var shards = new[]
        {
            CreateShard("shard-0"),
            CreateShard("shard-1"),
            CreateShard("shard-2")
        };
        var topology = CreateTopology(shards);

        var executor = Substitute.For<IMigrationExecutor>();
        // First shard fails
        executor.ExecuteSqlAsync(
                Arg.Is<ShardInfo>(s => s.ShardId == "shard-0"),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(
                EncinaErrors.Create(MigrationErrorCodes.MigrationFailed, "Shard-0 failed")));
        // Other shards would succeed (but shouldn't be called)
        executor.ExecuteSqlAsync(
                Arg.Is<ShardInfo>(s => s.ShardId != "shard-0"),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        var coordinator = CreateCoordinator(topology, executor: executor);
        var script = CreateTestScript();
        var options = CreateOptions(stopOnFirstFailure: true);

        // Act
        var result = await coordinator.ApplyToAllShardsAsync(script, options);

        // Assert
        var migrationResult = ExtractRight(result);
        migrationResult.AllSucceeded.ShouldBeFalse();
        migrationResult.PerShardStatus["shard-0"].Outcome.ShouldBe(MigrationOutcome.Failed);

        // Remaining shards should be Pending when StopOnFirstFailure is true
        var pendingShards = migrationResult.PerShardStatus
            .Where(kvp => kvp.Value.Outcome == MigrationOutcome.Pending)
            .ToList();
        pendingShards.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ApplyToAllShardsAsync_CallsEnsureHistoryTableForAllShards()
    {
        // Arrange
        var topology = CreateTopology(3);
        var historyStore = CreateSucceedingHistoryStore();
        var coordinator = CreateCoordinator(topology, historyStore: historyStore);
        var script = CreateTestScript();
        var options = CreateOptions();

        // Act
        await coordinator.ApplyToAllShardsAsync(script, options);

        // Assert
        await historyStore.Received(3).EnsureHistoryTableExistsAsync(
            Arg.Any<ShardInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyToAllShardsAsync_RecordsAppliedMigrationOnSuccess()
    {
        // Arrange
        var topology = CreateTopology(2);
        var historyStore = CreateSucceedingHistoryStore();
        var coordinator = CreateCoordinator(topology, historyStore: historyStore);
        var script = CreateTestScript();
        var options = CreateOptions();

        // Act
        await coordinator.ApplyToAllShardsAsync(script, options);

        // Assert
        await historyStore.Received(2).RecordAppliedAsync(
            Arg.Any<string>(),
            Arg.Is<MigrationScript>(s => s.Id == script.Id),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyToAllShardsAsync_DoesNotRecordAppliedOnFailure()
    {
        // Arrange
        var topology = CreateTopology(1);
        var executor = CreateFailingExecutor();
        var historyStore = CreateSucceedingHistoryStore();
        var coordinator = CreateCoordinator(topology, executor: executor, historyStore: historyStore);
        var script = CreateTestScript();
        var options = CreateOptions();

        // Act
        await coordinator.ApplyToAllShardsAsync(script, options);

        // Assert
        await historyStore.DidNotReceive().RecordAppliedAsync(
            Arg.Any<string>(),
            Arg.Any<MigrationScript>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region RollbackAsync

    [Fact]
    public async Task RollbackAsync_WithNoSucceededShards_ReturnsRightUnit()
    {
        // Arrange
        var topology = CreateTopology(2);
        var executor = CreateFailingExecutor();
        var coordinator = CreateCoordinator(topology, executor: executor);
        var script = CreateTestScript();
        var options = CreateOptions();

        // Apply migration (all fail)
        var applyResult = await coordinator.ApplyToAllShardsAsync(script, options);
        var migrationResult = ExtractRight(applyResult);
        migrationResult.SucceededCount.ShouldBe(0);

        // Act
        var rollbackResult = await coordinator.RollbackAsync(migrationResult);

        // Assert
        rollbackResult.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RollbackAsync_WithSucceededShards_ExecutesDownSql()
    {
        // Arrange
        var topology = CreateTopology(2);
        var executor = CreateSucceedingExecutor();
        var historyStore = CreateSucceedingHistoryStore();
        var coordinator = CreateCoordinator(topology, executor: executor, historyStore: historyStore);
        var script = CreateTestScript(downSql: "DROP TABLE test;");
        var options = CreateOptions();

        // Apply migration first
        var applyResult = await coordinator.ApplyToAllShardsAsync(script, options);
        var migrationResult = ExtractRight(applyResult);
        migrationResult.AllSucceeded.ShouldBeTrue();

        // Act
        var rollbackResult = await coordinator.RollbackAsync(migrationResult);

        // Assert
        rollbackResult.IsRight.ShouldBeTrue();
        await executor.Received().ExecuteSqlAsync(
            Arg.Any<ShardInfo>(),
            Arg.Is("DROP TABLE test;"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackAsync_WithUnknownMigrationId_ReturnsLeftError()
    {
        // Arrange
        var topology = CreateTopology(1);
        var coordinator = CreateCoordinator(topology);

        // Create a MigrationResult with an ID that was never applied through this coordinator
        var unknownResult = new MigrationResult(
            Guid.NewGuid(),
            new Dictionary<string, ShardMigrationStatus>
            {
                ["shard-0"] = new("shard-0", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(1))
            },
            TimeSpan.FromSeconds(1),
            DateTimeOffset.UtcNow);

        // Act
        var rollbackResult = await coordinator.RollbackAsync(unknownResult);

        // Assert
        rollbackResult.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(rollbackResult);
        error.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task RollbackAsync_WithPartialFailure_ReturnsLeftError()
    {
        // Arrange
        var shards = new[]
        {
            CreateShard("shard-0"),
            CreateShard("shard-1")
        };
        var topology = CreateTopology(shards);

        // Executor succeeds on apply but fails on rollback for shard-1
        var executor = Substitute.For<IMigrationExecutor>();
        executor.ExecuteSqlAsync(Arg.Any<ShardInfo>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        var historyStore = CreateSucceedingHistoryStore();
        var coordinator = CreateCoordinator(topology, executor: executor, historyStore: historyStore);
        var script = CreateTestScript();
        var options = CreateOptions();

        // Apply migration (all succeed)
        var applyResult = await coordinator.ApplyToAllShardsAsync(script, options);
        var migrationResult = ExtractRight(applyResult);
        migrationResult.AllSucceeded.ShouldBeTrue();

        // Now configure executor to fail on shard-1 for the DownSql
        executor.ExecuteSqlAsync(
                Arg.Is<ShardInfo>(s => s.ShardId == "shard-1"),
                Arg.Is(script.DownSql),
                Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(
                EncinaErrors.Create(MigrationErrorCodes.RollbackFailed, "Rollback failed on shard-1")));
        // shard-0 rollback succeeds
        executor.ExecuteSqlAsync(
                Arg.Is<ShardInfo>(s => s.ShardId == "shard-0"),
                Arg.Is(script.DownSql),
                Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        // Act
        var rollbackResult = await coordinator.RollbackAsync(migrationResult);

        // Assert
        rollbackResult.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(rollbackResult);
        error.Message.ShouldContain("shard-1");
    }

    #endregion

    #region DetectDriftAsync

    [Fact]
    public async Task DetectDriftAsync_WithSingleShard_ReturnsNoDrift()
    {
        // Arrange
        var topology = CreateTopology(1);
        var coordinator = CreateCoordinator(topology);

        // Act
        var result = await coordinator.DetectDriftAsync();

        // Assert
        var report = ExtractRight(result);
        report.HasDrift.ShouldBeFalse();
        report.Diffs.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DetectDriftAsync_WithNoDifferences_ReturnsNoDrift()
    {
        // Arrange
        var topology = CreateTopology(3);
        var introspector = Substitute.For<ISchemaIntrospector>();
        introspector.CompareAsync(
                Arg.Any<ShardInfo>(),
                Arg.Any<ShardInfo>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var shard = callInfo.ArgAt<ShardInfo>(0);
                var baseline = callInfo.ArgAt<ShardInfo>(1);
                return Either<EncinaError, ShardSchemaDiff>.Right(
                    new ShardSchemaDiff(shard.ShardId, baseline.ShardId, []));
            });

        var coordinator = CreateCoordinator(topology, introspector: introspector);

        // Act
        var result = await coordinator.DetectDriftAsync();

        // Assert
        var report = ExtractRight(result);
        report.HasDrift.ShouldBeFalse();
    }

    [Fact]
    public async Task DetectDriftAsync_WithDifferences_ReturnsDriftReport()
    {
        // Arrange
        var topology = CreateTopology(3);
        var introspector = Substitute.For<ISchemaIntrospector>();
        introspector.CompareAsync(
                Arg.Any<ShardInfo>(),
                Arg.Any<ShardInfo>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var shard = callInfo.ArgAt<ShardInfo>(0);
                var baseline = callInfo.ArgAt<ShardInfo>(1);
                // Only shard-1 has a diff (missing table)
                var diffs = shard.ShardId == "shard-1"
                    ? new List<TableDiff> { new("orders", TableDiffType.Missing) }
                    : new List<TableDiff>();
                return Either<EncinaError, ShardSchemaDiff>.Right(
                    new ShardSchemaDiff(shard.ShardId, baseline.ShardId, diffs));
            });

        var coordinator = CreateCoordinator(topology, introspector: introspector);

        // Act
        var result = await coordinator.DetectDriftAsync();

        // Assert
        var report = ExtractRight(result);
        report.HasDrift.ShouldBeTrue();
        report.Diffs.Count.ShouldBe(1);
        report.Diffs[0].ShardId.ShouldBe("shard-1");
        report.Diffs[0].TableDiffs[0].TableName.ShouldBe("orders");
        report.Diffs[0].TableDiffs[0].DiffType.ShouldBe(TableDiffType.Missing);
    }

    #endregion

    #region GetProgressAsync

    [Fact]
    public async Task GetProgressAsync_WithActiveMigration_ReturnsProgress()
    {
        // Arrange
        var topology = CreateTopology(2);
        var coordinator = CreateCoordinator(topology);
        var script = CreateTestScript();
        var options = CreateOptions();

        // Apply migration to register a tracker
        var applyResult = await coordinator.ApplyToAllShardsAsync(script, options);
        var migrationResult = ExtractRight(applyResult);

        // Act
        var progressResult = await coordinator.GetProgressAsync(migrationResult.Id);

        // Assert
        var progress = ExtractRight(progressResult);
        progress.MigrationId.ShouldBe(migrationResult.Id);
        progress.TotalShards.ShouldBe(2);
        progress.CurrentPhase.ShouldBe("Completed");
    }

    [Fact]
    public async Task GetProgressAsync_WithUnknownMigrationId_ReturnsLeftError()
    {
        // Arrange
        var topology = CreateTopology(1);
        var coordinator = CreateCoordinator(topology);
        var unknownId = Guid.NewGuid();

        // Act
        var result = await coordinator.GetProgressAsync(unknownId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.Message.ShouldContain("not found");
    }

    #endregion

    #region GetAppliedMigrationsAsync

    [Fact]
    public async Task GetAppliedMigrationsAsync_WithValidShard_DelegatesToHistoryStore()
    {
        // Arrange
        var topology = CreateTopology(1);
        var historyStore = CreateSucceedingHistoryStore();
        var coordinator = CreateCoordinator(topology, historyStore: historyStore);

        // Act
        var result = await coordinator.GetAppliedMigrationsAsync("shard-0");

        // Assert
        var migrations = ExtractRight(result);
        migrations.Count.ShouldBe(2);
        migrations[0].ShouldBe("migration-1");
        migrations[1].ShouldBe("migration-2");

        await historyStore.Received(1).GetAppliedAsync("shard-0", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAppliedMigrationsAsync_WithUnknownShard_ReturnsLeftError()
    {
        // Arrange
        var topology = CreateTopology(1);
        var coordinator = CreateCoordinator(topology);

        // Act
        var result = await coordinator.GetAppliedMigrationsAsync("nonexistent-shard");

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.Message.ShouldContain("not found");
    }

    #endregion
}
