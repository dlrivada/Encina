using System.Reflection;

using Encina.Compliance.DataResidency.Health;
using Encina.Compliance.DataResidency.InMemory;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Extension methods for configuring Encina Data Residency compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Data Residency (GDPR Chapter V — International Transfers) services
    /// to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="DataResidencyOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="DataResidencyOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="IResidencyPolicyStore"/> → <see cref="InMemoryResidencyPolicyStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IDataLocationStore"/> → <see cref="InMemoryDataLocationStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IResidencyAuditStore"/> → <see cref="InMemoryResidencyAuditStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IDataResidencyPolicy"/> → <see cref="DefaultDataResidencyPolicy"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="ICrossBorderTransferValidator"/> → <see cref="DefaultCrossBorderTransferValidator"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IRegionContextProvider"/> → <see cref="DefaultRegionContextProvider"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IAdequacyDecisionProvider"/> → <see cref="DefaultAdequacyDecisionProvider"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IRegionRouter"/> → <see cref="DefaultRegionRouter"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="DataResidencyPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a database-backed
    /// <see cref="IResidencyPolicyStore"/> or a custom <see cref="IRegionContextProvider"/>.
    /// </para>
    /// <para>
    /// <b>Conditional registrations:</b>
    /// <list type="bullet">
    /// <item>Health check: Only registered when <see cref="DataResidencyOptions.AddHealthCheck"/> is <c>true</c></item>
    /// <item>Auto-registration: Only registered when <see cref="DataResidencyOptions.AutoRegisterFromAttributes"/> is <c>true</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaDataResidency(options =>
    /// {
    ///     options.DefaultRegion = RegionRegistry.DE;
    ///     options.EnforcementMode = DataResidencyEnforcementMode.Block;
    ///     options.AddHealthCheck = true;
    ///     options.AssembliesToScan.Add(typeof(Program).Assembly);
    ///     options.AddPolicy("healthcare-data", p => p.AllowEU().RequireAdequacyDecision());
    /// });
    ///
    /// // With custom implementations (register before AddEncinaDataResidency)
    /// services.AddSingleton&lt;IResidencyPolicyStore, DatabaseResidencyPolicyStore&gt;();
    /// services.AddSingleton&lt;IRegionContextProvider, HttpHeaderRegionContextProvider&gt;();
    /// services.AddEncinaDataResidency(options =>
    /// {
    ///     options.EnforcementMode = DataResidencyEnforcementMode.Warn;
    ///     options.AutoRegisterFromAttributes = false;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaDataResidency(
        this IServiceCollection services,
        Action<DataResidencyOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<DataResidencyOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<DataResidencyOptions>, DataResidencyOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default store implementations (TryAdd allows override by satellite providers)
        services.TryAddSingleton<IResidencyPolicyStore, InMemoryResidencyPolicyStore>();
        services.TryAddSingleton<IDataLocationStore, InMemoryDataLocationStore>();
        services.TryAddSingleton<IResidencyAuditStore, InMemoryResidencyAuditStore>();

        // Register default high-level service implementations
        services.TryAddSingleton<IDataResidencyPolicy, DefaultDataResidencyPolicy>();
        services.TryAddSingleton<ICrossBorderTransferValidator, DefaultCrossBorderTransferValidator>();
        services.TryAddSingleton<IRegionContextProvider, DefaultRegionContextProvider>();
        services.TryAddSingleton<IAdequacyDecisionProvider, DefaultAdequacyDecisionProvider>();
        services.TryAddSingleton<IRegionRouter, DefaultRegionRouter>();

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(DataResidencyPipelineBehavior<,>));

        // Instantiate options to inspect flags for conditional registrations
        var optionsInstance = new DataResidencyOptions();
        configure?.Invoke(optionsInstance);

        // Conditional: Health check
        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<DataResidencyHealthCheck>(
                    DataResidencyHealthCheck.DefaultName,
                    tags: DataResidencyHealthCheck.Tags);
        }

        // Conditional: Auto-registration from [DataResidency] attributes
        if (optionsInstance.AutoRegisterFromAttributes)
        {
            var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
                ? optionsInstance.AssembliesToScan
                : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];

            // Register descriptor and hosted service for deferred auto-registration
            services.AddSingleton(new DataResidencyAutoRegistrationDescriptor(assembliesToScan));
            services.AddHostedService<DataResidencyAutoRegistrationHostedService>();
        }

        // Register fluent-configured policies for auto-creation at startup
        if (optionsInstance.ConfiguredPolicies.Count > 0)
        {
            services.AddSingleton(new DataResidencyFluentPolicyDescriptor(optionsInstance.ConfiguredPolicies));
            // The auto-registration hosted service or a separate initializer will create these
            if (!optionsInstance.AutoRegisterFromAttributes)
            {
                // If auto-registration is disabled but fluent policies exist,
                // still register the auto-registration service to process fluent policies
                var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
                    ? optionsInstance.AssembliesToScan
                    : (IReadOnlyList<Assembly>)[];

                services.TryAddSingleton(new DataResidencyAutoRegistrationDescriptor(assembliesToScan));
                services.AddHostedService<DataResidencyFluentPolicyHostedService>();
            }
        }

        return services;
    }
}
