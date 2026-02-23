using System.Reflection;

using Encina.Compliance.Consent.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Consent;

/// <summary>
/// Extension methods for configuring Encina consent compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina consent compliance services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="ConsentOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="ConsentOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="IConsentStore"/> → <see cref="InMemoryConsentStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IConsentValidator"/> → <see cref="DefaultConsentValidator"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="IConsentVersionManager"/> → <see cref="InMemoryConsentVersionManager"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IConsentAuditStore"/> → <see cref="InMemoryConsentAuditStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="ConsentRequiredPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a database-backed
    /// <see cref="IConsentStore"/> or a custom <see cref="IConsentValidator"/>.
    /// </para>
    /// <para>
    /// <b>Auto-registration:</b>
    /// When <see cref="ConsentOptions.AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the specified assemblies are scanned for <see cref="RequireConsentAttribute"/>
    /// decorations and purposes are validated at startup.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaConsent(options =>
    /// {
    ///     options.EnforcementMode = ConsentEnforcementMode.Block;
    ///     options.DefaultExpirationDays = 365;
    ///     options.AddHealthCheck = true;
    ///     options.AssembliesToScan.Add(typeof(Program).Assembly);
    ///
    ///     options.DefinePurpose(ConsentPurposes.Marketing, p =>
    ///     {
    ///         p.Description = "Marketing emails";
    ///         p.RequiresExplicitOptIn = true;
    ///     });
    /// });
    ///
    /// // With custom implementations (register before AddEncinaConsent)
    /// services.AddSingleton&lt;IConsentStore, DatabaseConsentStore&gt;();
    /// services.AddScoped&lt;IConsentValidator, MyCustomValidator&gt;();
    /// services.AddEncinaConsent(options =>
    /// {
    ///     options.AutoRegisterFromAttributes = false;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaConsent(
        this IServiceCollection services,
        Action<ConsentOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<ConsentOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<ConsentOptions>, ConsentOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default implementations (TryAdd allows override)
        services.TryAddSingleton<IConsentStore, InMemoryConsentStore>();
        services.TryAddSingleton<IConsentVersionManager, InMemoryConsentVersionManager>();
        services.TryAddSingleton<IConsentAuditStore, InMemoryConsentAuditStore>();
        services.TryAddScoped<IConsentValidator, DefaultConsentValidator>();

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(ConsentRequiredPipelineBehavior<,>));

        // Auto-register from attributes if enabled
        var optionsInstance = new ConsentOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<ConsentHealthCheck>(
                    ConsentHealthCheck.DefaultName,
                    tags: ConsentHealthCheck.Tags);
        }

        if (optionsInstance.AutoRegisterFromAttributes)
        {
            var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
                ? optionsInstance.AssembliesToScan
                : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];

            // Register descriptor and hosted service for deferred auto-registration
            services.AddSingleton(new ConsentAutoRegistrationDescriptor(assembliesToScan));
            services.AddHostedService<ConsentAutoRegistrationHostedService>();
        }

        return services;
    }
}
