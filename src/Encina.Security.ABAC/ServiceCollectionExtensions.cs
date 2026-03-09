using Encina.Caching;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.EEL;
using Encina.Security.ABAC.Evaluation;
using Encina.Security.ABAC.Health;
using Encina.Security.ABAC.Persistence;
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
    /// to be registered by a database provider package.
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
    ///     options.DefaultNotApplicableEffect = Effect.Deny;
    ///     options.IncludeAdvice = true;
    ///     options.FailOnMissingObligationHandler = true;
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
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<ABACOptions>(_ => { });
        }

        // Create a temporary options instance for feature-gating decisions
        var optionsInstance = new ABACOptions();
        configure?.Invoke(optionsInstance);

        // ── Function registry (Singleton) ──────────────────────────
        // Register with factory so custom functions from options are loaded
        services.TryAddSingleton<IFunctionRegistry>(sp =>
        {
            var registry = new DefaultFunctionRegistry();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ABACOptions>>().Value;

            foreach (var (functionId, function) in options.CustomFunctions)
            {
                registry.Register(functionId, function);
            }

            return registry;
        });

        // ── Combining algorithms (Singleton) ───────────────────────
        services.TryAddSingleton<CombiningAlgorithmFactory>();

        // ── Evaluators (Singleton) ─────────────────────────────────
        services.TryAddSingleton<TargetEvaluator>();
        services.TryAddSingleton<ConditionEvaluator>();

        // ── Policy Administration Point (Singleton) ────────────────
        if (optionsInstance.UsePersistentPAP)
        {
            // Register serializer (TryAdd — allows user to register a custom serializer first)
            services.TryAddSingleton<IPolicySerializer, DefaultPolicySerializer>();

            // Register PersistentPAP with factory for startup validation.
            // Uses AddSingleton (not TryAdd) to override any prior InMemoryPAP registration.
            services.AddSingleton<IPolicyAdministrationPoint>(sp =>
            {
                var store = sp.GetService<IPolicyStore>();
                if (store is null)
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

                // ── Policy Caching (decorator wrapping) ──────────────
                // When PolicyCaching.Enabled = true and an ICacheProvider is available,
                // wrap the inner IPolicyStore with CachingPolicyStoreDecorator for
                // cache-aside reads with stampede protection and write-through invalidation.
                var resolvedOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ABACOptions>>().Value;
                if (resolvedOptions.PolicyCaching.Enabled)
                {
                    var cacheProvider = sp.GetService<ICacheProvider>();
                    if (cacheProvider is not null)
                    {
                        var pubSubProvider = sp.GetService<IPubSubProvider>();
                        var cachingLogger = sp.GetRequiredService<ILogger<CachingPolicyStoreDecorator>>();

                        store = new CachingPolicyStoreDecorator(
                            store, cacheProvider, pubSubProvider,
                            resolvedOptions.PolicyCaching, cachingLogger);
                    }
                }

                var logger = sp.GetRequiredService<ILogger<PersistentPolicyAdministrationPoint>>();
                return new PersistentPolicyAdministrationPoint(store, logger);
            });

            // ── Policy Cache PubSub Hosted Service ───────────────────
            // When PubSub invalidation is enabled, register a hosted service that
            // subscribes to the invalidation channel for cross-instance cache eviction.
            if (optionsInstance.PolicyCaching is { Enabled: true, EnablePubSubInvalidation: true })
            {
                services.AddHostedService<PolicyCachePubSubHostedService>(sp =>
                {
                    var cacheProvider = sp.GetRequiredService<ICacheProvider>();
                    var pubSubProvider = sp.GetRequiredService<IPubSubProvider>();
                    var pubSubLogger = sp.GetRequiredService<ILogger<PolicyCachePubSubHostedService>>();
                    var resolvedOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ABACOptions>>().Value;

                    return new PolicyCachePubSubHostedService(
                        cacheProvider, pubSubProvider,
                        resolvedOptions.PolicyCaching, pubSubLogger);
                });
            }
        }
        else
        {
            services.TryAddSingleton<IPolicyAdministrationPoint, InMemoryPolicyAdministrationPoint>();
        }

        // ── Policy Decision Point (Singleton) ──────────────────────
        services.TryAddSingleton<IPolicyDecisionPoint, XACMLPolicyDecisionPoint>();

        // ── Policy Information Point (Singleton) ───────────────────
        services.TryAddSingleton<IPolicyInformationPoint, DefaultPolicyInformationPoint>();

        // ── Attribute Provider (Scoped — request-scoped attributes) ─
        services.TryAddScoped<IAttributeProvider, DefaultAttributeProvider>();

        // ── Obligation Executor (Scoped — uses scoped handlers) ────
        services.TryAddScoped<ObligationExecutor>();

        // ── Pipeline Behavior (Transient) ──────────────────────────
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(ABACPipelineBehavior<,>));

        // ── EEL Compiler (Singleton — IDisposable, disposed by container) ──
        services.TryAddSingleton<EELCompiler>();

        // ── Policy Seeding, Health Check & Expression Precompilation ──
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

        return services;
    }
}
