using Encina.Audit.Marten.Crypto;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.Audit.Marten.Health;

/// <summary>
/// Health check that verifies Marten event-sourced audit store accessibility and
/// temporal key provider availability.
/// </summary>
/// <remarks>
/// <para>
/// Performs the following checks:
/// <list type="number">
/// <item>Resolves <see cref="ITemporalKeyProvider"/> from DI to verify Marten/PostgreSQL connectivity.</item>
/// <item>Calls <see cref="ITemporalKeyProvider.GetActiveKeysAsync"/> to verify the key store is accessible.</item>
/// </list>
/// </para>
/// <para>
/// Returns <see cref="HealthCheckResult"/> with:
/// <list type="bullet">
/// <item><b>Healthy</b>: Marten store and key provider are accessible.</item>
/// <item><b>Degraded</b>: Key provider returned an error or has no active keys.</item>
/// <item><b>Unhealthy</b>: Marten store or key provider is unreachable.</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaAuditMarten(options =>
/// {
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </example>
public sealed class MartenOperationAuditHealthCheck : IHealthCheck
{
    /// <summary>
    /// The default name for this health check.
    /// </summary>
    public const string DefaultName = "encina-audit-marten";

    private static readonly string[] DefaultTags = ["encina", "audit", "marten", "security", "ready"];

    /// <summary>
    /// Gets the default tags for the Marten audit health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenOperationAuditHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving scoped dependencies.</param>
    public MartenOperationAuditHealthCheck(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var keyProvider = scope.ServiceProvider.GetService<ITemporalKeyProvider>();

            if (keyProvider is null)
            {
                return HealthCheckResult.Unhealthy(
                    "Missing service: ITemporalKeyProvider is not registered. "
                    + "Ensure AddEncinaAuditMarten() is called.");
            }

            var result = await keyProvider.GetActiveKeysAsync(cancellationToken).ConfigureAwait(false);

            return result.Match(
                Right: keys =>
                {
                    var data = new Dictionary<string, object>
                    {
                        ["active_key_count"] = keys.Count,
                        ["provider_type"] = keyProvider.GetType().Name
                    };

                    return keys.Count > 0
                        ? HealthCheckResult.Healthy(
                            "Marten audit store is accessible with active temporal keys.",
                            data)
                        : HealthCheckResult.Degraded(
                            "Marten audit store is accessible but has no active temporal keys. "
                            + "New audit entries will create keys on demand.",
                            data: data);
                },
                Left: error => HealthCheckResult.Degraded(
                    $"Marten audit temporal key provider returned an error: {ErrorCode(error)}",
                    data: new Dictionary<string, object>
                    {
                        ["error"] = ErrorCode(error)
                    }));
        }
        catch (Exception ex)
        {
            // Only the exception type: the message and the exception object can carry personal data.
            return HealthCheckResult.Unhealthy(
                $"Marten audit health check failed with exception: {ex.GetType().Name}");
        }
    }

    // Only the error code: EncinaError.Message can carry personal data.
    private static string ErrorCode(EncinaError error) => error.GetCode().IfNone("encina.unknown");
}
