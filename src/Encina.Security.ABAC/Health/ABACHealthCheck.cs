using Encina.Security.ABAC.Persistence;
using Encina.Security.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

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
/// <item><description><see cref="HealthStatus.Degraded"/> — No policies or policy sets are loaded. Every request
/// that requires a policy is denied with <see cref="ABACErrors.RequiredPolicyNotFoundCode"/>.</description></item>
/// <item><description><see cref="HealthStatus.Unhealthy"/> — The PAP or the persistent store could not be queried
/// (e.g., connection error).</description></item>
/// </list>
/// </para>
/// <para>
/// When <see cref="ABACOptions.DecisionAudit"/> is enabled the result also reflects the decision audit,
/// and the worse status wins. There is no write probe: the check reads
/// <see cref="DecisionAudit.ABACDecisionAuditHealthState"/>, which the Policy Enforcement Point updates on every
/// audited decision. <see cref="HealthStatus.Unhealthy"/> — no <see cref="IOperationAuditStore"/> is registered,
/// or the last write failed within <see cref="DecisionAudit.ABACDecisionAuditOptions.HealthFailureWindow"/> under
/// <see cref="DecisionAudit.ABACDecisionAuditFailureMode.FailClosed"/>. <see cref="HealthStatus.Degraded"/> — the
/// last write failed under <see cref="DecisionAudit.ABACDecisionAuditFailureMode.BestEffort"/> or outside the
/// window (a successful write clears the failure), or the store is <see cref="InMemoryOperationAuditStore"/>.
/// The description and data carry fixed text and a state code only, never a subject or tenant identifier.
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
        var engine = await CheckPolicyEngineAsync(cancellationToken).ConfigureAwait(false);

        // ── Step 3: Decision audit (only when enabled) ───────────────────
        var audit = CheckDecisionAudit();
        return audit is null ? engine : Combine(engine, audit.Value);
    }

    // The worse status wins; both descriptions are kept, the engine's first.
    private static HealthCheckResult Combine(HealthCheckResult engine, HealthCheckResult audit)
    {
        var status = engine.Status < audit.Status ? engine.Status : audit.Status;
        var description = $"{engine.Description} {audit.Description}";
        var data = audit.Data.Count == 0 ? null : audit.Data;
        return new HealthCheckResult(status, description, exception: null, data);
    }

    /// <summary>
    /// Reads the decision audit state without a write probe: nothing is written to the store.
    /// </summary>
    /// <returns>
    /// <c>null</c> when <see cref="DecisionAudit.ABACDecisionAuditOptions.Enabled"/> is false (or no options are
    /// registered), otherwise the audit's own result. The description and data carry codes and fixed text only,
    /// never a subject, tenant or error message.
    /// </returns>
    private HealthCheckResult? CheckDecisionAudit()
    {
        try
        {
            var audit = _serviceProvider.GetService<IOptions<ABACOptions>>()?.Value.DecisionAudit;
            if (audit is not { Enabled: true })
            {
                return null;
            }

            return DescribeDecisionAudit(audit);
        }
#pragma warning disable CA1031 // Do not catch general exception types — health checks must not throw
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Unhealthy(
                $"Failed to read the decision audit state: {ex.GetType().Name}.");
        }
    }

    private HealthCheckResult DescribeDecisionAudit(DecisionAudit.ABACDecisionAuditOptions audit)
    {
        // The store is resolved lazily and only here, so a disabled audit needs none.
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetService<IOperationAuditStore>();

        if (store is null)
        {
            return Audit(HealthStatus.Unhealthy, "no_store",
                "Decision audit is enabled but no IOperationAuditStore is registered.");
        }

        // Fail closed: without the state the check cannot tell a failed write from a healthy trail.
        var state = _serviceProvider.GetService<DecisionAudit.ABACDecisionAuditHealthState>();
        if (state is null)
        {
            return Audit(HealthStatus.Unhealthy, "state_unavailable",
                "Decision audit is enabled but its health state is not registered, so write failures cannot be seen.");
        }

        return state.LastFailureAtUtc is { } failedAt
            ? DescribeFailedWrite(failedAt, audit)
            : DescribeStore(store);
    }

    private HealthCheckResult DescribeFailedWrite(DateTimeOffset failedAt, DecisionAudit.ABACDecisionAuditOptions audit)
    {
        var now = (_serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow();
        var blocking = now - failedAt <= audit.HealthFailureWindow
            && audit.FailureMode == DecisionAudit.ABACDecisionAuditFailureMode.FailClosed;

        return blocking
            ? Audit(HealthStatus.Unhealthy, "write_failed",
                "The last decision audit write failed; requests that would proceed are denied (FailClosed).")
            : Audit(HealthStatus.Degraded, "write_failed",
                "The last decision audit write failed and no write has succeeded since.");
    }

    private static HealthCheckResult DescribeStore(IOperationAuditStore store) =>
        store is InMemoryOperationAuditStore
            ? Audit(HealthStatus.Degraded, "in_memory_store",
                "Decision audit writes to InMemoryOperationAuditStore, which does not survive a restart.")
            : Audit(HealthStatus.Healthy, "ok", "Decision audit is recording.");

    private static HealthCheckResult Audit(HealthStatus status, string state, string description) =>
        new(status, description, exception: null, new Dictionary<string, object> { ["decision_audit"] = state });

    private async Task<HealthCheckResult> CheckPolicyEngineAsync(CancellationToken cancellationToken)
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
                "Every request that requires a policy is denied with encina.authorization.abac_policy_not_found. " +
                "Seed policies via ABACOptions.SeedPolicySets or ABACOptions.SeedPolicies.");
        }
#pragma warning disable CA1031 // Do not catch general exception types — health checks must not throw
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Unhealthy(
                $"Failed to query the Policy Administration Point: {ex.GetType().Name}.");
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
            var error = setCountResult.Match(Left: err => err.GetCode().IfNone("encina.unknown"), Right: _ => "Unknown error");
            return HealthCheckResult.Unhealthy(
                $"Persistent policy store connectivity check failed: {error}");
        }

        var policyCountResult = await policyStore.GetPolicyCountAsync(cancellationToken)
            .ConfigureAwait(false);

        if (policyCountResult.IsLeft)
        {
            var error = policyCountResult.Match(Left: err => err.GetCode().IfNone("encina.unknown"), Right: _ => "Unknown error");
            return HealthCheckResult.Unhealthy(
                $"Persistent policy store connectivity check failed: {error}");
        }

        // Store is reachable — continue to PAP policy check
        return null;
    }
}
