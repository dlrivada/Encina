using Encina.DistributedLock.SqlServer.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.DistributedLock.SqlServer;

/// <summary>
/// Extension methods for configuring Encina SQL Server distributed lock services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina SQL Server distributed lock services with a connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers <see cref="IDistributedLockProvider"/> using SQL Server's
    /// sp_getapplock for distributed locking.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaDistributedLockSqlServer("Server=.;Database=MyApp;Trusted_Connection=True;");
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaDistributedLockSqlServer(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        return services.AddEncinaDistributedLockSqlServerCore(connectionString, null);
    }

    /// <summary>
    /// Adds Encina SQL Server distributed lock services with a connection string and options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaDistributedLockSqlServer(
        this IServiceCollection services,
        string connectionString,
        Action<SqlServerLockOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddEncinaDistributedLockSqlServerCore(connectionString, configure);
    }

    private static IServiceCollection AddEncinaDistributedLockSqlServerCore(
        this IServiceCollection services,
        string connectionString,
        Action<SqlServerLockOptions>? configure)
    {
        var options = new SqlServerLockOptions
        {
            ConnectionString = connectionString
        };

        if (configure is not null)
        {
            configure(options);
        }

        services.Configure<SqlServerLockOptions>(opt =>
        {
            opt.ConnectionString = connectionString;
            opt.KeyPrefix = options.KeyPrefix;
            opt.DefaultExpiry = options.DefaultExpiry;
            opt.DefaultWait = options.DefaultWait;
            opt.DefaultRetry = options.DefaultRetry;
            opt.ProviderHealthCheck = options.ProviderHealthCheck;
        });

        // Register provider
        services.TryAddSingleton<IDistributedLockProvider, SqlServerDistributedLockProvider>();

        // Register health check if enabled
        services.RegisterHealthCheck(options, connectionString);

        return services;
    }

    /// <summary>
    /// Registers health check for SQL Server distributed lock if enabled in options.
    /// </summary>
    internal static IServiceCollection RegisterHealthCheck(
        this IServiceCollection services,
        SqlServerLockOptions options,
        string connectionString)
    {
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(options.ProviderHealthCheck);
            services.AddSingleton(_ => new SqlServerDistributedLockHealthCheck(
                connectionString,
                options.ProviderHealthCheck));
            services.AddSingleton<IEncinaHealthCheck>(sp =>
                sp.GetRequiredService<SqlServerDistributedLockHealthCheck>());
        }

        return services;
    }
}
