using Encina.Security.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Security;

/// <summary>
/// Extension methods for configuring Encina security services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina security services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="SecurityOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="SecurityOptions"/> — Configured via the provided action</item>
    /// <item><see cref="ISecurityContextAccessor"/> → <c>SecurityContextAccessor</c> (Scoped, using TryAdd)</item>
    /// <item><see cref="IPermissionEvaluator"/> → <see cref="DefaultPermissionEvaluator"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="IResourceOwnershipEvaluator"/> → <see cref="DefaultResourceOwnershipEvaluator"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="SecurityPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a custom
    /// <see cref="IPermissionEvaluator"/> that checks permissions against a database.
    /// </para>
    /// <para>
    /// <b>Health check:</b>
    /// Set <see cref="SecurityOptions.AddHealthCheck"/> to <c>true</c> to register
    /// a health check that verifies all security services are resolvable.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup with defaults
    /// services.AddEncinaSecurity();
    ///
    /// // With custom options and health check
    /// services.AddEncinaSecurity(options =>
    /// {
    ///     options.RequireAuthenticatedByDefault = true;
    ///     options.AddHealthCheck = true;
    /// });
    ///
    /// // With custom evaluator (register before AddEncinaSecurity)
    /// services.AddScoped&lt;IPermissionEvaluator, DatabasePermissionEvaluator&gt;();
    /// services.AddEncinaSecurity();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaSecurity(
        this IServiceCollection services,
        Action<SecurityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<SecurityOptions>(_ => { });
        }

        // Register default implementations (TryAdd allows override)
        services.TryAddScoped<ISecurityContextAccessor, SecurityContextAccessor>();
        services.TryAddScoped<IPermissionEvaluator, DefaultPermissionEvaluator>();
        services.TryAddScoped<IResourceOwnershipEvaluator, DefaultResourceOwnershipEvaluator>();

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(SecurityPipelineBehavior<,>));

        // Register health check if enabled
        var optionsInstance = new SecurityOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<SecurityHealthCheck>(
                    SecurityHealthCheck.DefaultName,
                    tags: SecurityHealthCheck.Tags);
        }

        return services;
    }
}
