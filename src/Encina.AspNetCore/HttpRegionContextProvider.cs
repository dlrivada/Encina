using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.AspNetCore;

/// <summary>
/// HTTP-aware implementation of <see cref="IRegionContextProvider"/> that resolves the current
/// region from the HTTP request context with a multi-level fallback chain.
/// </summary>
/// <remarks>
/// <para>
/// This provider is designed for ASP.NET Core applications and uses the following resolution
/// order:
/// <list type="number">
/// <item><description>
///   <b>HTTP header</b>: Reads the <c>X-Data-Region</c> header (configurable via
///   <see cref="EncinaAspNetCoreOptions.DataRegionHeaderName"/>) from the current request.
///   The header value is looked up in <see cref="RegionRegistry"/> to obtain a full
///   <see cref="Region"/> instance. If the code is not found in the registry, a custom
///   region is created with unknown characteristics.
/// </description></item>
/// <item><description>
///   <b>Default region</b>: Falls back to <see cref="DataResidencyOptions.DefaultRegion"/>
///   if the header is absent or the HTTP context is not available.
/// </description></item>
/// </list>
/// </para>
/// <para>
/// This provider should be registered as a <b>scoped</b> service to match the HTTP request
/// lifetime. When registered via <c>AddEncinaAspNetCore()</c>, it replaces the
/// <see cref="DefaultRegionContextProvider"/> for web applications.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration (typically done in ServiceCollectionExtensions)
/// services.AddScoped&lt;IRegionContextProvider, HttpRegionContextProvider&gt;();
///
/// // Usage via dependency injection
/// public class MyHandler
/// {
///     private readonly IRegionContextProvider _regionProvider;
///
///     public MyHandler(IRegionContextProvider regionProvider)
///     {
///         _regionProvider = regionProvider;
///     }
///
///     public async Task HandleAsync()
///     {
///         var region = await _regionProvider.GetCurrentRegionAsync();
///         // region is resolved from X-Data-Region header → default
///     }
/// }
/// </code>
/// </example>
public sealed class HttpRegionContextProvider : IRegionContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DataResidencyOptions _dataResidencyOptions;
    private readonly EncinaAspNetCoreOptions _aspNetCoreOptions;
    private readonly ILogger<HttpRegionContextProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRegionContextProvider"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current HTTP context.</param>
    /// <param name="dataResidencyOptions">Data residency configuration options.</param>
    /// <param name="aspNetCoreOptions">ASP.NET Core integration options.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public HttpRegionContextProvider(
        IHttpContextAccessor httpContextAccessor,
        IOptions<DataResidencyOptions> dataResidencyOptions,
        IOptions<EncinaAspNetCoreOptions> aspNetCoreOptions,
        ILogger<HttpRegionContextProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(dataResidencyOptions);
        ArgumentNullException.ThrowIfNull(aspNetCoreOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _httpContextAccessor = httpContextAccessor;
        _dataResidencyOptions = dataResidencyOptions.Value;
        _aspNetCoreOptions = aspNetCoreOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Resolution order:
    /// <list type="number">
    /// <item><description>HTTP header (<c>X-Data-Region</c> by default)</description></item>
    /// <item><description><see cref="DataResidencyOptions.DefaultRegion"/> fallback</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// When the header contains a region code not found in <see cref="RegionRegistry"/>, a
    /// custom <see cref="Region"/> is created with <see cref="DataProtectionLevel.Unknown"/>.
    /// This allows custom cloud-region identifiers (e.g., <c>AZURE-WESTEU</c>) to be accepted
    /// from the header while still providing a valid <see cref="Region"/> object.
    /// </para>
    /// </remarks>
    public ValueTask<Either<EncinaError, Region>> GetCurrentRegionAsync(
        CancellationToken cancellationToken = default)
    {
        // 1. Try to resolve from HTTP header
        var regionFromHeader = ResolveFromHeader();
        if (regionFromHeader is not null)
        {
            return ValueTask.FromResult<Either<EncinaError, Region>>(
                Right<EncinaError, Region>(regionFromHeader));
        }

        // 2. Fallback to default region from options
        if (_dataResidencyOptions.DefaultRegion is not null)
        {
            _logger.LogDebug(
                "Data region header not present, falling back to default region '{RegionCode}'",
                _dataResidencyOptions.DefaultRegion.Code);

            return ValueTask.FromResult<Either<EncinaError, Region>>(
                Right<EncinaError, Region>(_dataResidencyOptions.DefaultRegion));
        }

        // 3. No region could be resolved
        _logger.LogWarning(
            "Could not resolve current region: no '{HeaderName}' header and no DefaultRegion configured",
            _aspNetCoreOptions.DataRegionHeaderName);

        return ValueTask.FromResult<Either<EncinaError, Region>>(
            Left<EncinaError, Region>(
                DataResidencyErrors.RegionNotResolved(
                    $"No '{_aspNetCoreOptions.DataRegionHeaderName}' header in the request "
                    + "and no DefaultRegion configured in DataResidencyOptions.")));
    }

    /// <summary>
    /// Attempts to resolve the region from the HTTP request header.
    /// </summary>
    /// <returns>The resolved <see cref="Region"/>, or <c>null</c> if the header is absent.</returns>
    private Region? ResolveFromHeader()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (!httpContext.Request.Headers.TryGetValue(
                _aspNetCoreOptions.DataRegionHeaderName, out var headerValue) ||
            string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        var regionCode = headerValue.ToString().Trim();

        // Look up in the well-known region registry first
        var knownRegion = RegionRegistry.GetByCode(regionCode);
        if (knownRegion is not null)
        {
            _logger.LogDebug(
                "Resolved region '{RegionCode}' from '{HeaderName}' header (well-known region)",
                knownRegion.Code,
                _aspNetCoreOptions.DataRegionHeaderName);

            return knownRegion;
        }

        // Create a custom region for unrecognized codes (e.g., custom cloud zones)
        _logger.LogDebug(
            "Resolved custom region '{RegionCode}' from '{HeaderName}' header (not in RegionRegistry)",
            regionCode,
            _aspNetCoreOptions.DataRegionHeaderName);

        return Region.Create(
            code: regionCode,
            country: regionCode.Length == 2 ? regionCode : "XX",
            protectionLevel: DataProtectionLevel.Unknown);
    }
}
