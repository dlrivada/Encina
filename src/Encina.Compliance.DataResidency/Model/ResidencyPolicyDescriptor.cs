namespace Encina.Compliance.DataResidency.Model;

/// <summary>
/// Describes a data residency policy for a specific data category.
/// </summary>
/// <remarks>
/// <para>
/// A residency policy descriptor defines which regions are allowed for storing and
/// processing data of a given category, whether an adequacy decision is required
/// for cross-border transfers, and which transfer legal bases are permitted.
/// </para>
/// <para>
/// Policies can be defined at startup via fluent configuration in
/// <c>DataResidencyOptions</c> or stored persistently via <c>IResidencyPolicyStore</c>
/// for runtime management. The <c>DataResidencyPipelineBehavior</c> evaluates these
/// policies before processing requests decorated with <c>[DataResidency]</c> attributes.
/// </para>
/// <para>
/// Per GDPR Article 44 (general principle for transfers), any transfer of personal data
/// to a third country shall take place only if the conditions of Chapter V are complied with.
/// Residency policy descriptors encode these conditions as enforceable rules.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Policy: healthcare data must stay in the EU
/// var policy = ResidencyPolicyDescriptor.Create(
///     dataCategory: "healthcare-data",
///     allowedRegions: RegionRegistry.EUMemberStates,
///     requireAdequacyDecision: true);
///
/// // Policy: marketing data can go to adequate countries with SCCs
/// var policy = ResidencyPolicyDescriptor.Create(
///     dataCategory: "marketing-data",
///     allowedRegions: RegionRegistry.EEACountries.Concat(RegionRegistry.AdequacyCountries).ToList(),
///     requireAdequacyDecision: false,
///     allowedTransferBases: [TransferLegalBasis.AdequacyDecision, TransferLegalBasis.StandardContractualClauses]);
/// </code>
/// </example>
public sealed record ResidencyPolicyDescriptor
{
    /// <summary>
    /// The data category this policy applies to.
    /// </summary>
    /// <remarks>
    /// Examples: "personal-data", "financial-records", "healthcare-data",
    /// "marketing-consent". Each category should have at most one policy.
    /// Must match the categories used in <c>[DataResidency]</c> attributes.
    /// </remarks>
    public required string DataCategory { get; init; }

    /// <summary>
    /// The regions where data of this category is allowed to be stored and processed.
    /// </summary>
    /// <remarks>
    /// An empty list means no region restrictions — data can be processed anywhere
    /// (subject to other transfer validation rules). For strict EU-only policies,
    /// use <see cref="RegionRegistry.EUMemberStates"/>.
    /// </remarks>
    public required IReadOnlyList<Region> AllowedRegions { get; init; }

    /// <summary>
    /// Whether an EU adequacy decision is required for cross-border transfers of this data.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, data can only be transferred to regions that have an EU adequacy
    /// decision (per GDPR Art. 45) or are within the EEA. Transfers to non-adequate
    /// countries are blocked even if SCCs or BCRs are available.
    /// </remarks>
    public required bool RequireAdequacyDecision { get; init; }

    /// <summary>
    /// The transfer legal bases allowed for cross-border transfers of this data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When non-empty, only the specified legal bases are accepted for cross-border
    /// transfers. When empty, all legal bases are potentially allowed (subject to
    /// the <see cref="RequireAdequacyDecision"/> flag and transfer validator rules).
    /// </para>
    /// <para>
    /// Common configurations:
    /// - Strict: <c>[AdequacyDecision]</c> only
    /// - Standard: <c>[AdequacyDecision, StandardContractualClauses, BindingCorporateRules]</c>
    /// - Permissive: empty (all bases allowed)
    /// </para>
    /// </remarks>
    public required IReadOnlyList<TransferLegalBasis> AllowedTransferBases { get; init; }

    /// <summary>
    /// Creates a new residency policy descriptor.
    /// </summary>
    /// <param name="dataCategory">The data category this policy applies to.</param>
    /// <param name="allowedRegions">The regions where data is allowed.</param>
    /// <param name="requireAdequacyDecision">Whether an adequacy decision is required for transfers.</param>
    /// <param name="allowedTransferBases">The allowed transfer legal bases.</param>
    /// <returns>A new <see cref="ResidencyPolicyDescriptor"/>.</returns>
    public static ResidencyPolicyDescriptor Create(
        string dataCategory,
        IReadOnlyList<Region> allowedRegions,
        bool requireAdequacyDecision = false,
        IReadOnlyList<TransferLegalBasis>? allowedTransferBases = null) =>
        new()
        {
            DataCategory = dataCategory,
            AllowedRegions = allowedRegions,
            RequireAdequacyDecision = requireAdequacyDecision,
            AllowedTransferBases = allowedTransferBases ?? []
        };
}
