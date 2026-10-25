using System.Reflection;
using Encina.Security.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Service registration of the break-the-glass package.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds break-the-glass emergency access: options with start-up validation, the access gate, the
    /// service, the expiry sweeper, the opt-in purge and, when requested, the health check.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures <see cref="BreakTheGlassOptions"/> (scopes, reviewer roles, limits).</param>
    /// <param name="assembliesToScan">
    /// The assemblies the start-up check scans for request types marked <see cref="BreakTheGlassEligibleAttribute"/>;
    /// the entry assembly (or the calling assembly when there is none) when none is given.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Prerequisites: <c>AddEncina</c> (the service publishes notifications through <c>IEncina</c> and reads the
    /// request context) and an <see cref="IOperationAuditStore"/>. No audit store is registered here: a
    /// forgotten registration fails the start with a message that names it, and an in-memory store only warns.
    /// </para>
    /// <para>
    /// The in-memory elevation store is registered with <c>TryAdd</c>; a database provider store wins in any
    /// registration order. The access gate replaces the core default (which keeps every denial) but never an
    /// application's own <see cref="IAccessElevationGate"/>. The expiry sweeper runs even when
    /// <see cref="BreakTheGlassOptions.Enabled"/> is <see langword="false"/>; the purge runs only when
    /// <see cref="BreakTheGlassOptions.EnableAutoPurge"/> is set.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncina(typeof(Program).Assembly);
    /// services.AddEncinaBreakTheGlass(options =>
    /// {
    ///     options.RequireTenant = true;
    ///     options.ReviewerRoles.Add("clinical-director");
    ///     options.DeclareScope("patient-record", scope => scope.OpenerRoles.Add("psychologist"));
    /// }, typeof(Program).Assembly);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddEncinaBreakTheGlass(
        this IServiceCollection services,
        Action<BreakTheGlassOptions> configure,
        params Assembly[] assembliesToScan)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(assembliesToScan);

        // The registration-time flags are read from a throwaway instance (the options are built later).
        var flags = new BreakTheGlassOptions();
        configure(flags);

        AddOptions(services, configure);
        AddCoreServices(services);
        AddGate(services);
        AddBackgroundServices(services, flags, ScanTargets(assembliesToScan, Assembly.GetCallingAssembly()));

        if (flags.AddHealthCheck)
        {
            services.AddHealthChecks().AddCheck<Health.BreakTheGlassHealthCheck>(
                Health.BreakTheGlassHealthCheck.DefaultName,
                tags: Health.BreakTheGlassHealthCheck.Tags);
        }

        return services;
    }

    private static void AddOptions(IServiceCollection services, Action<BreakTheGlassOptions> configure)
    {
        services.AddOptions<BreakTheGlassOptions>().Configure(configure).ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<BreakTheGlassOptions>, BreakTheGlassOptionsValidator>());
    }

    private static void AddCoreServices(IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        // By implementation type, so a provider's RemoveInMemoryDefault helper finds and replaces it.
        services.TryAddSingleton<IBreakTheGlassStore, InMemoryBreakTheGlassStore>();

        // The gate writes the read-audit purpose; only the context is needed, not the read-audit store.
        services.TryAddScoped<IReadAuditContext, ReadAuditContext>();
        services.TryAddScoped<BreakTheGlassRequestGrants>();
        services.TryAddScoped<IBreakTheGlassOpenAuthorizer, RoleBreakTheGlassAuthorizer>();
        services.TryAddSingleton<BreakTheGlassRequestValidator>();
        services.TryAddScoped<IBreakTheGlassService, DefaultBreakTheGlassService>();
    }

    // The core default keeps every denial. It is removed only when it is the default (by type), so an
    // application's own gate is never overridden, and the TryAdd leaves a gate registered earlier alone,
    // which makes the result independent of the order of AddEncina and AddEncinaBreakTheGlass.
    private static void AddGate(IServiceCollection services)
    {
        foreach (var descriptor in services.Where(static d => d.ServiceType == typeof(IAccessElevationGate) && d.ImplementationType == typeof(NoAccessElevationGate)).ToList())
        {
            services.Remove(descriptor);
        }

        services.TryAddScoped<IAccessElevationGate, BreakTheGlassAccessGate>();
    }

    private static void AddBackgroundServices(IServiceCollection services, BreakTheGlassOptions flags, BreakTheGlassScanTargets targets)
    {
        // The sweeper runs under a built-in service identity; it also registers the request identity services.
        services.AddBuiltInServiceIdentity(BreakTheGlassExpiryService.ServiceIdentityName);
        services.TryAddSingleton(targets);
        services.TryAddSingleton<BreakTheGlassSweepState>();

        // The validator is registered first, so a misconfiguration fails the start before the sweeper runs.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, BreakTheGlassStartupValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, BreakTheGlassExpiryService>());

        if (flags.EnableAutoPurge)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, BreakTheGlassPurgeService>());
        }
    }

    private static BreakTheGlassScanTargets ScanTargets(Assembly[] assembliesToScan, Assembly callingAssembly) =>
        new(assembliesToScan.Length > 0 ? assembliesToScan : [Assembly.GetEntryAssembly() ?? callingAssembly]);
}
