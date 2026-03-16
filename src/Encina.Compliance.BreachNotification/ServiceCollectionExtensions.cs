using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Detection;
using Encina.Compliance.BreachNotification.Detection.Rules;
using Encina.Compliance.BreachNotification.Health;
using Encina.Compliance.BreachNotification.Services;

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
    /// <item><see cref="IBreachNotificationService"/> → <see cref="DefaultBreachNotificationService"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="IBreachDetector"/> → <see cref="DefaultBreachDetector"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IBreachNotifier"/> → <see cref="DefaultBreachNotifier"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="BreachDetectionPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// <item>Built-in detection rules: <see cref="UnauthorizedAccessRule"/>, <see cref="MassDataExfiltrationRule"/>,
    ///   <see cref="PrivilegeEscalationRule"/>, <see cref="AnomalousQueryPatternRule"/> (Singleton)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Event sourcing:</b>
    /// The breach notification service uses event-sourced aggregates via Marten. Call
    /// <see cref="BreachNotificationMartenExtensions.AddBreachNotificationAggregates"/> to register
    /// the aggregate repository and projection infrastructure.
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a custom
    /// <see cref="IBreachNotificationService"/> or a custom <see cref="IBreachNotifier"/>.
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
    /// // Basic setup with Marten event sourcing
    /// services.AddEncinaBreachNotification(options =>
    /// {
    ///     options.EnforcementMode = BreachDetectionEnforcementMode.Block;
    ///     options.EnableDeadlineMonitoring = true;
    ///     options.AddHealthCheck = true;
    ///     options.AddDetectionRule&lt;MyCustomRule&gt;();
    /// });
    ///
    /// // Register Marten aggregate repository and projection
    /// services.AddBreachNotificationAggregates();
    ///
    /// // With custom service implementation (register before AddEncinaBreachNotification)
    /// services.AddScoped&lt;IBreachNotificationService, MyCustomBreachNotificationService&gt;();
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

        // Register event-sourced breach notification service (TryAdd allows custom override)
        services.TryAddScoped<IBreachNotificationService, DefaultBreachNotificationService>();

        // Register detection and notification infrastructure
        services.TryAddSingleton<IBreachDetector, DefaultBreachDetector>();
        services.TryAddSingleton<IBreachNotifier, DefaultBreachNotifier>();

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
