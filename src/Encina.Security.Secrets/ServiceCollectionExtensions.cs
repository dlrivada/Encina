using Encina.Security.Audit;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Auditing;
using Encina.Security.Secrets.Caching;
using Encina.Security.Secrets.Configuration;
using Encina.Security.Secrets.Diagnostics;
using Encina.Security.Secrets.Health;
using Encina.Security.Secrets.Injection;
using Encina.Security.Secrets.Providers;
using Encina.Security.Secrets.Rotation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.Secrets;

/// <summary>
/// Extension methods for configuring Encina secrets management services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina secrets management services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="SecretsOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="SecretsOptions"/> — Configured via the provided action</item>
    /// <item><see cref="ISecretReader"/> → <see cref="EnvironmentSecretProvider"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="CachedSecretReaderDecorator"/> — Wraps the reader when <see cref="SecretsOptions.EnableCaching"/> is <c>true</c></item>
    /// <item><see cref="AuditedSecretReaderDecorator"/> — Wraps the reader when <see cref="SecretsOptions.EnableAccessAuditing"/> is <c>true</c></item>
    /// <item><see cref="SecretRotationCoordinator"/> — Registered when rotation handlers are present</item>
    /// <item><see cref="SecretsHealthCheck"/> — Registered when <see cref="SecretsOptions.ProviderHealthCheck"/> is <c>true</c></item>
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
    /// // Basic setup with defaults (EnvironmentSecretProvider)
    /// services.AddEncinaSecrets();
    ///
    /// // With custom options
    /// services.AddEncinaSecrets(options =>
    /// {
    ///     options.EnableCaching = true;
    ///     options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
    ///     options.ProviderHealthCheck = true;
    /// });
    ///
    /// // With custom reader (register before AddEncinaSecrets)
    /// services.AddSingleton&lt;ISecretReader, MyVaultSecretReader&gt;();
    /// services.AddEncinaSecrets();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaSecrets(
        this IServiceCollection services,
        Action<SecretsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register default ISecretReader with optional decorator chain
        services.TryAddSingleton<ISecretReader>(sp =>
        {
            ISecretReader reader = new EnvironmentSecretProvider(
                sp.GetRequiredService<ILogger<EnvironmentSecretProvider>>());

            return WrapWithDecorators(sp, reader);
        });

        return services.AddEncinaSecretsCore(configure);
    }

    /// <summary>
    /// Adds Encina secrets management services with a specific <see cref="ISecretReader"/> implementation.
    /// </summary>
    /// <typeparam name="TReader">The secret reader implementation type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="SecretsOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This overload registers the specified <typeparamref name="TReader"/> as the
    /// <see cref="ISecretReader"/> implementation instead of the default <see cref="EnvironmentSecretProvider"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSecrets&lt;ConfigurationSecretProvider&gt;(options =>
    /// {
    ///     options.ProviderHealthCheck = true;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaSecrets<TReader>(
        this IServiceCollection services,
        Action<SecretsOptions>? configure = null)
        where TReader : class, ISecretReader
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register specific reader with optional decorator chain
        services.TryAddSingleton<ISecretReader>(sp =>
        {
            ISecretReader reader = ActivatorUtilities.CreateInstance<TReader>(sp);
            return WrapWithDecorators(sp, reader);
        });

        return services.AddEncinaSecretsCore(configure);
    }

    /// <summary>
    /// Adds a secret rotation handler for a specific secret name.
    /// </summary>
    /// <typeparam name="THandler">The rotation handler implementation type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Register rotation handlers to react when secrets are rotated.
    /// Multiple handlers can be registered for different secrets.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddSecretRotationHandler&lt;DatabaseCredentialRotationHandler&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddSecretRotationHandler<THandler>(
        this IServiceCollection services)
        where THandler : class, ISecretRotationHandler
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<ISecretRotationHandler, THandler>();
        return services;
    }

    /// <summary>
    /// Adds secrets from an <see cref="ISecretReader"/> to the .NET configuration system.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="serviceProvider">The service provider used to resolve <see cref="ISecretReader"/>.</param>
    /// <param name="configure">Optional action to configure <see cref="SecretsConfigurationOptions"/>.</param>
    /// <returns>The configuration builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method adds a configuration source that reads secrets from the registered
    /// <see cref="ISecretReader"/> and exposes them as configuration values.
    /// </para>
    /// <para>
    /// Secrets are loaded synchronously during application startup.
    /// When <see cref="SecretsConfigurationOptions.ReloadInterval"/> is set, secrets
    /// are periodically refreshed in the background.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Configuration.AddEncinaSecrets(builder.Services.BuildServiceProvider(), options =>
    /// {
    ///     options.SecretNames = ["database-connection-string", "api-key"];
    ///     options.KeyDelimiter = "--";
    ///     options.ReloadInterval = TimeSpan.FromMinutes(5);
    /// });
    /// </code>
    /// </example>
    public static IConfigurationBuilder AddEncinaSecrets(
        this IConfigurationBuilder builder,
        IServiceProvider serviceProvider,
        Action<SecretsConfigurationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var options = new SecretsConfigurationOptions();
        configure?.Invoke(options);

        builder.Add(new SecretsConfigurationSource(serviceProvider, options));
        return builder;
    }

    private static IServiceCollection AddEncinaSecretsCore(
        this IServiceCollection services,
        Action<SecretsOptions>? configure)
    {
        // Configure options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<SecretsOptions>(_ => { });
        }

        // Register rotation coordinator
        services.TryAddSingleton<SecretRotationCoordinator>();

        // Register health check if enabled
        var optionsInstance = new SecretsOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.ProviderHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<SecretsHealthCheck>(
                    SecretsHealthCheck.DefaultName,
                    tags: SecretsHealthCheck.Tags);
        }

        // Register secret injection pipeline behavior if enabled
        if (optionsInstance.EnableSecretInjection)
        {
            services.TryAddScoped<SecretInjectionOrchestrator>();
            services.TryAddEnumerable(ServiceDescriptor.Transient(
                typeof(IPipelineBehavior<,>),
                typeof(SecretInjectionPipelineBehavior<,>)));
        }

        // Register metrics if enabled
        if (optionsInstance.EnableMetrics)
        {
            services.AddMetrics();
            services.TryAddSingleton<SecretsMetrics>();
        }

        return services;
    }

    private static ISecretReader WrapWithDecorators(IServiceProvider sp, ISecretReader reader)
    {
        var options = sp.GetRequiredService<IOptions<SecretsOptions>>().Value;

        // Layer 1: Caching (innermost decorator, closest to provider)
        if (options.EnableCaching)
        {
            reader = new CachedSecretReaderDecorator(
                reader,
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<IOptions<SecretsOptions>>(),
                sp.GetRequiredService<ILogger<CachedSecretReaderDecorator>>());
        }

        // Layer 2: Auditing (outer decorator, wraps caching)
        if (options.EnableAccessAuditing)
        {
            var auditStore = sp.GetService<IAuditStore>();
            var requestContext = sp.GetService<IRequestContext>();

            if (auditStore is not null && requestContext is not null)
            {
                reader = new AuditedSecretReaderDecorator(
                    reader,
                    auditStore,
                    requestContext,
                    options,
                    sp.GetRequiredService<ILogger<AuditedSecretReaderDecorator>>());
            }
        }

        return reader;
    }
}
