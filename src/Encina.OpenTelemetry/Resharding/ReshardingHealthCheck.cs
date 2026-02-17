using Encina.Sharding.Resharding;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.OpenTelemetry.Resharding;

/// <summary>
/// Health check that monitors active resharding operations.
/// </summary>
/// <remarks>
/// <para>
/// Returns <see cref="HealthStatus.Healthy"/> if no active resharding operations exist,
/// <see cref="HealthStatus.Degraded"/> if resharding is active and progressing within
/// the configured time limit, and <see cref="HealthStatus.Unhealthy"/> if resharding
/// exceeds <see cref="ReshardingHealthCheckOptions.MaxReshardingDuration"/> or is in
/// a <c>Failed</c> state without rollback.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddHealthChecks()
///     .Add(new HealthCheckRegistration(
///         "resharding",
///         sp => new ReshardingHealthCheck(
///             sp.GetRequiredService&lt;IReshardingStateStore&gt;(),
///             new ReshardingHealthCheckOptions()),
///         failureStatus: HealthStatus.Degraded,
///         tags: ["resharding", "sharding"]));
/// </code>
/// </example>
public sealed class ReshardingHealthCheck : IHealthCheck
{
    private readonly IReshardingStateStore _stateStore;
    private readonly ReshardingHealthCheckOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReshardingHealthCheck"/> class.
    /// </summary>
    /// <param name="stateStore">The resharding state store for querying active operations.</param>
    /// <param name="options">Options controlling the health check behavior.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    public ReshardingHealthCheck(
        IReshardingStateStore stateStore,
        ReshardingHealthCheckOptions options,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(stateStore);
        ArgumentNullException.ThrowIfNull(options);

        _stateStore = stateStore;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.Timeout);

        try
        {
            var result = await _stateStore.GetActiveReshardingsAsync(cts.Token);

            return result.Match(
                Right: activeStates =>
                {
                    if (activeStates.Count == 0)
                    {
                        return HealthCheckResult.Healthy(
                            "No active resharding operations.",
                            new Dictionary<string, object>
                            {
                                ["activeCount"] = 0
                            });
                    }

                    var now = _timeProvider.GetUtcNow().UtcDateTime;
                    var failedStates = new List<ReshardingState>();
                    var overdueStates = new List<ReshardingState>();
                    var activeInProgress = new List<ReshardingState>();

                    foreach (var state in activeStates)
                    {
                        if (state.CurrentPhase == ReshardingPhase.Failed)
                        {
                            failedStates.Add(state);
                        }
                        else if (now - state.StartedAtUtc > _options.MaxReshardingDuration)
                        {
                            overdueStates.Add(state);
                        }
                        else
                        {
                            activeInProgress.Add(state);
                        }
                    }

                    var data = new Dictionary<string, object>
                    {
                        ["activeCount"] = activeStates.Count,
                        ["inProgressCount"] = activeInProgress.Count,
                        ["failedCount"] = failedStates.Count,
                        ["overdueCount"] = overdueStates.Count,
                    };

                    if (failedStates.Count > 0)
                    {
                        data["failedIds"] = string.Join(", ", failedStates.Select(s => s.Id));
                    }

                    if (overdueStates.Count > 0)
                    {
                        data["overdueIds"] = string.Join(", ", overdueStates.Select(s => s.Id));
                    }

                    // Unhealthy if any failed or overdue
                    if (failedStates.Count > 0 || overdueStates.Count > 0)
                    {
                        var reasons = new List<string>();

                        if (failedStates.Count > 0)
                        {
                            reasons.Add($"{failedStates.Count} failed without rollback");
                        }

                        if (overdueStates.Count > 0)
                        {
                            reasons.Add(
                                $"{overdueStates.Count} exceeded max duration " +
                                $"of {_options.MaxReshardingDuration.TotalHours:F1}h");
                        }

                        return HealthCheckResult.Unhealthy(
                            $"Resharding issues: {string.Join("; ", reasons)}.",
                            data: data);
                    }

                    // Degraded if active and in progress
                    var phases = activeInProgress
                        .Select(s => $"{s.Id}={s.CurrentPhase}")
                        .ToList();

                    data["activeOperations"] = string.Join(", ", phases);

                    return HealthCheckResult.Degraded(
                        $"{activeInProgress.Count} resharding operation(s) in progress.",
                        data: data);
                },
                Left: error => HealthCheckResult.Unhealthy(
                    $"Failed to query resharding state: {error.Message}",
                    data: new Dictionary<string, object>
                    {
                        ["error"] = error.Message
                    }));
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                $"Resharding health check timed out after {_options.Timeout.TotalSeconds}s.");
        }
    }
}
