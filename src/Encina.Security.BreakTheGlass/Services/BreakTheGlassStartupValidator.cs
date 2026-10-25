using System.Reflection;
using Encina.Security.Audit;
using Encina.Security.BreakTheGlass.Diagnostics;
using Encina.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Checks the break-the-glass configuration once when the host starts, so a misconfiguration fails the
/// start instead of denying (or silently weakening) an emergency access later.
/// </summary>
/// <remarks>
/// <para>
/// It fails the start, naming every problem, when: tenancy is registered (<c>ITenantProvider</c>) but
/// <see cref="BreakTheGlassOptions.RequireTenant"/> is <see langword="false"/> and
/// <see cref="BreakTheGlassOptions.AllowMissingTenantRequirement"/> is not set (an opt-out is logged);
/// a type marked <see cref="BreakTheGlassEligibleAttribute"/> in a scanned assembly names an undeclared
/// scope or a missing or unconvertible subject property; no <see cref="IOperationAuditStore"/> is
/// registered (the evidence of every emergency access needs one: there is no in-memory fallback); or the
/// in-memory elevation store runs outside the <c>Development</c> environment without
/// <see cref="BreakTheGlassOptions.AllowInMemoryStore"/> (when allowed, the use is logged). An
/// in-memory operation audit store only logs a warning.
/// </para>
/// <para>
/// With <see cref="BreakTheGlassOptions.Enabled"/> <see langword="false"/> it returns at once and resolves
/// nothing. It reads the final <see cref="IOptions{TOptions}"/> and resolves the stores from a scope of
/// its own because database stores are scoped.
/// </para>
/// </remarks>
internal sealed class BreakTheGlassStartupValidator : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<BreakTheGlassOptions> _options;
    private readonly BreakTheGlassScanTargets _targets;
    private readonly ILogger<BreakTheGlassStartupValidator> _logger;

    public BreakTheGlassStartupValidator(
        IServiceProvider serviceProvider,
        IServiceScopeFactory scopeFactory,
        IOptions<BreakTheGlassOptions> options,
        BreakTheGlassScanTargets targets,
        ILogger<BreakTheGlassStartupValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(logger);
        _serviceProvider = serviceProvider;
        _scopeFactory = scopeFactory;
        _options = options;
        _targets = targets;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            return Task.CompletedTask;
        }

        var failures = new List<string>();
        CheckTenant(options, failures);
        CheckEligibleTypes(options, failures);
        CheckStores(options, failures);

        if (failures.Count > 0)
        {
            BreakTheGlassLogMessages.StartupValidationFailed(_logger, failures.Count);
            throw new InvalidOperationException("Break-the-glass start-up validation failed: " + string.Join(" ", failures));
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void CheckTenant(BreakTheGlassOptions options, List<string> failures)
    {
        var tenancyRegistered = _serviceProvider.GetService<IServiceProviderIsService>()?.IsService(typeof(ITenantProvider)) == true;
        if (!tenancyRegistered || options.RequireTenant)
        {
            return;
        }

        if (options.AllowMissingTenantRequirement)
        {
            BreakTheGlassLogMessages.TenantRequirementOptedOut(_logger);
            return;
        }

        failures.Add(
            "Tenancy is registered (ITenantProvider) but BreakTheGlassOptions.RequireTenant is false: set RequireTenant to true, " +
            "or set AllowMissingTenantRequirement to accept elevations without a tenant.");
    }

    private void CheckEligibleTypes(BreakTheGlassOptions options, List<string> failures)
    {
        foreach (var assembly in _targets.Assemblies)
        {
            foreach (var type in EligibleTypes(assembly))
            {
                CheckEligibleType(type, options, failures);
            }
        }
    }

    private static IEnumerable<Type> EligibleTypes(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(static type => type is not null).ToArray()!;
        }

        return types.Where(static type => type.GetCustomAttribute<BreakTheGlassEligibleAttribute>(inherit: true) is not null);
    }

    private static void CheckEligibleType(Type type, BreakTheGlassOptions options, List<string> failures)
    {
        var attribute = type.GetCustomAttribute<BreakTheGlassEligibleAttribute>(inherit: true)!;
        if (!options.Scopes.ContainsKey(attribute.Scope))
        {
            failures.Add($"Request type '{type.Name}' is marked [BreakTheGlassEligible] for scope '{attribute.Scope}', which is not declared in BreakTheGlassOptions.Scopes.");
        }

        if (SubjectPropertyFailure(type, attribute) is { } failure)
        {
            failures.Add(failure);
        }
    }

    // The problem with the subject id property named by the attribute, or null when it is usable.
    private static string? SubjectPropertyFailure(Type type, BreakTheGlassEligibleAttribute attribute)
    {
        var property = string.IsNullOrWhiteSpace(attribute.SubjectIdProperty)
            ? null
            : type.GetProperty(attribute.SubjectIdProperty, BindingFlags.Public | BindingFlags.Instance);
        if (property is null || !property.CanRead || property.GetIndexParameters().Length != 0)
        {
            return $"Request type '{type.Name}' is marked [BreakTheGlassEligible] but names no readable public instance property in SubjectIdProperty.";
        }

        return BreakTheGlassSubjectIdConversion.IsSupportedType(property.PropertyType)
            ? null
            : $"The subject id property of request type '{type.Name}' has a type that cannot be converted to a subject id.";
    }

    private void CheckStores(BreakTheGlassOptions options, List<string> failures)
    {
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;

        var auditStore = provider.GetService<IOperationAuditStore>();
        if (auditStore is null)
        {
            BreakTheGlassLogMessages.AuditStoreMissing(_logger);
            failures.Add(
                "BreakTheGlass is enabled but no IOperationAuditStore is registered. Register an operation audit store " +
                "(for example through a provider package or AddEncinaAudit) or set BreakTheGlassOptions.Enabled to false.");
        }
        else if (auditStore is InMemoryOperationAuditStore)
        {
            BreakTheGlassLogMessages.InMemoryAuditStoreInUse(_logger);
        }

        if (provider.GetService<IBreakTheGlassStore>() is InMemoryBreakTheGlassStore)
        {
            CheckInMemoryStore(options, failures);
        }
    }

    private void CheckInMemoryStore(BreakTheGlassOptions options, List<string> failures)
    {
        var environment = _serviceProvider.GetService<IHostEnvironment>();
        if (environment is not null && environment.IsDevelopment())
        {
            BreakTheGlassLogMessages.InMemoryStoreInUse(_logger, environment.EnvironmentName);
            return;
        }

        if (options.AllowInMemoryStore)
        {
            BreakTheGlassLogMessages.InMemoryStoreInUse(_logger, environment?.EnvironmentName ?? "unknown");
            return;
        }

        failures.Add(
            "The in-memory break-the-glass store is registered outside the Development environment. Register a database " +
            "store through a provider package, or set BreakTheGlassOptions.AllowInMemoryStore (the use is logged).");
    }
}

/// <summary>
/// The assemblies the start-up validator scans for request types marked <see cref="BreakTheGlassEligibleAttribute"/>.
/// </summary>
/// <param name="Assemblies">The assemblies to scan.</param>
internal sealed record BreakTheGlassScanTargets(IReadOnlyList<Assembly> Assemblies);
