using Encina.Compliance.NIS2.Model;

namespace Encina.Compliance.NIS2;

/// <summary>
/// Configuration options for the NIS2 Directive compliance module.
/// </summary>
/// <remarks>
/// <para>
/// Register via <c>AddEncinaNIS2(options =&gt; { ... })</c>.
/// These options control entity classification, enforcement behavior, incident notification
/// parameters, supplier registry, and security measure configuration.
/// </para>
/// <para>
/// Per NIS2 Article 21(1), measures shall be "appropriate and proportionate" to the entity's
/// size, risk exposure, and the criticality of its services. Configuration should reflect
/// the entity's specific regulatory context.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaNIS2(options =>
/// {
///     options.EntityType = NIS2EntityType.Essential;
///     options.Sector = NIS2Sector.DigitalInfrastructure;
///     options.EnforcementMode = NIS2EnforcementMode.Block;
///     options.EnforceMFA = true;
///     options.EnforceEncryption = true;
///     options.CompetentAuthority = "bsi@bsi.bund.de";
///
///     options.AddSupplier("payment-provider", supplier =>
///     {
///         supplier.Name = "PayCorp";
///         supplier.RiskLevel = SupplierRiskLevel.High;
///     });
/// });
/// </code>
/// </example>
public sealed class NIS2Options
{
    // --- Entity classification ---

    /// <summary>
    /// Gets or sets the entity type classification under the NIS2 Directive.
    /// </summary>
    /// <remarks>Per Art. 3, determines supervisory regime and fine thresholds.</remarks>
    public NIS2EntityType EntityType { get; set; } = NIS2EntityType.Essential;

    /// <summary>
    /// Gets or sets the sector in which the entity operates.
    /// </summary>
    /// <remarks>Per Annexes I and II. Required for compliance validation.</remarks>
    public NIS2Sector Sector { get; set; }

    // --- Enforcement ---

    /// <summary>
    /// Gets or sets the enforcement mode for the NIS2 compliance pipeline behavior.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="NIS2EnforcementMode.Warn"/> to support gradual adoption.
    /// Production systems should use <see cref="NIS2EnforcementMode.Block"/>.
    /// </remarks>
    public NIS2EnforcementMode EnforcementMode { get; set; } = NIS2EnforcementMode.Warn;

    /// <summary>
    /// Gets or sets whether MFA enforcement is enabled for requests decorated with <c>[RequireMFA]</c>.
    /// </summary>
    /// <remarks>Per Art. 21(2)(j). Default is <c>true</c>.</remarks>
    public bool EnforceMFA { get; set; } = true;

    /// <summary>
    /// Gets or sets whether encryption validation is enabled.
    /// </summary>
    /// <remarks>Per Art. 21(2)(h). Default is <c>true</c>.</remarks>
    public bool EnforceEncryption { get; set; } = true;

    // --- Incident notification ---

    /// <summary>
    /// Gets or sets the early warning deadline in hours from incident detection.
    /// </summary>
    /// <remarks>Per Art. 23(4)(a). Default is 24 hours.</remarks>
    public int IncidentNotificationHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the contact information for the competent authority or CSIRT.
    /// </summary>
    /// <remarks>
    /// Per Art. 23(1), entities must notify their CSIRT or competent authority.
    /// This should contain the authority's email or contact endpoint.
    /// </remarks>
    public string? CompetentAuthority { get; set; }

    // --- Management accountability ---

    /// <summary>
    /// Gets or sets the management accountability record.
    /// </summary>
    /// <remarks>Per Art. 20, management body members must approve and oversee cybersecurity measures.</remarks>
    public ManagementAccountabilityRecord? ManagementAccountability { get; set; }

    // --- Notifications & health ---

    /// <summary>
    /// Gets or sets whether to publish domain notifications for NIS2 compliance events.
    /// </summary>
    public bool PublishNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to register the NIS2 compliance health check.
    /// </summary>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets the timeout for external service calls during compliance evaluation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// External calls include <c>IKeyProvider</c> (encryption infrastructure verification),
    /// <c>IBreachNotificationService</c> (incident forwarding), <c>ICacheProvider</c> (result caching),
    /// and <c>IProcessingActivityRegistry</c> (GDPR alignment).
    /// </para>
    /// <para>
    /// When a <c>ResiliencePipelineProvider&lt;string&gt;</c> is registered in DI with a pipeline
    /// named <c>"nis2-external"</c>, that pipeline is used instead (with retry, circuit breaker, etc.).
    /// This timeout serves as the fallback when no resilience pipeline is configured.
    /// </para>
    /// <para>Default is 5 seconds.</para>
    /// </remarks>
    public TimeSpan ExternalCallTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the time-to-live for cached compliance evaluation results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>Encina.Caching</c>'s <c>ICacheProvider</c> is registered,
    /// compliance validation results are cached for this duration to avoid re-evaluating
    /// all 10 measures on every request. Set to <see cref="TimeSpan.Zero"/> to disable caching.
    /// </para>
    /// <para>Default is 5 minutes.</para>
    /// </remarks>
    public TimeSpan ComplianceCacheTTL { get; set; } = TimeSpan.FromMinutes(5);

