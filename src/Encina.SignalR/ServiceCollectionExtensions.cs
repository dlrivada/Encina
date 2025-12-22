using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.SignalR;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register Encina SignalR integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina SignalR integration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><description><see cref="ISignalRNotificationBroadcaster"/> for broadcasting notifications</description></item>
    /// <item><description>Default <see cref="SignalROptions"/> configuration</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// After calling this method, inherit from <see cref="EncinaHub"/> to create your application hub.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    ///
    /// builder.Services.AddEncina(cfg => { }, typeof(Program).Assembly);
    /// builder.Services.AddEncinaSignalR();
    /// builder.Services.AddSignalR();
    ///
    /// var app = builder.Build();
    ///
    /// app.MapHub&lt;AppHub&gt;("/hub");
    /// app.Run();
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaSignalR(this IServiceCollection services)
    {
        return services.AddEncinaSignalR(_ => { });
    }

    /// <summary>
    /// Adds Encina SignalR integration services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure SignalR options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddEncinaSignalR(options =>
    /// {
    ///     options.EnableNotificationBroadcast = true;
    ///     options.AuthorizationPolicy = "RequireAuth";
    ///     options.IncludeDetailedErrors = builder.Environment.IsDevelopment();
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaSignalR(
        this IServiceCollection services,
        Action<SignalROptions> configureOptions)
    {
        // Register options
        services.Configure(configureOptions);

        // Register broadcaster
        services.TryAddSingleton<ISignalRNotificationBroadcaster, SignalRNotificationBroadcaster>();

        return services;
    }

    /// <summary>
    /// Adds SignalR notification broadcasting to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This registers a notification handler that automatically broadcasts notifications
    /// marked with <see cref="BroadcastToSignalRAttribute"/> to SignalR clients.
    /// </para>
    /// <para>
    /// Call this method after <see cref="AddEncinaSignalR(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddEncina(cfg => { }, typeof(Program).Assembly);
    /// builder.Services.AddEncinaSignalR();
    /// builder.Services.AddSignalRBroadcasting(); // Enable [BroadcastToSignalR] attribute
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRBroadcasting(this IServiceCollection services)
    {
        // Register the open generic handler - it will be resolved for each notification type
        services.AddTransient(typeof(INotificationHandler<>), typeof(SignalRBroadcastHandler<>));
        return services;
    }
}
