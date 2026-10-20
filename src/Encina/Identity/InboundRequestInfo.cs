using System.Security.Claims;

namespace Encina;

/// <summary>
/// Everything an entry point read from its caller, handed to
/// <see cref="IRequestContextScopeFactory.RunInboundAsync{T}"/> to open the scope of one inbound
/// unit of work (an HTTP request, a circuit activity, one server-sent event).
/// </summary>
/// <param name="Principal">The authenticated principal, validated by the authentication handler, or <see langword="null"/>.</param>
/// <param name="CorrelationId">The caller's correlation id, if any.</param>
/// <param name="TenantHeaderValue">The tenant header value, used only when the principal has no tenant claim.</param>
/// <param name="IdempotencyKey">The caller's idempotency key, if any.</param>
/// <param name="IpAddress">The caller's IP address, if any.</param>
/// <param name="UserAgent">The caller's user agent, if any.</param>
/// <param name="DataRegion">The caller's data region hint, if any.</param>
/// <remarks>
/// <para>
/// Every member except <paramref name="Principal"/> is client-controlled. The scope factory
/// <b>normalizes</b> them and never refuses a scope because of them:
/// </para>
/// <list type="bullet">
/// <item><description>a correlation id that is blank, longer than <see cref="MaxIdLength"/> or has
/// control characters is replaced by the current activity id (bounded the same way) or a new
/// GUID;</description></item>
/// <item><description>a tenant header value longer than <see cref="MaxIdLength"/> or with control
/// characters is dropped;</description></item>
/// <item><description>an idempotency key longer than <see cref="MaxIdempotencyKeyLength"/> or with
/// control characters is dropped;</description></item>
/// <item><description>the user agent loses its control characters and is truncated to
/// <see cref="MaxUserAgentLength"/>;</description></item>
/// <item><description>an IP address that does not parse, and a data region longer than
/// <see cref="MaxDataRegionLength"/>, are dropped.</description></item>
/// </list>
/// <para>
/// The record copies values; it never holds the transport object it was built from, so it is safe
/// to keep beyond the request. <see cref="ToString"/> prints no member value.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var info = new InboundRequestInfo(principal, CorrelationId: correlationId, TenantHeaderValue: tenantHeader);
/// var result = await scopes.RunInboundAsync(info, (context, ct) => encina.Send(new RecordEvent(), ct), ct);
/// </code>
/// </example>
public sealed record InboundRequestInfo(
    ClaimsPrincipal? Principal,
    string? CorrelationId = null,
    string? TenantHeaderValue = null,
    string? IdempotencyKey = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? DataRegion = null)
{
    /// <summary>
    /// The maximum length of a correlation id (and of the activity id used in its place) and of a
    /// tenant header value.
    /// </summary>
    public const int MaxIdLength = 128;

    /// <summary>
    /// The maximum length of an idempotency key: the width of the inbox message id column.
    /// </summary>
    public const int MaxIdempotencyKeyLength = 255;

    /// <summary>
    /// The length a user agent is truncated to, after its control characters are stripped.
    /// </summary>
    public const int MaxUserAgentLength = 512;

    /// <summary>
    /// The maximum length of a data region hint; longer values are dropped.
    /// </summary>
    public const int MaxDataRegionLength = 16;

    /// <summary>
    /// Creates the inbound information of one activity of a long-lived interactive connection (a
    /// Blazor Server circuit): the circuit's current principal and correlation id only.
    /// </summary>
    /// <param name="principal">The circuit's current principal.</param>
    /// <param name="correlationId">The circuit's correlation id.</param>
    /// <returns>The inbound information.</returns>
    /// <exception cref="ArgumentException"><paramref name="correlationId"/> is null, empty or whitespace.</exception>
    public static InboundRequestInfo ForCircuit(ClaimsPrincipal? principal, string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        return new InboundRequestInfo(principal, correlationId);
    }

    /// <summary>
    /// Returns the type name only; client-controlled values are never printed.
    /// </summary>
    /// <returns>The text <c>InboundRequestInfo</c>.</returns>
    public override string ToString() => nameof(InboundRequestInfo);
}
