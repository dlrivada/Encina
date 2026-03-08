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
/// and adds them to the PAP. Duplicate IDs are logged as warnings and skipped.
/// </para>
/// <para>
/// The service is automatically registered when either seed list contains entries
/// in the <see cref="ServiceCollectionExtensions.AddEncinaABAC"/> method.
/// </para>
/// </remarks>
internal sealed class ABACPolicySeedingHostedService : IHostedService
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

        // ── Seed policy sets ───────────────────────────────────────
        var seededSets = 0;
        foreach (var policySet in policySets)
        {
            var result = await _pap.AddPolicySetAsync(policySet, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Left: error => _logger.LogWarning(
                    "Failed to seed policy set '{PolicySetId}': {ErrorMessage}",
                    policySet.Id,
                    error.Message),
                Right: _ =>
                {
                    seededSets++;
                    _logger.LogDebug("Seeded policy set '{PolicySetId}'", policySet.Id);
                });
        }

        // ── Seed standalone policies ───────────────────────────────
        var seededPolicies = 0;
        foreach (var policy in policies)
        {
            var result = await _pap.AddPolicyAsync(policy, parentPolicySetId: null, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Left: error => _logger.LogWarning(
                    "Failed to seed standalone policy '{PolicyId}': {ErrorMessage}",
                    policy.Id,
                    error.Message),
                Right: _ =>
                {
                    seededPolicies++;
                    _logger.LogDebug("Seeded standalone policy '{PolicyId}'", policy.Id);
                });
        }

        _logger.LogInformation(
            "ABAC policy seeding completed: {SeededSets}/{TotalSets} policy set(s), {SeededPolicies}/{TotalPolicies} standalone policy(ies)",
            seededSets,
            policySets.Count,
            seededPolicies,
            policies.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
