using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.EEL;
using Encina.Security.ABAC.Evaluation;
using Encina.Security.ABAC.Health;
using Encina.Security.ABAC.Providers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// <item><description><see cref="IPolicyAdministrationPoint"/> → <see cref="InMemoryPolicyAdministrationPoint"/> (Singleton)</description></item>
    /// <item><description><see cref="IPolicyDecisionPoint"/> → <see cref="XACMLPolicyDecisionPoint"/> (Singleton)</description></item>
    /// <item><description><see cref="IPolicyInformationPoint"/> → <see cref="DefaultPolicyInformationPoint"/> (Singleton)</description></item>
    /// <item><description><see cref="IAttributeProvider"/> → <see cref="DefaultAttributeProvider"/> (Scoped)</description></item>
    /// <item><description><see cref="ObligationExecutor"/> (Scoped)</description></item>
    /// <item><description><see cref="ABACPipelineBehavior{TRequest, TResponse}"/> (Transient)</description></item>
    /// </list>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a database-backed
    /// <see cref="IPolicyAdministrationPoint"/> or a custom <see cref="IAttributeProvider"/>.
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
    /// // Basic setup with defaults
    /// services.AddEncinaABAC();
    ///
    /// // Full configuration
    /// services.AddEncinaABAC(options =>
    /// {
    ///     options.EnforcementMode = ABACEnforcementMode.Block;
    ///     options.DefaultNotApplicableEffect = Effect.Deny;
    ///     options.IncludeAdvice = true;
    ///     options.FailOnMissingObligationHandler = true;
    ///     options.AddHealthCheck = true;
    ///
    ///     // Register custom functions
    ///     options.AddFunction("custom:geo-distance", new GeoDistanceFunction());
    ///
    ///     // Seed policies at startup
    ///     options.SeedPolicySets.Add(myPolicySet);
    /// });
    ///
    /// // With custom PAP (register before AddEncinaABAC)
    /// services.AddSingleton&lt;IPolicyAdministrationPoint, DatabasePolicyAdministrationPoint&gt;();
    /// services.AddEncinaABAC();
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
        services.TryAddSingleton<IPolicyAdministrationPoint, InMemoryPolicyAdministrationPoint>();

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
        var optionsInstance = new ABACOptions();
        configure?.Invoke(optionsInstance);

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
