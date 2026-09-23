using Encina.Compliance.DataResidency.Model;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Default implementation of <see cref="IAdequacyDecisionProvider"/> using the built-in
/// <see cref="RegionRegistry"/> and optional user-configured additional regions.
/// </summary>
/// <remarks>
/// <para>
/// Provides adequacy status based on the well-known EU adequacy decisions as of 2025,
/// including all 27 EU member states, 3 EEA-only countries (Iceland, Liechtenstein, Norway),
/// and 15 third countries with formal adequacy decisions (Art. 45).
/// </para>
/// <para>
/// Extensible via <see cref="DataResidencyOptions.AdditionalAdequateRegions"/> to support
/// custom regions (e.g., private cloud zones) that should be treated as adequate.
/// </para>
/// <para>
/// EEA member states are implicitly considered adequate because GDPR applies directly
/// within the EEA. Transfers between EEA countries do not constitute "international
/// transfers" under GDPR Chapter V.
/// </para>
/// </remarks>
public sealed class DefaultAdequacyDecisionProvider : IAdequacyDecisionProvider
{
    private readonly HashSet<Region> _adequateRegions;
    private readonly IReadOnlyList<Region> _adequateRegionsList;
    private readonly ILogger<DefaultAdequacyDecisionProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAdequacyDecisionProvider"/> class.
    /// </summary>
    /// <param name="options">Configuration options containing additional adequate regions.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public DefaultAdequacyDecisionProvider(
        IOptions<DataResidencyOptions> options,
        ILogger<DefaultAdequacyDecisionProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        var opts = options.Value;

        // Build the combined set: EEA + adequacy countries + user-configured additional regions
        _adequateRegions = new HashSet<Region>(
            RegionRegistry.EEACountries
                .Concat(RegionRegistry.AdequacyCountries)
                .Concat(opts.AdditionalAdequateRegions));

        _adequateRegionsList = _adequateRegions.ToList().AsReadOnly();

        if (opts.AdditionalAdequateRegions.Count > 0)
        {
            _logger.LogInformation(
                "Adequacy decision provider initialized with {BuiltInCount} built-in and {AdditionalCount} additional adequate regions",
                RegionRegistry.EEACountries.Count + RegionRegistry.AdequacyCountries.Count,
                opts.AdditionalAdequateRegions.Count);
        }
    }

    /// <inheritdoc />
    public bool HasAdequacy(Region region, bool isRecipientCertified = false)
    {
        ArgumentNullException.ThrowIfNull(region);

        // EEA countries have GDPR directly.
        if (region.IsEU || region.IsEEA)
        {
            return true;
        }

        // Prefer the canonical registered region's metadata (matched by code) when available,
        // since callers sometimes construct a bare Region with only Code/Country set (e.g.
        // DefaultTransferValidator.CheckAdequacyDecision). Region equality is code-based, so
        // TryGetValue resolves to the registry's/options' fully-populated instance when one
        // exists. Membership in the adequate set (built-in adequacy countries plus any
        // caller-configured DataResidencyOptions.AdditionalAdequateRegions) counts as adequate
        // even if the caller's own Region instance did not set HasAdequacyDecision itself.
        if (_adequateRegions.TryGetValue(region, out var registered))
        {
            // A partial adequacy decision (e.g. US DPF, Canada PIPEDA) only covers
            // certified/in-scope recipients. Without that confirmation, it is not adequate.
            return !registered.RequiresRecipientCertification || isRecipientCertified;
        }

        if (!region.HasAdequacyDecision)
        {
            return false;
        }

        return !region.RequiresRecipientCertification || isRecipientCertified;
    }

    /// <inheritdoc />
    public IReadOnlyList<Region> GetAdequateRegions() => _adequateRegionsList;
}
