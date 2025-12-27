using Encina.Messaging.Health;
using Microsoft.Extensions.Options;

namespace Encina.AzureFunctions.Durable;

/// <summary>
/// Health check for Durable Functions integration.
/// </summary>
/// <remarks>
/// <para>
/// This health check validates that Durable Functions integration is properly configured.
/// Since Durable Functions relies on external storage (Azure Storage, SQL Server, etc.),
/// this check verifies configuration rather than connectivity (use dedicated storage health checks).
/// </para>
/// </remarks>
public sealed class DurableFunctionsHealthCheck : IEncinaHealthCheck
{
    private readonly DurableFunctionsOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DurableFunctionsHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The Durable Functions options.</param>
    public DurableFunctionsHealthCheck(IOptions<DurableFunctionsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc/>
    public string Name => _options.ProviderHealthCheck.Name ?? "encina-durable-functions";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Tags => _options.ProviderHealthCheck.Tags;

    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["defaultMaxRetries"] = _options.DefaultMaxRetries,
            ["defaultFirstRetryInterval"] = _options.DefaultFirstRetryInterval.ToString(),
            ["defaultBackoffCoefficient"] = _options.DefaultBackoffCoefficient,
            ["continueCompensationOnError"] = _options.ContinueCompensationOnError
        };

        if (_options.DefaultSagaTimeout.HasValue)
        {
            data["defaultSagaTimeout"] = _options.DefaultSagaTimeout.Value.ToString();
        }

        // Validate configuration
        var issues = new List<string>();

        if (_options.DefaultMaxRetries < 0)
        {
            issues.Add("DefaultMaxRetries cannot be negative");
        }

        if (_options.DefaultFirstRetryInterval <= TimeSpan.Zero)
        {
            issues.Add("DefaultFirstRetryInterval must be positive");
        }

        if (_options.DefaultBackoffCoefficient <= 0)
        {
            issues.Add("DefaultBackoffCoefficient must be positive");
        }

        if (_options.DefaultMaxRetryInterval <= TimeSpan.Zero)
        {
            issues.Add("DefaultMaxRetryInterval must be positive");
        }

        if (_options.DefaultSagaTimeout.HasValue && _options.DefaultSagaTimeout.Value <= TimeSpan.Zero)
        {
            issues.Add("DefaultSagaTimeout must be positive if set");
        }

        if (issues.Count > 0)
        {
            data["issues"] = issues;
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Durable Functions configuration has issues: {string.Join("; ", issues)}",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Durable Functions integration is configured and ready",
            data: data));
    }
}
