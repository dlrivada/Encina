using Encina.Cdc.Abstractions;
using Encina.Cdc.SqlServer.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Cdc.SqlServer;

/// <summary>
/// Extension methods for configuring SQL Server Change Tracking CDC services.
/// </summary>
/// <example>
/// <code>
/// services.AddEncinaCdc(config =>
/// {
///     config.UseCdc()
///           .AddHandler&lt;Order, OrderChangeHandler&gt;()
///           .WithTableMapping&lt;Order&gt;("dbo.Orders");
/// });
///
/// services.AddEncinaCdcSqlServer(options =>
/// {
///     options.ConnectionString = "Server=.;Database=MyDb;...";
///     options.TrackedTables = ["dbo.Orders", "dbo.Customers"];
/// });
/// </code>
/// </example>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQL Server Change Tracking CDC connector services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for SQL Server CDC options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaCdcSqlServer(
        this IServiceCollection services,
        Action<SqlServerCdcOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SqlServerCdcOptions();
        configure(options);

        services.AddSingleton(options);
        services.TryAddSingleton<ICdcConnector, SqlServerCdcConnector>();
        services.TryAddSingleton<SqlServerCdcHealthCheck>();

        return services;
    }
}
