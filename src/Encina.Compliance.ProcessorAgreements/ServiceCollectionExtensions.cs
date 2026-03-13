using Encina.Compliance.ProcessorAgreements.Health;
using Encina.Compliance.ProcessorAgreements.Scheduling;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Extension methods for configuring Encina Processor Agreements compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Processor Agreements compliance services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="ProcessorAgreementOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="ProcessorAgreementOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="IProcessorRegistry"/> → <see cref="InMemoryProcessorRegistry"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IDPAStore"/> → <see cref="InMemoryDPAStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IProcessorAuditStore"/> → <see cref="InMemoryProcessorAuditStore"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IDPAValidator"/> → <see cref="DefaultDPAValidator"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="ProcessorValidationPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// <item><see cref="CheckDPAExpirationHandler"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a database-backed
    /// <see cref="IDPAStore"/> from <c>Encina.EntityFrameworkCore</c> or <c>Encina.Dapper</c>.
    /// </para>
    /// <para>
    /// <b>Conditional registrations:</b>
    /// <list type="bullet">
    /// <item>Health check: Only registered when <see cref="ProcessorAgreementOptions.AddHealthCheck"/> is <c>true</c></item>
    /// <item>Expiration monitoring: Only registers the handler when <see cref="ProcessorAgreementOptions.EnableExpirationMonitoring"/> is <c>true</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaProcessorAgreements(options =>
    /// {
    ///     options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
    ///     options.MaxSubProcessorDepth = 3;
    /// });
    ///
    /// // With BlockWithoutValidDPA alias
    /// services.AddEncinaProcessorAgreements(options =>
    /// {
    ///     options.BlockWithoutValidDPA = true;
    ///     options.AddHealthCheck = true;
    ///     options.EnableExpirationMonitoring = true;
    /// });
    ///
    /// // With custom store (register before AddEncinaProcessorAgreements)
    /// services.AddSingleton&lt;IDPAStore, DatabaseDPAStore&gt;();
    /// services.AddEncinaProcessorAgreements(options =>
    /// {
    ///     options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaProcessorAgreements(
        this IServiceCollection services,
        Action<ProcessorAgreementOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<ProcessorAgreementOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<ProcessorAgreementOptions>, ProcessorAgreementOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default implementations (TryAdd allows override by provider packages)
        services.TryAddSingleton<IProcessorRegistry, InMemoryProcessorRegistry>();
        services.TryAddSingleton<IDPAStore, InMemoryDPAStore>();
        services.TryAddSingleton<IProcessorAuditStore, InMemoryProcessorAuditStore>();
        services.TryAddScoped<IDPAValidator, DefaultDPAValidator>();

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(ProcessorValidationPipelineBehavior<,>));

        // Register expiration monitoring handler (always available for manual invocation)
        services.TryAddTransient<ICommandHandler<CheckDPAExpirationCommand, LanguageExt.Unit>, CheckDPAExpirationHandler>();

        // Evaluate conditional features from a local options instance
        var optionsInstance = new ProcessorAgreementOptions();
        configure?.Invoke(optionsInstance);

        // Conditional: Health check
        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<ProcessorAgreementHealthCheck>(
                    ProcessorAgreementHealthCheck.DefaultName,
                    tags: ProcessorAgreementHealthCheck.Tags);
        }

        return services;
    }
}
