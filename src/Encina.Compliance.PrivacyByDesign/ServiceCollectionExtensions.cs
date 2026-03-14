using Encina.Compliance.PrivacyByDesign.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Extension methods for configuring Encina Privacy by Design compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Privacy by Design compliance services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="PrivacyByDesignOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="PrivacyByDesignOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="IDataMinimizationAnalyzer"/> → <see cref="DefaultDataMinimizationAnalyzer"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IPurposeRegistry"/> → <see cref="InMemoryPurposeRegistry"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IPrivacyByDesignValidator"/> → <see cref="DefaultPrivacyByDesignValidator"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="DataMinimizationPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a database-backed
    /// <see cref="IPurposeRegistry"/> from <c>Encina.EntityFrameworkCore</c> or <c>Encina.Dapper</c>.
    /// </para>
    /// <para>
    /// <b>Purpose pre-population:</b>
    /// Purposes registered via <see cref="PrivacyByDesignOptions.AddPurpose(string, Action{PurposeBuilder})"/>
    /// are automatically populated into the <see cref="IPurposeRegistry"/> at startup using
    /// a hosted service.
    /// </para>
    /// <para>
    /// <b>Conditional registrations:</b>
    /// <list type="bullet">
    /// <item>Health check: Only registered when <see cref="PrivacyByDesignOptions.AddHealthCheck"/> is <see langword="true"/></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaPrivacyByDesign(options =>
    /// {
    ///     options.EnforcementMode = PrivacyByDesignEnforcementMode.Block;
    ///     options.MinimizationScoreThreshold = 0.7;
    ///     options.PrivacyLevel = PrivacyLevel.Maximum;
    /// });
    ///
    /// // With BlockOnViolation alias
    /// services.AddEncinaPrivacyByDesign(options =>
    /// {
    ///     options.BlockOnViolation = true;
    ///     options.AddHealthCheck = true;
    /// });
    ///
    /// // With purpose registration
    /// services.AddEncinaPrivacyByDesign(options =>
    /// {
    ///     options.EnforcementMode = PrivacyByDesignEnforcementMode.Warn;
    ///
    ///     options.AddPurpose("Order Processing", purpose =>
    ///     {
    ///         purpose.Description = "Processing personal data for order fulfillment.";
    ///         purpose.LegalBasis = "Contract";
    ///         purpose.AllowedFields.AddRange(["ProductId", "Quantity", "ShippingAddress"]);
    ///     });
    /// });
    ///
    /// // With custom registry (register before AddEncinaPrivacyByDesign)
    /// services.AddSingleton&lt;IPurposeRegistry, DatabasePurposeRegistry&gt;();
    /// services.AddEncinaPrivacyByDesign(options =>
    /// {
    ///     options.EnforcementMode = PrivacyByDesignEnforcementMode.Block;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaPrivacyByDesign(
        this IServiceCollection services,
        Action<PrivacyByDesignOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<PrivacyByDesignOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<PrivacyByDesignOptions>, PrivacyByDesignOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default implementations (TryAdd allows override by provider packages)
        services.TryAddSingleton<IDataMinimizationAnalyzer, DefaultDataMinimizationAnalyzer>();
        services.TryAddSingleton<IPurposeRegistry, InMemoryPurposeRegistry>();
        services.TryAddScoped<IPrivacyByDesignValidator, DefaultPrivacyByDesignValidator>();

        // Register pipeline behavior
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(DataMinimizationPipelineBehavior<,>));

        // Evaluate conditional features from a local options instance
        var optionsInstance = new PrivacyByDesignOptions();
        configure?.Invoke(optionsInstance);

        // Pre-populate purposes from options into the registry at startup
        if (optionsInstance.PurposeBuilders.Count > 0)
        {
            services.AddHostedService<PurposeRegistrationHostedService>();
        }

        // Conditional: Health check
        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<PrivacyByDesignHealthCheck>(
                    PrivacyByDesignHealthCheck.DefaultName,
                    tags: PrivacyByDesignHealthCheck.Tags);
        }

        return services;
    }
}
