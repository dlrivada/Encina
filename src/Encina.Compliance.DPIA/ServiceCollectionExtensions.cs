using System.Reflection;

using Encina.Compliance.DPIA.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Extension methods for configuring Encina DPIA (Data Protection Impact Assessment) compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina DPIA compliance services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="DPIAOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="DPIAOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="IDPIAStore"/> → <see cref="InMemoryDPIAStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IDPIAAuditStore"/> → <see cref="InMemoryDPIAAuditStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IDPIATemplateProvider"/> → <see cref="DefaultDPIATemplateProvider"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IDPIAAssessmentEngine"/> → <see cref="DefaultDPIAAssessmentEngine"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="IRiskCriterion"/> — 6 built-in risk criteria (Singleton, using TryAddEnumerable)</item>
    /// <item><see cref="DPIARequiredPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a database-backed
    /// <see cref="IDPIAStore"/> from <c>Encina.EntityFrameworkCore</c> or <c>Encina.Dapper</c>.
    /// </para>
    /// <para>
    /// <b>Auto-registration:</b>
    /// When <see cref="DPIAOptions.AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the specified assemblies are scanned for <see cref="RequiresDPIAAttribute"/>
    /// decorations and draft assessments are created at startup.
    /// </para>
    /// <para>
    /// <b>Auto-detection:</b>
    /// When <see cref="DPIAOptions.AutoDetectHighRisk"/> is also <c>true</c>,
    /// heuristic analysis supplements attribute-based discovery to identify request types
    /// that might require a DPIA based on naming patterns (e.g., biometric, health, profiling).
    /// </para>
    /// <para>
    /// <b>Conditional registrations:</b>
    /// <list type="bullet">
    /// <item>Health check: Only registered when <see cref="DPIAOptions.AddHealthCheck"/> is <c>true</c></item>
    /// <item>Expiration monitoring: Only registered when <see cref="DPIAOptions.EnableExpirationMonitoring"/> is <c>true</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaDPIA(options =>
    /// {
    ///     options.EnforcementMode = DPIAEnforcementMode.Block;
    ///     options.DefaultReviewPeriod = TimeSpan.FromDays(365);
    ///     options.DPOEmail = "dpo@company.com";
    ///     options.DPOName = "Jane Doe";
    /// });
    ///
    /// // With BlockWithoutDPIA alias
    /// services.AddEncinaDPIA(options =>
    /// {
    ///     options.BlockWithoutDPIA = true;
    ///     options.AddHealthCheck = true;
    ///     options.EnableExpirationMonitoring = true;
    /// });
    ///
    /// // With auto-registration and heuristic detection
    /// services.AddEncinaDPIA(options =>
    /// {
    ///     options.EnforcementMode = DPIAEnforcementMode.Warn;
    ///     options.AutoRegisterFromAttributes = true;
    ///     options.AutoDetectHighRisk = true;
    ///     options.AssembliesToScan.Add(typeof(Program).Assembly);
    /// });
    ///
    /// // With custom store (register before AddEncinaDPIA)
    /// services.AddSingleton&lt;IDPIAStore, DatabaseDPIAStore&gt;();
    /// services.AddEncinaDPIA(options =>
    /// {
    ///     options.EnforcementMode = DPIAEnforcementMode.Block;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaDPIA(
        this IServiceCollection services,
        Action<DPIAOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<DPIAOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<DPIAOptions>, DPIAOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default implementations (TryAdd allows override by provider packages)
        services.TryAddSingleton<IDPIAStore, InMemoryDPIAStore>();
        services.TryAddSingleton<IDPIAAuditStore, InMemoryDPIAAuditStore>();
        services.TryAddSingleton<IDPIATemplateProvider, DefaultDPIATemplateProvider>();
        services.TryAddScoped<IDPIAAssessmentEngine, DefaultDPIAAssessmentEngine>();

        // Register built-in risk criteria (TryAddEnumerable prevents duplicates per implementation type)
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRiskCriterion, SystematicProfilingCriterion>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRiskCriterion, SpecialCategoryDataCriterion>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRiskCriterion, SystematicMonitoringCriterion>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRiskCriterion, AutomatedDecisionMakingCriterion>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRiskCriterion, LargeScaleProcessingCriterion>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRiskCriterion, VulnerableSubjectsCriterion>());

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(DPIARequiredPipelineBehavior<,>));

        // Evaluate conditional features from a local options instance
        var optionsInstance = new DPIAOptions();
        configure?.Invoke(optionsInstance);

        // Auto-registration from attributes (and optionally auto-detection)
        if (optionsInstance.AutoRegisterFromAttributes)
        {
            var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
                ? optionsInstance.AssembliesToScan
                : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];

            services.AddSingleton(new DPIAAutoRegistrationDescriptor(assembliesToScan));
            services.AddHostedService<DPIAAutoRegistrationHostedService>();
        }

        // Conditional: Health check
        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<DPIAHealthCheck>(
                    DPIAHealthCheck.DefaultName,
                    tags: DPIAHealthCheck.Tags);
        }

        // Conditional: Expiration monitoring / review reminder service
        if (optionsInstance.EnableExpirationMonitoring)
        {
            services.AddHostedService<DPIAReviewReminderService>();
        }

        return services;
    }
}
