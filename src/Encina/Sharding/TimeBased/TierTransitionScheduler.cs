using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// Background service that automates tier transitions and shard creation
/// for time-based sharding.
/// </summary>
/// <remarks>
/// <para>
/// The scheduler runs on a configurable interval (default: 1 hour) and performs two tasks:
/// </para>
/// <list type="number">
/// <item>
/// <b>Tier transitions</b>: Queries the <see cref="ITierStore"/> for shards due for promotion
/// and delegates to <see cref="IShardArchiver.TransitionTierAsync"/> for each.
/// </item>
/// <item>
/// <b>Auto-shard creation</b>: Creates new Hot-tier shards before the next time period
/// begins, using <see cref="PeriodBoundaryCalculator"/> to compute boundaries and a
/// configurable lead time.
/// </item>
/// </list>
/// <para>
/// Dependencies are resolved from a scoped <see cref="IServiceProvider"/> on each tick,
/// ensuring compatibility with scoped services like database contexts.
/// </para>
/// <para>
/// Errors are logged but never crash the service. The scheduler continues running and
/// retries on the next interval.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.Configure&lt;TimeBasedShardingOptions&gt;(options =>
/// {
///     options.Enabled = true;
///     options.Period = ShardPeriod.Monthly;
///     options.ShardIdPrefix = "orders";
///     options.ConnectionStringTemplate = "Server=hot;Database=orders_{0}";
///     options.Transitions =
///     [
///         new TierTransition(ShardTier.Hot, ShardTier.Warm, TimeSpan.FromDays(30)),
///         new TierTransition(ShardTier.Warm, ShardTier.Cold, TimeSpan.FromDays(90)),
///     ];
/// });
///
/// services.AddHostedService&lt;TierTransitionScheduler&gt;();
/// </code>
/// </example>
public sealed class TierTransitionScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeBasedShardingOptions _options;
    private readonly ILogger<TierTransitionScheduler> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="TierTransitionScheduler"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving scoped dependencies.</param>
    /// <param name="options">The time-based sharding configuration.</param>
    /// <param name="logger">The logger for recording scheduler operations.</param>
    /// <param name="timeProvider">
    /// Optional time provider for testability. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    public TierTransitionScheduler(
        IServiceProvider serviceProvider,
        IOptions<TimeBasedShardingOptions> options,
        ILogger<TierTransitionScheduler> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            Log.SchedulerDisabled(_logger);
            return;
        }

        Log.SchedulerStarted(_logger, _options.CheckInterval, _options.Transitions.Count);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.CheckInterval, _timeProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ExecuteTickAsync(stoppingToken).ConfigureAwait(false);
        }

        Log.SchedulerStopped(_logger);
    }

    private async Task ExecuteTickAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var tierStore = scope.ServiceProvider.GetRequiredService<ITierStore>();
            var archiver = scope.ServiceProvider.GetRequiredService<IShardArchiver>();

            await ExecuteTransitionsAsync(tierStore, archiver, cancellationToken).ConfigureAwait(false);

            if (_options.EnableAutoShardCreation)
            {
                await ExecuteAutoShardCreationAsync(tierStore, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            Log.TransitionCheckError(_logger, ex);
        }
    }

    private async Task ExecuteTransitionsAsync(
        ITierStore tierStore,
        IShardArchiver archiver,
        CancellationToken cancellationToken)
    {
        Log.TransitionCheckStarted(_logger);

        var successCount = 0;
        var failureCount = 0;

        foreach (var transition in _options.Transitions)
        {
            var dueShards = await tierStore.GetShardsDueForTransitionAsync(
                transition.FromTier,
                transition.AgeThreshold,
                cancellationToken).ConfigureAwait(false);

            foreach (var shard in dueShards)
            {
                Log.TransitioningTier(_logger, shard.ShardId, transition.FromTier, transition.ToTier);

                var result = await archiver.TransitionTierAsync(
                    shard.ShardId,
                    transition.ToTier,
                    cancellationToken).ConfigureAwait(false);

                result.Match(
                    Right: _ =>
                    {
                        Log.TransitionSucceeded(_logger, shard.ShardId, transition.ToTier);
                        successCount++;
                    },
                    Left: error =>
                    {
                        Log.TransitionFailed(_logger, shard.ShardId, error.Message);
                        failureCount++;
                    });
            }
        }

        Log.TransitionCheckCompleted(_logger, successCount, failureCount);
    }

    private async Task ExecuteAutoShardCreationAsync(
        ITierStore tierStore,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionStringTemplate))
        {
            Log.AutoCreateSkippedNoTemplate(_logger);
            return;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(nowUtc);

        // Determine next period boundaries
        var currentPeriodEnd = PeriodBoundaryCalculator.GetPeriodEnd(today, _options.Period, _options.WeekStart);
        var leadTimeDate = DateOnly.FromDateTime(nowUtc + _options.ShardCreationLeadTime);

        // Only create if we're within the lead time window
        if (leadTimeDate < currentPeriodEnd)
        {
            return;
        }

        // Compute the next period's boundaries
        var nextPeriodStart = currentPeriodEnd;
        var nextPeriodEnd = PeriodBoundaryCalculator.GetPeriodEnd(nextPeriodStart, _options.Period, _options.WeekStart);
        var periodLabel = PeriodBoundaryCalculator.GetPeriodLabel(nextPeriodStart, _options.Period, _options.WeekStart);
        var shardId = $"{_options.ShardIdPrefix}-{periodLabel}";

        // Check if the shard already exists
        var existing = await tierStore.GetTierInfoAsync(shardId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            Log.AutoCreateNotNeeded(_logger, shardId);
            return;
        }

        // Create the new shard
        var connectionString = string.Format(
            CultureInfo.InvariantCulture,
            _options.ConnectionStringTemplate,
            periodLabel);

        var tierInfo = new ShardTierInfo(
            ShardId: shardId,
            CurrentTier: ShardTier.Hot,
            PeriodStart: nextPeriodStart,
            PeriodEnd: nextPeriodEnd,
            IsReadOnly: false,
            ConnectionString: connectionString,
            CreatedAtUtc: nowUtc);

        try
        {
            Log.AutoCreatingShard(_logger, shardId, nextPeriodStart, nextPeriodEnd);
            await tierStore.AddShardAsync(tierInfo, cancellationToken).ConfigureAwait(false);
            Log.AutoCreateSucceeded(_logger, shardId);
        }
        catch (ArgumentException)
        {
            // Race condition: another instance created it between our check and add
            Log.AutoCreateSkippedAlreadyExists(_logger, shardId);
        }
        catch (Exception ex)
        {
            Log.AutoCreateFailed(_logger, ex, shardId);
        }
    }
}
