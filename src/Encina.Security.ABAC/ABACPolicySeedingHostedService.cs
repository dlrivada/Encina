using LanguageExt;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.ABAC;

/// <summary>
/// Hosted service that seeds policy sets and standalone policies into the
/// <see cref="IPolicyAdministrationPoint"/> at application startup.
/// </summary>
/// <remarks>
/// <para>
/// This service runs once during application startup. It reads the seed lists from
/// <see cref="ABACOptions.SeedPolicySets"/> and <see cref="ABACOptions.SeedPolicies"/>,
/// and adds them to the PAP. Duplicate IDs are logged as warnings and skipped; any other failure
/// (for example an audit-store or policy-store failure) fails startup with an
/// <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// Seeding runs under the built-in service identity <see cref="ServiceIdentityName"/>, opened
/// through the internal scope API (core EventId 166 logs the opening), so the persistent PAP
/// records <c>service:encina.abac.policy-seeding</c> as the actor of every seeded change. When the
/// scope cannot be opened, a cancelled start surfaces as <see cref="OperationCanceledException"/>
/// and any other refusal as <see cref="InvalidOperationException"/> carrying the error code only.
/// EventId 9096 logs the summary (seeded and total counts).
/// </para>
/// <para>
/// The service is automatically registered when either seed list contains entries
/// in the <see cref="ServiceCollectionExtensions.AddEncinaABAC"/> method, together with the
/// declaration of its built-in service identity.
/// </para>
/// </remarks>
internal sealed partial class ABACPolicySeedingHostedService : IHostedService
{
    /// <summary>
    /// The built-in service identity that policy seeding runs under (subject
    /// <c>service:encina.abac.policy-seeding</c>). Only Encina packages can open it.
    /// </summary>
    internal const string ServiceIdentityName = "encina.abac.policy-seeding";

    private readonly IPolicyAdministrationPoint _pap;
    private readonly IInternalRequestContextScopeFactory _scopes;
    private readonly ABACOptions _options;
    private readonly ILogger<ABACPolicySeedingHostedService> _logger;

    public ABACPolicySeedingHostedService(
        IPolicyAdministrationPoint pap,
        IInternalRequestContextScopeFactory scopes,
        IOptions<ABACOptions> options,
        ILogger<ABACPolicySeedingHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(pap);
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _pap = pap;
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var policySets = _options.SeedPolicySets;
        var policies = _options.SeedPolicies;

        if (policySets.Count == 0 && policies.Count == 0)
        {
            _logger.LogDebug("No ABAC policies to seed; skipping");
            return;
        }

        var outcome = await _scopes.RunAsBuiltInAsync(
            ServiceIdentityName,
            (_, token) => SeedAllAsync(policySets, policies, token),
            cancellationToken).ConfigureAwait(false);

        var summary = outcome.Match(Right: seeded => seeded, Left: ToStartupFailure);

        LogSeedingCompleted(_logger, summary.PolicySets, policySets.Count, summary.Policies, policies.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<Either<EncinaError, SeedingSummary>> SeedAllAsync(
        IEnumerable<PolicySet> policySets,
        IEnumerable<Policy> policies,
        CancellationToken cancellationToken)
    {
        var seededSets = await SeedPolicySetsAsync(policySets, cancellationToken).ConfigureAwait(false);
        var seededPolicies = await SeedPoliciesAsync(policies, cancellationToken).ConfigureAwait(false);

        return new SeedingSummary(seededSets, seededPolicies);
    }

    // The scope was refused (decision N7 of #1705): a cancelled start is a cancellation, not a
    // misconfiguration; any other refusal fails startup with the error code only, never the message.
    private static SeedingSummary ToStartupFailure(EncinaError error)
    {
        var code = error.GetCode().IfNone("encina.unknown");
        if (string.Equals(code, EncinaErrorCodes.RequestCancelled, StringComparison.Ordinal))
        {
            throw new OperationCanceledException("ABAC policy seeding was cancelled before it started.");
        }

        throw new InvalidOperationException(
            $"ABAC policy seeding could not open its service identity scope (error code '{code}'); the application cannot start without its seeded policies.");
    }

    private async ValueTask<int> SeedPolicySetsAsync(IEnumerable<PolicySet> policySets, CancellationToken cancellationToken)
    {
        var seeded = 0;
        foreach (var policySet in policySets)
        {
            if (await SeedAsync(
                    () => _pap.AddPolicySetAsync(policySet, cancellationToken),
                    ABACErrors.DuplicatePolicySetCode,
                    "policy set",
                    policySet.Id,
                    cancellationToken).ConfigureAwait(false))
            {
                seeded++;
                _logger.LogDebug("Seeded policy set '{PolicySetId}'", policySet.Id);
            }
        }

        return seeded;
    }

    private async ValueTask<int> SeedPoliciesAsync(IEnumerable<Policy> policies, CancellationToken cancellationToken)
    {
        var seeded = 0;
        foreach (var policy in policies)
        {
            if (await SeedAsync(
                    () => _pap.AddPolicyAsync(policy, parentPolicySetId: null, cancellationToken),
                    ABACErrors.DuplicatePolicyCode,
                    "standalone policy",
                    policy.Id,
                    cancellationToken).ConfigureAwait(false))
            {
                seeded++;
                _logger.LogDebug("Seeded standalone policy '{PolicyId}'", policy.Id);
            }
        }

        return seeded;
    }

    /// <summary>
    /// Returns <c>true</c> when the seed was applied, <c>false</c> when it already existed (logged
    /// and skipped), and throws for any other failure so the host never starts with a partial
    /// policy set. An exception thrown by the PAP is wrapped the same way. The message carries the
    /// id and the error code or exception type, never the error message.
    /// </summary>
    private async ValueTask<bool> SeedAsync(
        Func<ValueTask<Either<EncinaError, Unit>>> add,
        string duplicateCode,
        string kind,
        string id,
        CancellationToken cancellationToken)
    {
        Either<EncinaError, Unit> result;
        try
        {
            result = await add().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                $"ABAC seeding failed for {kind} '{id}' with exception '{ex.GetType().Name}'; the application cannot start with a partial policy set.",
                ex);
        }

        return IsAccepted(result, duplicateCode, kind, id);
    }

    private bool IsAccepted(
        Either<EncinaError, Unit> result,
        string duplicateCode,
        string kind,
        string id)
    {
        if (result.IsRight)
        {
            return true;
        }

        var errorCode = result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone("encina.unknown"));

        if (errorCode == duplicateCode)
        {
            _logger.LogWarning("Failed to seed {Kind} '{SeedId}': {ErrorCode}", kind, id, errorCode);
            return false;
        }

        throw new InvalidOperationException(
            $"ABAC seeding failed for {kind} '{id}' with error code '{errorCode}'; the application cannot start with a partial policy set.");
    }

    // EventId 9096 (see EventIdRanges.SecurityABAC): the seeding summary. Counts only.
    [LoggerMessage(
        EventId = 9096,
        Level = LogLevel.Information,
        Message = "ABAC policy seeding completed under the built-in service identity: {SeededPolicySets}/{TotalPolicySets} policy set(s), {SeededPolicies}/{TotalPolicies} standalone policy(ies)")]
    private static partial void LogSeedingCompleted(
        ILogger logger,
        int seededPolicySets,
        int totalPolicySets,
        int seededPolicies,
        int totalPolicies);

    private readonly record struct SeedingSummary(int PolicySets, int Policies);
}
