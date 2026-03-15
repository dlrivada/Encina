using System.Reflection;

using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.DataSubjectRights.Health;
using Encina.Compliance.DataSubjectRights.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Extension methods for configuring Encina Data Subject Rights compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Data Subject Rights (GDPR Articles 15-22) services to the specified
    /// <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="DataSubjectRightsOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="DataSubjectRightsOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="IDSRService"/> → <see cref="DefaultDSRService"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="IDataErasureExecutor"/> → <see cref="DefaultDataErasureExecutor"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="IDataErasureStrategy"/> → <see cref="HardDeleteErasureStrategy"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IDataPortabilityExporter"/> → <see cref="DefaultDataPortabilityExporter"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="IDataSubjectIdExtractor"/> → <see cref="DefaultDataSubjectIdExtractor"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="JsonExportFormatWriter"/>, <see cref="CsvExportFormatWriter"/>, <see cref="XmlExportFormatWriter"/> — all three export writers</item>
    /// <item><see cref="ProcessingRestrictionPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a custom
    /// <see cref="IDSRService"/> or a custom <see cref="IDataErasureStrategy"/>.
    /// </para>
    /// <para>
    /// <b>Marten aggregates:</b>
    /// Call <see cref="DSRMartenExtensions.AddDSRRequestAggregates"/>
    /// separately to register the event-sourced aggregate repositories with Marten.
    /// </para>
    /// <para>
    /// <b>Auto-registration:</b>
    /// When <see cref="DataSubjectRightsOptions.AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the specified assemblies are scanned for <see cref="PersonalDataAttribute"/>
    /// decorations and a personal data map is built at startup.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaDataSubjectRights(options =>
    /// {
    ///     options.DefaultDeadlineDays = 30;
    ///     options.AddHealthCheck = true;
    ///     options.AssembliesToScan.Add(typeof(Program).Assembly);
    /// });
    ///
    /// // Register Marten aggregates separately
    /// services.AddDSRRequestAggregates();
    ///
    /// // With custom implementations (register before AddEncinaDataSubjectRights)
    /// services.AddScoped&lt;IDSRService, MyCustomDSRService&gt;();
    /// services.AddSingleton&lt;IDataErasureStrategy, AnonymizationErasureStrategy&gt;();
    /// services.AddEncinaDataSubjectRights(options =>
    /// {
    ///     options.RestrictionEnforcementMode = DSREnforcementMode.Block;
    ///     options.AutoRegisterFromAttributes = false;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaDataSubjectRights(
        this IServiceCollection services,
        Action<DataSubjectRightsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<DataSubjectRightsOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<DataSubjectRightsOptions>, DataSubjectRightsOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default implementations (TryAdd allows override)
        services.TryAddScoped<IDSRService, DefaultDSRService>();
        services.TryAddScoped<IDataErasureExecutor, DefaultDataErasureExecutor>();
        services.TryAddSingleton<IDataErasureStrategy, HardDeleteErasureStrategy>();
        services.TryAddScoped<IDataPortabilityExporter, DefaultDataPortabilityExporter>();
        services.TryAddSingleton<IDataSubjectIdExtractor, DefaultDataSubjectIdExtractor>();

        // Register export format writers (TryAdd allows override)
        services.TryAddSingleton<JsonExportFormatWriter>();
        services.TryAddSingleton<CsvExportFormatWriter>();
        services.TryAddSingleton<XmlExportFormatWriter>();

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(ProcessingRestrictionPipelineBehavior<,>));

        // Instantiate options to inspect flags for health check and auto-registration
        var optionsInstance = new DataSubjectRightsOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<DataSubjectRightsHealthCheck>(
                    DataSubjectRightsHealthCheck.DefaultName,
                    tags: DataSubjectRightsHealthCheck.Tags);
        }

        if (optionsInstance.AutoRegisterFromAttributes)
        {
            var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
                ? optionsInstance.AssembliesToScan
                : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];

            // Register descriptor and hosted service for deferred auto-registration
            services.AddSingleton(new DSRAutoRegistrationDescriptor(assembliesToScan));
            services.AddHostedService<DSRAutoRegistrationHostedService>();
        }

        return services;
    }
}
