using Encina.Compliance.PrivacyByDesign.Model;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Configuration options for Privacy by Design enforcement.
/// </summary>
/// <remarks>
/// <para>
/// Controls how the <see cref="DataMinimizationPipelineBehavior{TRequest, TResponse}"/>
/// enforces data minimization, purpose limitation, and default privacy requirements.
/// </para>
/// <para>
/// Per GDPR Article 25(1), the controller shall implement "appropriate technical and
/// organisational measures [...] designed to implement data-protection principles,
/// such as data minimisation, in an effective manner."
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaPrivacyByDesign(options =>
/// {
///     options.EnforcementMode = PrivacyByDesignEnforcementMode.Block;
///     options.MinimizationScoreThreshold = 0.7;
///     options.PrivacyLevel = PrivacyLevel.Maximum;
///
///     // Register a global processing purpose
///     options.AddPurpose("Order Processing", purpose =>
///     {
///         purpose.Description = "Processing personal data for order fulfillment.";
///         purpose.LegalBasis = "Contract";
///         purpose.AllowedFields.AddRange(["ProductId", "Quantity", "ShippingAddress"]);
///     });
///
///     // Register a module-scoped purpose
///     options.AddPurpose("Marketing Analytics", "marketing", purpose =>
///     {
///         purpose.Description = "Processing data for marketing analytics.";
///         purpose.LegalBasis = "Consent";
///         purpose.AllowedFields.AddRange(["Email", "Preferences"]);
///     });
/// });
/// </code>
/// </example>
public sealed class PrivacyByDesignOptions
{
    // --- Enforcement ---

    /// <summary>
    /// Gets or sets the enforcement mode for the Privacy by Design pipeline behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how the <c>DataMinimizationPipelineBehavior</c> responds when
    /// privacy violations are detected:
    /// <list type="bullet">
    /// <item><description><see cref="PrivacyByDesignEnforcementMode.Block"/>: Returns an error, blocking the request.</description></item>
    /// <item><description><see cref="PrivacyByDesignEnforcementMode.Warn"/>: Logs a warning but allows the request through.</description></item>
    /// <item><description><see cref="PrivacyByDesignEnforcementMode.Disabled"/>: Skips all enforcement entirely.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Default is <see cref="PrivacyByDesignEnforcementMode.Warn"/> to support gradual adoption.
    /// Production systems should use <see cref="PrivacyByDesignEnforcementMode.Block"/>.
    /// </para>
    /// </remarks>
    public PrivacyByDesignEnforcementMode EnforcementMode { get; set; } = PrivacyByDesignEnforcementMode.Warn;

    /// <summary>
    /// Gets or sets whether requests that have privacy violations should be blocked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a convenience alias for setting <see cref="EnforcementMode"/> to
    /// <see cref="PrivacyByDesignEnforcementMode.Block"/>. Setting this to <see langword="true"/> sets
    /// <c>EnforcementMode = Block</c>; reading it returns <see langword="true"/> when
    /// <c>EnforcementMode</c> is <see cref="PrivacyByDesignEnforcementMode.Block"/>.
    /// </para>
    /// <para>
    /// Provides a more intuitive configuration experience:
    /// <code>
    /// options.BlockOnViolation = true;
    /// // Equivalent to: options.EnforcementMode = PrivacyByDesignEnforcementMode.Block;
    /// </code>
    /// </para>
    /// </remarks>
    public bool BlockOnViolation
    {
        get => EnforcementMode == PrivacyByDesignEnforcementMode.Block;
        set
        {
            if (value)
            {
                EnforcementMode = PrivacyByDesignEnforcementMode.Block;
            }
        }
    }

    // --- Data Minimization ---

    /// <summary>
    /// Gets or sets the minimum acceptable minimization score (0.0–1.0).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the computed minimization score for a request falls below this threshold,
    /// the pipeline behavior treats it as a violation. A higher threshold is stricter.
    /// </para>
    /// <para>
    /// Default is <c>0.0</c>, meaning any number of unnecessary fields with values
    /// triggers a violation (based on field-level <see cref="MinimizationSeverity"/>).
    /// Set to <c>0.7</c> or higher for production environments to enforce a minimum
    /// ratio of necessary-to-total fields.
    /// </para>
    /// </remarks>
    public double MinimizationScoreThreshold { get; set; }

    // --- Privacy Level ---

    /// <summary>
    /// Gets or sets the overall privacy enforcement level.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls which checks are applied during validation:
    /// <list type="bullet">
    /// <item><description><see cref="PrivacyLevel.Minimum"/>: Only <c>[NotStrictlyNecessary]</c> fields.</description></item>
    /// <item><description><see cref="PrivacyLevel.Standard"/>: Minimization + purpose limitation.</description></item>
    /// <item><description><see cref="PrivacyLevel.Maximum"/>: Minimization + purpose limitation + default privacy.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Default is <see cref="PrivacyLevel.Standard"/>.
    /// </para>
    /// </remarks>
    public PrivacyLevel PrivacyLevel { get; set; } = PrivacyLevel.Standard;

    // --- Audit ---

    /// <summary>
    /// Gets or sets whether the pipeline behavior should record audit trail entries
    /// for enforcement actions.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="true"/>. Supports the accountability principle
    /// (Article 5(2)) by maintaining a record of Privacy by Design enforcement decisions.
    /// </remarks>
    public bool TrackAuditTrail { get; set; } = true;

    // --- Health check ---

    /// <summary>
    /// Gets or sets whether to register a Privacy by Design health check.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="false"/>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    // --- Purpose Registration ---

    /// <summary>
    /// Gets the list of purpose builders configured via <see cref="AddPurpose(string, Action{PurposeBuilder})"/>
    /// or <see cref="AddPurpose(string, string, Action{PurposeBuilder})"/>.
    /// </summary>
    /// <remarks>
    /// These are used during DI registration to populate the <see cref="IPurposeRegistry"/>
    /// with initial purpose definitions.
    /// </remarks>
    internal List<PurposeBuilder> PurposeBuilders { get; } = [];

    /// <summary>
    /// Registers a global processing purpose definition.
    /// </summary>
    /// <param name="name">The purpose name (e.g., "Order Processing").</param>
    /// <param name="configure">Action to configure the purpose definition.</param>
    /// <returns>This options instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Purposes registered here are pre-populated into the <see cref="IPurposeRegistry"/>
    /// during DI registration. This provides a declarative way to define purposes
    /// alongside service configuration.
    /// </para>
    /// <para>
    /// For module-scoped purposes, use <see cref="AddPurpose(string, string, Action{PurposeBuilder})"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.AddPurpose("Order Processing", purpose =>
    /// {
    ///     purpose.Description = "Processing personal data for order fulfillment.";
    ///     purpose.LegalBasis = "Contract";
    ///     purpose.AllowedFields.AddRange(["ProductId", "Quantity", "ShippingAddress"]);
    /// });
    /// </code>
    /// </example>
    public PrivacyByDesignOptions AddPurpose(string name, Action<PurposeBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new PurposeBuilder(name);
        configure(builder);
        PurposeBuilders.Add(builder);
        return this;
    }

    /// <summary>
    /// Registers a module-scoped processing purpose definition.
    /// </summary>
    /// <param name="name">The purpose name (e.g., "Marketing Analytics").</param>
    /// <param name="moduleId">The module identifier for modular monolith scoping.</param>
    /// <param name="configure">Action to configure the purpose definition.</param>
    /// <returns>This options instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Module-scoped purposes are isolated to a specific module and take precedence
    /// over global purposes with the same name when resolving within that module.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.AddPurpose("Marketing Analytics", "marketing", purpose =>
    /// {
    ///     purpose.Description = "Processing data for marketing analytics.";
    ///     purpose.LegalBasis = "Consent";
    ///     purpose.AllowedFields.AddRange(["Email", "Preferences"]);
    /// });
    /// </code>
    /// </example>
    public PrivacyByDesignOptions AddPurpose(string name, string moduleId, Action<PurposeBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(moduleId);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new PurposeBuilder(name) { ModuleId = moduleId };
        configure(builder);
        PurposeBuilders.Add(builder);
        return this;
    }
}
