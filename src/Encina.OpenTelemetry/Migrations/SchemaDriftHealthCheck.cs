using Encina.Sharding.Migrations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.OpenTelemetry.Migrations;

/// <summary>
/// Health check that periodically detects schema drift across shards.
/// </summary>
/// <remarks>
/// <para>
/// Returns <see cref="HealthStatus.Healthy"/> if no drift is detected,
/// <see cref="HealthStatus.Degraded"/> if drift is detected on non-critical tables,
/// and <see cref="HealthStatus.Unhealthy"/> if drift is detected on critical tables
/// or if the detection fails entirely.
/// </para>
/// <para>
/// The list of critical tables is configured via <see cref="SchemaDriftHealthCheckOptions.CriticalTables"/>
/// or inherited from <see cref="DriftDetectionOptions.CriticalTables"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddHealthChecks()
///     .Add(new HealthCheckRegistration(
///         "schema-drift",
///         sp => new SchemaDriftHealthCheck(
///             sp.GetRequiredService&lt;IShardedMigrationCoordinator&gt;(),
///             new SchemaDriftHealthCheckOptions()),
///         failureStatus: HealthStatus.Degraded,
///         tags: ["migration", "schema"]));
/// </code>
/// </example>
public sealed class SchemaDriftHealthCheck : IHealthCheck
{
    private readonly IShardedMigrationCoordinator _coordinator;
    private readonly SchemaDriftHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaDriftHealthCheck"/> class.
    /// </summary>
    /// <param name="coordinator">The migration coordinator for drift detection.</param>
    /// <param name="options">Options controlling the health check behavior.</param>
    public SchemaDriftHealthCheck(
        IShardedMigrationCoordinator coordinator,
        SchemaDriftHealthCheckOptions options)
    {
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentNullException.ThrowIfNull(options);

        _coordinator = coordinator;
        _options = options;
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
            var driftOptions = new DriftDetectionOptions
            {
                BaselineShardId = _options.BaselineShardId,
                IncludeColumnDiffs = true,
                ComparisonDepth = SchemaComparisonDepth.TablesAndColumns,
                CriticalTables = _options.CriticalTables
            };

            var result = await _coordinator.DetectDriftAsync(driftOptions, cts.Token);

            return result.Match(
                Right: report =>
                {
                    if (!report.HasDrift)
                    {
                        return HealthCheckResult.Healthy(
                            "No schema drift detected across shards.",
                            new Dictionary<string, object>
                            {
                                ["detectedAtUtc"] = report.DetectedAtUtc.ToString("O"),
                                ["shardsCompared"] = report.Diffs.Count
                            });
                    }

                    var driftedShardIds = report.Diffs
                        .Where(d => d.TableDiffs.Count > 0)
                        .Select(d => d.ShardId)
                        .ToList();

                    var driftedTables = report.Diffs
                        .SelectMany(d => d.TableDiffs.Select(t => t.TableName))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var criticalTables = _options.CriticalTables;
                    var criticalDrift = driftedTables
                        .Where(t => criticalTables.Contains(t, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    var data = new Dictionary<string, object>
                    {
                        ["detectedAtUtc"] = report.DetectedAtUtc.ToString("O"),
                        ["driftedShardCount"] = driftedShardIds.Count,
                        ["driftedShardIds"] = string.Join(", ", driftedShardIds),
                        ["driftedTables"] = string.Join(", ", driftedTables)
                    };

                    if (criticalDrift.Count > 0)
                    {
                        data["criticalTablesAffected"] = string.Join(", ", criticalDrift);

                        return HealthCheckResult.Unhealthy(
                            $"Schema drift detected on {criticalDrift.Count} critical table(s): " +
                            $"{string.Join(", ", criticalDrift)}.",
                            data: data);
                    }

                    return HealthCheckResult.Degraded(
                        $"Schema drift detected on {driftedShardIds.Count} shard(s) " +
                        $"affecting {driftedTables.Count} table(s). No critical tables affected.",
                        data: data);
                },
                Left: error => HealthCheckResult.Unhealthy(
                    $"Schema drift detection failed: {error.Message}",
                    data: new Dictionary<string, object>
                    {
                        ["error"] = error.Message
                    }));
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                $"Schema drift detection timed out after {_options.Timeout.TotalSeconds}s.");
        }
    }
}
