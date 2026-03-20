using System.Reflection;

using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.AIAct;

/// <summary>
/// Extension methods for configuring Encina AI Act compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina AI Act compliance services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="AIActOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="AIActOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="IAISystemRegistry"/> → <see cref="InMemoryAISystemRegistry"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IAIActClassifier"/> → <see cref="DefaultAIActClassifier"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IHumanOversightEnforcer"/> → <see cref="DefaultHumanOversightEnforcer"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IDataQualityValidator"/> → <see cref="DefaultDataQualityValidator"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IAIActDocumentation"/> → <see cref="DefaultAIActDocumentation"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IAIActComplianceValidator"/> → <see cref="DefaultAIActComplianceValidator"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="AIActCompliancePipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a database-backed
    /// <see cref="IAISystemRegistry"/> or a custom <see cref="IAIActComplianceValidator"/>.
    /// </para>
    /// <para>
    /// <b>Auto-registration:</b>
    /// When <see cref="AIActOptions.AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the specified assemblies are scanned for <see cref="Attributes.HighRiskAIAttribute"/>
    /// decorations and systems are auto-registered at startup.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaAIAct(options =>
    /// {
    ///     options.EnforcementMode = AIActEnforcementMode.Block;
    ///     options.AssembliesToScan.Add(typeof(Program).Assembly);
    /// });
    ///
    /// // With custom implementations (register before AddEncinaAIAct)
    /// services.AddSingleton&lt;IAISystemRegistry, DatabaseAISystemRegistry&gt;();
    /// services.AddEncinaAIAct(options =>
    /// {
    ///     options.AutoRegisterFromAttributes = false;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaAIAct(
        this IServiceCollection services,
        Action<AIActOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<AIActOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<AIActOptions>, AIActOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default implementations (TryAdd allows override)
        services.TryAddSingleton<IAISystemRegistry, InMemoryAISystemRegistry>();
        services.TryAddSingleton<IAIActClassifier, DefaultAIActClassifier>();
        services.TryAddSingleton<IHumanOversightEnforcer, DefaultHumanOversightEnforcer>();
        services.TryAddSingleton<IDataQualityValidator, DefaultDataQualityValidator>();
        services.TryAddSingleton<IAIActDocumentation, DefaultAIActDocumentation>();
        services.TryAddScoped<IAIActComplianceValidator, DefaultAIActComplianceValidator>();

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(AIActCompliancePipelineBehavior<,>));

        // Auto-register from attributes if enabled
        var optionsInstance = new AIActOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<AIActHealthCheck>(
                    AIActHealthCheck.DefaultName,
                    tags: AIActHealthCheck.Tags);
        }

        if (optionsInstance.AutoRegisterFromAttributes)
        {
            var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
                ? optionsInstance.AssembliesToScan
                : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];

            // Register descriptor and hosted service for deferred auto-registration
            services.AddSingleton(new AIActAutoRegistrationDescriptor(assembliesToScan));
            services.AddHostedService<AIActAutoRegistrationHostedService>();
        }

        return services;
    }
}
