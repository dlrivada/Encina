using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Health;
using Encina.Audit.Marten.Projections;
using Encina.Marten;
using Encina.Security.Audit;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Audit.Marten;

/// <summary>
/// Extension methods for configuring Encina Marten event-sourced audit trail services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Marten event-sourced audit trail services to the specified <see cref="IServiceCollection"/>,
    /// replacing the default <see cref="IOperationAuditStore"/> and <see cref="IReadAuditStore"/> with
    /// Marten-backed implementations using temporal crypto-shredding.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="MartenOperationAuditOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="MartenOperationAuditOptions"/> — Configured via the provided action</item>
    /// <item><see cref="ITemporalKeyProvider"/> → <see cref="MartenTemporalKeyProvider"/> (Scoped)</item>
    /// <item><see cref="AuditEventEncryptor"/> (Scoped)</item>
    /// <item><see cref="IOperationAuditStore"/> → <see cref="MartenOperationAuditStore"/> (Scoped, replaces default InMemory)</item>
    /// <item><see cref="IReadAuditStore"/> → <see cref="MartenReadAuditStore"/> (Scoped, replaces default InMemory)</item>
    /// <item><see cref="IConfigureOptions{StoreOptions}"/> → <see cref="ConfigureMartenOperationAuditProjections"/>
    /// (registers async projections and document indexes)</item>
    /// <item>A startup guard that stops the host when the application left Marten's projection error flags
    /// (<c>SkipApplyErrors</c>, <c>SkipSerializationErrors</c>, store-wide) skipping failing events; the application
    /// sets them to <c>false</c> itself, or opts out with
    /// <see cref="MartenOperationAuditOptions.AllowSkippedProjectionErrors"/> (logged)</item>
    /// <item><see cref="MartenOperationAuditHealthCheck"/> (optional, when <see cref="MartenOperationAuditOptions.AddHealthCheck"/> is true)</item>
    /// <item><see cref="MartenOperationAuditRetentionService"/> (optional, when <see cref="MartenOperationAuditOptions.EnableAutoPurge"/> is true)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Prerequisites:</b> This package requires Marten to be configured in the DI container
    /// (via <c>AddMarten()</c>) and <c>AddEncinaAudit()</c> to be called for the audit pipeline behavior.
    /// <c>AddEncinaAudit()</c> requires an <see cref="IPiiMasker"/> to be registered, or the host fails at startup.
    /// </para>
    /// <para>
    /// <b>Store replacement:</b> Unlike satellite packages that use <c>TryAdd</c>, this method uses
    /// <c>Replace</c> for <see cref="IOperationAuditStore"/> and <see cref="IReadAuditStore"/> to override the
    /// default <see cref="InMemoryOperationAuditStore"/> / <see cref="InMemoryReadAuditStore"/> registrations
    /// from <c>AddEncinaAudit()</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 1. Configure Marten
    /// services.AddMarten(options =>
    /// {
    ///     options.Connection("Host=localhost;Database=myapp;...");
    /// });
    ///
    /// // 2. Add core audit services (pipeline behavior, factory); the audit module requires a PII masker
    /// services.AddSingleton&lt;IPiiMasker, DefaultSensitiveDataRedactor&gt;();
    /// services.AddEncinaAudit(options =>
    /// {
    ///     options.AuditAllCommands = true;
    /// });
    ///
    /// // 3. Add Marten event-sourced audit store (replaces InMemory)
    /// services.AddEncinaAuditMarten(options =>
    /// {
    ///     options.TemporalGranularity = TemporalKeyGranularity.Monthly;
    ///     options.RetentionPeriod = TimeSpan.FromDays(2555); // 7 years (SOX)
    ///     options.EnableAutoPurge = true;
    ///     options.AddHealthCheck = true;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <exception cref="OptionsValidationException">
    /// Thrown when <see cref="MartenOperationAuditOptions.RetentionPolicyId"/> is set but empty or whitespace. The
    /// same validation runs again at host startup (<c>ValidateOnStart</c>).
    /// </exception>
    public static IServiceCollection AddEncinaAuditMarten(
        this IServiceCollection services,
        Action<MartenOperationAuditOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Validate eagerly so a plain BuildServiceProvider (no host, no ValidateOnStart) is protected too.
        var optionsInstance = ValidatedOptions(configure);

        // Configure options
        services.AddOptions<MartenOperationAuditOptions>()
            .Configure(options => configure?.Invoke(options))
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<MartenOperationAuditOptions>, MartenOperationAuditOptionsValidator>());

        // Ensure TimeProvider is available
        services.TryAddSingleton(TimeProvider.System);

        // Ensure ILoggerFactory is available for ConfigureMartenOperationAuditProjections to build
        // typed loggers. AddLogging() is idempotent and pay-for-what-you-use: all of its
        // internal registrations use TryAdd, so a host that has already configured logging
        // providers (Console, Serilog, etc.) keeps winning. Using a bare NullLoggerFactory
        // TryAddSingleton here would introduce a registration-order footgun — if this
        // extension runs before the host's own AddLogging() call, NullLoggerFactory would
        // claim the ILoggerFactory slot and silently swallow the host's configured logs.
        services.AddLogging();

        // Register temporal key provider (scoped — uses IDocumentSession)
        services.TryAddScoped<ITemporalKeyProvider, MartenTemporalKeyProvider>();

        // Register audit event encryptor (scoped — depends on ITemporalKeyProvider)
        services.AddScoped<AuditEventEncryptor>();

        // Replace default audit stores with Marten implementations
        services.Replace(ServiceDescriptor.Scoped<IOperationAuditStore, MartenOperationAuditStore>());
        services.Replace(ServiceDescriptor.Scoped<IReadAuditStore, MartenReadAuditStore>());

        // Register Marten projection configuration (async projections + document indexes)
        services.AddSingleton<IConfigureOptions<StoreOptions>, ConfigureMartenOperationAuditProjections>();
        services.AddEncinaMartenStoreOptionsBridge();

        // Fail closed at startup when the application's Marten error flags would skip audit events (D22).
        services.AddHostedService<AuditProjectionErrorPolicyStartupGuard>();

        AddOptInServices(services, optionsInstance);

        return services;
    }

    // Opt-in registrations: the health check and the auto-purge background service.
    private static void AddOptInServices(IServiceCollection services, MartenOperationAuditOptions options)
    {
        if (options.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<MartenOperationAuditHealthCheck>(
                    MartenOperationAuditHealthCheck.DefaultName,
                    tags: MartenOperationAuditHealthCheck.Tags);
        }

        if (options.EnableAutoPurge)
        {
            services.AddHostedService<MartenOperationAuditRetentionService>();
        }
    }

    private static MartenOperationAuditOptions ValidatedOptions(Action<MartenOperationAuditOptions>? configure)
    {
        var options = new MartenOperationAuditOptions();
        configure?.Invoke(options);
        var validation = new MartenOperationAuditOptionsValidator().Validate(Options.DefaultName, options);
        return validation.Failed
            ? throw new OptionsValidationException(Options.DefaultName, typeof(MartenOperationAuditOptions), validation.Failures)
            : options;
    }
}
