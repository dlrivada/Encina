using Encina.Compliance.DataResidency.Model;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Fluent builder for configuring residency policies within
/// <see cref="DataResidencyOptions.AddPolicy"/>.
/// </summary>
/// <remarks>
/// <para>
/// This builder provides a fluent API for defining data residency policies that
/// specify which regions are allowed for specific data categories. Policies can
/// reference individual regions, EU/EEA groups, or all adequate regions.
/// </para>
/// <para>
/// Per GDPR Chapter V, controllers must identify the legal basis for international
/// data transfers. This builder captures the allowed regions and transfer bases
/// for each data category.
/// </para>
/// </remarks>
public sealed class ResidencyPolicyBuilder
{
    private readonly string _dataCategory;
    private readonly List<Region> _allowedRegions = [];
    private readonly List<TransferLegalBasis> _allowedTransferBases = [];
    private bool _requireAdequacyDecision;

    internal ResidencyPolicyBuilder(string dataCategory)
    {
        _dataCategory = dataCategory;
    }

    /// <summary>
    /// Adds specific regions to the allowed list for this data category.
    /// </summary>
    /// <param name="regions">The regions to allow.</param>
    /// <returns>This builder for chaining.</returns>
    public ResidencyPolicyBuilder AllowRegions(params Region[] regions)
    {
        ArgumentNullException.ThrowIfNull(regions);

        _allowedRegions.AddRange(regions);
        return this;
    }

    /// <summary>
    /// Adds all EU member states to the allowed list.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// Adds all 27 EU member states from <see cref="RegionRegistry.EUMemberStates"/>.
    /// Data transfer within the EU does not require additional safeguards under GDPR.
    /// </remarks>
    public ResidencyPolicyBuilder AllowEU()
    {
        _allowedRegions.AddRange(RegionRegistry.EUMemberStates);
        return this;
    }

    /// <summary>
    /// Adds all EEA countries to the allowed list.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// Adds all EEA countries from <see cref="RegionRegistry.EEACountries"/>
    /// (EU member states plus Norway, Iceland, and Liechtenstein).
    /// </remarks>
    public ResidencyPolicyBuilder AllowEEA()
    {
        _allowedRegions.AddRange(RegionRegistry.EEACountries);
        return this;
    }

    /// <summary>
    /// Adds all countries with an EU adequacy decision to the allowed list.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// Adds countries from <see cref="RegionRegistry.AdequacyCountries"/>
    /// that have received an adequacy decision from the European Commission
    /// under GDPR Article 45.
    /// </remarks>
    public ResidencyPolicyBuilder AllowAdequate()
    {
        _allowedRegions.AddRange(RegionRegistry.AdequacyCountries);
        return this;
    }

    /// <summary>
    /// Sets whether the current region must have an EU adequacy decision.
    /// </summary>
    /// <param name="require">Whether to require an adequacy decision. Default is <c>true</c>.</param>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// When <c>true</c>, the pipeline behavior will verify that the processing region
    /// has an adequacy decision (Article 45) in addition to being in the allowed list.
    /// </remarks>
    public ResidencyPolicyBuilder RequireAdequacyDecision(bool require = true)
    {
        _requireAdequacyDecision = require;
        return this;
    }

    /// <summary>
    /// Adds transfer legal bases that are acceptable for cross-border transfers
    /// involving this data category.
    /// </summary>
    /// <param name="bases">The acceptable transfer legal bases.</param>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// Per GDPR Articles 46-49, when no adequacy decision exists, transfers require
    /// appropriate safeguards such as Standard Contractual Clauses or Binding Corporate Rules.
    /// </remarks>
    public ResidencyPolicyBuilder AllowTransferBasis(params TransferLegalBasis[] bases)
    {
        ArgumentNullException.ThrowIfNull(bases);

        _allowedTransferBases.AddRange(bases);
        return this;
    }

    internal DataResidencyFluentPolicyEntry Build() => new(
        _dataCategory,
        _allowedRegions.Distinct().ToList(),
        _requireAdequacyDecision,
        _allowedTransferBases.Distinct().ToList());
}

/// <summary>
/// Internal descriptor for a residency policy configured via the fluent API.
/// </summary>
/// <param name="DataCategory">The data category.</param>
/// <param name="AllowedRegions">The allowed regions for this data category.</param>
/// <param name="RequireAdequacyDecision">Whether an adequacy decision is required.</param>
/// <param name="AllowedTransferBases">The acceptable transfer legal bases.</param>
internal sealed record DataResidencyFluentPolicyEntry(
    string DataCategory,
    IReadOnlyList<Region> AllowedRegions,
    bool RequireAdequacyDecision,
    IReadOnlyList<TransferLegalBasis> AllowedTransferBases);
