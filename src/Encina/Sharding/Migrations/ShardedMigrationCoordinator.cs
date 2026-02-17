using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Sharding.Migrations.Strategies;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Sharding.Migrations;

/// <summary>
/// Default implementation of <see cref="IShardedMigrationCoordinator"/> that coordinates
/// schema migrations across all shards in a topology using configurable strategies.
/// </summary>
/// <remarks>
/// <para>
/// The coordinator selects an execution strategy based on <see cref="MigrationOptions.Strategy"/>
/// and delegates per-shard DDL execution to <see cref="IMigrationExecutor"/>. Migration history
/// is tracked via <see cref="IMigrationHistoryStore"/> and schema drift detection is delegated
/// to <see cref="ISchemaIntrospector"/>.
/// </para>
/// <para>
/// In-flight migration progress is tracked in memory using a <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// and can be queried via <see cref="GetProgressAsync"/>.
/// </para>
/// </remarks>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
internal sealed class ShardedMigrationCoordinator : IShardedMigrationCoordinator
{
    private readonly ShardTopology _topology;
    private readonly IMigrationExecutor _executor;
    private readonly ISchemaIntrospector _introspector;
    private readonly IMigrationHistoryStore _historyStore;
    private readonly ILogger<ShardedMigrationCoordinator> _logger;

    // In-flight migration tracking
    private readonly ConcurrentDictionary<Guid, MigrationProgressTracker> _activeTrackers = new();
    private readonly ConcurrentDictionary<Guid, MigrationScript> _migrationScripts = new();

    // Strategy instances (stateless, reusable)
    private static readonly SequentialMigrationStrategy SequentialStrategy = new();
    private static readonly ParallelMigrationStrategy ParallelStrategy = new();
    private static readonly RollingUpdateStrategy RollingStrategy = new();
    private static readonly CanaryFirstStrategy CanaryStrategy = new();

    /// <summary>
    /// Initializes a new instance of <see cref="ShardedMigrationCoordinator"/>.
    /// </summary>
    /// <param name="topology">The shard topology for discovering active shards.</param>
    /// <param name="executor">The provider-specific DDL executor.</param>
    /// <param name="introspector">The provider-specific schema introspector for drift detection.</param>
    /// <param name="historyStore">The migration history store for tracking applied migrations.</param>
    /// <param name="logger">The logger instance for structured logging.</param>
    public ShardedMigrationCoordinator(
        ShardTopology topology,
        IMigrationExecutor executor,
        ISchemaIntrospector introspector,
        IMigrationHistoryStore historyStore,
        ILogger<ShardedMigrationCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(introspector);
        ArgumentNullException.ThrowIfNull(historyStore);
        ArgumentNullException.ThrowIfNull(logger);

        _topology = topology;
        _executor = executor;
        _introspector = introspector;
        _historyStore = historyStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, MigrationResult>> ApplyToAllShardsAsync(
        MigrationScript script,
        MigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(options);

        var activeShards = _topology.GetActiveShards();

        if (activeShards.Count == 0)
        {
            return Either<EncinaError, MigrationResult>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.NoActiveShards,
                    "No active shards found in the topology."));
        }

        var migrationId = Guid.NewGuid();
        var tracker = new MigrationProgressTracker(migrationId, activeShards.Count);
        _activeTrackers[migrationId] = tracker;
        _migrationScripts[migrationId] = script;

        _logger.LogInformation(
            "Migration coordination started. MigrationId={MigrationId}, Script={ScriptDescription}, Strategy={Strategy}, ShardCount={ShardCount}",
            migrationId, script.Description, options.Strategy, activeShards.Count);

        var overallStart = Stopwatch.GetTimestamp();
        var shardIndex = 0;

