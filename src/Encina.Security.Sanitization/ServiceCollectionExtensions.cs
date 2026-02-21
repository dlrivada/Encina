using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Encoders;
using Encina.Security.Sanitization.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Security.Sanitization;

/// <summary>
/// Extension methods for configuring Encina input sanitization and output encoding services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina input sanitization and output encoding services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="SanitizationOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="SanitizationOptions"/> — Configured via the provided action</item>
    /// <item><see cref="ISanitizer"/> → <see cref="DefaultSanitizer"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IOutputEncoder"/> → <see cref="DefaultOutputEncoder"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="SanitizationOrchestrator"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="InputSanitizationPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// <item><see cref="OutputEncodingPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup with defaults
    /// services.AddEncinaSanitization();
    ///
    /// // With custom options
    /// services.AddEncinaSanitization(options =>
    /// {
    ///     options.SanitizeAllStringInputs = true;
    ///     options.EncodeAllOutputs = true;
    ///     options.AddHealthCheck = true;
    ///     options.EnableTracing = true;
    ///     options.EnableMetrics = true;
    ///
    ///     options.AddProfile("BlogPost", profile =>
    ///     {
    ///         profile.AllowTags("p", "h1", "h2", "a", "img");
    ///         profile.AllowAttributes("href", "src", "alt");
    ///         profile.AllowProtocols("https", "mailto");
    ///         profile.WithStripScripts(true);
    ///     });
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaSanitization(
        this IServiceCollection services,
        Action<SanitizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<SanitizationOptions>(_ => { });
        }

        // Register default implementations (TryAdd allows override)
        services.TryAddSingleton<ISanitizer, DefaultSanitizer>();
        services.TryAddSingleton<IOutputEncoder, DefaultOutputEncoder>();
        services.TryAddScoped<SanitizationOrchestrator>();

        // Register pipeline behaviors (TryAddEnumerable checks both ServiceType AND ImplementationType,
        // allowing multiple behaviors while preventing duplicate registrations of the same implementation)
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(InputSanitizationPipelineBehavior<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(OutputEncodingPipelineBehavior<,>)));

        // Register health check if enabled
        var optionsInstance = new SanitizationOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<SanitizationHealthCheck>(
                    SanitizationHealthCheck.DefaultName,
                    tags: SanitizationHealthCheck.Tags);
        }

        return services;
    }
}
