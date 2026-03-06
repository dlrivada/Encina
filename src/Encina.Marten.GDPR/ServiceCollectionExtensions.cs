using System.Reflection;

using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Health;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Marten.GDPR;

/// <summary>
/// Extension methods for configuring Encina Marten GDPR crypto-shredding services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Marten GDPR crypto-shredding services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="CryptoShreddingOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="CryptoShreddingOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="ISubjectKeyProvider"/> → <see cref="InMemorySubjectKeyProvider"/> or
    /// <see cref="PostgreSqlSubjectKeyProvider"/> (based on <see cref="CryptoShreddingOptions.UsePostgreSqlKeyStore"/>)</item>
    /// <item><see cref="IForgottenSubjectHandler"/> → <see cref="DefaultForgottenSubjectHandler"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IDataErasureStrategy"/> → <see cref="CryptoShredErasureStrategy"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="IPersonalDataLocator"/> → <see cref="MartenEventPersonalDataLocator"/> (Scoped, additive)</item>
    /// <item><see cref="IConfigureOptions{StoreOptions}"/> → <see cref="ConfigureMartenCryptoShredding"/> (serializer wrapping)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Prerequisites:</b> This package requires <c>AddEncinaEncryption()</c> to be called first
    /// for <c>IFieldEncryptor</c> and <c>IKeyProvider</c> availability. The health check will
    /// report Unhealthy if encryption services are not registered.
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c> where appropriate, allowing you to register
    /// custom implementations before calling this method.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup (InMemory key store, auto-registration enabled)
    /// services.AddEncinaEncryption();
    /// services.AddEncinaMartenGdpr();
    ///
    /// // Production setup with PostgreSQL key store
    /// services.AddEncinaEncryption();
    /// services.AddEncinaMartenGdpr(options =>
    /// {
    ///     options.UsePostgreSqlKeyStore = true;
    ///     options.AddHealthCheck = true;
    ///     options.AssembliesToScan.Add(typeof(Program).Assembly);
    /// });
    ///
    /// // With custom forgotten subject handler (register before AddEncinaMartenGdpr)
    /// services.AddSingleton&lt;IForgottenSubjectHandler, CustomForgottenSubjectHandler&gt;();
    /// services.AddEncinaMartenGdpr();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaMartenGdpr(
        this IServiceCollection services,
        Action<CryptoShreddingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<CryptoShreddingOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<CryptoShreddingOptions>, CryptoShreddingOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Instantiate options to inspect flags for conditional registrations
        var optionsInstance = new CryptoShreddingOptions();
        configure?.Invoke(optionsInstance);

        // Register key provider based on configuration
        if (optionsInstance.UsePostgreSqlKeyStore)
        {
            services.TryAddScoped<ISubjectKeyProvider, PostgreSqlSubjectKeyProvider>();
        }
        else
        {
            services.TryAddSingleton<ISubjectKeyProvider, InMemorySubjectKeyProvider>();
        }

        // Register forgotten subject handler (TryAdd allows override)
        services.TryAddSingleton<IForgottenSubjectHandler, DefaultForgottenSubjectHandler>();

        // Register crypto-shred erasure strategy (TryAdd — only if no other strategy is registered)
        services.TryAddScoped<IDataErasureStrategy, CryptoShredErasureStrategy>();

        // Register personal data locator as additive (CompositePersonalDataLocator aggregates all)
        services.AddScoped<IPersonalDataLocator, MartenEventPersonalDataLocator>();

        // Configure Marten to wrap the serializer with CryptoShredderSerializer
        services.AddSingleton<IConfigureOptions<StoreOptions>, ConfigureMartenCryptoShredding>();

        // Conditional: health check
        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<CryptoShreddingHealthCheck>(
                    CryptoShreddingHealthCheck.DefaultName,
                    tags: CryptoShreddingHealthCheck.Tags);
        }

        // Conditional: auto-registration
        if (optionsInstance.AutoRegisterFromAttributes)
        {
            var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
                ? optionsInstance.AssembliesToScan
                : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];

            services.AddSingleton(new CryptoShreddingAutoRegistrationDescriptor(assembliesToScan));
            services.AddHostedService<CryptoShreddingAutoRegistrationHostedService>();
        }

        return services;
    }
}
