using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Providers;
using Encina.Security.Secrets.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.Security.Secrets.Health;

/// <summary>
/// Health check that verifies the secrets subsystem is operational by validating
/// that <see cref="ISecretReader"/> is resolvable and can retrieve a probe secret.
/// </summary>
/// <remarks>
/// <para>
/// This health check performs the following verifications:
/// <list type="number">
/// <item><description>Resolves <see cref="ISecretReader"/> from the DI container.</description></item>
/// <item><description>If <see cref="SecretsOptions.HealthCheckSecretName"/> is configured,
/// attempts to read the probe secret to verify provider connectivity.</description></item>
/// <item><description>If the reader is a <see cref="FailoverSecretReader"/>, reports
/// individual provider status in health check metadata.</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="SecretsOptions.ProviderHealthCheck"/>:
/// <code>
/// services.AddEncinaSecrets(options =>
/// {
///     options.ProviderHealthCheck = true;
///     options.HealthCheckSecretName = "health-probe-secret"; // optional
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class SecretsHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-secrets";

    private static readonly string[] DefaultTags = ["encina", "secrets", "ready"];

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretsHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve secrets services.</param>
    public SecretsHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets the default tags for the secrets health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            // Verify ISecretReader is resolvable
            var secretReader = scopedProvider.GetService<ISecretReader>();

            if (secretReader is null)
            {
                return HealthCheckResult.Unhealthy(
                    "Missing secrets service: ISecretReader is not registered.");
            }

            var options = scopedProvider.GetService<IOptions<SecretsOptions>>()?.Value;
            var data = BuildBaseData(secretReader, options);

            // Report circuit breaker state when resilience is enabled
            var circuitBreakerResult = CheckCircuitBreakerState(scopedProvider, data);
            if (circuitBreakerResult is not null)
            {
                return circuitBreakerResult.Value;
            }

            // If a probe secret is configured, attempt to read it
            if (options?.HealthCheckSecretName is { } probeSecretName)
            {
                return await ProbeSecretAsync(secretReader, probeSecretName, data, cancellationToken)
                    .ConfigureAwait(false);
            }

            return HealthCheckResult.Healthy(
                "Secrets subsystem is healthy. ISecretReader is available.",
                data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Secrets health check failed with exception: {ex.GetType().Name}");
        }
    }

    /// <summary>
    /// Builds the base health-check data from the resolved reader and options, including
    /// caching, failover and decorator-chain metadata.
    /// </summary>
    private static Dictionary<string, object> BuildBaseData(ISecretReader secretReader, SecretsOptions? options)
    {
        var data = new Dictionary<string, object>
        {
            ["readerType"] = secretReader.GetType().Name
        };

        if (options is not null)
        {
            data["cachingEnabled"] = options.EnableCaching;
        }

        if (secretReader is FailoverSecretReader failoverReader)
        {
            data["failoverProviders"] = failoverReader.ProviderCount;
        }

        // Report decorator chain (check options, not runtime type,
        // because auditing/resilience decorators wrap the caching decorator)
        if (options is { EnableCaching: true })
        {
            data["decorators"] = "cached";
        }

        return data;
    }

    /// <summary>
    /// Reports the circuit breaker state when resilience is enabled, returning a degraded
    /// result when the breaker is open; otherwise <see langword="null"/>.
    /// </summary>
    private static HealthCheckResult? CheckCircuitBreakerState(
        IServiceProvider scopedProvider,
        Dictionary<string, object> data)
    {
        var circuitBreakerState = scopedProvider.GetService<SecretsCircuitBreakerState>();
        if (circuitBreakerState is null)
        {
            return null;
        }

        data["resilienceEnabled"] = true;
        data["circuitBreakerState"] = circuitBreakerState.State.ToString();

        if (circuitBreakerState.State == CircuitBreakerStateValue.Opened)
        {
            return HealthCheckResult.Degraded(
                "Secrets subsystem is degraded. Circuit breaker is open — provider may be unavailable.",
                data: data);
        }

        return null;
    }

    /// <summary>
    /// Attempts to read the configured probe secret and reports success or failure.
    /// </summary>
    private static async Task<HealthCheckResult> ProbeSecretAsync(
        ISecretReader secretReader,
        string probeSecretName,
        Dictionary<string, object> data,
        CancellationToken cancellationToken)
    {
        var probeResult = await secretReader
            .GetSecretAsync(probeSecretName, cancellationToken)
            .ConfigureAwait(false);

        return probeResult.Match(
            Right: _ =>
            {
                data["probeResult"] = "success";
                return HealthCheckResult.Healthy(
                    "Secrets subsystem is healthy. Provider probe succeeded.",
                    data);
            },
            Left: error =>
            {
                data["probeResult"] = "failed";
                data["probeError"] = error.GetCode().IfNone("encina.unknown");
                return HealthCheckResult.Degraded(
                    $"Secrets subsystem is degraded. Probe secret '{probeSecretName}' could not be retrieved: {error.GetCode().IfNone("encina.unknown")}",
                    data: data);
            });
    }
}
