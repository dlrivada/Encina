using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Wolverine;

/// <summary>
/// Extension methods for configuring Encina Wolverine integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Wolverine integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaWolverine(
        this IServiceCollection services,
        Action<EncinaWolverineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EncinaWolverineOptions();
        configure?.Invoke(options);

        services.Configure<EncinaWolverineOptions>(opt =>
        {
            opt.AutoPublishDomainEvents = options.AutoPublishDomainEvents;
            opt.UseOutbox = options.UseOutbox;
            opt.IncludeExceptionDetails = options.IncludeExceptionDetails;
            opt.DefaultTimeout = options.DefaultTimeout;
        });

        services.TryAddScoped<IWolverineMessagePublisher, WolverineMessagePublisher>();

        return services;
    }
}
