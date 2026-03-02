using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Default implementation of <see cref="IDataResidencyPolicy"/> that resolves allowed regions
/// from the policy store.
/// </summary>
/// <remarks>
/// <para>
/// Resolves residency policies by querying <see cref="IResidencyPolicyStore.GetByCategoryAsync"/>
/// for category-specific policies. If no policy exists and enforcement mode is
/// <see cref="DataResidencyEnforcementMode.Warn"/> or <see cref="DataResidencyEnforcementMode.Disabled"/>,
/// all regions are considered allowed. In <see cref="DataResidencyEnforcementMode.Block"/> mode,
/// a missing policy returns an error.
/// </para>
/// <para>
/// Per GDPR Article 44 (general principle for transfers), controllers must establish explicit
/// residency policies for all categories of personal data. This service enables programmatic
/// resolution of those policies.
/// </para>
/// </remarks>
public sealed class DefaultDataResidencyPolicy : IDataResidencyPolicy
{
    private readonly IResidencyPolicyStore _policyStore;
    private readonly DataResidencyOptions _options;
    private readonly ILogger<DefaultDataResidencyPolicy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDataResidencyPolicy"/> class.
    /// </summary>
    /// <param name="policyStore">Store for residency policy lookups.</param>
    /// <param name="options">Configuration options for the data residency module.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public DefaultDataResidencyPolicy(
        IResidencyPolicyStore policyStore,
        IOptions<DataResidencyOptions> options,
        ILogger<DefaultDataResidencyPolicy> logger)
    {
        ArgumentNullException.ThrowIfNull(policyStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _policyStore = policyStore;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> IsAllowedAsync(
        string dataCategory,
        Region targetRegion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);
        ArgumentNullException.ThrowIfNull(targetRegion);

        var policyResult = await _policyStore.GetByCategoryAsync(dataCategory, cancellationToken);

        return policyResult.Match(
            Right: optPolicy => optPolicy.Match(
                Some: policy =>
                {
                    // Empty allowed regions means no restrictions — all regions allowed
                    if (policy.AllowedRegions.Count == 0)
                    {
                        _logger.LogDebug(
                            "Region '{RegionCode}' allowed for category '{DataCategory}' (no region restrictions)",
                            targetRegion.Code, dataCategory);
                        return Right<EncinaError, bool>(true);
                    }

                    var isAllowed = policy.AllowedRegions.Contains(targetRegion);

                    _logger.LogDebug(
                        "Region '{RegionCode}' {Outcome} for category '{DataCategory}'",
                        targetRegion.Code, isAllowed ? "allowed" : "denied", dataCategory);

                    return Right<EncinaError, bool>(isAllowed);
                },
                None: () =>
                {
                    _logger.LogDebug(
                        "No residency policy found for category '{DataCategory}'",
                        dataCategory);
                    return Left<EncinaError, bool>(DataResidencyErrors.PolicyNotFound(dataCategory));
                }),
            Left: error => Left<EncinaError, bool>(error));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Region>>> GetAllowedRegionsAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        var policyResult = await _policyStore.GetByCategoryAsync(dataCategory, cancellationToken);

        return policyResult.Match(
            Right: optPolicy => optPolicy.Match(
                Some: policy =>
                {
                    _logger.LogDebug(
                        "Resolved {RegionCount} allowed regions for category '{DataCategory}'",
                        policy.AllowedRegions.Count, dataCategory);
                    return Right<EncinaError, IReadOnlyList<Region>>(policy.AllowedRegions);
                },
                None: () =>
                {
                    _logger.LogDebug(
                        "No residency policy found for category '{DataCategory}'",
                        dataCategory);
                    return Left<EncinaError, IReadOnlyList<Region>>(
                        DataResidencyErrors.PolicyNotFound(dataCategory));
                }),
            Left: error => Left<EncinaError, IReadOnlyList<Region>>(error));
    }
}
