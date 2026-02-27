using Encina.Compliance.DataSubjectRights.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DataSubjectRights.Health;

/// <summary>
/// Health check that verifies Data Subject Rights infrastructure is properly configured
/// and monitors for overdue DSR requests.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The DSR options are configured</description></item>
/// <item><description>The DSR request store (<see cref="IDSRRequestStore"/>) is resolvable</description></item>
/// <item><description>The personal data locator (<see cref="IPersonalDataLocator"/>) is resolvable (optional)</description></item>
/// <item><description>The data erasure executor (<see cref="IDataErasureExecutor"/>) is resolvable (optional)</description></item>
/// <item><description>No overdue DSR requests exist (Degraded if any found)</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="DataSubjectRightsOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaDataSubjectRights(options =>
/// {
///     options.AddHealthCheck = true;
///     options.AssembliesToScan.Add(typeof(Program).Assembly);
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class DataSubjectRightsHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-dsr";

    private static readonly string[] DefaultTags = ["encina", "gdpr", "dsr", "compliance", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataSubjectRightsHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataSubjectRightsHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve DSR services.</param>
    /// <param name="logger">The logger instance.</param>
    public DataSubjectRightsHealthCheck(IServiceProvider serviceProvider, ILogger<DataSubjectRightsHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the DSR health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var warnings = new List<string>();

        using var scope = _serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // 1. Verify options are valid
        var options = scopedProvider.GetService<IOptions<DataSubjectRightsOptions>>()?.Value;
        if (options is null)
        {
            return HealthCheckResult.Unhealthy(
                "DataSubjectRightsOptions are not configured. Call AddEncinaDataSubjectRights() in DI setup.");
        }

        data["enforcementMode"] = options.RestrictionEnforcementMode.ToString();

        // 2. Verify DSR request store is resolvable
        var requestStore = scopedProvider.GetService<IDSRRequestStore>();
        if (requestStore is null)
        {
            return HealthCheckResult.Unhealthy(
                "IDSRRequestStore is not registered.",
                data: data);
        }

        data["requestStoreType"] = requestStore.GetType().Name;

        // 3. Check for overdue requests
        var overdueCount = 0;
        var overdueResult = await requestStore.GetOverdueRequestsAsync(cancellationToken).ConfigureAwait(false);

        overdueResult.Match(
            Right: overdueRequests =>
            {
                overdueCount = overdueRequests.Count;
                data["overdueRequestCount"] = overdueCount;

                if (overdueCount > 0)
                {
                    warnings.Add(
                        $"{overdueCount} overdue DSR request(s) found. "
                        + "Per GDPR Article 12(3), responses must be provided within one month.");
                }
            },
            Left: error =>
            {
                warnings.Add($"Failed to check for overdue requests: {error.Message}");
            });

        // 4. Verify personal data locator is resolvable (optional, degraded if missing)
        if (scopedProvider.GetService<IPersonalDataLocator>() is null)
        {
            warnings.Add("IPersonalDataLocator is not registered. "
                          + "Personal data discovery will not be available for access and portability requests.");
        }

        // 5. Verify data erasure executor is resolvable (optional, degraded if missing)
        if (scopedProvider.GetService<IDataErasureExecutor>() is null)
        {
            warnings.Add("IDataErasureExecutor is not registered. "
                          + "Right-to-erasure (Article 17) operations will not be available.");
        }

        _logger.DSRHealthCheckCompleted(
            warnings.Count == 0 ? "Healthy" : "Degraded",
            overdueCount);

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return HealthCheckResult.Degraded(
                $"DSR compliance is partially configured: {string.Join("; ", warnings)}",
                data: data);
        }

        return HealthCheckResult.Healthy(
            "DSR compliance infrastructure is fully configured.",
            data: data);
    }
}
