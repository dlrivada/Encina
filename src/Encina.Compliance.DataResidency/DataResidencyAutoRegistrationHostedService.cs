using System.Reflection;

using Encina.Compliance.DataResidency.Attributes;
using Encina.Compliance.DataResidency.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Hosted service that scans configured assemblies for <see cref="DataResidencyAttribute"/>
/// decorations at startup and creates matching <see cref="ResidencyPolicyDescriptor"/> entries in the store.
/// </summary>
/// <remarks>
/// <para>
/// This service runs once at application startup. It discovers all types
/// decorated with <see cref="DataResidencyAttribute"/> in the configured assemblies,
/// then creates corresponding <see cref="ResidencyPolicyDescriptor"/> entries in the
/// <see cref="IResidencyPolicyStore"/> for any data categories that don't already have policies.
/// </para>
/// <para>
/// Per GDPR Chapter V (Articles 44-49), controllers should establish explicit residency
/// policies for all categories of personal data subject to international transfer.
/// This auto-registration ensures that attribute-based residency declarations are
/// reflected in the policy store.
/// </para>
/// </remarks>
internal sealed class DataResidencyAutoRegistrationHostedService : IHostedService
{
    private readonly DataResidencyAutoRegistrationDescriptor _descriptor;
    private readonly DataResidencyFluentPolicyDescriptor? _fluentDescriptor;
    private readonly DataResidencyOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataResidencyAutoRegistrationHostedService> _logger;

    public DataResidencyAutoRegistrationHostedService(
        DataResidencyAutoRegistrationDescriptor descriptor,
        IOptions<DataResidencyOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<DataResidencyAutoRegistrationHostedService> logger,
        DataResidencyFluentPolicyDescriptor? fluentDescriptor = null)
    {
        _descriptor = descriptor;
        _fluentDescriptor = fluentDescriptor;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var hasAttributeWork = _options.AutoRegisterFromAttributes && _descriptor.Assemblies.Count > 0;
        var hasFluentWork = _fluentDescriptor is { Policies.Count: > 0 };

        if (!hasAttributeWork && !hasFluentWork)
        {
            _logger.LogDebug("Data residency auto-registration skipped: no assemblies to scan and no fluent policies");
            return;
        }

        var discoveredPolicies = hasAttributeWork
            ? DiscoverResidencyPolicies(_descriptor.Assemblies)
            : [];

        // Merge fluent-configured policies
        if (hasFluentWork)
        {
            foreach (var fluentPolicy in _fluentDescriptor!.Policies)
            {
                var existing = discoveredPolicies.Find(p =>
                    string.Equals(p.DataCategory, fluentPolicy.DataCategory, StringComparison.OrdinalIgnoreCase));

                if (existing is null)
                {
                    discoveredPolicies.Add(new DiscoveredResidencyPolicy(
                        fluentPolicy.DataCategory,
                        fluentPolicy.AllowedRegions.ToList(),
                        fluentPolicy.RequireAdequacyDecision,
                        fluentPolicy.AllowedTransferBases.ToList(),
                        "FluentConfiguration"));
                }
            }
        }

        if (discoveredPolicies.Count == 0)
        {
            _logger.LogDebug(
                "Data residency auto-registration completed: 0 policies found in {AssemblyCount} assemblies",
                _descriptor.Assemblies.Count);
            return;
        }

        var policiesCreated = await CreatePoliciesAsync(discoveredPolicies, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Data residency auto-registration completed: {PoliciesCreated} policies created from {AssemblyCount} assemblies",
            policiesCreated, _descriptor.Assemblies.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Scans assemblies for types decorated with <see cref="DataResidencyAttribute"/>
    /// and collects unique residency policy descriptors.
    /// </summary>
    private List<DiscoveredResidencyPolicy> DiscoverResidencyPolicies(IReadOnlyList<Assembly> assemblies)
    {
        var discovered = new Dictionary<string, DiscoveredResidencyPolicy>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttribute<DataResidencyAttribute>();
                if (attr is null)
                {
                    continue;
                }

                var dataCategory = attr.DataCategory ?? type.Name;
                var entityTypeName = type.FullName ?? type.Name;

                if (!discovered.ContainsKey(dataCategory))
                {
                    // Convert region codes to Region objects via the registry
                    var allowedRegions = attr.AllowedRegionCodes
                        .Select(code => RegionRegistry.GetByCode(code))
                        .Where(r => r is not null)
                        .Cast<Region>()
                        .Distinct()
                        .ToList();

                    discovered[dataCategory] = new DiscoveredResidencyPolicy(
                        dataCategory,
                        allowedRegions,
                        attr.RequireAdequacyDecision,
                        AllowedTransferBases: [],
                        entityTypeName);

                    _logger.LogDebug(
                        "Discovered data residency policy for '{EntityType}': category='{DataCategory}', "
                        + "regions={RegionCount}, requireAdequacy={RequireAdequacy}",
                        entityTypeName, dataCategory, allowedRegions.Count, attr.RequireAdequacyDecision);
                }
            }
        }

        return [.. discovered.Values];
    }

    /// <summary>
    /// Creates residency policies in the store for discovered data categories
    /// that don't already have policies.
    /// </summary>
    private async Task<int> CreatePoliciesAsync(
        List<DiscoveredResidencyPolicy> discoveredPolicies,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var policyStore = scope.ServiceProvider.GetRequiredService<IResidencyPolicyStore>();
        var policiesCreated = 0;

        foreach (var discovered in discoveredPolicies)
        {
            try
            {
                // Check if a policy already exists for this category
                var existingResult = await policyStore
                    .GetByCategoryAsync(discovered.DataCategory, cancellationToken)
                    .ConfigureAwait(false);

                var policyExists = existingResult.Match(
                    Right: option => option.IsSome,
                    Left: _ => false);

                if (policyExists)
                {
                    _logger.LogDebug(
                        "Residency policy already exists for data category '{DataCategory}', skipping",
                        discovered.DataCategory);
                    continue;
                }

                // Create the policy
                var policy = ResidencyPolicyDescriptor.Create(
                    dataCategory: discovered.DataCategory,
                    allowedRegions: discovered.AllowedRegions,
                    requireAdequacyDecision: discovered.RequireAdequacyDecision,
                    allowedTransferBases: discovered.AllowedTransferBases.Count > 0
                        ? discovered.AllowedTransferBases
                        : null);

                var createResult = await policyStore
                    .CreateAsync(policy, cancellationToken)
                    .ConfigureAwait(false);

                createResult.Match(
                    Right: _ => policiesCreated++,
                    Left: error => _logger.LogWarning(
                        "Failed to auto-register residency policy for '{DataCategory}': {ErrorMessage}",
                        discovered.DataCategory, error.Message));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to auto-register residency policy for '{DataCategory}'",
                    discovered.DataCategory);
            }
        }

        return policiesCreated;
    }

    /// <summary>
    /// Internal descriptor for a residency policy discovered from attributes.
    /// </summary>
    private sealed record DiscoveredResidencyPolicy(
        string DataCategory,
        IReadOnlyList<Region> AllowedRegions,
        bool RequireAdequacyDecision,
        IReadOnlyList<TransferLegalBasis> AllowedTransferBases,
        string SourceEntityType);
}
