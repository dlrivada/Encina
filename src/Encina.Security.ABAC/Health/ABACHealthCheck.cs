using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.Security.ABAC.Health;

/// <summary>
/// Health check that verifies the ABAC engine has at least one policy or policy set loaded
/// in the <see cref="IPolicyAdministrationPoint"/>.
/// </summary>
/// <remarks>
/// <para>
/// Returns the following statuses:
/// <list type="bullet">
/// <item><description><see cref="HealthStatus.Healthy"/> — At least one policy or policy set is loaded.</description></item>
/// <item><description><see cref="HealthStatus.Degraded"/> — No policies or policy sets are loaded. The ABAC engine
/// will return <see cref="Effect.NotApplicable"/> for all requests.</description></item>
/// <item><description><see cref="HealthStatus.Unhealthy"/> — The PAP could not be queried (e.g., connection error).</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="ABACOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaABAC(options => options.AddHealthCheck = true);
/// </code>
/// </para>
/// </remarks>
public sealed class ABACHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-abac";

    private static readonly string[] DefaultTags = ["encina", "security", "abac", "ready"];

    private readonly IPolicyAdministrationPoint _pap;

    /// <summary>
    /// Initializes a new instance of the <see cref="ABACHealthCheck"/> class.
    /// </summary>
    /// <param name="pap">The policy administration point to query for loaded policies.</param>
    public ABACHealthCheck(IPolicyAdministrationPoint pap)
    {
        ArgumentNullException.ThrowIfNull(pap);
        _pap = pap;
    }

    /// <summary>
    /// Gets the default tags for the ABAC health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policySetsResult = await _pap.GetPolicySetsAsync(cancellationToken)
                .ConfigureAwait(false);

            var hasPolicySets = policySetsResult.Match(
                Left: _ => false,
                Right: sets => sets.Count > 0);

            if (hasPolicySets)
            {
                return HealthCheckResult.Healthy("ABAC engine has loaded policy sets.");
            }

            var policiesResult = await _pap.GetPoliciesAsync(null, cancellationToken)
                .ConfigureAwait(false);

            var hasPolicies = policiesResult.Match(
                Left: _ => false,
                Right: policies => policies.Count > 0);

            if (hasPolicies)
            {
                return HealthCheckResult.Healthy("ABAC engine has loaded standalone policies.");
            }

            return HealthCheckResult.Degraded(
                "No policies or policy sets loaded. " +
                "The ABAC engine will return NotApplicable for all requests. " +
                "Seed policies via ABACOptions.SeedPolicySets or ABACOptions.SeedPolicies.");
        }
#pragma warning disable CA1031 // Do not catch general exception types — health checks must not throw
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Unhealthy(
                "Failed to query the Policy Administration Point.",
                exception: ex);
        }
    }
}
