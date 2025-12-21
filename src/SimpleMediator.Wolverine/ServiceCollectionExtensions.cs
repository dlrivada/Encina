using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator.Wolverine;

/// <summary>
/// Extension methods for configuring SimpleMediator Wolverine integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator Wolverine integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorWolverine(
        this IServiceCollection services,
        Action<SimpleMediatorWolverineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SimpleMediatorWolverineOptions();
        configure?.Invoke(options);

        services.Configure<SimpleMediatorWolverineOptions>(opt =>
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
