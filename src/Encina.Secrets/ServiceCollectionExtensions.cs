using Encina.Secrets.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Secrets;

/// <summary>
/// Extension methods for configuring Encina secrets caching services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a caching decorator around the registered <see cref="ISecretProvider"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method wraps the existing <see cref="ISecretProvider"/> registration with
    /// a <see cref="CachedSecretProvider"/> decorator that caches read operations using
    /// <see cref="IMemoryCache"/>.
    /// </para>
    /// <para>
    /// An <see cref="ISecretProvider"/> must already be registered before calling this method.
    /// If no provider is registered, the decorator will fail at runtime when resolved.
    /// </para>
    /// <para>
    /// Default cache settings: TTL of 5 minutes, caching enabled.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register a provider first, then add caching
    /// services.AddSingleton&lt;ISecretProvider, MyVaultProvider&gt;();
    /// services.AddEncinaSecretsCaching();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaSecretsCaching(
        this IServiceCollection services)
    {
        return services.AddEncinaSecretsCaching(_ => { });
    }

    /// <summary>
    /// Adds a caching decorator around the registered <see cref="ISecretProvider"/>
    /// with custom cache options.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">An action to configure <see cref="SecretCacheOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method wraps the existing <see cref="ISecretProvider"/> registration with
    /// a <see cref="CachedSecretProvider"/> decorator that caches read operations using
    /// <see cref="IMemoryCache"/>.
    /// </para>
    /// <para>
    /// An <see cref="ISecretProvider"/> must already be registered before calling this method.
    /// The decorator captures the existing <see cref="ISecretProvider"/> descriptor, removes it,
    /// and re-registers it as the inner provider wrapped by <see cref="CachedSecretProvider"/>.
    /// </para>
    /// <para>
    /// The following services are registered:
    /// <list type="bullet">
    /// <item><see cref="SecretCacheOptions"/> — Configured via the provided action</item>
    /// <item><see cref="IMemoryCache"/> — Registered via <see cref="MemoryCacheServiceCollectionExtensions.AddMemoryCache(IServiceCollection)"/> (TryAdd)</item>
    /// <item><see cref="ISecretProvider"/> — Replaced with <see cref="CachedSecretProvider"/> decorator</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddSingleton&lt;ISecretProvider, MyVaultProvider&gt;();
    /// services.AddEncinaSecretsCaching(options =>
    /// {
    ///     options.DefaultTtl = TimeSpan.FromMinutes(10);
    ///     options.Enabled = true;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    public static IServiceCollection AddEncinaSecretsCaching(
        this IServiceCollection services,
        Action<SecretCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.AddMemoryCache();

        // Find the existing ISecretProvider registration to wrap it
        var innerDescriptor = services.LastOrDefault(d => d.ServiceType == typeof(ISecretProvider));

        if (innerDescriptor is not null)
        {
            services.Remove(innerDescriptor);
        }

        services.AddSingleton<ISecretProvider>(sp =>
        {
            // Resolve the inner provider from the captured descriptor
            var inner = ResolveInnerProvider(sp, innerDescriptor);
            var cache = sp.GetRequiredService<IMemoryCache>();
            var options = sp.GetRequiredService<IOptions<SecretCacheOptions>>();
            var logger = sp.GetRequiredService<ILogger<CachedSecretProvider>>();

            return new CachedSecretProvider(inner, cache, options, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds OpenTelemetry instrumentation (tracing and metrics) for <see cref="ISecretProvider"/> operations.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method wraps the existing <see cref="ISecretProvider"/> registration with an
    /// <see cref="InstrumentedSecretProvider"/> decorator that emits distributed traces
    /// and metrics for every secret operation.
    /// </para>
    /// <para>
    /// An <see cref="ISecretProvider"/> must already be registered before calling this method.
    /// </para>
    /// <para>
    /// Default settings: tracing and metrics enabled, secret names <b>not</b> recorded
    /// in telemetry data (for security).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddSingleton&lt;ISecretProvider, MyVaultProvider&gt;();
    /// services.AddEncinaSecretsInstrumentation();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaSecretsInstrumentation(
        this IServiceCollection services)
    {
        return services.AddEncinaSecretsInstrumentation(_ => { });
    }

    /// <summary>
    /// Adds OpenTelemetry instrumentation (tracing and metrics) for <see cref="ISecretProvider"/> operations
    /// with custom options.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">An action to configure <see cref="SecretsInstrumentationOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method wraps the existing <see cref="ISecretProvider"/> registration with an
    /// <see cref="InstrumentedSecretProvider"/> decorator that emits distributed traces
    /// and metrics for every secret operation.
    /// </para>
    /// <para>
    /// An <see cref="ISecretProvider"/> must already be registered before calling this method.
    /// The decorator captures the existing <see cref="ISecretProvider"/> descriptor, removes it,
    /// and re-registers it wrapped by <see cref="InstrumentedSecretProvider"/>.
    /// </para>
    /// <para>
    /// The following services are registered:
    /// <list type="bullet">
    /// <item><see cref="SecretsInstrumentationOptions"/> — Configured via the provided action</item>
    /// <item><see cref="SecretsMetrics"/> — Registered when <see cref="SecretsInstrumentationOptions.EnableMetrics"/> is <c>true</c></item>
    /// <item><see cref="ISecretProvider"/> — Replaced with <see cref="InstrumentedSecretProvider"/> decorator</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddSingleton&lt;ISecretProvider, MyVaultProvider&gt;();
    /// services.AddEncinaSecretsInstrumentation(options =>
    /// {
    ///     options.RecordSecretNames = true;
    ///     options.EnableTracing = true;
    ///     options.EnableMetrics = true;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    public static IServiceCollection AddEncinaSecretsInstrumentation(
        this IServiceCollection services,
        Action<SecretsInstrumentationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SecretsInstrumentationOptions();
        configure(options);

        // Register metrics if enabled
        if (options.EnableMetrics)
        {
            services.TryAddSingleton<SecretsMetrics>();
        }

        // Find the existing ISecretProvider registration to wrap it
        var innerDescriptor = services.LastOrDefault(d => d.ServiceType == typeof(ISecretProvider));

        if (innerDescriptor is not null)
        {
            services.Remove(innerDescriptor);
        }

        services.AddSingleton<ISecretProvider>(sp =>
        {
            var inner = ResolveInnerProvider(sp, innerDescriptor);
            var metrics = options.EnableMetrics ? sp.GetService<SecretsMetrics>() : null;
            var logger = sp.GetRequiredService<ILogger<InstrumentedSecretProvider>>();

            return new InstrumentedSecretProvider(inner, options, metrics, logger);
        });

        return services;
    }

    private static ISecretProvider ResolveInnerProvider(
        IServiceProvider serviceProvider,
        ServiceDescriptor? descriptor)
    {
        if (descriptor is null)
        {
            throw new InvalidOperationException(
                "No ISecretProvider registration found. Register a secret provider before calling AddEncinaSecretsCaching().");
        }

        if (descriptor.ImplementationInstance is ISecretProvider instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return (ISecretProvider)descriptor.ImplementationFactory(serviceProvider);
        }

        if (descriptor.ImplementationType is not null)
        {
            return (ISecretProvider)ActivatorUtilities.CreateInstance(
                serviceProvider,
                descriptor.ImplementationType);
        }

        throw new InvalidOperationException(
            "Cannot resolve the inner ISecretProvider from the existing service descriptor.");
    }
}
