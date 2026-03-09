using Encina.Security.ABAC.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.Security.ABAC.Health;

/// <summary>
/// Health check that verifies the ABAC engine has at least one policy or policy set loaded
/// in the <see cref="IPolicyAdministrationPoint"/> and, when persistent PAP is enabled,
/// verifies connectivity to the underlying <see cref="IPolicyStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Returns the following statuses:
/// <list type="bullet">
/// <item><description><see cref="HealthStatus.Healthy"/> — At least one policy or policy set is loaded
/// (and the persistent store is reachable, if configured).</description></item>
/// <item><description><see cref="HealthStatus.Degraded"/> — No policies or policy sets are loaded. The ABAC engine
/// will return <see cref="Effect.NotApplicable"/> for all requests.</description></item>
/// <item><description><see cref="HealthStatus.Unhealthy"/> — The PAP or the persistent store could not be queried
/// (e.g., connection error).</description></item>
/// </list>
/// </para>
/// <para>
/// The <see cref="IPolicyStore"/> dependency is resolved optionally via
/// <see cref="IServiceProvider.GetService(Type)"/>. When no store is registered
/// (in-memory PAP mode), the store connectivity check is skipped.
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
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ABACHealthCheck"/> class.
    /// </summary>
    /// <param name="pap">The policy administration point to query for loaded policies.</param>
    /// <param name="serviceProvider">
    /// The service provider used to optionally resolve <see cref="IPolicyStore"/>
    /// for persistent store connectivity verification.
    /// </param>
    public ABACHealthCheck(IPolicyAdministrationPoint pap, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(pap);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _pap = pap;
        _serviceProvider = serviceProvider;
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
            // ── Step 1: Verify persistent store connectivity (if registered) ──

            var storeCheckResult = await CheckPolicyStoreConnectivityAsync(cancellationToken)
                .ConfigureAwait(false);

            if (storeCheckResult is not null)
            {
                return storeCheckResult.Value;
            }

            // ── Step 2: Verify PAP has loaded policies ───────────────────────

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

    /// <summary>
    /// Checks persistent store connectivity by calling
    /// <see cref="IPolicyStore.GetPolicySetCountAsync"/> and
    /// <see cref="IPolicyStore.GetPolicyCountAsync"/>.
    /// </summary>
    /// <returns>
    /// <c>null</c> if no <see cref="IPolicyStore"/> is registered (in-memory mode),
    /// or an <see cref="HealthCheckResult"/> if the store is registered and the check
    /// returned an unhealthy result.
    /// </returns>
    private async Task<HealthCheckResult?> CheckPolicyStoreConnectivityAsync(
        CancellationToken cancellationToken)
    {
        // Resolve IPolicyStore optionally — it is only registered when UsePersistentPAP = true
        using var scope = _serviceProvider.CreateScope();
        var policyStore = scope.ServiceProvider.GetService<IPolicyStore>();

        if (policyStore is null)
        {
            // In-memory mode — no store to check
            return null;
        }

        var setCountResult = await policyStore.GetPolicySetCountAsync(cancellationToken)
            .ConfigureAwait(false);

        // LanguageExt's Match<Ret> throws ResultIsNullException on null returns,
        // so we use IsLeft + IfLeft instead of Match with nullable strings.
        if (setCountResult.IsLeft)
        {
            var error = setCountResult.Match(Left: err => err.Message, Right: _ => "Unknown error");
            return HealthCheckResult.Unhealthy(
                $"Persistent policy store connectivity check failed: {error}");
        }

        var policyCountResult = await policyStore.GetPolicyCountAsync(cancellationToken)
            .ConfigureAwait(false);

        if (policyCountResult.IsLeft)
        {
            var error = policyCountResult.Match(Left: err => err.Message, Right: _ => "Unknown error");
            return HealthCheckResult.Unhealthy(
                $"Persistent policy store connectivity check failed: {error}");
        }

        // Store is reachable — continue to PAP policy check
        return null;
    }
}