    // --- Supply chain ---

    /// <summary>
    /// Gets the registered suppliers for supply chain security validation.
    /// </summary>
    internal Dictionary<string, SupplierConfiguration> Suppliers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a supplier for NIS2 supply chain security assessment (Art. 21(2)(d)).
    /// </summary>
    /// <param name="supplierId">Unique identifier for the supplier, used in <c>[NIS2SupplyChainCheck]</c> attributes.</param>
    /// <param name="configure">Action to configure the supplier details.</param>
    /// <returns>This options instance for method chaining.</returns>
    public NIS2Options AddSupplier(string supplierId, Action<SupplierConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(supplierId);
        ArgumentNullException.ThrowIfNull(configure);

        var config = new SupplierConfiguration { SupplierId = supplierId };
        configure(config);
        Suppliers[supplierId] = config;
        return this;
    }

    // --- Encryption ---

    /// <summary>
    /// Gets the data categories confirmed to be encrypted at rest.
    /// </summary>
    /// <remarks>Per Art. 21(2)(h). Add categories via <c>EncryptedDataCategories.Add("PII")</c>.</remarks>
    public HashSet<string> EncryptedDataCategories { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the endpoints confirmed to use encryption in transit.
    /// </summary>
    /// <remarks>Per Art. 21(2)(h). Add endpoints via <c>EncryptedEndpoints.Add("https://api.example.com")</c>.</remarks>
    public HashSet<string> EncryptedEndpoints { get; } = new(StringComparer.OrdinalIgnoreCase);

    // --- Measure-specific flags ---

    /// <summary>
    /// Gets or sets whether a risk analysis policy is in place.
    /// </summary>
    /// <remarks>Per Art. 21(2)(a).</remarks>
    public bool HasRiskAnalysisPolicy { get; set; }

    /// <summary>
    /// Gets or sets whether incident handling procedures are in place.
    /// </summary>
    /// <remarks>Per Art. 21(2)(b).</remarks>
    public bool HasIncidentHandlingProcedures { get; set; }

    /// <summary>
    /// Gets or sets whether business continuity plans are in place.
    /// </summary>
    /// <remarks>Per Art. 21(2)(c).</remarks>
    public bool HasBusinessContinuityPlan { get; set; }

    /// <summary>
    /// Gets or sets whether network and system security policies are in place.
    /// </summary>
    /// <remarks>Per Art. 21(2)(e).</remarks>
    public bool HasNetworkSecurityPolicy { get; set; }

    /// <summary>
    /// Gets or sets whether effectiveness assessment procedures are in place.
    /// </summary>
    /// <remarks>Per Art. 21(2)(f).</remarks>
    public bool HasEffectivenessAssessment { get; set; }

    /// <summary>
    /// Gets or sets whether cyber hygiene and training programs are in place.
    /// </summary>
    /// <remarks>Per Art. 21(2)(g).</remarks>
    public bool HasCyberHygieneProgram { get; set; }

    /// <summary>
    /// Gets or sets whether HR security and access control policies are in place.
    /// </summary>
    /// <remarks>Per Art. 21(2)(i).</remarks>
    public bool HasHumanResourcesSecurity { get; set; }
}

/// <summary>
/// Configuration for a single supplier in the supply chain registry.
/// </summary>
public sealed class SupplierConfiguration
{
    /// <summary>Unique identifier for the supplier.</summary>
    public string SupplierId { get; internal set; } = string.Empty;

    /// <summary>Display name of the supplier.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Assessed risk level.</summary>
    public SupplierRiskLevel RiskLevel { get; set; }

    /// <summary>Timestamp of the most recent security assessment (UTC).</summary>
    public DateTimeOffset? LastAssessmentAtUtc { get; set; }

    /// <summary>Certification status (e.g., "ISO 27001", "SOC 2").</summary>
    public string? CertificationStatus { get; set; }

    /// <summary>Converts this configuration to a <see cref="SupplierInfo"/> record.</summary>
    internal SupplierInfo ToSupplierInfo() => new()
    {
        SupplierId = SupplierId,
        Name = Name,
        RiskLevel = RiskLevel,
        LastAssessmentAtUtc = LastAssessmentAtUtc,
        CertificationStatus = CertificationStatus
    };
}
