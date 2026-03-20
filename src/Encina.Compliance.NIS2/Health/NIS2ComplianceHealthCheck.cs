using Encina.Compliance.NIS2.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.NIS2.Health;

/// <summary>
/// Health check that verifies NIS2 compliance posture by evaluating all 10 mandatory
/// cybersecurity measures (Art. 21(2)).
/// </summary>
/// <remarks>
/// <para>
/// This health check resolves <see cref="INIS2ComplianceValidator"/> via a scoped service
/// provider and runs a full compliance validation. The result maps to:
/// <list type="bullet">
/// <item><description><see cref="HealthStatus.Healthy"/> — All 10 measures are satisfied.</description></item>
/// <item><description><see cref="HealthStatus.Degraded"/> — Some measures are satisfied but gaps exist.</description></item>
/// <item><description><see cref="HealthStatus.Unhealthy"/> — Compliance validation failed or critical measures are missing.</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="NIS2Options.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaNIS2(options =>
/// {
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class NIS2ComplianceHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-nis2-compliance";

    private static readonly string[] DefaultTags =
        ["encina", "nis2", "compliance", "security", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NIS2ComplianceHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NIS2ComplianceHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve NIS2 compliance services.</param>
    /// <param name="logger">The logger instance.</param>
    public NIS2ComplianceHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<NIS2ComplianceHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the NIS2 compliance health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

            var result = await validator.ValidateAsync(cancellationToken).ConfigureAwait(false);

            return result.Match(
                Right: compliance =>
                {
                    data["compliancePercentage"] = compliance.CompliancePercentage;
                    data["missingCount"] = compliance.MissingCount;
                    data["entityType"] = compliance.EntityType.ToString();
                    data["sector"] = compliance.Sector.ToString();
                    data["evaluatedAtUtc"] = compliance.EvaluatedAtUtc.ToString("O");

                    if (compliance.MissingMeasures.Count > 0)
                    {
                        data["missingMeasures"] = string.Join(", ",
                            compliance.MissingMeasures.Select(m => m.ToString()));
                    }

                    if (compliance.IsCompliant)
                    {
                        return HealthCheckResult.Healthy(
                            "All 10 NIS2 mandatory measures are satisfied.",
                            data);
                    }

                    return HealthCheckResult.Degraded(
                        $"NIS2 compliance at {compliance.CompliancePercentage}%: "
                        + $"{compliance.MissingCount} measure(s) not satisfied.",
                        data: data);
                },
                Left: error =>
                {
                    data["error"] = error.Message;

                    _logger.LogWarning(
                        "NIS2 compliance health check failed: {Error}",
                        error.Message);

                    return HealthCheckResult.Unhealthy(
                        $"NIS2 compliance validation failed: {error.Message}",
                        data: data);
                });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "NIS2 compliance health check encountered an exception.");

            return HealthCheckResult.Unhealthy(
                $"NIS2 compliance health check exception: {ex.Message}",
                ex,
                data);
        }
    }
}
