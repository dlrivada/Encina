namespace Encina.AspNetCore;

/// <summary>
/// Configuration options for Encina ASP.NET Core integration.
/// </summary>
/// <remarks>
/// The claim types that map a principal to the request identity (user id, roles, permissions,
/// tenant) are configured once for every entry point through <see cref="RequestIdentityOptions"/>
/// (<c>services.AddEncinaRequestIdentity(options => ...)</c>).
/// </remarks>
public sealed class EncinaAspNetCoreOptions
{
    /// <summary>
    /// HTTP header name for correlation ID.
    /// Default: "X-Correlation-ID"
    /// </summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// HTTP header name for tenant ID. The header chooses the tenant only when
    /// <see cref="TrustTenantIdHeader"/> is enabled and the caller's principal carries no tenant claim.
    /// Default: "X-Tenant-ID"
    /// </summary>
    public string TenantIdHeader { get; set; } = "X-Tenant-ID";

    /// <summary>
    /// Whether the tenant header may choose the tenant of a request whose principal has no tenant claim.
    /// Default: false (the header is ignored).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Enable it only when a gateway sets or strips the header and the application is not reachable
    /// around that gateway: any client can send the header, so a directly reachable application would
    /// let the caller pick its tenant. The verified tenant claim always wins, and a header that
    /// disagrees with the claim is refused with 403 whether this option is enabled or not. Enabling
    /// the option is an explicit opt-out of the default and is logged once at startup.
    /// </para>
    /// <para>
    /// An address-based trust list is not offered: <c>UseEncinaContext()</c> must run after
    /// <c>UseForwardedHeaders()</c>, so the peer address a check could use is the forwarded client
    /// address, never the gateway's.
    /// </para>
    /// </remarks>
    public bool TrustTenantIdHeader { get; set; }

    /// <summary>
    /// HTTP header name for idempotency key.
    /// Default: "X-Idempotency-Key"
    /// </summary>
    public string IdempotencyKeyHeader { get; set; } = "X-Idempotency-Key";

    /// <summary>
    /// Whether to include request path in Problem Details.
    /// Default: false
    /// </summary>
    public bool IncludeRequestPathInProblemDetails { get; set; }

    /// <summary>
    /// Whether to include exception details in Problem Details (only in Development).
    /// Default: false (controlled by environment)
    /// </summary>
    public bool IncludeExceptionDetails { get; set; }

    /// <summary>
    /// HTTP header name for the data region hint used by the data residency module.
    /// Default: "X-Data-Region"
    /// </summary>
    /// <remarks>
    /// <para>
    /// When present in the incoming request, the middleware stores the header value in
    /// <see cref="IRequestContext"/> metadata so that <c>HttpRegionContextProvider</c> can
    /// resolve the region from the HTTP request without directly accessing
    /// <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>.
    /// </para>
    /// <para>
    /// The header is optional — if absent, the middleware does not set any region metadata
    /// and region resolution falls back to tenant mapping or the configured default region.
    /// </para>
    /// </remarks>
    public string DataRegionHeaderName { get; set; } = "X-Data-Region";
}
