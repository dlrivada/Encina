using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.Health;
using Encina.Security.AntiTampering.HMAC;
using Encina.Security.AntiTampering.Http;
using Encina.Security.AntiTampering.Nonce;
using Encina.Security.AntiTampering.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Security.AntiTampering;

/// <summary>
/// Extension methods for configuring Encina HMAC-based anti-tampering services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina anti-tampering services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="AntiTamperingOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="AntiTamperingOptions"/> — Configured via the provided action.</item>
    /// <item><see cref="IRequestSigner"/> → <see cref="HMACSigner"/> (Singleton, using TryAdd).</item>
    /// <item><see cref="INonceStore"/> → <see cref="InMemoryNonceStore"/> (Singleton, using TryAdd).</item>
    /// <item><see cref="IKeyProvider"/> → <see cref="InMemoryKeyProvider"/> (Singleton, using TryAdd).</item>
    /// <item><see cref="HMACValidationPipelineBehavior{TRequest,TResponse}"/> (Transient, using TryAdd).</item>
    /// <item><see cref="IRequestSigningClient"/> → <see cref="RequestSigningClient"/> (Singleton, using TryAdd).</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a custom
    /// <see cref="IKeyProvider"/> backed by Azure Key Vault:
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup with defaults
    /// services.AddEncinaAntiTampering();
    ///
    /// // With custom options and test keys
    /// services.AddEncinaAntiTampering(options =>
    /// {
    ///     options.Algorithm = HMACAlgorithm.SHA256;
    ///     options.TimestampToleranceMinutes = 5;
    ///     options.AddHealthCheck = true;
    ///     options.AddKey("test-key", "my-secret-value");
    /// });
    ///
    /// // With custom key provider (register before AddEncinaAntiTampering)
    /// services.AddSingleton&lt;IKeyProvider, AzureKeyVaultKeyProvider&gt;();
    /// services.AddEncinaAntiTampering();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddEncinaAntiTampering(
        this IServiceCollection services,
        Action<AntiTamperingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<AntiTamperingOptions>(_ => { });
        }

        // Register default implementations (TryAdd allows override)
        services.TryAddSingleton<IRequestSigner, HMACSigner>();
        services.TryAddSingleton<INonceStore, InMemoryNonceStore>();
        services.TryAddSingleton<IKeyProvider, InMemoryKeyProvider>();

        // Register pipeline behavior
        services.TryAddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(HMACValidationPipelineBehavior<,>));

        // Register signing client
        services.TryAddSingleton<IRequestSigningClient, RequestSigningClient>();

        // Register health check if enabled
        var optionsInstance = new AntiTamperingOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<AntiTamperingHealthCheck>(
                    AntiTamperingHealthCheck.DefaultName,
                    tags: AntiTamperingHealthCheck.Tags);
        }

        return services;
    }

    /// <summary>
    /// Replaces the default <see cref="InMemoryNonceStore"/> with a distributed cache-backed
    /// <see cref="DistributedCacheNonceStore"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Call this method after <see cref="AddEncinaAntiTampering"/> to switch from in-memory
    /// nonce storage to a distributed cache backed by <c>ICacheProvider</c>.
    /// </para>
    /// <para>
    /// Requires an <c>ICacheProvider</c> registration (e.g., from <c>Encina.Caching.Redis</c>,
    /// <c>Encina.Caching.Valkey</c>, or any other Encina caching provider).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaAntiTampering(options =>
    /// {
    ///     options.AddKey("api-key-v1", "my-secret");
    /// });
    /// services.AddDistributedNonceStore();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddDistributedNonceStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Remove existing INonceStore registration(s) and replace with distributed
        var existingDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(INonceStore));

        if (existingDescriptor is not null)
        {
            services.Remove(existingDescriptor);
        }

        services.AddSingleton<INonceStore, DistributedCacheNonceStore>();

        return services;
    }
}
