using Encina.Compliance.DataResidency.Model;

using LanguageExt;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Provides the current region context for the executing request.
/// </summary>
/// <remarks>
/// <para>
/// The region context provider resolves the region associated with the current execution
/// context — typically the region where the application instance is deployed or the region
/// determined by the incoming request's origin. This information is used by the
/// <see cref="IRegionRouter"/> and the pipeline behavior to determine source and target
/// regions for residency enforcement.
/// </para>
/// <para>
/// Common implementations include:
/// - Static configuration: a fixed region set at deployment time (e.g., via environment variable).
/// - HTTP header-based: extracting the region from a request header (e.g., <c>X-Region</c>).
/// - Cloud metadata: reading the deployment region from cloud provider metadata services.
/// - Geo-IP: determining the region from the client's IP address.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get the region for the current request
/// var region = await regionContextProvider.GetCurrentRegionAsync(cancellationToken);
///
/// // Use in a pipeline behavior
/// region.Match(
///     Right: r => Console.WriteLine($"Processing in region: {r.Code}"),
///     Left: e => Console.WriteLine($"Could not determine region: {e.Message}"));
/// </code>
/// </example>
public interface IRegionContextProvider
{
    /// <summary>
    /// Resolves the current region for the executing request or application context.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The <see cref="Region"/> associated with the current execution context,
    /// or an <see cref="EncinaError"/> if the region could not be determined.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The resolved region represents the "source" region in cross-border transfer
    /// validation — i.e., the region from which data originates or where the processing
    /// request was initiated.
    /// </para>
    /// <para>
    /// Implementations should be deterministic for the same execution context and should
    /// not change during the processing of a single request.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Region>> GetCurrentRegionAsync(
        CancellationToken cancellationToken = default);
}
