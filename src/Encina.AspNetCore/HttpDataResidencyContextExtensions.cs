namespace Encina.AspNetCore;

/// <summary>
/// Extension methods for adding HTTP data-region context to <see cref="IRequestContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// These extensions store the <c>X-Data-Region</c> header value in <see cref="IRequestContext"/>
/// metadata so that <c>HttpRegionContextProvider</c> can resolve the region from the request
/// context without directly accessing <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>.
/// </para>
/// <para>
/// The metadata key used is <see cref="DataRegionKey"/>:
/// <c>Encina.DataResidency.Region</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Adding data region to a request context
/// var context = RequestContext.Create()
///     .WithDataRegion("DE");
///
/// // Reading data region
/// var region = context.GetDataRegion(); // "DE" or null
/// </code>
/// </example>
public static class HttpDataResidencyContextExtensions
{
    /// <summary>
    /// Metadata key for storing the data region code extracted from the HTTP header.
    /// </summary>
    public const string DataRegionKey = "Encina.DataResidency.Region";

    /// <summary>
    /// Gets the data region code from the request context metadata.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>The region code string if present in metadata; otherwise, <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <remarks>
    /// The data region is typically set by <see cref="EncinaContextMiddleware"/> from the
    /// <c>X-Data-Region</c> HTTP header. Used by <c>HttpRegionContextProvider</c> to resolve
    /// the current region for data residency enforcement.
    /// </remarks>
    public static string? GetDataRegion(this IRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.TryGetValue(DataRegionKey, out var value)
            ? value as string
            : null;
    }

    /// <summary>
    /// Creates a new context with the specified data region code in metadata.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="regionCode">The region code to store (e.g., "DE", "US", "EU"),
    /// or <c>null</c> to not include it.</param>
    /// <returns>A new context instance with the data region in metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Follows the immutable pattern — returns a new context instance without modifying the original.
    /// </para>
    /// <para>
    /// If <paramref name="regionCode"/> is <c>null</c> or empty, the metadata key is still set
    /// but with a <c>null</c> value.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var contextWithRegion = context.WithDataRegion("DE");
    /// </code>
    /// </example>
    public static IRequestContext WithDataRegion(this IRequestContext context, string? regionCode)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.WithMetadata(DataRegionKey, regionCode);
    }
}
