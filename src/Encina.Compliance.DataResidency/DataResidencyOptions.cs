using System.Reflection;

using Encina.Compliance.DataResidency.Model;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Configuration options for the Data Residency module.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the data residency enforcement service,
/// including the default deployment region, enforcement mode, data location tracking,
/// and audit trail preferences. All options have sensible defaults aligned with GDPR
/// Chapter V (Articles 44-49) international transfer requirements.
/// </para>
/// <para>
/// Register via <c>AddEncinaDataResidency(options => { ... })</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaDataResidency(options =>
/// {
///     options.DefaultRegion = RegionRegistry.DE;
///     options.EnforcementMode = DataResidencyEnforcementMode.Block;
///     options.TrackDataLocations = true;
///     options.AssembliesToScan.Add(typeof(Program).Assembly);
///     options.AddPolicy("healthcare-data", policy =>
///     {
///         policy.AllowEU();
///         policy.RequireAdequacyDecision();
///     });
/// });
/// </code>
/// </example>
public sealed class DataResidencyOptions
{
    /// <summary>
    /// Gets or sets the default deployment region for the application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used as a fallback when the <see cref="IRegionContextProvider"/> cannot resolve
    /// the current region from the request context. This typically represents the
    /// primary data center or cloud region where the application is deployed.
    /// </para>
    /// <para>
    /// When <see cref="EnforcementMode"/> is <see cref="DataResidencyEnforcementMode.Block"/>,
    /// this value should be set to ensure region resolution always succeeds.
    /// </para>
    /// </remarks>
    public Region? DefaultRegion { get; set; }

    /// <summary>
    /// Gets or sets the enforcement mode for the data residency pipeline behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how the <c>DataResidencyPipelineBehavior</c> responds when a residency
    /// policy check fails or when data would be stored in a non-compliant region:
    /// <list type="bullet">
    /// <item><description><see cref="DataResidencyEnforcementMode.Block"/>: Returns an error, blocking the response.</description></item>
    /// <item><description><see cref="DataResidencyEnforcementMode.Warn"/>: Logs a warning but allows the response through.</description></item>
    /// <item><description><see cref="DataResidencyEnforcementMode.Disabled"/>: Skips all residency checks entirely.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Default is <see cref="DataResidencyEnforcementMode.Warn"/> to support gradual adoption.
    /// Production systems should use <see cref="DataResidencyEnforcementMode.Block"/>.
    /// </para>
    /// </remarks>
    public DataResidencyEnforcementMode EnforcementMode { get; set; } = DataResidencyEnforcementMode.Warn;

    /// <summary>
    /// Gets or sets whether to track data locations via <see cref="Abstractions.IDataLocationService"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the pipeline behavior records where data is stored after
    /// successful processing. This supports GDPR Article 30 (records of processing
    /// activities) and enables compliance audits to verify data residency.
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool TrackDataLocations { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to block non-compliant cross-border transfers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the <see cref="ICrossBorderTransferValidator"/> will deny
    /// transfers to regions without an adequacy decision, appropriate safeguards,
    /// or valid derogation. When <c>false</c>, transfers are allowed with warnings.
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool BlockNonCompliantTransfers { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to register a data residency health check with <c>IHealthChecksBuilder</c>.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies the residency stores
    /// are reachable and the enforcement service is operational.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets the additional regions to consider as having an EU adequacy decision.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Extends the built-in list of adequate regions in <see cref="DefaultAdequacyDecisionProvider"/>.
    /// Use this for custom regions (e.g., private cloud zones) that should be treated as
    /// adequate for cross-border transfer purposes.
    /// </para>
    /// <para>
    /// These regions are merged with the built-in list at startup and are available
    /// via <see cref="IAdequacyDecisionProvider.GetAdequateRegions"/>.
    /// </para>
    /// </remarks>
    public List<Region> AdditionalAdequateRegions { get; } = [];

    // --- Auto-Registration ---

    /// <summary>
    /// Gets or sets whether to automatically scan assemblies for
    /// <see cref="Attributes.DataResidencyAttribute"/> decorations at startup and create
    /// matching residency policy entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the <c>DataResidencyAutoRegistrationHostedService</c> scans the
    /// assemblies in <see cref="AssembliesToScan"/> for types decorated with
    /// <see cref="Attributes.DataResidencyAttribute"/>. For each discovered data category
    /// without an existing policy, a new residency policy is
    /// created in the store.
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool AutoRegisterFromAttributes { get; set; } = true;

    /// <summary>
    /// Gets the assemblies to scan for <see cref="Attributes.DataResidencyAttribute"/> decorations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only used when <see cref="AutoRegisterFromAttributes"/> is <c>true</c>.
    /// Add assemblies that contain your types decorated with
    /// <see cref="Attributes.DataResidencyAttribute"/>.
    /// </para>
    /// <para>
    /// If empty and <see cref="AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the entry assembly is used as a fallback.
    /// </para>
    /// </remarks>
    public List<Assembly> AssembliesToScan { get; } = [];

    // --- Fluent Policy Configuration ---

    /// <summary>
    /// Gets the residency policies configured via the fluent API.
    /// </summary>
    /// <remarks>
    /// Policies added via <see cref="AddPolicy"/> are created in the store at startup
    /// alongside attribute-discovered policies.
    /// </remarks>
    internal List<DataResidencyFluentPolicyEntry> ConfiguredPolicies { get; } = [];

    /// <summary>
    /// Adds a residency policy for a specific data category using the fluent builder API.
    /// </summary>
    /// <param name="dataCategory">The data category to create a policy for.</param>
    /// <param name="configure">An action to configure the policy via <see cref="ResidencyPolicyBuilder"/>.</param>
    /// <returns>This options instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Provides a fluent alternative to attribute-based policy declaration.
    /// Policies configured here are created in the <see cref="Abstractions.IResidencyPolicyService"/>
    /// at startup if a policy for the same category doesn't already exist.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaDataResidency(options =>
    /// {
    ///     options.AddPolicy("healthcare-data", policy =>
    ///     {
    ///         policy.AllowEU();
    ///         policy.RequireAdequacyDecision();
    ///     });
    ///
    ///     options.AddPolicy("financial-data", policy =>
    ///     {
    ///         policy.AllowRegions(RegionRegistry.DE, RegionRegistry.FR);
    ///         policy.AllowTransferBasis(TransferLegalBasis.StandardContractualClauses);
    ///     });
    /// });
    /// </code>
    /// </example>
    public DataResidencyOptions AddPolicy(string dataCategory, Action<ResidencyPolicyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(dataCategory);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ResidencyPolicyBuilder(dataCategory);
        configure(builder);
        ConfiguredPolicies.Add(builder.Build());

        return this;
    }
}
