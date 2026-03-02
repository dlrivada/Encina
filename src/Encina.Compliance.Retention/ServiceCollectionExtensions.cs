using System.Reflection;

using Encina.Compliance.Retention.Health;
using Encina.Compliance.Retention.InMemory;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Retention;

/// <summary>
/// Extension methods for configuring Encina Retention compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Retention (GDPR Article 5(1)(e) — Storage Limitation) services to the specified
    /// <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="RetentionOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="RetentionOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="IRetentionRecordStore"/> → <see cref="InMemoryRetentionRecordStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IRetentionPolicyStore"/> → <see cref="InMemoryRetentionPolicyStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="ILegalHoldStore"/> → <see cref="InMemoryLegalHoldStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IRetentionAuditStore"/> → <see cref="InMemoryRetentionAuditStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IRetentionPolicy"/> → <see cref="DefaultRetentionPolicy"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IRetentionEnforcer"/> → <see cref="DefaultRetentionEnforcer"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="ILegalHoldManager"/> → <see cref="DefaultLegalHoldManager"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="RetentionValidationPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a database-backed
    /// <see cref="IRetentionRecordStore"/> or a custom <see cref="IRetentionAuditStore"/>.
    /// </para>
    /// <para>
    /// <b>Conditional registrations:</b>
    /// <list type="bullet">
    /// <item>Health check: Only registered when <see cref="RetentionOptions.AddHealthCheck"/> is <c>true</c></item>
    /// <item>Enforcement service: Only registered when <see cref="RetentionOptions.EnableAutomaticEnforcement"/> is <c>true</c></item>
    /// <item>Auto-registration: Only registered when <see cref="RetentionOptions.AutoRegisterFromAttributes"/> is <c>true</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaRetention(options =>
    /// {
    ///     options.EnforcementMode = RetentionEnforcementMode.Block;
    ///     options.AddHealthCheck = true;
    ///     options.AssembliesToScan.Add(typeof(Program).Assembly);
    ///     options.AddPolicy("user-profiles", p => p.RetainForDays(365).WithAutoDelete());
    /// });
    ///
    /// // With custom implementations (register before AddEncinaRetention)
    /// services.AddSingleton&lt;IRetentionRecordStore, DatabaseRetentionRecordStore&gt;();
    /// services.AddSingleton&lt;IRetentionAuditStore, DatabaseRetentionAuditStore&gt;();
    /// services.AddEncinaRetention(options =>
    /// {
    ///     options.EnforcementMode = RetentionEnforcementMode.Warn;
    ///     options.AutoRegisterFromAttributes = false;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaRetention(
        this IServiceCollection services,
        Action<RetentionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<RetentionOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<RetentionOptions>, RetentionOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default store implementations (TryAdd allows override by satellite providers)
        services.TryAddSingleton<IRetentionRecordStore, InMemoryRetentionRecordStore>();
        services.TryAddSingleton<IRetentionPolicyStore, InMemoryRetentionPolicyStore>();
        services.TryAddSingleton<ILegalHoldStore, InMemoryLegalHoldStore>();
        services.TryAddSingleton<IRetentionAuditStore, InMemoryRetentionAuditStore>();

        // Register default high-level service implementations
        services.TryAddSingleton<IRetentionPolicy, DefaultRetentionPolicy>();
        services.TryAddSingleton<IRetentionEnforcer, DefaultRetentionEnforcer>();
        services.TryAddSingleton<ILegalHoldManager, DefaultLegalHoldManager>();

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(RetentionValidationPipelineBehavior<,>));

        // Instantiate options to inspect flags for conditional registrations
        var optionsInstance = new RetentionOptions();
        configure?.Invoke(optionsInstance);

        // Conditional: Health check
        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<RetentionHealthCheck>(
                    RetentionHealthCheck.DefaultName,
                    tags: RetentionHealthCheck.Tags);
        }

        // Conditional: Automatic enforcement service
        if (optionsInstance.EnableAutomaticEnforcement)
        {
            services.AddHostedService<RetentionEnforcementService>();
        }

        // Conditional: Auto-registration from [RetentionPeriod] attributes
        if (optionsInstance.AutoRegisterFromAttributes)
        {
            var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
                ? optionsInstance.AssembliesToScan
                : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];

            // Register descriptor and hosted service for deferred auto-registration
            services.AddSingleton(new RetentionAutoRegistrationDescriptor(assembliesToScan));
            services.AddHostedService<RetentionAutoRegistrationHostedService>();
        }

        // Register fluent-configured policies for auto-creation at startup
        if (optionsInstance.ConfiguredPolicies.Count > 0)
        {
            services.AddSingleton(new RetentionFluentPolicyDescriptor(optionsInstance.ConfiguredPolicies));
            // The auto-registration hosted service or a separate initializer will create these
            if (!optionsInstance.AutoRegisterFromAttributes)
            {
                // If auto-registration is disabled but fluent policies exist,
                // still register the auto-registration service to process fluent policies
                var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
                    ? optionsInstance.AssembliesToScan
                    : (IReadOnlyList<Assembly>)[];

                services.TryAddSingleton(new RetentionAutoRegistrationDescriptor(assembliesToScan));
                services.AddHostedService<RetentionFluentPolicyHostedService>();
            }
        }

        return services;
    }
}
