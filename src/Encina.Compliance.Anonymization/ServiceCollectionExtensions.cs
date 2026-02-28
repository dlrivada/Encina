using System.Reflection;

using Encina.Compliance.Anonymization.Health;
using Encina.Compliance.Anonymization.InMemory;
using Encina.Compliance.Anonymization.Techniques;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Extension methods for configuring Encina Anonymization compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Anonymization (GDPR Articles 4(5), 25, 32, 89) services to the specified
    /// <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="AnonymizationOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="AnonymizationOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="IAnonymizationAuditStore"/> → <see cref="InMemoryAnonymizationAuditStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IKeyProvider"/> → <see cref="InMemoryKeyProvider"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="ITokenMappingStore"/> → <see cref="InMemoryTokenMappingStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IAnonymizer"/> → <see cref="DefaultAnonymizer"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IPseudonymizer"/> → <see cref="DefaultPseudonymizer"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="ITokenizer"/> → <see cref="DefaultTokenizer"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IRiskAssessor"/> → <see cref="DefaultRiskAssessor"/> (Singleton, using TryAdd)</item>
    /// <item>All five <see cref="IAnonymizationTechnique"/> implementations (Singleton)</item>
    /// <item><see cref="AnonymizationPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a database-backed
    /// <see cref="IKeyProvider"/> or a custom <see cref="IAnonymizationAuditStore"/>.
    /// </para>
    /// <para>
    /// <b>Auto-registration:</b>
    /// When <see cref="AnonymizationOptions.AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the specified assemblies are scanned for <see cref="AnonymizeAttribute"/>,
    /// <see cref="PseudonymizeAttribute"/>, and <see cref="TokenizeAttribute"/>
    /// decorations and the discovered fields are logged at startup.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaAnonymization(options =>
    /// {
    ///     options.EnforcementMode = AnonymizationEnforcementMode.Block;
    ///     options.AddHealthCheck = true;
    ///     options.AssembliesToScan.Add(typeof(Program).Assembly);
    /// });
    ///
    /// // With custom implementations (register before AddEncinaAnonymization)
    /// services.AddSingleton&lt;IKeyProvider, AzureKeyVaultKeyProvider&gt;();
    /// services.AddSingleton&lt;IAnonymizationAuditStore, DatabaseAnonymizationAuditStore&gt;();
    /// services.AddEncinaAnonymization(options =>
    /// {
    ///     options.EnforcementMode = AnonymizationEnforcementMode.Warn;
    ///     options.AutoRegisterFromAttributes = false;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaAnonymization(
        this IServiceCollection services,
        Action<AnonymizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<AnonymizationOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<AnonymizationOptions>, AnonymizationOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default store implementations (TryAdd allows override by satellite providers)
        services.TryAddSingleton<IAnonymizationAuditStore, InMemoryAnonymizationAuditStore>();
        services.TryAddSingleton<IKeyProvider, InMemoryKeyProvider>();
        services.TryAddSingleton<ITokenMappingStore, InMemoryTokenMappingStore>();

        // Register default high-level service implementations
        services.TryAddSingleton<IAnonymizer, DefaultAnonymizer>();
        services.TryAddSingleton<IPseudonymizer, DefaultPseudonymizer>();
        services.TryAddSingleton<ITokenizer, DefaultTokenizer>();
        services.TryAddSingleton<IRiskAssessor, DefaultRiskAssessor>();

        // Register anonymization technique strategies
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAnonymizationTechnique, GeneralizationTechnique>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAnonymizationTechnique, SuppressionTechnique>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAnonymizationTechnique, PerturbationTechnique>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAnonymizationTechnique, SwappingTechnique>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAnonymizationTechnique, DataMaskingTechnique>());

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(AnonymizationPipelineBehavior<,>));

        // Instantiate options to inspect flags for health check and auto-registration
        var optionsInstance = new AnonymizationOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<AnonymizationHealthCheck>(
                    AnonymizationHealthCheck.DefaultName,
                    tags: AnonymizationHealthCheck.Tags);
        }

        if (optionsInstance.AutoRegisterFromAttributes)
        {
            var assembliesToScan = optionsInstance.AssembliesToScan.Count > 0
                ? optionsInstance.AssembliesToScan
                : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];

            // Register descriptor and hosted service for deferred auto-registration
            services.AddSingleton(new AnonymizationAutoRegistrationDescriptor(assembliesToScan));
            services.AddHostedService<AnonymizationAutoRegistrationHostedService>();
        }

        return services;
    }
}
