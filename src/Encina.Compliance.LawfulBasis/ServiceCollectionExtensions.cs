using System.Reflection;

using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.AutoRegistration;
using Encina.Compliance.LawfulBasis.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using GDPR = Encina.Compliance.GDPR;

namespace Encina.Compliance.LawfulBasis;

/// <summary>
/// Extension methods for configuring Encina lawful basis compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina lawful basis validation services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="LawfulBasisOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="LawfulBasisOptions"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="ILawfulBasisService"/> → <see cref="DefaultLawfulBasisService"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="ILawfulBasisProvider"/> → <see cref="DefaultLawfulBasisProvider"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="GDPR.ILawfulBasisSubjectIdExtractor"/> → <see cref="GDPR.DefaultLawfulBasisSubjectIdExtractor"/> (Scoped, using TryAdd)</item>
    /// <item><see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/> (Transient)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Pipeline ordering:</b> The <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/>
    /// is registered using <c>AddTransient</c> (not TryAdd) to ensure it is always present.
    /// Call <c>AddEncinaLawfulBasis</c> before <c>AddEncinaGDPR</c> for correct ordering.
    /// </para>
    /// <para>
    /// <b>Marten aggregates:</b>
    /// Call <see cref="LawfulBasisMartenExtensions.AddLawfulBasisAggregates"/>
    /// separately to register the event-sourced aggregate repositories with Marten.
    /// </para>
    /// <para>
    /// <b>Auto-registration:</b>
    /// When <see cref="LawfulBasisOptions.AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the specified assemblies are scanned for <see cref="GDPR.LawfulBasisAttribute"/>
    /// decorations and registrations are created in the <see cref="ILawfulBasisService"/> at startup.
    /// Additionally, <see cref="LawfulBasisOptions.DefaultBases"/> are registered programmatically.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup
    /// services.AddEncinaLawfulBasis(options =>
    /// {
    ///     options.EnforcementMode = LawfulBasisEnforcementMode.Block;
    ///     options.ScanAssemblyContaining&lt;Program&gt;();
    /// });
    ///
    /// // Register Marten aggregate repositories
    /// services.AddLawfulBasisAggregates();
    ///
    /// // Full setup with defaults and health check
    /// services.AddEncinaLawfulBasis(options =>
    /// {
    ///     options.EnforcementMode = LawfulBasisEnforcementMode.Block;
    ///     options.ScanAssemblyContaining&lt;Program&gt;();
    ///     options.DefaultBasis&lt;GetProfileQuery&gt;(GDPR.LawfulBasis.Contract);
    ///     options.AddHealthCheck = true;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaLawfulBasis(
        this IServiceCollection services,
        Action<LawfulBasisOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure and validate options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<LawfulBasisOptions>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<LawfulBasisOptions>, LawfulBasisOptionsValidator>();

        // Ensure TimeProvider is available (generic host registers it, but standalone DI may not)
        services.TryAddSingleton(TimeProvider.System);

        // Register default implementations (TryAdd allows override)
        services.TryAddScoped<ILawfulBasisService, DefaultLawfulBasisService>();
        services.TryAddScoped<ILawfulBasisProvider, DefaultLawfulBasisProvider>();
        services.TryAddScoped<GDPR.ILawfulBasisSubjectIdExtractor, GDPR.DefaultLawfulBasisSubjectIdExtractor>();

        // Register pipeline behavior (AddTransient, not TryAdd — always registered)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LawfulBasisValidationPipelineBehavior<,>));

        // Instantiate options to inspect flags for health check and auto-registration
        var optionsInstance = new LawfulBasisOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<Health.LawfulBasisHealthCheck>(
                    Health.LawfulBasisHealthCheck.DefaultName,
                    tags: Health.LawfulBasisHealthCheck.Tags);
        }

        if (optionsInstance.AutoRegisterFromAttributes || optionsInstance.DefaultBases.Count > 0)
        {
            List<Assembly> assembliesToScan;
            if (optionsInstance.AssembliesToScan.Count > 0)
            {
                assembliesToScan = optionsInstance.AssembliesToScan.ToList();
            }
            else if (optionsInstance.AutoRegisterFromAttributes)
            {
                assembliesToScan = [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];
            }
            else
            {
                assembliesToScan = new List<Assembly>();
            }

            // Register descriptor and hosted service for deferred auto-registration
            services.AddSingleton(new LawfulBasisAutoRegistrationDescriptor(
                assembliesToScan,
                new Dictionary<Type, GDPR.LawfulBasis>(optionsInstance.DefaultBases)));
            services.AddHostedService<LawfulBasisAutoRegistrationHostedService>();
        }

        return services;
    }
}
