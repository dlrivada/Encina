using Encina.Cdc.Abstractions;
using Encina.Cdc.MySql.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Cdc.MySql;

/// <summary>
/// Extension methods for configuring MySQL Binary Log Replication CDC services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MySQL Binary Log Replication CDC connector services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for MySQL CDC options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="OptionsValidationException">
    /// Thrown when <see cref="MySqlCdcOptions.ConnectionString"/> is missing or malformed, or when
    /// <see cref="MySqlCdcOptions.Hostname"/> or a server in the connection string targets a loopback
    /// host or local socket (unless <see cref="MySqlCdcOptions.AllowLocalEndpoints"/> is set) or a
    /// link-local, cloud metadata or unspecified address. The same validation runs again at host
    /// startup (<c>ValidateOnStart</c>).
    /// </exception>
    public static IServiceCollection AddEncinaCdcMySql(
        this IServiceCollection services,
        Action<MySqlCdcOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddSingleton(TimeProvider.System);

        // Validate eagerly so a plain BuildServiceProvider (no host, no ValidateOnStart) is protected too.
        var options = new MySqlCdcOptions();
        configure(options);
        var validation = new MySqlCdcOptionsValidator().Validate(Options.DefaultName, options);
        if (validation.Failed)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(MySqlCdcOptions), validation.Failures);
        }

        services.AddOptions<MySqlCdcOptions>()
            .Configure(configure)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<MySqlCdcOptions>, MySqlCdcOptionsValidator>());

        // The connector takes the concrete options; resolve them through IOptions so validation always runs.
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<MySqlCdcOptions>>().Value);
        services.TryAddSingleton<ICdcConnector, MySqlCdcConnector>();
        services.TryAddSingleton<MySqlCdcHealthCheck>();

        return services;
    }
}
