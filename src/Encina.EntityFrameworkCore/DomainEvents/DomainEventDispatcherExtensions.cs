using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.DomainEvents;

/// <summary>
/// Extension methods for configuring domain event dispatching in Entity Framework Core.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide two ways to configure domain event dispatching:
/// <list type="number">
/// <item><description>Service collection level: Registers the interceptor as a singleton service</description></item>
/// <item><description>DbContext level: Adds the interceptor directly to DbContext options</description></item>
/// </list>
/// </para>
/// <para>
/// The recommended approach is to use <see cref="AddEncinaEntityFrameworkCore{TDbContext}"/>
/// with <c>UseDomainEvents = true</c>, which handles both registrations automatically.
/// </para>
/// </remarks>
public static class DomainEventDispatcherExtensions
{
    /// <summary>
    /// Adds the domain event dispatcher interceptor to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This registers:
    /// <list type="bullet">
    /// <item><description><see cref="DomainEventDispatcherOptions"/> as a singleton</description></item>
    /// <item><description><see cref="DomainEventDispatcherInterceptor"/> as a singleton</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// After calling this method, you still need to add the interceptor to your DbContext
    /// using <see cref="UseDomainEventDispatcher(DbContextOptionsBuilder, IServiceProvider)"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDomainEventDispatcher();
    ///
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .UseDomainEventDispatcher(sp);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddDomainEventDispatcher(this IServiceCollection services)
    {
        return services.AddDomainEventDispatcher(_ => { });
    }

    /// <summary>
    /// Adds the domain event dispatcher interceptor to the service collection with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the dispatcher options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This registers:
    /// <list type="bullet">
    /// <item><description><see cref="DomainEventDispatcherOptions"/> as a singleton (with provided configuration)</description></item>
    /// <item><description><see cref="DomainEventDispatcherInterceptor"/> as a singleton</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDomainEventDispatcher(options =>
    /// {
    ///     options.StopOnFirstError = true;
    ///     options.RequireINotification = false;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddDomainEventDispatcher(
        this IServiceCollection services,
        Action<DomainEventDispatcherOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new DomainEventDispatcherOptions();
        configure(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<DomainEventDispatcherInterceptor>();

        return services;
    }

    /// <summary>
    /// Adds the domain event dispatcher interceptor to the DbContext options.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="serviceProvider">The service provider to resolve the interceptor from.</param>
    /// <returns>The options builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method retrieves the <see cref="DomainEventDispatcherInterceptor"/> from the
    /// service provider and adds it to the DbContext's interceptors collection.
    /// </para>
    /// <para>
    /// You must call <see cref="AddDomainEventDispatcher(IServiceCollection)"/> or
    /// <see cref="AddDomainEventDispatcher(IServiceCollection, Action{DomainEventDispatcherOptions})"/>
    /// before using this method to ensure the interceptor is registered.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDomainEventDispatcher();
    ///
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .UseDomainEventDispatcher(sp);
    /// });
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder UseDomainEventDispatcher(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var interceptor = serviceProvider.GetRequiredService<DomainEventDispatcherInterceptor>();
        optionsBuilder.AddInterceptors(interceptor);

        return optionsBuilder;
    }

    /// <summary>
    /// Adds the domain event dispatcher interceptor to the DbContext options with inline configuration.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="serviceProvider">The service provider to resolve dependencies from.</param>
    /// <param name="configure">Optional action to configure the dispatcher options.</param>
    /// <returns>The options builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method that creates a new interceptor instance with the specified
    /// options. Unlike <see cref="UseDomainEventDispatcher(DbContextOptionsBuilder, IServiceProvider)"/>,
    /// this does not require pre-registration of the interceptor in the service collection.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .UseDomainEventDispatcher(sp, dispatcherOptions =>
    ///            {
    ///                dispatcherOptions.StopOnFirstError = true;
    ///            });
    /// });
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder UseDomainEventDispatcher(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider,
        Action<DomainEventDispatcherOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var options = new DomainEventDispatcherOptions();
        configure?.Invoke(options);

        var logger = serviceProvider.GetRequiredService<ILogger<DomainEventDispatcherInterceptor>>();

        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);
        optionsBuilder.AddInterceptors(interceptor);

        return optionsBuilder;
    }
}
