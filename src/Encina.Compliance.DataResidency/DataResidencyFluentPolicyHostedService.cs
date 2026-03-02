using Encina.Compliance.DataResidency.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Hosted service that creates fluent-configured residency policies in the store at startup.
/// </summary>
/// <remarks>
/// This service is only registered when fluent policies are configured via
/// <see cref="DataResidencyOptions.AddPolicy"/> and auto-registration from attributes is disabled.
/// When auto-registration is enabled, the <see cref="DataResidencyAutoRegistrationHostedService"/>
/// handles both attribute-discovered and fluent-configured policies.
/// </remarks>
internal sealed class DataResidencyFluentPolicyHostedService : IHostedService
{
    private readonly DataResidencyFluentPolicyDescriptor _descriptor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataResidencyFluentPolicyHostedService> _logger;

    public DataResidencyFluentPolicyHostedService(
        DataResidencyFluentPolicyDescriptor descriptor,
        IServiceScopeFactory scopeFactory,
        ILogger<DataResidencyFluentPolicyHostedService> logger)
    {
        _descriptor = descriptor;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_descriptor.Policies.Count == 0)
        {
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var policyStore = scope.ServiceProvider.GetRequiredService<IResidencyPolicyStore>();
        var policiesCreated = 0;

        foreach (var entry in _descriptor.Policies)
        {
            try
            {
                // Check if a policy already exists for this category
                var existingResult = await policyStore
                    .GetByCategoryAsync(entry.DataCategory, cancellationToken)
                    .ConfigureAwait(false);

                var policyExists = existingResult.Match(
                    Right: option => option.IsSome,
                    Left: _ => false);

                if (policyExists)
                {
                    _logger.LogDebug(
                        "Residency policy already exists for data category '{DataCategory}', skipping",
                        entry.DataCategory);
                    continue;
                }

                // Create the policy
                var policy = ResidencyPolicyDescriptor.Create(
                    dataCategory: entry.DataCategory,
                    allowedRegions: entry.AllowedRegions,
                    requireAdequacyDecision: entry.RequireAdequacyDecision,
                    allowedTransferBases: entry.AllowedTransferBases.Count > 0
                        ? entry.AllowedTransferBases
                        : null);

                var createResult = await policyStore
                    .CreateAsync(policy, cancellationToken)
                    .ConfigureAwait(false);

                createResult.Match(
                    Right: _ => policiesCreated++,
                    Left: error => _logger.LogWarning(
                        "Failed to create fluent residency policy for '{DataCategory}': {ErrorMessage}",
                        entry.DataCategory, error.Message));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to create fluent residency policy for '{DataCategory}'",
                    entry.DataCategory);
            }
        }

        _logger.LogInformation(
            "Data residency fluent policy registration completed: {PoliciesCreated} policies created",
            policiesCreated);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
