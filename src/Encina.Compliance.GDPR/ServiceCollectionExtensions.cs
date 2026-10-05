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
    /// <item><see cref="GDPRCompliancePipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAddEnumerable)</item>
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
        ConfigureOptions(services, configure);

        services.TryAddSingleton<IValidateOptions<GDPROptions>, GDPROptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default implementations (TryAdd allows override)
        services.TryAddSingleton<IProcessingActivityRegistry, InMemoryProcessingActivityRegistry>();
        services.TryAddScoped<IGDPRComplianceValidator, DefaultGDPRComplianceValidator>();

        // Register pipeline behavior
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(GDPRCompliancePipelineBehavior<,>)));

        // Register RoPA exporters (TryAdd allows override)
        services.TryAddSingleton<JsonRoPAExporter>();
        services.TryAddSingleton<CsvRoPAExporter>();

        // Auto-register from attributes if enabled
        var optionsInstance = new GDPROptions();
        configure?.Invoke(optionsInstance);

        AddHealthCheck(services, optionsInstance);
        AddAutoRegistration(services, optionsInstance, Assembly.GetCallingAssembly());

        return services;
    }

    private static void ConfigureOptions(IServiceCollection services, Action<GDPROptions>? configure)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<GDPROptions>(_ => { });
        }
    }

    private static void AddHealthCheck(IServiceCollection services, GDPROptions options)
    {
        if (options.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<GDPRHealthCheck>(
                    GDPRHealthCheck.DefaultName,
                    tags: GDPRHealthCheck.Tags);
        }
    }

    private static void AddAutoRegistration(IServiceCollection services, GDPROptions options, Assembly callingAssembly)
    {
        if (options.AutoRegisterFromAttributes)
        {
            var assembliesToScan = options.AssembliesToScan.Count > 0
                ? options.AssembliesToScan
                : [Assembly.GetEntryAssembly() ?? callingAssembly];

            // Register descriptor and hosted service for deferred auto-registration
            services.AddSingleton(new GDPRAutoRegistrationDescriptor(assembliesToScan));
            services.AddHostedService<GDPRAutoRegistrationHostedService>();
        }
    }

}
