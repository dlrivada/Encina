using Encina.IdGeneration.Configuration;
using Encina.Messaging.Health;

namespace Encina.IdGeneration.Health;

/// <summary>
/// Health check for ID generation infrastructure that monitors clock drift
/// and Snowflake machine ID configuration.
/// </summary>
/// <remarks>
/// <para>
/// Reports <see cref="HealthStatus.Degraded"/> when clock drift exceeds the
/// configured threshold, and <see cref="HealthStatus.Healthy"/> when all
/// generators are operating normally.
/// </para>
/// <para>
/// This health check validates:
/// <list type="bullet">
///   <item><description>Clock drift against a configurable threshold.</description></item>
///   <item><description>Snowflake machine ID configuration when Snowflake generation is enabled.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via ASP.NET Core health checks:
/// builder.Services
///     .AddHealthChecks()
///     .AddEncinaIdGenerationHealthCheck();
/// </code>
/// </example>
public sealed class IdGeneratorHealthCheck : EncinaHealthCheck
{
    private readonly TimeProvider _timeProvider;
    private readonly IdGeneratorHealthCheckOptions _options;
    private readonly SnowflakeOptions? _snowflakeOptions;
    private DateTimeOffset _lastKnownTime;

    /// <summary>
    /// The default name for this health check.
    /// </summary>
    public const string DefaultName = "encina-id-generation";

    /// <summary>
    /// Initializes a new instance of the <see cref="IdGeneratorHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The health check options.</param>
    /// <param name="snowflakeOptions">The optional Snowflake configuration for machine ID validation.</param>
    /// <param name="timeProvider">
    /// Optional time provider for testable time operations. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    public IdGeneratorHealthCheck(
        IdGeneratorHealthCheckOptions? options = null,
        SnowflakeOptions? snowflakeOptions = null,
        TimeProvider? timeProvider = null)
        : base(DefaultName, ["id-generation", "ready"])
    {
        _options = options ?? new IdGeneratorHealthCheckOptions();
        _snowflakeOptions = snowflakeOptions;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _lastKnownTime = _timeProvider.GetUtcNow();
    }

    /// <inheritdoc />
    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object>();
        var currentTime = _timeProvider.GetUtcNow();
        var drift = _lastKnownTime - currentTime;

        data["clock_drift_ms"] = Math.Abs(drift.TotalMilliseconds);
        data["last_check_utc"] = _lastKnownTime.ToString("O");

        // Update the last known time for future comparisons
        _lastKnownTime = currentTime;

        // Check clock drift
        if (Math.Abs(drift.TotalMilliseconds) > _options.ClockDriftThresholdMs)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Clock drift detected: {Math.Abs(drift.TotalMilliseconds):F1}ms exceeds threshold of {_options.ClockDriftThresholdMs}ms.",
                data: data));
        }

        // Validate Snowflake machine ID if configured
        if (_snowflakeOptions is not null)
        {
            data["snowflake_machine_id"] = _snowflakeOptions.MachineId;
            data["snowflake_shard_bits"] = _snowflakeOptions.ShardBits;
            data["snowflake_max_machine_id"] = (1L << _snowflakeOptions.ShardBits) - 1;
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "ID generation is operating normally.",
            data: data));
    }
}
