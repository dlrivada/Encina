using Encina.Sharding;
using Encina.Sharding.Resharding;
using Encina.Sharding.Resharding.Phases;
using LanguageExt;

namespace Encina.UnitTests.Sharding.Resharding;

/// <summary>
/// Shared test builders and helpers for all resharding unit tests.
/// Provides factory methods for creating test instances of resharding record types
/// and helper methods for extracting values from <see cref="Either{L,R}"/> results.
/// </summary>
internal static class ReshardingTestBuilders
{
    #region Record Builders

    /// <summary>
    /// Creates a <see cref="KeyRange"/> with configurable start and end positions.
    /// </summary>
    public static KeyRange CreateKeyRange(ulong start = 0, ulong end = 1000)
    {
        return new KeyRange(start, end);
    }

    /// <summary>
    /// Creates a <see cref="ShardMigrationStep"/> with sensible defaults.
    /// </summary>
    public static ShardMigrationStep CreateMigrationStep(
        string source = "shard-0",
        string target = "shard-1",
        long estimatedRows = 1000)
    {
        return new ShardMigrationStep(source, target, CreateKeyRange(), estimatedRows);
    }

    /// <summary>
    /// Creates an <see cref="EstimatedResources"/> with configurable rows and bytes.
    /// Duration is derived from the row count at an assumed rate.
    /// </summary>
    public static EstimatedResources CreateEstimate(long rows = 5000, long bytes = 1_280_000)
    {
        return new EstimatedResources(rows, bytes, TimeSpan.FromMinutes(rows / 1000.0));
    }

    /// <summary>
    /// Creates a <see cref="ReshardingPlan"/> with the specified number of migration steps.
    /// </summary>
    public static ReshardingPlan CreatePlan(int stepCount = 2, Guid? id = null)
    {
        var steps = Enumerable.Range(0, stepCount)
            .Select(i => CreateMigrationStep(
                source: $"shard-{i}",
                target: $"shard-{i + stepCount}",
                estimatedRows: 1000 * (i + 1)))
            .ToList();

        var totalRows = steps.Sum(s => s.EstimatedRows);
        var estimate = CreateEstimate(totalRows, totalRows * 256);

        return new ReshardingPlan(id ?? Guid.NewGuid(), steps, estimate);
    }

    /// <summary>
    /// Creates a <see cref="ReshardingOptions"/> with default settings.
    /// </summary>
    public static ReshardingOptions CreateOptions()
    {
        return new ReshardingOptions();
    }

    /// <summary>
    /// Creates a <see cref="ReshardingProgress"/> with configurable phase and percentage.
    /// </summary>
    public static ReshardingProgress CreateProgress(
        Guid? id = null,
        ReshardingPhase phase = ReshardingPhase.Copying,
        double percent = 0.0)
    {
        return new ReshardingProgress(
            id ?? Guid.NewGuid(),
            phase,
            percent,
            new Dictionary<string, ShardMigrationProgress>());
    }

    /// <summary>
    /// Creates a <see cref="ReshardingState"/> with configurable id and phase.
    /// </summary>
    public static ReshardingState CreateState(
        Guid? id = null,
        ReshardingPhase phase = ReshardingPhase.Copying)
    {
        var resolvedId = id ?? Guid.NewGuid();

        return new ReshardingState(
            resolvedId,
            phase,
            CreatePlan(id: resolvedId),
            CreateProgress(id: resolvedId, phase: phase),
            LastCompletedPhase: null,
            StartedAtUtc: DateTime.UtcNow,
            Checkpoint: null);
    }

    /// <summary>
    /// Creates a <see cref="PhaseContext"/> with optional services mock.
    /// Uses <see cref="NSubstitute"/> to create a mock <see cref="IReshardingServices"/> when null.
    /// </summary>
    public static PhaseContext CreatePhaseContext(
        Guid? id = null,
        IReshardingServices? services = null)
    {
        var resolvedId = id ?? Guid.NewGuid();

        return new PhaseContext(
            resolvedId,
            CreatePlan(id: resolvedId),
            CreateOptions(),
            CreateProgress(id: resolvedId),
            Checkpoint: null,
            services ?? Substitute.For<IReshardingServices>());
    }

    /// <summary>
    /// Creates a <see cref="ShardTopology"/> with the specified number of shards.
    /// </summary>
    public static ShardTopology CreateTopology(int shardCount = 3)
    {
        var shards = Enumerable.Range(0, shardCount)
            .Select(i => new ShardInfo($"shard-{i}", $"Server=localhost;Database=shard_{i}"))
            .ToList();

        return new ShardTopology(shards);
    }

    /// <summary>
    /// Creates a <see cref="ReshardingResult"/> with configurable final phase and rollback metadata.
    /// </summary>
    public static ReshardingResult CreateResult(
        ReshardingPhase finalPhase = ReshardingPhase.Completed,
        RollbackMetadata? rollback = null)
    {
        var history = new List<PhaseHistoryEntry>
        {
            new(
                ReshardingPhase.Planning,
                DateTime.UtcNow.AddMinutes(-10),
                DateTime.UtcNow.AddMinutes(-9)),
            new(
                ReshardingPhase.Copying,
                DateTime.UtcNow.AddMinutes(-9),
                DateTime.UtcNow.AddMinutes(-5)),
        };

        return new ReshardingResult(Guid.NewGuid(), finalPhase, history, rollback);
    }

    #endregion

    #region Either Extraction Helpers

    /// <summary>
    /// Extracts the Right value from an <see cref="Either{EncinaError, T}"/> result.
    /// Throws if the result is Left.
    /// </summary>
    public static T ExtractRight<T>(Either<EncinaError, T> result)
    {
        T value = default!;
        _ = result.IfRight(v => value = v);
        return value;
    }

    /// <summary>
    /// Extracts the Left (error) value from an <see cref="Either{EncinaError, T}"/> result.
    /// Returns default if the result is Right.
    /// </summary>
    public static EncinaError ExtractLeft<T>(Either<EncinaError, T> result)
    {
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        return error;
    }

    #endregion
}
