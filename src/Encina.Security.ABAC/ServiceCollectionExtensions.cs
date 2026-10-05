using Encina.Caching;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.EEL;
using Encina.Security.ABAC.Evaluation;
using Encina.Security.ABAC.Health;
using Encina.Security.ABAC.Persistence;
using Encina.Security.ABAC.Persistence.Xacml;
using Encina.Security.ABAC.Providers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Encina.Security.ABAC;

/// <summary>
/// Extension methods for configuring Encina ABAC (Attribute-Based Access Control) services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina ABAC services to the specified <see cref="IServiceCollection"/>,
    /// registering the full XACML 3.0 evaluation engine, combining algorithms,
    /// function registry, obligation handling, and pipeline behavior.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="ABACOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="ABACOptions"/> — Configured via the provided action</description></item>
    /// <item><description><see cref="IFunctionRegistry"/> → <see cref="DefaultFunctionRegistry"/> (Singleton)</description></item>
    /// <item><description><see cref="CombiningAlgorithmFactory"/> (Singleton)</description></item>
    /// <item><description><see cref="TargetEvaluator"/> (Singleton)</description></item>
    /// <item><description><see cref="ConditionEvaluator"/> (Singleton)</description></item>
    /// <item><description><see cref="IPolicyAdministrationPoint"/> → <see cref="InMemoryPolicyAdministrationPoint"/> or <see cref="PersistentPolicyAdministrationPoint"/> (Singleton)</description></item>
    /// <item><description><see cref="IPolicyDecisionPoint"/> → <see cref="XACMLPolicyDecisionPoint"/> (Singleton)</description></item>
    /// <item><description><see cref="IPolicyInformationPoint"/> → <see cref="DefaultPolicyInformationPoint"/> (Singleton)</description></item>
    /// <item><description><see cref="IAttributeProvider"/> → <see cref="DefaultAttributeProvider"/> (Scoped)</description></item>
    /// <item><description><see cref="ObligationExecutor"/> (Scoped)</description></item>
    /// <item><description><see cref="ABACPipelineBehavior{TRequest, TResponse}"/> (Transient)</description></item>
    /// </list>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a custom
    /// <see cref="IAttributeProvider"/> or <see cref="IPolicySerializer"/>.
    /// </para>
    /// <para>
    /// <b>Persistent PAP:</b>
    /// When <see cref="ABACOptions.UsePersistentPAP"/> is <c>true</c>, the
    /// <see cref="PersistentPolicyAdministrationPoint"/> is registered instead of the default
    /// <see cref="InMemoryPolicyAdministrationPoint"/>. This requires an <see cref="IPolicyStore"/>
    /// to be registered by a database provider package. Policy changes are attributed to the
    /// principal of the request context and refused without one; when an <c>IAuditStore</c> is
    /// registered (scoped or not), each change is audited fail closed in its own DI scope.
    /// </para>
    /// <para>
    /// <b>Policy seeding:</b>
    /// When <see cref="ABACOptions.SeedPolicySets"/> or <see cref="ABACOptions.SeedPolicies"/>
    /// contain entries, an <see cref="ABACPolicySeedingHostedService"/> is registered to seed
    /// them into the PAP at application startup.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup with defaults (in-memory PAP)
    /// services.AddEncinaABAC();
    ///
    /// // Full configuration with persistent PAP
    /// services.AddEncinaABAC(options =>
    /// {
    ///     options.EnforcementMode = ABACEnforcementMode.Block;
    ///     options.IncludeAdvice = true;
    ///     options.AddHealthCheck = true;
    ///
    ///     // Enable persistent PAP (requires IPolicyStore from a provider package)
    ///     options.UsePersistentPAP = true;
    ///
    ///     // Optional: enable policy caching
    ///     options.PolicyCaching.Enabled = true;
    ///     options.PolicyCaching.Duration = TimeSpan.FromMinutes(15);
    ///
    ///     // Register custom functions
    ///     options.AddFunction("custom:geo-distance", new GeoDistanceFunction());
    ///
    ///     // Seed policies at startup
    ///     options.SeedPolicySets.Add(myPolicySet);
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaABAC(
        this IServiceCollection services,
        Action<ABACOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // ── Configure options ──────────────────────────────────────
        services.Configure(configure ?? (_ => { }));

        // Create a temporary options instance for feature-gating decisions
        var optionsInstance = new ABACOptions();
        configure?.Invoke(optionsInstance);

        // Register the ambient request context accessor so the persistent PAP can resolve the
        // current actor for audit entries even when the host only wires Encina.Security.ABAC,
        // without the core mediator's AddEncina() (TryAdd is idempotent when both are called).
        services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();

        // ── Function registry (Singleton) ──────────────────────────
        // Register with factory so custom functions from options are loaded
        services.TryAddSingleton<IFunctionRegistry>(CreateFunctionRegistry);

        // ── Combining algorithms (Singleton) ───────────────────────
        services.TryAddSingleton<CombiningAlgorithmFactory>();

        // ── Evaluators (Singleton) ─────────────────────────────────
        services.TryAddSingleton<TargetEvaluator>();
        services.TryAddSingleton<ConditionEvaluator>();

        // ── Policy Administration Point (Singleton) ────────────────
        AddPolicyAdministrationPoint(services, optionsInstance);

        // ── Policy Decision Point (Singleton) ──────────────────────
        services.TryAddSingleton<IPolicyDecisionPoint, XACMLPolicyDecisionPoint>();

        // ── Policy Information Point (Singleton) ───────────────────
        services.TryAddSingleton<IPolicyInformationPoint, DefaultPolicyInformationPoint>();

        // ── Attribute Provider (Scoped — request-scoped attributes) ─
        services.TryAddScoped<IAttributeProvider, DefaultAttributeProvider>();

        // ── Obligation Executor (Scoped — uses scoped handlers) ────
        services.TryAddScoped<ObligationExecutor>();

        // ── Pipeline Behavior (Transient) ──────────────────────────
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ABACPipelineBehavior<,>)));

        // ── EEL Compiler (Singleton — IDisposable, disposed by container) ──
        services.TryAddSingleton<EELCompiler>();

        // ── Policy Seeding, Health Check & Expression Precompilation ──
        AddStartupServices(services, optionsInstance);

        return services;
    }

    private static DefaultFunctionRegistry CreateFunctionRegistry(IServiceProvider sp)
    {
        var registry = new DefaultFunctionRegistry();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ABACOptions>>().Value;

        foreach (var (functionId, function) in options.CustomFunctions)
        {
            registry.Register(functionId, function);
        }

        return registry;
    }

    private static void AddStartupServices(IServiceCollection services, ABACOptions optionsInstance)
    {
        if (optionsInstance.SeedPolicySets.Count > 0 || optionsInstance.SeedPolicies.Count > 0)
        {
            services.AddHostedService<ABACPolicySeedingHostedService>();
        }

        if (optionsInstance.ValidateExpressionsAtStartup && optionsInstance.ExpressionScanAssemblies.Count > 0)
        {
            services.AddHostedService<EELExpressionPrecompilationService>();
        }

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<ABACHealthCheck>(
                    ABACHealthCheck.DefaultName,
                    tags: ABACHealthCheck.Tags);
        }
    }

    private static void AddPolicyAdministrationPoint(IServiceCollection services, ABACOptions optionsInstance)
    {
        if (!optionsInstance.UsePersistentPAP)
        {
            services.TryAddSingleton<IPolicyAdministrationPoint, InMemoryPolicyAdministrationPoint>();
            return;
        }

        AddPolicySerializers(services, optionsInstance);

        // Register PersistentPAP with factory for startup validation.
        // Uses AddSingleton (not TryAdd) to override any prior InMemoryPAP registration.
        services.AddSingleton<IPolicyAdministrationPoint>(CreatePersistentPolicyAdministrationPoint);

        // ── Policy Cache PubSub Hosted Service ───────────────────
        // When PubSub invalidation is enabled, register a hosted service that
        // subscribes to the invalidation channel for cross-instance cache eviction.
        if (optionsInstance.PolicyCaching is { Enabled: true, EnablePubSubInvalidation: true })
        {
            services.AddHostedService<PolicyCachePubSubHostedService>(CreatePolicyCachePubSubHostedService);
        }
    }

    private static void AddPolicySerializers(IServiceCollection services, ABACOptions optionsInstance)
    {
        // Register serializer (TryAdd — allows user to register a custom serializer first)
        if (optionsInstance.UseXacmlXml)
        {
            // Use XACML 3.0 XML as the primary serializer
            services.TryAddSingleton<IPolicySerializer, XacmlXmlPolicySerializer>();
        }
        else
        {
            services.TryAddSingleton<IPolicySerializer, DefaultPolicySerializer>();
        }

        // Optionally register XACML XML serializer as a keyed service for import/export
        if (optionsInstance.RegisterXacmlXmlAsKeyed)
        {
            services.TryAddKeyedSingleton<IPolicySerializer, XacmlXmlPolicySerializer>("xacml-xml");
        }
    }

    private static PersistentPolicyAdministrationPoint CreatePersistentPolicyAdministrationPoint(IServiceProvider sp)
    {
        // The store is checked by registration, never resolved here: database stores are scoped and
        // this PAP is a singleton, so it resolves (and caches-wraps) the store per operation scope.
        var isService = sp.GetRequiredService<IServiceProviderIsService>();
        if (!isService.IsService(typeof(IPolicyStore)))
        {
            var startupLogger = sp.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(ServiceCollectionExtensions));

            startupLogger.LogCritical(
                "UsePersistentPAP is enabled but no IPolicyStore is registered. " +
                "Register a provider package (e.g., AddEncinaEntityFrameworkCore with UseABACPolicyStore = true)");

            throw new InvalidOperationException(
                "UsePersistentPAP is enabled but no IPolicyStore implementation is registered. " +
                "Register a provider package (e.g., services.AddEncinaEntityFrameworkCore(c => c.UseABACPolicyStore = true)).");
        }

        // The PAP takes the scope factory: it opens one scope per operation and resolves the
        // IPolicyStore (wrapped by ResolvePolicyStore) from it, and the IAuditStore from a second scope.
        return new PersistentPolicyAdministrationPoint(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<PersistentPolicyAdministrationPoint>>(),
            sp.GetService<IRequestContextAccessor>(),
            sp.GetService<TimeProvider>(),
            ResolvePolicyStore);
    }

    // ── Policy Caching (decorator wrapping) ──────────────
    // Resolves the policy store of one operation scope. When PolicyCaching.Enabled = true and an
    // ICacheProvider is available, the scoped inner IPolicyStore is wrapped with a
    // CachingPolicyStoreDecorator for cache-aside reads with stampede protection and write-through
    // invalidation. The decorator holds no per-request state; the cache and the pub/sub channel are
    // shared through the singleton providers.
    private static IPolicyStore ResolvePolicyStore(IServiceProvider scopedProvider)
    {
        var store = scopedProvider.GetRequiredService<IPolicyStore>();
        var resolvedOptions = scopedProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ABACOptions>>().Value;
        var cacheProvider = resolvedOptions.PolicyCaching.Enabled ? scopedProvider.GetService<ICacheProvider>() : null;
        if (cacheProvider is null)
        {
            return store;
        }

        return new CachingPolicyStoreDecorator(
            store,
            cacheProvider,
            scopedProvider.GetService<IPubSubProvider>(),
            resolvedOptions.PolicyCaching,
            scopedProvider.GetRequiredService<ILogger<CachingPolicyStoreDecorator>>(),
            scopedProvider.GetService<TimeProvider>() ?? TimeProvider.System);
    }

    private static PolicyCachePubSubHostedService CreatePolicyCachePubSubHostedService(IServiceProvider sp)
    {
        var resolvedOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ABACOptions>>().Value;

        return new PolicyCachePubSubHostedService(
            sp.GetRequiredService<ICacheProvider>(),
            sp.GetRequiredService<IPubSubProvider>(),
            resolvedOptions.PolicyCaching,
            sp.GetRequiredService<ILogger<PolicyCachePubSubHostedService>>());
    }
}