        try
        {
            // Ensure history table exists on all target shards
            foreach (var shard in activeShards)
            {
                var ensureResult = await _historyStore.EnsureHistoryTableExistsAsync(shard, cancellationToken);
                if (ensureResult.IsLeft)
                {
                    return Either<EncinaError, MigrationResult>.Left(
                        ensureResult.Match(Right: _ => default, Left: e => e));
                }
            }

            var strategy = SelectStrategy(options.Strategy);
            tracker.UpdatePhase(GetPhaseLabel(options.Strategy));

            var perShardStatus = await strategy.ExecuteAsync(
                activeShards,
                async (shard, ct) =>
                {
                    var position = Interlocked.Increment(ref shardIndex);

                    _logger.LogDebug(
                        "Shard migration started. MigrationId={MigrationId}, ShardId={ShardId}, Position={Position} of {Total}",
                        migrationId, shard.ShardId, position, activeShards.Count);

                    var shardStart = Stopwatch.GetTimestamp();
                    var result = await _executor.ExecuteSqlAsync(shard, script.UpSql, ct);
                    var shardDuration = Stopwatch.GetElapsedTime(shardStart);

                    // Record in history on success
                    if (result.IsRight)
                    {
                        await _historyStore.RecordAppliedAsync(
                            shard.ShardId, script, shardDuration, ct);

                        _logger.LogInformation(
                            "Shard migration completed. MigrationId={MigrationId}, ShardId={ShardId}, Duration={DurationMs:F1}ms, Outcome=Succeeded",
                            migrationId, shard.ShardId, shardDuration.TotalMilliseconds);
                    }
                    else
                    {
                        var error = result.Match(Right: _ => default!, Left: e => e);

                        _logger.LogError(
                            "Shard migration failed. MigrationId={MigrationId}, ShardId={ShardId}, ErrorMessage={ErrorMessage}, RollbackInitiated={RollbackInitiated}",
                            migrationId, shard.ShardId, error.Message, options.StopOnFirstFailure);
                    }

                    return result;
                },
                options,
                (shardId, status) => tracker.UpdateShard(shardId, status),
                cancellationToken);

            var totalDuration = Stopwatch.GetElapsedTime(overallStart);
            tracker.UpdatePhase("Completed");

            var migrationResult = new MigrationResult(
                migrationId, perShardStatus, totalDuration, DateTimeOffset.UtcNow);

            _logger.LogInformation(
                "Migration coordination completed. MigrationId={MigrationId}, TotalDuration={TotalDurationMs:F1}ms, Succeeded={SucceededCount}, Failed={FailedCount}",
                migrationId, totalDuration.TotalMilliseconds, migrationResult.SucceededCount, migrationResult.FailedCount);

            return Either<EncinaError, MigrationResult>.Right(migrationResult);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            tracker.UpdatePhase("Cancelled");
            _logger.LogWarning(
                "Migration coordination cancelled. MigrationId={MigrationId}",
                migrationId);
            throw;
        }
        finally
        {
            // Keep tracker available for progress queries after completion
            // (will be naturally evicted when the coordinator is disposed or reset)
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RollbackAsync(
        MigrationResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!_migrationScripts.TryGetValue(result.Id, out var script))
        {
            return Either<EncinaError, Unit>.Left(
                EncinaErrors.Create(
                    MigrationErrorCodes.MigrationNotFound,
                    $"Migration script for migration ID '{result.Id}' was not found. " +
                    "Rollback is only supported for migrations applied by this coordinator instance."));
        }

        var shardsToRollback = result.PerShardStatus
            .Where(kvp => kvp.Value.Outcome == MigrationOutcome.Succeeded)
            .Select(kvp => kvp.Key)
            .ToList();

        if (shardsToRollback.Count == 0)
        {
            return Either<EncinaError, Unit>.Right(unit);
        }

        _logger.LogWarning(
            "Migration rollback started. MigrationId={MigrationId}, ShardsToRollback={ShardsToRollback}",
            result.Id, shardsToRollback.Count);

        var rollbackStart = Stopwatch.GetTimestamp();
        var rollbackErrors = new List<(string ShardId, EncinaError Error)>();

        // Rollback in parallel — the migration was already verified as compatible
        var tasks = shardsToRollback.Select(async shardId =>
        {
            var shardResult = _topology.GetShard(shardId);

            return await shardResult.MatchAsync(
                RightAsync: async shard =>
                {
                    var execResult = await _executor.ExecuteSqlAsync(shard, script.DownSql, cancellationToken);

                    await execResult.MatchAsync(
                        RightAsync: async _ =>
                        {
                            await _historyStore.RecordRolledBackAsync(shardId, script.Id, cancellationToken);
                            return unit;
                        },
                        Left: error =>
                        {
                            lock (rollbackErrors)
                            {
                                rollbackErrors.Add((shardId, error));
                            }

                            return unit;
                        });

                    return unit;
                },
                Left: error =>
                {
                    lock (rollbackErrors)
                    {
                        rollbackErrors.Add((shardId, error));
                    }

                    return unit;
                });
        });

        await Task.WhenAll(tasks);

        var rollbackDuration = Stopwatch.GetElapsedTime(rollbackStart);

        if (rollbackErrors.Count > 0)
        {
            var failedShards = string.Join(", ", rollbackErrors.Select(e => e.ShardId));

            _logger.LogError(
                "Migration rollback partially failed. MigrationId={MigrationId}, ShardsRolledBack={ShardsRolledBack}, ShardsFailed={ShardsFailed}, TotalRollbackDuration={TotalRollbackDurationMs:F1}ms",
                result.Id, shardsToRollback.Count - rollbackErrors.Count, rollbackErrors.Count, rollbackDuration.TotalMilliseconds);

            return Either<EncinaError, Unit>.Left(
                EncinaErrors.Create(
                    MigrationErrorCodes.RollbackFailed,
                    $"Rollback failed on {rollbackErrors.Count} shard(s): {failedShards}."));
        }

        _logger.LogWarning(
            "Migration rollback executed. MigrationId={MigrationId}, ShardsRolledBack={ShardsRolledBack}, TotalRollbackDuration={TotalRollbackDurationMs:F1}ms",
            result.Id, shardsToRollback.Count, rollbackDuration.TotalMilliseconds);

        return Either<EncinaError, Unit>.Right(unit);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, SchemaDriftReport>> DetectDriftAsync(
        DriftDetectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var activeShards = _topology.GetActiveShards();

        if (activeShards.Count < 2)
        {
            // Need at least 2 shards to compare
            return Either<EncinaError, SchemaDriftReport>.Right(
                new SchemaDriftReport([], DateTimeOffset.UtcNow));
        }

        var includeColumnDiffs = options?.IncludeColumnDiffs ?? true;

        // Select baseline shard
        ShardInfo baselineShard;
        if (options?.BaselineShardId is not null)
        {
            var baselineResult = _topology.GetShard(options.BaselineShardId);
            if (baselineResult.IsLeft)
            {
                return Either<EncinaError, SchemaDriftReport>.Left(
                    baselineResult.Match(Right: _ => default, Left: e => e));
            }

            baselineShard = baselineResult.Match(Right: s => s, Left: _ => default!);
        }
        else
        {
            baselineShard = activeShards[0];
        }

        var diffs = new List<ShardSchemaDiff>();
        var shardsToCompare = activeShards.Where(s => s.ShardId != baselineShard.ShardId).ToList();

        foreach (var shard in shardsToCompare)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var compareResult = await _introspector.CompareAsync(
                shard, baselineShard, includeColumnDiffs, cancellationToken);

            var matchResult = compareResult.Match(
                Right: diff =>
                {
                    if (diff.TableDiffs.Count > 0)
                    {
                        diffs.Add(diff);
                    }

                    return Either<EncinaError, Unit>.Right(unit);
                },
                Left: error => Either<EncinaError, Unit>.Left(error));

            if (matchResult.IsLeft)
            {
                return Either<EncinaError, SchemaDriftReport>.Left(
                    EncinaErrors.Create(
                        MigrationErrorCodes.SchemaComparisonFailed,
                        $"Schema comparison failed for shard '{shard.ShardId}'."));
            }
        }

        var report = new SchemaDriftReport(diffs, DateTimeOffset.UtcNow);

        if (report.HasDrift)
        {
            var driftedShardIds = diffs.Select(d => d.ShardId).ToList();
            var totalTableDiffs = diffs.Sum(d => d.TableDiffs.Count);

            _logger.LogWarning(
                "Schema drift detected. ShardIdsWithDrift={ShardIdsWithDrift}, DiffSummary={DriftedShardCount} shard(s) with {TotalTableDiffs} table diff(s)",
                string.Join(", ", driftedShardIds), driftedShardIds.Count, totalTableDiffs);
        }

        return Either<EncinaError, SchemaDriftReport>.Right(report);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, MigrationProgress>> GetProgressAsync(
        Guid migrationId,
        CancellationToken cancellationToken = default)
    {
        if (_activeTrackers.TryGetValue(migrationId, out var tracker))
        {
            return Task.FromResult(
                Either<EncinaError, MigrationProgress>.Right(tracker.ToSnapshot()));
        }

        return Task.FromResult(
            Either<EncinaError, MigrationProgress>.Left(
                EncinaErrors.Create(
                    MigrationErrorCodes.MigrationNotFound,
                    $"Migration '{migrationId}' was not found.")));
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyList<string>>> GetAppliedMigrationsAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        if (!_topology.ContainsShard(shardId))
        {
            return Either<EncinaError, IReadOnlyList<string>>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ShardNotFound,
                    $"Shard '{shardId}' was not found in the topology."));
        }

