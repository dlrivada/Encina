using System.Reflection;

using Encina.Compliance.GDPR.Export;
using Encina.Compliance.GDPR.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Extension methods for configuring Encina GDPR compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina GDPR compliance services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="GDPROptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="GDPROptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="IProcessingActivityRegistry"/> → <see cref="InMemoryProcessingActivityRegistry"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IGDPRComplianceValidator"/> → <see cref="DefaultGDPRComplianceValidator"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="GDPRCompliancePipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a database-backed
    /// <see cref="IProcessingActivityRegistry"/> or a custom <see cref="IGDPRComplianceValidator"/>.
    /// </para>
    /// <para>
    /// <b>Auto-registration:</b>
    /// When <see cref="GDPROptions.AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the specified assemblies are scanned for <see cref="ProcessingActivityAttribute"/>
    /// decorations and activities are auto-registered at startup.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaGDPR(options =>
    /// {
    ///     options.ControllerName = "Acme Corp";
    ///     options.ControllerEmail = "privacy@acme.com";
    ///     options.AssembliesToScan.Add(typeof(Program).Assembly);
    /// });
    ///
    /// // With custom implementations (register before AddEncinaGDPR)
    /// services.AddScoped&lt;IGDPRComplianceValidator, MyCustomValidator&gt;();
    /// services.AddSingleton&lt;IProcessingActivityRegistry, DatabaseProcessingActivityRegistry&gt;();
    /// services.AddEncinaGDPR(options =>
    /// {
    ///     options.ControllerName = "Acme Corp";
    ///     options.ControllerEmail = "privacy@acme.com";
    ///     options.AutoRegisterFromAttributes = false;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaGDPR(
        this IServiceCollection services,
        Action<GDPROptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<GDPROptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<GDPROptions>, GDPROptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default implementations (TryAdd allows override)
        services.TryAddSingleton<IProcessingActivityRegistry, InMemoryProcessingActivityRegistry>();
        services.TryAddScoped<IGDPRComplianceValidator, DefaultGDPRComplianceValidator>();

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(GDPRCompliancePipelineBehavior<,>));

        // Register RoPA exporters (TryAdd allows override)
        services.TryAddSingleton<JsonRoPAExporter>();
        services.TryAddSingleton<CsvRoPAExporter>();

        // Auto-register from attributes if enabled
        var optionsInstance = new GDPROptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<GDPRHealthCheck>(
                    GDPRHealthCheck.DefaultName,
                    tags: GDPRHealthCheck.Tags);
        }

        if (optionsInstance.AutoRegisterFromAttributes)
        {
            var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
                ? optionsInstance.AssembliesToScan
                : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];

            // Register descriptor and hosted service for deferred auto-registration
            services.AddSingleton(new GDPRAutoRegistrationDescriptor(assembliesToScan));
            services.AddHostedService<GDPRAutoRegistrationHostedService>();
        }

        return services;
    }

    /// <summary>
    /// Adds Encina lawful basis validation services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="LawfulBasisOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="LawfulBasisOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="ILawfulBasisRegistry"/> → <see cref="InMemoryLawfulBasisRegistry"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="ILawfulBasisProvider"/> → <see cref="DefaultLawfulBasisProvider"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="ILIAStore"/> → <see cref="InMemoryLIAStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="ILegitimateInterestAssessment"/> → <see cref="DefaultLegitimateInterestAssessment"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="ILawfulBasisSubjectIdExtractor"/> → <see cref="DefaultLawfulBasisSubjectIdExtractor"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/> (Transient)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Pipeline ordering:</b> The <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/>
    /// is registered using <c>AddTransient</c> (not TryAdd) to ensure it is always present.
    /// It is designed to run before <see cref="GDPRCompliancePipelineBehavior{TRequest, TResponse}"/>.
    /// Call <c>AddEncinaLawfulBasis</c> before <c>AddEncinaGDPR</c> for correct ordering.
    /// </para>
    /// <para>
    /// <b>Auto-registration:</b>
    /// When <see cref="LawfulBasisOptions.AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the specified assemblies are scanned for <see cref="LawfulBasisAttribute"/>
    /// decorations and registrations are created in the <see cref="ILawfulBasisRegistry"/> at startup.
    /// Additionally, <see cref="LawfulBasisOptions.DefaultBases"/> are registered programmatically.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaLawfulBasis(options =>
    /// {
    ///     options.EnforcementMode = LawfulBasisEnforcementMode.Block;
    ///     options.ScanAssemblyContaining&lt;Program&gt;();
    /// });
    ///
    /// // Full setup with defaults and health check
    /// services.AddEncinaLawfulBasis(options =>
    /// {
    ///     options.EnforcementMode = LawfulBasisEnforcementMode.Block;
    ///     options.ScanAssemblyContaining&lt;Program&gt;();
    ///     options.DefaultBasis&lt;GetProfileQuery&gt;(LawfulBasis.Contract);
    ///     options.AddHealthCheck = true;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaLawfulBasis(
        this IServiceCollection services,
        Action<LawfulBasisOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<LawfulBasisOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<LawfulBasisOptions>, LawfulBasisOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default implementations (TryAdd allows override)
        services.TryAddSingleton<ILawfulBasisRegistry, InMemoryLawfulBasisRegistry>();
        services.TryAddScoped<ILawfulBasisProvider, DefaultLawfulBasisProvider>();
        services.TryAddSingleton<ILIAStore, InMemoryLIAStore>();
        services.TryAddScoped<ILegitimateInterestAssessment, DefaultLegitimateInterestAssessment>();
        services.TryAddScoped<ILawfulBasisSubjectIdExtractor, DefaultLawfulBasisSubjectIdExtractor>();

        // Register pipeline behavior (AddTransient, not TryAdd — always registered)
        // Call AddEncinaLawfulBasis BEFORE AddEncinaGDPR for correct pipeline ordering.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LawfulBasisValidationPipelineBehavior<,>));

        // Instantiate options to inspect flags for health check and auto-registration
        var optionsInstance = new LawfulBasisOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<LawfulBasisHealthCheck>(
                    LawfulBasisHealthCheck.DefaultName,
                    tags: LawfulBasisHealthCheck.Tags);
        }

        if (optionsInstance.AutoRegisterFromAttributes || optionsInstance.DefaultBases.Count > 0)
        {
            var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
                ? optionsInstance.AssembliesToScan.ToList()
                : optionsInstance.AutoRegisterFromAttributes
                    ? [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()]
                    : new List<Assembly>();

            // Register descriptor and hosted service for deferred auto-registration
            services.AddSingleton(new LawfulBasisAutoRegistrationDescriptor(
                assembliesToScan,
                new Dictionary<Type, LawfulBasis>(optionsInstance.DefaultBases)));
            services.AddHostedService<LawfulBasisAutoRegistrationHostedService>();
        }

        return services;
    }
}
