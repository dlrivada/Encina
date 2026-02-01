namespace Encina.AspNetCore;

/// <summary>
/// Extension methods for adding HTTP audit context information to <see cref="IRequestContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// These extensions enable audit trail functionality by capturing HTTP-specific metadata
/// such as client IP addresses and User-Agent strings.
/// </para>
/// <para>
/// The metadata keys used are:
/// <list type="bullet">
/// <item><c>Encina.Audit.IpAddress</c> - Client IP address</item>
/// <item><c>Encina.Audit.UserAgent</c> - User-Agent header value</item>
/// </list>
/// </para>
/// <para>
/// These keys are read by <c>Encina.Security.Audit.DefaultAuditEntryFactory</c>
/// when creating audit entries.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Adding audit context to a request context
/// var context = RequestContext.Create()
///     .WithIpAddress("192.168.1.1")
///     .WithUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
///
/// // Reading audit context
/// var ip = context.GetIpAddress();
/// var userAgent = context.GetUserAgent();
/// </code>
/// </example>
public static class HttpAuditContextExtensions
{
    /// <summary>
    /// Metadata key for storing client IP address.
    /// </summary>
    public const string IpAddressKey = "Encina.Audit.IpAddress";

    /// <summary>
    /// Metadata key for storing User-Agent header value.
    /// </summary>
    public const string UserAgentKey = "Encina.Audit.UserAgent";

    /// <summary>
    /// Gets the client IP address from the request context metadata.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>The IP address if present in metadata; otherwise, <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <remarks>
    /// The IP address is typically set by <see cref="EncinaContextMiddleware"/> from HTTP context.
    /// Used by audit trail components to record the source of requests.
    /// </remarks>
    public static string? GetIpAddress(this IRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.TryGetValue(IpAddressKey, out var value)
            ? value as string
            : null;
    }

    /// <summary>
    /// Creates a new context with the specified IP address in metadata.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="ipAddress">The IP address to store, or <c>null</c> to not include it.</param>
    /// <returns>A new context instance with the IP address in metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Follows the immutable pattern - returns a new context instance without modifying the original.
    /// </para>
    /// <para>
    /// If <paramref name="ipAddress"/> is <c>null</c> or empty, the metadata key is still set
    /// but with a <c>null</c> value.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var contextWithIp = context.WithIpAddress("192.168.1.100");
    /// </code>
    /// </example>
    public static IRequestContext WithIpAddress(this IRequestContext context, string? ipAddress)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.WithMetadata(IpAddressKey, ipAddress);
    }

    /// <summary>
    /// Gets the User-Agent header value from the request context metadata.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>The User-Agent if present in metadata; otherwise, <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <remarks>
    /// The User-Agent is typically set by <see cref="EncinaContextMiddleware"/> from HTTP headers.
    /// Used by audit trail components to identify the client application or browser.
    /// </remarks>
    public static string? GetUserAgent(this IRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.TryGetValue(UserAgentKey, out var value)
            ? value as string
            : null;
    }

    /// <summary>
    /// Creates a new context with the specified User-Agent in metadata.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="userAgent">The User-Agent string to store, or <c>null</c> to not include it.</param>
    /// <returns>A new context instance with the User-Agent in metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Follows the immutable pattern - returns a new context instance without modifying the original.
    /// </para>
    /// <para>
    /// If <paramref name="userAgent"/> is <c>null</c> or empty, the metadata key is still set
    /// but with a <c>null</c> value.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var contextWithAgent = context.WithUserAgent("MyApp/1.0");
    /// </code>
    /// </example>
    public static IRequestContext WithUserAgent(this IRequestContext context, string? userAgent)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.WithMetadata(UserAgentKey, userAgent);
    }
}
