using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Default implementation of <see cref="IRegionRouter"/> that routes requests to the
/// current region if it complies with the data category's residency policy.
/// </summary>
/// <remarks>
/// <para>
/// The default router uses the following logic:
/// 1. Resolve the current region from <see cref="IRegionContextProvider"/>.
/// 2. Check if the current region is allowed for the data category via <see cref="IResidencyPolicyService"/>.
/// 3. If allowed, route to the current region.
/// 4. If not allowed, attempt to select the first allowed region from the policy.
/// 5. If no region can be determined, return an error.
/// </para>
/// <para>
/// For advanced routing (proximity-based, load-balanced, or failover-aware), replace
/// this with a custom implementation that considers additional factors.
/// </para>
/// </remarks>
public sealed class DefaultRegionRouter : IRegionRouter
{
    private readonly IResidencyPolicyService _residencyPolicyService;
    private readonly IRegionContextProvider _regionContextProvider;
    private readonly ILogger<DefaultRegionRouter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRegionRouter"/> class.
    /// </summary>
    /// <param name="residencyPolicyService">Policy service for checking region compliance.</param>
    /// <param name="regionContextProvider">Provider for the current region context.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public DefaultRegionRouter(
        IResidencyPolicyService residencyPolicyService,
        IRegionContextProvider regionContextProvider,
        ILogger<DefaultRegionRouter> logger)
    {
        ArgumentNullException.ThrowIfNull(residencyPolicyService);
        ArgumentNullException.ThrowIfNull(regionContextProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _residencyPolicyService = residencyPolicyService;
        _regionContextProvider = regionContextProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Region>> DetermineTargetRegionAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Resolve the current region
        var regionResult = await _regionContextProvider.GetCurrentRegionAsync(cancellationToken);

        return await regionResult.MatchAsync(
            RightAsync: async currentRegion =>
            {
                // Step 2: Extract data category from the request type's [DataResidency] attribute
                var dataCategory = ResolveDataCategory<TRequest>();

                if (dataCategory is null)
                {
                    // No [DataResidency] attribute — route to current region by default
                    _logger.LogDebug(
                        "No [DataResidency] attribute on request type '{RequestType}' — routing to current region '{RegionCode}'",
                        typeof(TRequest).Name, currentRegion.Code);
                    return Right<EncinaError, Region>(currentRegion);
                }

                // Step 3: Check if current region is allowed
                var isAllowedResult = await _residencyPolicyService.IsAllowedAsync(
                    dataCategory, currentRegion, cancellationToken);

                return await isAllowedResult.MatchAsync(
                    RightAsync: async isAllowed =>
                    {
                        if (isAllowed)
                        {
                            _logger.LogDebug(
                                "Current region '{RegionCode}' is compliant for category '{DataCategory}' — routing here",
                                currentRegion.Code, dataCategory);
                            return Right<EncinaError, Region>(currentRegion);
                        }

                        // Step 4: Current region not allowed — find first allowed region
                        var allowedRegionsResult = await _residencyPolicyService.GetAllowedRegionsAsync(
                            dataCategory, cancellationToken);

                        return allowedRegionsResult.Match(
                            Right: allowedRegions =>
                            {
                                if (allowedRegions.Count == 0)
                                {
                                    // No restrictions — use current region
                                    return Right<EncinaError, Region>(currentRegion);
                                }

                                var targetRegion = allowedRegions[0];
                                _logger.LogInformation(
                                    "Current region '{CurrentRegion}' not allowed for category '{DataCategory}' — "
                                    + "routing to '{TargetRegion}'",
                                    currentRegion.Code, dataCategory, targetRegion.Code);
                                return Right<EncinaError, Region>(targetRegion);
                            },
                            Left: error => Left<EncinaError, Region>(error));
                    },
                    Left: error =>
                    {
                        // Policy not found — route to current region (policy errors handled upstream)
                        _logger.LogDebug(
                            "Could not check policy for category '{DataCategory}': {ErrorMessage} — "
                            + "routing to current region '{RegionCode}'",
                            dataCategory, error.Message, currentRegion.Code);
                        return Right<EncinaError, Region>(currentRegion);
                    });
            },
            Left: error => Left<EncinaError, Region>(error));
    }

    private static string? ResolveDataCategory<TRequest>()
    {
        // Look for [DataResidency("category")] attribute on the request type
        var attribute = typeof(TRequest)
            .GetCustomAttributes(inherit: true)
            .OfType<Attribute>()
            .FirstOrDefault(a => a.GetType().Name == "DataResidencyAttribute");

        if (attribute is null)
        {
            return null;
        }

        // Try to read the DataCategory property from the attribute
        var dataCategoryProperty = attribute.GetType().GetProperty("DataCategory");
        return dataCategoryProperty?.GetValue(attribute) as string;
    }
}
