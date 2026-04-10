using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Health;
using Encina.Audit.Marten.Projections;
using Encina.Security.Audit;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Audit.Marten;

/// <summary>
/// Extension methods for configuring Encina Marten event-sourced audit trail services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Marten event-sourced audit trail services to the specified <see cref="IServiceCollection"/>,
    /// replacing the default <see cref="IAuditStore"/> and <see cref="IReadAuditStore"/> with
    /// Marten-backed implementations using temporal crypto-shredding.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="MartenAuditOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="MartenAuditOptions"/> — Configured via the provided action</item>
    /// <item><see cref="ITemporalKeyProvider"/> → <see cref="MartenTemporalKeyProvider"/> (Scoped)</item>
    /// <item><see cref="AuditEventEncryptor"/> (Scoped)</item>
    /// <item><see cref="IAuditStore"/> → <see cref="MartenAuditStore"/> (Scoped, replaces default InMemory)</item>
    /// <item><see cref="IReadAuditStore"/> → <see cref="MartenReadAuditStore"/> (Scoped, replaces default InMemory)</item>
    /// <item><see cref="IConfigureOptions{StoreOptions}"/> → <see cref="ConfigureMartenAuditProjections"/>
    /// (registers async projections and document indexes)</item>
    /// <item><see cref="MartenAuditHealthCheck"/> (optional, when <see cref="MartenAuditOptions.AddHealthCheck"/> is true)</item>
    /// <item><see cref="MartenAuditRetentionService"/> (optional, when <see cref="MartenAuditOptions.EnableAutoPurge"/> is true)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Prerequisites:</b> This package requires Marten to be configured in the DI container
    /// (via <c>AddMarten()</c>) and <c>AddEncinaAudit()</c> to be called for the audit pipeline behavior.
    /// </para>
    /// <para>
    /// <b>Store replacement:</b> Unlike satellite packages that use <c>TryAdd</c>, this method uses
    /// <c>Replace</c> for <see cref="IAuditStore"/> and <see cref="IReadAuditStore"/> to override the
    /// default <see cref="InMemoryAuditStore"/> / <see cref="InMemoryReadAuditStore"/> registrations
    /// from <c>AddEncinaAudit()</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 1. Configure Marten
    /// services.AddMarten(options =>
    /// {
    ///     options.Connection("Host=localhost;Database=myapp;...");
    /// });
    ///
    /// // 2. Add core audit services (pipeline behavior, factory)
    /// services.AddEncinaAudit(options =>
    /// {
    ///     options.AuditAllCommands = true;
    /// });
    ///
    /// // 3. Add Marten event-sourced audit store (replaces InMemory)
    /// services.AddEncinaAuditMarten(options =>
    /// {
    ///     options.TemporalGranularity = TemporalKeyGranularity.Monthly;
    ///     options.RetentionPeriod = TimeSpan.FromDays(2555); // 7 years (SOX)
    ///     options.EnableAutoPurge = true;
    ///     options.AddHealthCheck = true;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaAuditMarten(
        this IServiceCollection services,
        Action<MartenAuditOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<MartenAuditOptions>(_ => { });
        }

        // Ensure TimeProvider is available
        services.TryAddSingleton(TimeProvider.System);

        // Ensure ILoggerFactory is available for projection logger injection
        services.AddLogging();

        // Instantiate options to inspect flags for conditional registrations
        var optionsInstance = new MartenAuditOptions();
        configure?.Invoke(optionsInstance);

        // Register temporal key provider (scoped — uses IDocumentSession)
        services.TryAddScoped<ITemporalKeyProvider, MartenTemporalKeyProvider>();

        // Register audit event encryptor (scoped — depends on ITemporalKeyProvider)
        services.AddScoped<AuditEventEncryptor>();

        // Replace default audit stores with Marten implementations
        services.Replace(ServiceDescriptor.Scoped<IAuditStore, MartenAuditStore>());
        services.Replace(ServiceDescriptor.Scoped<IReadAuditStore, MartenReadAuditStore>());

        // Register Marten projection configuration (async projections + document indexes)
        services.AddSingleton<IConfigureOptions<StoreOptions>, ConfigureMartenAuditProjections>();

        // Conditional: health check
        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<MartenAuditHealthCheck>(
                    MartenAuditHealthCheck.DefaultName,
                    tags: MartenAuditHealthCheck.Tags);
        }

        // Conditional: auto-purge background service
        if (optionsInstance.EnableAutoPurge)
        {
            services.AddHostedService<MartenAuditRetentionService>();
        }

        return services;
    }
}
