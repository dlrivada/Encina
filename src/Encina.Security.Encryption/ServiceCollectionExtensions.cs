using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using Encina.Security.Encryption.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Security.Encryption;

/// <summary>
/// Extension methods for configuring Encina field-level encryption services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina field-level encryption services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="EncryptionOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="EncryptionOptions"/> — Configured via the provided action</item>
    /// <item><see cref="IFieldEncryptor"/> → <c>AesGcmFieldEncryptor</c> (Singleton, using TryAdd)</item>
    /// <item><see cref="IKeyProvider"/> → <see cref="InMemoryKeyProvider"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IEncryptionOrchestrator"/> → <c>EncryptionOrchestrator</c> (Scoped, using TryAdd)</item>
    /// <item><see cref="EncryptionPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
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
    /// // Basic setup with defaults (InMemoryKeyProvider + AES-256-GCM)
    /// services.AddEncinaEncryption();
    ///
    /// // With custom options
    /// services.AddEncinaEncryption(options =>
    /// {
    ///     options.FailOnDecryptionError = true;
    ///     options.AddHealthCheck = true;
    /// });
    ///
    /// // With custom key provider (register before AddEncinaEncryption)
    /// services.AddSingleton&lt;IKeyProvider, AzureKeyVaultKeyProvider&gt;();
    /// services.AddEncinaEncryption();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaEncryption(
        this IServiceCollection services,
        Action<EncryptionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<EncryptionOptions>(_ => { });
        }

        // Register default implementations (TryAdd allows override)
        services.TryAddSingleton<IFieldEncryptor, AesGcmFieldEncryptor>();
        services.TryAddSingleton<IKeyProvider, InMemoryKeyProvider>();
        services.TryAddScoped<IEncryptionOrchestrator, EncryptionOrchestrator>();

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(EncryptionPipelineBehavior<,>));

        // Register health check if enabled
        var optionsInstance = new EncryptionOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<EncryptionHealthCheck>(
                    EncryptionHealthCheck.DefaultName,
                    tags: EncryptionHealthCheck.Tags);
        }

        return services;
    }

    /// <summary>
    /// Adds Encina field-level encryption services with a specific <see cref="IKeyProvider"/> implementation.
    /// </summary>
    /// <typeparam name="TKeyProvider">The key provider implementation type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="EncryptionOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This overload registers the specified <typeparamref name="TKeyProvider"/> as the
    /// <see cref="IKeyProvider"/> implementation instead of the default <see cref="InMemoryKeyProvider"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // With Azure Key Vault key provider
    /// services.AddEncinaEncryption&lt;AzureKeyVaultKeyProvider&gt;(options =>
    /// {
    ///     options.AddHealthCheck = true;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaEncryption<TKeyProvider>(
        this IServiceCollection services,
        Action<EncryptionOptions>? configure = null)
        where TKeyProvider : class, IKeyProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register specific key provider before general registration
        services.TryAddSingleton<IKeyProvider, TKeyProvider>();

        return services.AddEncinaEncryption(configure);
    }
}
