using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Encina.Security.Audit;

/// <summary>
/// Extension methods for configuring Encina audit trail services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina audit trail services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="AuditOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="AuditOptions"/> - Configured via the provided action</item>
    /// <item><see cref="IAuditEntryFactory"/> → <see cref="DefaultAuditEntryFactory"/> (Scoped)</item>
    /// <item><see cref="IPiiMasker"/> → <see cref="NullPiiMasker"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IAuditStore"/> → <see cref="InMemoryAuditStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="AuditPipelineBehavior{TRequest, TResponse}"/> (Scoped, open generic)</item>
    /// <item><see cref="AuditRetentionService"/> (Hosted service, only when <see cref="AuditOptions.EnableAutoPurge"/> is true)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// <see cref="IPiiMasker"/> and <see cref="IAuditStore"/> are registered using <c>TryAdd</c>,
    /// allowing you to register custom implementations before calling this method.
    /// </para>
    /// <para>
    /// <b>Production considerations:</b>
    /// For production use, register a persistent <see cref="IAuditStore"/> implementation
    /// (e.g., SQL Server, PostgreSQL) before calling this method.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup with defaults
    /// services.AddEncinaAudit();
    ///
    /// // With custom options
    /// services.AddEncinaAudit(options =>
    /// {
    ///     options.AuditAllCommands = true;
    ///     options.AuditAllQueries = false;
    ///     options.RetentionDays = 2555; // 7 years for SOX
    ///
    ///     options.ExcludeType&lt;HealthCheckQuery&gt;();
    ///     options.IncludeQueryType&lt;GetUserPersonalDataQuery&gt;();
    /// });
    ///
    /// // With custom store (register before AddEncinaAudit)
    /// services.AddSingleton&lt;IAuditStore, SqlServerAuditStore&gt;();
    /// services.AddEncinaAudit();
    ///
    /// // With custom PII masker
    /// services.AddSingleton&lt;IPiiMasker, CustomPiiMasker&gt;();
    /// services.AddEncinaAudit();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaAudit(
        this IServiceCollection services,
        Action<AuditOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<AuditOptions>(_ => { });
        }

        // Register default implementations (TryAdd allows override)
        services.TryAddSingleton<IPiiMasker, NullPiiMasker>();
        services.TryAddSingleton<IAuditStore, InMemoryAuditStore>();

        // Register factory and behavior
        services.AddScoped<IAuditEntryFactory, DefaultAuditEntryFactory>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditPipelineBehavior<,>));

        // Register auto-purge service if enabled
        // Note: We need to evaluate the options to check if EnableAutoPurge is true
        var optionsInstance = new AuditOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.EnableAutoPurge)
        {
            services.AddHostedService<AuditRetentionService>();
        }

        return services;
    }
}
