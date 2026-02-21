using Encina.Security.Audit;
using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Health;
using Encina.Security.PII.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Security.PII;

/// <summary>
/// Extension methods for configuring Encina PII masking and data protection services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina PII masking and data protection services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="PIIOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="PIIOptions"/> — Configured via the provided action</item>
    /// <item>Built-in <see cref="IMaskingStrategy"/> implementations — Registered as singletons
    /// for each <see cref="PIIType"/> (Email, Phone, CreditCard, SSN, Name, Address, DateOfBirth,
    /// IPAddress, Custom)</item>
    /// <item><see cref="IPIIMasker"/> → <see cref="PIIMasker"/> (Singleton, using TryAdd)</item>
    /// <item><see cref="IPiiMasker"/> → <see cref="PIIMasker"/> (Singleton, replaces <c>NullPiiMasker</c>)</item>
    /// <item><see cref="PIIMaskingPipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAddEnumerable)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Important — Registration order:</b>
    /// Call <c>AddEncinaPII()</c> <b>after</b> <c>AddEncinaAudit()</c> to correctly replace
    /// the default <c>NullPiiMasker</c> with the PII-aware <see cref="PIIMasker"/> implementation.
    /// The <see cref="IPiiMasker"/> registration uses <c>AddSingleton</c> (not <c>TryAdd</c>)
    /// to ensure the real masker takes precedence over the no-op default.
    /// </para>
    /// <para>
    /// <b>Custom strategies:</b>
    /// Register custom <see cref="IMaskingStrategy"/> implementations in the DI container
    /// before calling this method, then configure them via <see cref="PIIOptions.AddStrategy{TStrategy}"/>.
    /// Custom strategies override the built-in ones for the specified <see cref="PIIType"/>.
    /// </para>
    /// <para>
    /// <b>Conditional features:</b>
    /// Health checks, tracing, and metrics are registered only when their respective
    /// <see cref="PIIOptions"/> flags are enabled (<see cref="PIIOptions.AddHealthCheck"/>,
    /// <see cref="PIIOptions.EnableTracing"/>, <see cref="PIIOptions.EnableMetrics"/>).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 1. Register Audit first, then PII to replace NullPiiMasker
    /// services.AddEncinaAudit(options =>
    /// {
    ///     options.AuditAllCommands = true;
    /// });
    ///
    /// services.AddEncinaPII(options =>
    /// {
    ///     options.MaskInResponses = true;
    ///     options.MaskInLogs = true;
    ///     options.MaskInAuditTrails = true;
    ///     options.DefaultMode = MaskingMode.Partial;
    /// });
    ///
    /// // 2. With custom strategy override
    /// services.AddSingleton&lt;CustomPhoneMasker&gt;();
    /// services.AddEncinaPII(options =>
    /// {
    ///     options.AddStrategy&lt;CustomPhoneMasker&gt;(PIIType.Phone);
    /// });
    ///
    /// // 3. With observability enabled
    /// services.AddEncinaPII(options =>
    /// {
    ///     options.EnableTracing = true;
    ///     options.EnableMetrics = true;
    ///     options.AddHealthCheck = true;
    /// });
    ///
    /// // 4. Minimal setup (all defaults)
    /// services.AddEncinaPII();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaPII(
        this IServiceCollection services,
        Action<PIIOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<PIIOptions>(_ => { });
        }

        // Register built-in masking strategies as singletons (TryAdd allows override)
        services.TryAddSingleton<EmailMaskingStrategy>();
        services.TryAddSingleton<PhoneMaskingStrategy>();
        services.TryAddSingleton<CreditCardMaskingStrategy>();
        services.TryAddSingleton<SSNMaskingStrategy>();
        services.TryAddSingleton<NameMaskingStrategy>();
        services.TryAddSingleton<AddressMaskingStrategy>();
        services.TryAddSingleton<DateOfBirthMaskingStrategy>();
        services.TryAddSingleton<IPAddressMaskingStrategy>();
        services.TryAddSingleton<FullMaskingStrategy>();

        // Register custom strategy types from options (if configured)
        var optionsInstance = new PIIOptions();
        configure?.Invoke(optionsInstance);

        foreach (var (_, strategyType) in optionsInstance.CustomStrategies)
        {
            services.TryAddSingleton(strategyType);
        }

        // Register IPIIMasker (TryAdd allows override with custom implementation)
        services.TryAddSingleton<IPIIMasker, PIIMasker>();

        // Register IPiiMasker → PIIMasker (AddSingleton, NOT TryAdd)
        // This replaces the NullPiiMasker registered by AddEncinaAudit().
        // Must be called AFTER AddEncinaAudit() for correct replacement.
        services.AddSingleton<IPiiMasker>(sp => (IPiiMasker)sp.GetRequiredService<IPIIMasker>());

        // Register pipeline behavior (TryAddEnumerable prevents duplicate registration
        // while allowing multiple different pipeline behaviors to coexist)
        services.TryAddEnumerable(ServiceDescriptor.Transient(
            typeof(IPipelineBehavior<,>),
            typeof(PIIMaskingPipelineBehavior<,>)));

        // Conditional: Health check registration
        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<PIIHealthCheck>(
                    PIIHealthCheck.DefaultName,
                    tags: PIIHealthCheck.Tags);
        }

        // Note: Tracing (ActivitySource) and Metrics (Meter) are handled internally
        // by PIIMasker and PIIMaskingPipelineBehavior when their respective options
        // (EnableTracing, EnableMetrics) are enabled. No additional DI registration
        // is required — activities and counters are static and self-contained.

        return services;
    }
}
