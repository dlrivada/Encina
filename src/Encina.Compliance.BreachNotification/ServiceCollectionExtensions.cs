using Encina.Compliance.BreachNotification.Detection;
using Encina.Compliance.BreachNotification.Detection.Rules;
using Encina.Compliance.BreachNotification.Health;
using Encina.Compliance.BreachNotification.InMemory;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Extension methods for configuring Encina Breach Notification compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Breach Notification (GDPR Articles 33-34 — 72-Hour Data Breach Notification)
    /// services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="BreachNotificationOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="BreachNotificationOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="IBreachRecordStore"/> → <see cref="InMemoryBreachRecordStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IBreachAuditStore"/> → <see cref="InMemoryBreachAuditStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IBreachDetector"/> → <see cref="DefaultBreachDetector"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IBreachNotifier"/> → <see cref="DefaultBreachNotifier"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IBreachHandler"/> → <see cref="DefaultBreachHandler"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="BreachDetectionPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// <item>Built-in detection rules: <see cref="UnauthorizedAccessRule"/>, <see cref="MassDataExfiltrationRule"/>,
    ///   <see cref="PrivilegeEscalationRule"/>, <see cref="AnomalousQueryPatternRule"/> (Singleton)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a database-backed
    /// <see cref="IBreachRecordStore"/> or a custom <see cref="IBreachNotifier"/>.
    /// </para>
    /// <para>
    /// <b>Conditional registrations:</b>
    /// <list type="bullet">
    /// <item>Health check: Only registered when <see cref="BreachNotificationOptions.AddHealthCheck"/> is <c>true</c></item>
    /// <item>Deadline monitoring: Only registered when <see cref="BreachNotificationOptions.EnableDeadlineMonitoring"/> is <c>true</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaBreachNotification(options =>
    /// {
    ///     options.EnforcementMode = BreachDetectionEnforcementMode.Block;
    ///     options.EnableDeadlineMonitoring = true;
    ///     options.AddHealthCheck = true;
    ///     options.AddDetectionRule&lt;MyCustomRule&gt;();
    /// });
    ///
    /// // With custom implementations (register before AddEncinaBreachNotification)
    /// services.AddSingleton&lt;IBreachRecordStore, DatabaseBreachRecordStore&gt;();
    /// services.AddSingleton&lt;IBreachAuditStore, DatabaseBreachAuditStore&gt;();
    /// services.AddEncinaBreachNotification(options =>
    /// {
    ///     options.EnforcementMode = BreachDetectionEnforcementMode.Warn;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaBreachNotification(
        this IServiceCollection services,
        Action<BreachNotificationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<BreachNotificationOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<BreachNotificationOptions>, BreachNotificationOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default store implementations (TryAdd allows override by satellite providers)
        services.TryAddSingleton<IBreachRecordStore, InMemoryBreachRecordStore>();
        services.TryAddSingleton<IBreachAuditStore, InMemoryBreachAuditStore>();

        // Register default high-level service implementations
        services.TryAddSingleton<IBreachDetector, DefaultBreachDetector>();
        services.TryAddSingleton<IBreachNotifier, DefaultBreachNotifier>();
        services.TryAddScoped<IBreachHandler, DefaultBreachHandler>();

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(BreachDetectionPipelineBehavior<,>));

        // Register built-in detection rules
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBreachDetectionRule, UnauthorizedAccessRule>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBreachDetectionRule, MassDataExfiltrationRule>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBreachDetectionRule, PrivilegeEscalationRule>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBreachDetectionRule, AnomalousQueryPatternRule>());

        // Instantiate options to inspect flags for conditional registrations
        var optionsInstance = new BreachNotificationOptions();
        configure?.Invoke(optionsInstance);

        // Register custom detection rules from fluent API
        foreach (var ruleType in optionsInstance.DetectionRuleTypes)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IBreachDetectionRule), ruleType));
        }

        // Conditional: Health check
        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<BreachNotificationHealthCheck>(
                    BreachNotificationHealthCheck.DefaultName,
                    tags: BreachNotificationHealthCheck.Tags);
        }

        // Conditional: Deadline monitoring service
        if (optionsInstance.EnableDeadlineMonitoring)
        {
            services.AddHostedService<BreachDeadlineMonitorService>();
        }

        return services;
    }
}
