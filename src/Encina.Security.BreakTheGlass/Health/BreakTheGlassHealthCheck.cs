using Encina.Security.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.Security.BreakTheGlass.Health;

/// <summary>
/// Health check of the break-the-glass infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="HealthStatus.Degraded"/> when the store probe returns an error, when the expiry sweeper has
/// not completed a cycle within three sweep intervals (counted from its start), or when the elevation store
/// or the operation audit store is in-memory. <see cref="HealthStatus.Unhealthy"/> only when the check
/// itself throws. <see cref="HealthStatus.Degraded"/> does not fail a readiness probe by default (ASP.NET
/// Core answers HTTP 200), so a break-the-glass outage alerts operators without pulling the whole
/// application out of rotation. The description and data carry fixed state codes only: never a tenant,
/// user, subject or elevation id and never an error message.
/// </para>
/// <para>
/// <c>AddEncinaBreakTheGlass</c> registers it under <see cref="DefaultName"/> with the <c>ready</c> tag.
/// </para>
/// </remarks>
public sealed class BreakTheGlassHealthCheck : IHealthCheck
{
    /// <summary>The default health check name.</summary>
    public const string DefaultName = "encina-break-the-glass";

    private const int StaleSweepIntervals = 3;

    private static readonly string[] DefaultTags = ["encina", "security", "break-the-glass", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<BreakTheGlassOptions> _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreakTheGlassHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">Resolves the (possibly scoped) stores through a scope of its own.</param>
    /// <param name="options">The options (sweep interval).</param>
    /// <param name="timeProvider">The clock.</param>
    public BreakTheGlassHealthCheck(IServiceProvider serviceProvider, IOptions<BreakTheGlassOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _serviceProvider = serviceProvider;
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <summary>Gets the default tags of the health check.</summary>
    public static IReadOnlyCollection<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IBreakTheGlassStore>();

            var probe = await store.ProbeAsync(cancellationToken).ConfigureAwait(false);
            if (probe.IsLeft)
            {
                return HealthCheckResult.Degraded(
                    "The break-the-glass store probe returned an error.",
                    data: new Dictionary<string, object> { ["state"] = "store_probe_failed" });
            }

            var states = DegradedStates(store, scope.ServiceProvider.GetService<IOperationAuditStore>());
            return states.Count == 0
                ? HealthCheckResult.Healthy("Break-the-glass is operational.")
                : HealthCheckResult.Degraded(
                    "Break-the-glass is degraded.",
                    data: new Dictionary<string, object> { ["state"] = string.Join(',', states) });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("The break-the-glass health check was cancelled.");
        }
        catch (Exception ex)
        {
            // Only the exception type travels: its message can carry data.
            return HealthCheckResult.Unhealthy($"Health check failed: {ex.GetType().Name}");
        }
    }

    private List<string> DegradedStates(IBreakTheGlassStore store, IOperationAuditStore? auditStore)
    {
        var states = new List<string>();
        if (store is InMemoryBreakTheGlassStore)
        {
            states.Add("in_memory_store");
        }

        if (auditStore is InMemoryOperationAuditStore)
        {
            states.Add("in_memory_audit_store");
        }

        if (SweepIsStale())
        {
            states.Add("sweep_stale");
        }

        return states;
    }

    // Not applicable while the sweeper has not started. The grace is counted from the start until a
    // cycle completes, and from the last completed cycle afterwards.
    private bool SweepIsStale()
    {
        var state = _serviceProvider.GetService<BreakTheGlassSweepState>();
        if (state?.StartedAtUtc is not { } started)
        {
            return false;
        }

        var reference = state.LastCompletedAtUtc ?? started;
        return _timeProvider.GetUtcNow() - reference > _options.Value.ExpirySweepInterval * StaleSweepIntervals;
    }
}
