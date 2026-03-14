using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Health;
using Encina.Compliance.CrossBorderTransfer.Notifications;
using Encina.Compliance.CrossBorderTransfer.Pipeline;
using Encina.Compliance.CrossBorderTransfer.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.CrossBorderTransfer;

/// <summary>
/// Extension methods for configuring Encina cross-border transfer compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina cross-border transfer compliance services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="CrossBorderTransferOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="CrossBorderTransferOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="ITIAService"/> → <see cref="DefaultTIAService"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="ISCCService"/> → <see cref="DefaultSCCService"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="IApprovedTransferService"/> → <see cref="DefaultApprovedTransferService"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="ITransferValidator"/> → <see cref="DefaultTransferValidator"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="ITIARiskAssessor"/> → <see cref="DefaultTIARiskAssessor"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="TransferBlockingPipelineBehavior{TRequest, TResponse}"/> (Transient)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a custom
    /// <see cref="ITIARiskAssessor"/> with specialized risk assessment logic.
    /// </para>
    /// <para>
    /// <b>Marten aggregates:</b>
    /// Call <see cref="CrossBorderTransferMartenExtensions.AddCrossBorderTransferAggregates"/>
    /// separately to register the event-sourced aggregate repositories with Marten.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaCrossBorderTransfer(options =>
    /// {
    ///     options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
    ///     options.DefaultSourceCountryCode = "DE";
    ///     options.TIARiskThreshold = 0.6;
    ///     options.AddHealthCheck = true;
    /// });
    ///
    /// // Register Marten aggregates separately
    /// services.AddCrossBorderTransferAggregates();
    ///
    /// // With custom implementations (register before AddEncinaCrossBorderTransfer)
    /// services.AddScoped&lt;ITIARiskAssessor, MyCustomRiskAssessor&gt;();
    /// services.AddEncinaCrossBorderTransfer();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaCrossBorderTransfer(
        this IServiceCollection services,
        Action<CrossBorderTransferOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<CrossBorderTransferOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<CrossBorderTransferOptions>, CrossBorderTransferOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default service implementations (TryAdd allows override)
        services.TryAddScoped<ITIAService, DefaultTIAService>();
        services.TryAddScoped<ISCCService, DefaultSCCService>();
        services.TryAddScoped<IApprovedTransferService, DefaultApprovedTransferService>();
        services.TryAddScoped<ITransferValidator, DefaultTransferValidator>();
        services.TryAddScoped<ITIARiskAssessor, DefaultTIARiskAssessor>();

        // Register pipeline behavior (Transient — must always be in pipeline)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransferBlockingPipelineBehavior<,>));

        // Conditional health check registration
        var optionsInstance = new CrossBorderTransferOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<CrossBorderTransferHealthCheck>(
                    CrossBorderTransferHealthCheck.DefaultName,
                    tags: CrossBorderTransferHealthCheck.Tags);
        }

        // Conditional expiration monitoring registration
        if (optionsInstance.EnableExpirationMonitoring)
        {
            services.AddHostedService<TransferExpirationMonitor>();
        }

        return services;
    }
}
