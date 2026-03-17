using System.Reflection;

using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Diagnostics;
using Encina.Compliance.Retention.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Retention;

/// <summary>
/// Hosted service that scans configured assemblies for <see cref="RetentionPeriodAttribute"/>
/// decorations at startup and creates matching retention policies via the event-sourced service.
/// </summary>
/// <remarks>
/// <para>
/// This service runs once at application startup. It discovers all types and properties
/// decorated with <see cref="RetentionPeriodAttribute"/> in the configured assemblies,
/// then creates corresponding retention policies via <see cref="IRetentionPolicyService"/>
/// for any data categories that don't already have policies.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e), controllers should establish explicit retention periods
/// for all categories of personal data. This auto-registration ensures that attribute-based
/// retention declarations are reflected in the event-sourced policy aggregates.
/// </para>
/// </remarks>
internal sealed class RetentionAutoRegistrationHostedService : IHostedService
{
    private readonly RetentionAutoRegistrationDescriptor _descriptor;
    private readonly RetentionFluentPolicyDescriptor? _fluentDescriptor;
    private readonly RetentionOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetentionAutoRegistrationHostedService> _logger;

    public RetentionAutoRegistrationHostedService(
        RetentionAutoRegistrationDescriptor descriptor,
        IOptions<RetentionOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<RetentionAutoRegistrationHostedService> logger,
        RetentionFluentPolicyDescriptor? fluentDescriptor = null)
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
            _logger.RetentionAutoRegistrationSkipped();
            return;
        }

        var discoveredPolicies = hasAttributeWork
            ? DiscoverRetentionPolicies(_descriptor.Assemblies)
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
                    discoveredPolicies.Add(new DiscoveredRetentionPolicy(
                        fluentPolicy.DataCategory,
                        fluentPolicy.RetentionPeriod,
                        fluentPolicy.AutoDelete,
                        fluentPolicy.Reason ?? fluentPolicy.LegalBasis ?? "Configured via AddPolicy() fluent API",
                        "FluentConfiguration"));
                }
            }
        }

        if (discoveredPolicies.Count == 0)
        {
            _logger.RetentionAutoRegistrationCompleted(0, _descriptor.Assemblies.Count);
            return;
        }

        var policiesCreated = await CreatePoliciesAsync(discoveredPolicies, cancellationToken)
            .ConfigureAwait(false);

        _logger.RetentionAutoRegistrationCompleted(policiesCreated, _descriptor.Assemblies.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Scans assemblies for types and properties decorated with <see cref="RetentionPeriodAttribute"/>
    /// and collects unique retention policy descriptors.
    /// </summary>
    /// <returns>A list of discovered retention policy descriptors (unique by data category).</returns>
    private List<DiscoveredRetentionPolicy> DiscoverRetentionPolicies(IReadOnlyList<Assembly> assemblies)
    {
        var discovered = new Dictionary<string, DiscoveredRetentionPolicy>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                // Check class-level attribute
                var classAttr = type.GetCustomAttribute<RetentionPeriodAttribute>();
                if (classAttr is not null)
                {
                    var dataCategory = classAttr.DataCategory ?? type.Name;
                    var entityTypeName = type.FullName ?? type.Name;

                    if (!discovered.ContainsKey(dataCategory))
                    {
                        discovered[dataCategory] = new DiscoveredRetentionPolicy(
                            dataCategory,
                            classAttr.RetentionPeriod,
                            classAttr.AutoDelete,
                            classAttr.Reason,
                            entityTypeName);

                        _logger.RetentionPolicyDiscovered(entityTypeName, dataCategory, classAttr.RetentionPeriod);
                    }
                }

                // Check property-level attributes
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var propAttr = property.GetCustomAttribute<RetentionPeriodAttribute>();
                    if (propAttr is null)
                    {
                        continue;
                    }

                    var propCategory = propAttr.DataCategory ?? $"{type.Name}.{property.Name}";
                    var propEntityType = type.FullName ?? type.Name;

                    if (!discovered.ContainsKey(propCategory))
                    {
                        discovered[propCategory] = new DiscoveredRetentionPolicy(
                            propCategory,
                            propAttr.RetentionPeriod,
                            propAttr.AutoDelete,
                            propAttr.Reason,
                            propEntityType);

                        _logger.RetentionPolicyDiscovered(propEntityType, propCategory, propAttr.RetentionPeriod);
                    }
                }
            }
        }

        return [.. discovered.Values];
    }

    /// <summary>
    /// Creates retention policies via the event-sourced service for discovered data categories
    /// that don't already have policies.
    /// </summary>
    /// <returns>The number of policies created.</returns>
    private async Task<int> CreatePoliciesAsync(
        List<DiscoveredRetentionPolicy> discoveredPolicies,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var policyService = scope.ServiceProvider.GetRequiredService<IRetentionPolicyService>();
        var policiesCreated = 0;

        foreach (var discovered in discoveredPolicies)
        {
            try
            {
                // Check if a policy already exists for this category
                var existingResult = await policyService
                    .GetPolicyByCategoryAsync(discovered.DataCategory, cancellationToken)
                    .ConfigureAwait(false);

                // If the query succeeded (Right), a policy exists; if it returned a not-found error, we can create
                var policyExists = existingResult.IsRight;

                if (policyExists)
                {
                    _logger.RetentionPolicyAlreadyExists(discovered.DataCategory);
                    continue;
                }

                // Create the policy via the event-sourced service
                var createResult = await policyService
                    .CreatePolicyAsync(
                        dataCategory: discovered.DataCategory,
                        retentionPeriod: discovered.RetentionPeriod,
                        autoDelete: discovered.AutoDelete,
                        policyType: RetentionPolicyType.TimeBased,
                        reason: discovered.Reason ?? "Auto-registered from [RetentionPeriod] attribute",
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                createResult.Match(
                    Right: _ => policiesCreated++,
                    Left: error => _logger.RetentionAutoRegistrationPolicyFailed(
                        discovered.DataCategory,
                        new InvalidOperationException(error.Message)));
            }
            catch (Exception ex)
            {
                _logger.RetentionAutoRegistrationPolicyFailed(discovered.DataCategory, ex);
            }
        }

        return policiesCreated;
    }

    /// <summary>
    /// Internal descriptor for a retention policy discovered from attributes.
    /// </summary>
    private sealed record DiscoveredRetentionPolicy(
        string DataCategory,
        TimeSpan RetentionPeriod,
        bool AutoDelete,
        string? Reason,
        string SourceEntityType);
}
