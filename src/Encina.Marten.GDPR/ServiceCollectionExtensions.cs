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
    /// <item><see cref="IDataErasureStrategy"/> → an internal router: locations of the Marten locator go to
    /// <see cref="CryptoShredErasureStrategy"/>, every other location to the strategy registered before this call</item>
    /// <item><see cref="IPersonalDataLocator"/> → <see cref="MartenEventPersonalDataLocator"/> (Scoped, additive)</item>
    /// <item><see cref="IConfigureOptions{StoreOptions}"/> → the configurator that wraps Marten's System.Text.Json
    /// serializer and turns off the async daemon's <c>SkipSerializationErrors</c></item>
    /// <item>The startup validation hosted service (it logs the opt-out when <see cref="CryptoShreddingOptions.ValidateOnStartup"/> is <c>false</c>)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Registration order.</b> Register a custom <see cref="IDataErasureStrategy"/> <b>before</b> this call: it
    /// becomes the router's inner strategy and receives only locations of other locators. A strategy registered
    /// after this call bypasses crypto-shredding, and the startup validation stops the host. If the application
    /// registers its own <see cref="IPersonalDataLocator"/>, resolve a <see cref="CompositePersonalDataLocator"/>
    /// that includes the Marten locator, otherwise the startup validation stops the host. Call Marten's
    /// <c>UseTypeInfoResolver(context)</c> before this call.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Production setup with the PostgreSQL key store
    /// services.AddEncinaMartenGdpr(options =>
    /// {
    ///     options.UsePostgreSqlKeyStore = true;
    ///     options.AddHealthCheck = true;
    ///     options.AssembliesToScan.Add(typeof(Program).Assembly);
    /// });
    ///
    /// // With a custom forgotten subject handler (register before AddEncinaMartenGdpr)
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

        services.Configure(configure ?? (_ => { }));
        services.TryAddSingleton<IValidateOptions<CryptoShreddingOptions>, CryptoShreddingOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // The flags decide which services are registered, so they are read from a local instance.
        var optionsInstance = new CryptoShreddingOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.UsePostgreSqlKeyStore)
        {
            services.TryAddScoped<ISubjectKeyProvider, PostgreSqlSubjectKeyProvider>();
        }
        else
        {
            services.TryAddSingleton<ISubjectKeyProvider, InMemorySubjectKeyProvider>();
        }

        services.TryAddSingleton<IForgottenSubjectHandler, DefaultForgottenSubjectHandler>();
        RegisterErasureRouter(services);

        // Additive: the Marten locator takes part in data subject requests through a composite; the startup
        // validation fails when another locator is resolved without it.
        services.AddScoped<IPersonalDataLocator, MartenEventPersonalDataLocator>();

        services.AddSingleton<IConfigureOptions<StoreOptions>, ConfigureMartenCryptoShredding>();
        services.AddEncinaMartenStoreOptionsBridge();

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<CryptoShreddingHealthCheck>(
                    CryptoShreddingHealthCheck.DefaultName,
                    tags: CryptoShreddingHealthCheck.Tags);
        }

        var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
            ? optionsInstance.AssembliesToScan
            : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];
        services.TryAddSingleton(new CryptoShreddingValidationDescriptor(assembliesToScan));
        services.AddHostedService<CryptoShreddingStartupValidationHostedService>();

        return services;
    }

    /// <summary>
    /// Registers the routing erasure strategy as the <see cref="IDataErasureStrategy"/>; a strategy registered before
    /// is kept as its inner strategy under an internal service key.
    /// </summary>
    private static void RegisterErasureRouter(IServiceCollection services)
    {
        services.TryAddScoped<CryptoShredErasureStrategy>();

        var existing = services
            .Where(d => d.ServiceType == typeof(IDataErasureStrategy) && !d.IsKeyedService)
            .ToList();
        if (existing.Any(d => d.ImplementationType == typeof(CryptoShredRoutingErasureStrategy)))
        {
            return;
        }

        foreach (var descriptor in existing)
        {
            services.Remove(descriptor);
            services.Add(AsInnerStrategy(descriptor));
        }

        services.AddScoped<IDataErasureStrategy, CryptoShredRoutingErasureStrategy>();
    }

    private static ServiceDescriptor AsInnerStrategy(ServiceDescriptor descriptor)
    {
        const string key = CryptoShredRoutingErasureStrategy.InnerStrategyKey;
        if (descriptor.ImplementationInstance is { } instance)
        {
            return new ServiceDescriptor(typeof(IDataErasureStrategy), key, instance);
        }

        if (descriptor.ImplementationFactory is { } factory)
        {
            return new ServiceDescriptor(typeof(IDataErasureStrategy), key, (sp, _) => factory(sp), descriptor.Lifetime);
        }

        return new ServiceDescriptor(typeof(IDataErasureStrategy), key, descriptor.ImplementationType!, descriptor.Lifetime);
    }
}
