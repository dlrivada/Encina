using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Default implementation of <see cref="IRegionContextProvider"/> that resolves the current
/// region from configuration with a static fallback to <see cref="DataResidencyOptions.DefaultRegion"/>.
/// </summary>
/// <remarks>
/// <para>
/// The default implementation provides a simple, configuration-based region resolution
/// that returns <see cref="DataResidencyOptions.DefaultRegion"/> as the current region.
/// This is suitable for single-region deployments or as a baseline implementation.
/// </para>
/// <para>
/// For multi-region deployments, replace this with a custom implementation that resolves
/// the region from HTTP headers (e.g., <c>X-Region</c>), cloud metadata services,
/// geo-IP resolution, or tenant configuration.
/// </para>
/// <para>
/// The resolved region represents the "source" region in cross-border transfer
/// validation — i.e., the region where the processing request was initiated.
/// </para>
/// </remarks>
public sealed class DefaultRegionContextProvider : IRegionContextProvider
{
    private readonly DataResidencyOptions _options;
    private readonly ILogger<DefaultRegionContextProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRegionContextProvider"/> class.
    /// </summary>
    /// <param name="options">Configuration options containing the default region.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public DefaultRegionContextProvider(
        IOptions<DataResidencyOptions> options,
        ILogger<DefaultRegionContextProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Region>> GetCurrentRegionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_options.DefaultRegion is not null)
        {
            _logger.LogDebug(
                "Resolved current region to '{RegionCode}' from default configuration",
                _options.DefaultRegion.Code);

            return ValueTask.FromResult<Either<EncinaError, Region>>(
                Right<EncinaError, Region>(_options.DefaultRegion));
        }

        _logger.LogWarning("Could not resolve current region: no DefaultRegion configured");

        return ValueTask.FromResult<Either<EncinaError, Region>>(
            Left<EncinaError, Region>(
                DataResidencyErrors.RegionNotResolved(
                    "No DefaultRegion configured in DataResidencyOptions. "
                    + "Set DataResidencyOptions.DefaultRegion or register a custom IRegionContextProvider.")));
    }
}
