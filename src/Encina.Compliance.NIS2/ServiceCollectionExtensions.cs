using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Evaluators;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.NIS2;

/// <summary>
/// Extension methods for configuring Encina NIS2 Directive compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina NIS2 Directive (EU 2022/2555) compliance services to the specified
    /// <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure <see cref="NIS2Options"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="NIS2Options"/> — Configured via the provided action, validated at first access</item>
    /// <item><see cref="INIS2ComplianceValidator"/> → <c>DefaultNIS2ComplianceValidator</c> (Scoped, using TryAdd)</item>
    /// <item><see cref="INIS2IncidentHandler"/> → <c>DefaultNIS2IncidentHandler</c> (Scoped, using TryAdd)</item>
    /// <item><see cref="ISupplyChainSecurityValidator"/> → <c>DefaultSupplyChainSecurityValidator</c> (Singleton, using TryAdd)</item>
    /// <item><see cref="IMFAEnforcer"/> → <c>DefaultMFAEnforcer</c> (Singleton, using TryAdd)</item>
    /// <item><see cref="IEncryptionValidator"/> → <c>DefaultEncryptionValidator</c> (Singleton, using TryAdd)</item>
    /// <item>10 <see cref="INIS2MeasureEvaluator"/> implementations (Singleton, one per Art. 21(2) measure)</item>
    /// <item><see cref="NIS2CompliancePipelineBehavior{TRequest, TResponse}"/> (Transient, using TryAdd)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Default registrations:</b>
    /// All service registrations use <c>TryAdd</c>, allowing you to register custom
    /// implementations before calling this method. For example, register a custom
    /// <see cref="IMFAEnforcer"/> integrated with your identity provider, or a custom
    /// <see cref="IEncryptionValidator"/> that checks actual infrastructure encryption status.
    /// </para>
    /// <para>
    /// <b>Conditional registrations:</b>
    /// <list type="bullet">
    /// <item>Health check: Only registered when <see cref="NIS2Options.AddHealthCheck"/> is <c>true</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic NIS2 compliance setup
    /// services.AddEncinaNIS2(options =>
    /// {
    ///     options.EntityType = NIS2EntityType.Essential;
    ///     options.Sector = NIS2Sector.DigitalInfrastructure;
    ///     options.EnforcementMode = NIS2EnforcementMode.Block;
    ///     options.EnforceMFA = true;
    ///     options.EnforceEncryption = true;
    ///     options.CompetentAuthority = "bsi@bsi.bund.de";
    ///     options.HasRiskAnalysisPolicy = true;
    ///     options.HasIncidentHandlingProcedures = true;
    ///     options.HasBusinessContinuityPlan = true;
    ///
    ///     options.EncryptedDataCategories.Add("PII");
    ///     options.EncryptedEndpoints.Add("https://api.example.com");
    ///
    ///     options.AddSupplier("payment-provider", supplier =>
    ///     {
    ///         supplier.Name = "PayCorp";
    ///         supplier.RiskLevel = SupplierRiskLevel.High;
    ///         supplier.LastAssessmentAtUtc = DateTimeOffset.UtcNow.AddMonths(-3);
    ///     });
    /// });
    ///
    /// // With custom MFA enforcer (register before AddEncinaNIS2)
    /// services.AddSingleton&lt;IMFAEnforcer, AzureAdMFAEnforcer&gt;();
    /// services.AddEncinaNIS2(options =>
    /// {
    ///     options.EntityType = NIS2EntityType.Important;
    ///     options.Sector = NIS2Sector.Manufacturing;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaNIS2(
        this IServiceCollection services,
        Action<NIS2Options>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // ---------------------------------------------------------------
        // 1. Configure and validate options
        // ---------------------------------------------------------------
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<NIS2Options>(_ => { });
        }

        services.TryAddSingleton<IValidateOptions<NIS2Options>, NIS2OptionsValidator>();

        // ---------------------------------------------------------------
        // 2. Ensure TimeProvider is available
        // ---------------------------------------------------------------
        services.TryAddSingleton(TimeProvider.System);

        // ---------------------------------------------------------------
        // 3. Register core compliance services (TryAdd allows custom override)
        // ---------------------------------------------------------------
        services.TryAddScoped<INIS2ComplianceValidator, DefaultNIS2ComplianceValidator>();
        services.TryAddScoped<INIS2IncidentHandler, DefaultNIS2IncidentHandler>();
        services.TryAddSingleton<ISupplyChainSecurityValidator, DefaultSupplyChainSecurityValidator>();
        services.TryAddSingleton<IMFAEnforcer, DefaultMFAEnforcer>();
        services.TryAddSingleton<IEncryptionValidator, DefaultEncryptionValidator>();

        // ---------------------------------------------------------------
        // 4. Register 10 measure evaluators (Art. 21(2)(a)-(j))
        // ---------------------------------------------------------------
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INIS2MeasureEvaluator, RiskAnalysisEvaluator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INIS2MeasureEvaluator, IncidentHandlingEvaluator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INIS2MeasureEvaluator, BusinessContinuityEvaluator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INIS2MeasureEvaluator, SupplyChainSecurityEvaluator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INIS2MeasureEvaluator, NetworkSecurityEvaluator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INIS2MeasureEvaluator, EffectivenessAssessmentEvaluator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INIS2MeasureEvaluator, CyberHygieneEvaluator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INIS2MeasureEvaluator, CryptographyEvaluator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INIS2MeasureEvaluator, HumanResourcesSecurityEvaluator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INIS2MeasureEvaluator, MultiFactorAuthenticationEvaluator>());

        // ---------------------------------------------------------------
        // 5. Register pipeline behavior
        // ---------------------------------------------------------------
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(NIS2CompliancePipelineBehavior<,>));

        // ---------------------------------------------------------------
        // 6. Conditional registrations
        // ---------------------------------------------------------------
        var optionsInstance = new NIS2Options();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<Health.NIS2ComplianceHealthCheck>(
                    Health.NIS2ComplianceHealthCheck.DefaultName,
                    tags: Health.NIS2ComplianceHealthCheck.Tags);
        }

        return services;
    }
}