        return await _historyStore.GetAppliedAsync(shardId, cancellationToken);
    }

    private static IMigrationStrategy SelectStrategy(MigrationStrategy strategy) => strategy switch
    {
        MigrationStrategy.Sequential => SequentialStrategy,
        MigrationStrategy.Parallel => ParallelStrategy,
        MigrationStrategy.RollingUpdate => RollingStrategy,
        MigrationStrategy.CanaryFirst => CanaryStrategy,
        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown migration strategy.")
    };

    private static string GetPhaseLabel(MigrationStrategy strategy) => strategy switch
    {
        MigrationStrategy.Sequential => "Sequential",
        MigrationStrategy.Parallel => "Parallel",
        MigrationStrategy.RollingUpdate => "RollingUpdate",
        MigrationStrategy.CanaryFirst => "Canary",
        _ => "Unknown"
    };

    /// <summary>
    /// Mutable thread-safe tracker for an in-flight migration, producing immutable
    /// <see cref="MigrationProgress"/> snapshots on demand.
    /// </summary>
    private sealed class MigrationProgressTracker
    {
        private readonly Guid _migrationId;
        private readonly int _totalShards;
        private readonly ConcurrentDictionary<string, ShardMigrationStatus> _shardStatuses = new(StringComparer.OrdinalIgnoreCase);
        private volatile string _currentPhase = "Initializing";

        public MigrationProgressTracker(Guid migrationId, int totalShards)
        {
            _migrationId = migrationId;
            _totalShards = totalShards;
        }

        public void UpdateShard(string shardId, ShardMigrationStatus status)
        {
            _shardStatuses[shardId] = status;
        }

        public void UpdatePhase(string phase)
        {
            _currentPhase = phase;
        }

        public MigrationProgress ToSnapshot()
        {
            var statuses = new Dictionary<string, ShardMigrationStatus>(_shardStatuses, StringComparer.OrdinalIgnoreCase);
            var completed = statuses.Values.Count(s => s.Outcome == MigrationOutcome.Succeeded);
            var failed = statuses.Values.Count(s => s.Outcome == MigrationOutcome.Failed);

            return new MigrationProgress(
                _migrationId,
                _totalShards,
                completed,
                failed,
                _currentPhase,
                statuses);
        }
    }
}
#pragma warning restore CA1848
