using Encina.Security.ABAC.Administration;
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
/// The service is automatically registered when either seed list contains entries
/// in the <see cref="ServiceCollectionExtensions.AddEncinaABAC"/> method.
/// </para>
/// </remarks>
internal sealed partial class ABACPolicySeedingHostedService : IHostedService
{
    private readonly IPolicyAdministrationPoint _pap;
    private readonly ABACOptions _options;
    private readonly ILogger<ABACPolicySeedingHostedService> _logger;

    public ABACPolicySeedingHostedService(
        IPolicyAdministrationPoint pap,
        IOptions<ABACOptions> options,
        ILogger<ABACPolicySeedingHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(pap);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _pap = pap;
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

        _logger.LogInformation(
            "Seeding ABAC policies: {PolicySetCount} policy set(s), {PolicyCount} standalone policy(ies)",
            policySets.Count,
            policies.Count);

        // Startup seeding has no request principal: it runs inside an explicit system-actor
        // scope, logged here, so the persistent PAP records the system actor instead of
        // refusing the change (and never attributes it silently).
        LogSystemActorScopeOpened(_logger);
        using var systemActor = PolicyChangeActorScope.BeginSystemActor();

        // ── Seed policy sets ───────────────────────────────────────
        var seededSets = 0;
        foreach (var policySet in policySets)
        {
            if (await SeedAsync(
                    () => _pap.AddPolicySetAsync(policySet, cancellationToken),
                    ABACErrors.DuplicatePolicySetCode,
                    "policy set",
                    policySet.Id,
                    cancellationToken))
            {
                seededSets++;
                _logger.LogDebug("Seeded policy set '{PolicySetId}'", policySet.Id);
            }
        }

        // ── Seed standalone policies ───────────────────────────────
        var seededPolicies = 0;
        foreach (var policy in policies)
        {
            if (await SeedAsync(
                    () => _pap.AddPolicyAsync(policy, parentPolicySetId: null, cancellationToken),
                    ABACErrors.DuplicatePolicyCode,
                    "standalone policy",
                    policy.Id,
                    cancellationToken))
            {
                seededPolicies++;
                _logger.LogDebug("Seeded standalone policy '{PolicyId}'", policy.Id);
            }
        }

        _logger.LogInformation(
            "ABAC policy seeding completed: {SeededSets}/{TotalSets} policy set(s), {SeededPolicies}/{TotalPolicies} standalone policy(ies)",
            seededSets,
            policySets.Count,
            seededPolicies,
            policies.Count);
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
        var errorCode =result.Match(Right: _ => (string?)null, Left: e => e.GetCode().IfNone("encina.unknown"));
        if (errorCode is null)
        {
            return true;
        }

        if (errorCode == duplicateCode)
        {
            _logger.LogWarning("Failed to seed {Kind} '{SeedId}': {ErrorCode}", kind, id, errorCode);
            return false;
        }

        throw new InvalidOperationException(
            $"ABAC seeding failed for {kind} '{id}' with error code '{errorCode}'; the application cannot start with a partial policy set.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = 9096,
        Level = LogLevel.Information,
        Message = "System actor scope opened for ABAC policy seeding; policy changes are recorded as made by the system actor")]
    private static partial void LogSystemActorScopeOpened(ILogger logger);
}
