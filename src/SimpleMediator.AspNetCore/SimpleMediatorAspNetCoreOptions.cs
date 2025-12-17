using System.Security.Claims;

namespace SimpleMediator.AspNetCore;

/// <summary>
/// Configuration options for SimpleMediator ASP.NET Core integration.
/// </summary>
public sealed class SimpleMediatorAspNetCoreOptions
{
    /// <summary>
    /// HTTP header name for correlation ID.
    /// Default: "X-Correlation-ID"
    /// </summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// HTTP header name for tenant ID.
    /// Default: "X-Tenant-ID"
    /// </summary>
    public string TenantIdHeader { get; set; } = "X-Tenant-ID";

    /// <summary>
    /// HTTP header name for idempotency key.
    /// Default: "X-Idempotency-Key"
    /// </summary>
    public string IdempotencyKeyHeader { get; set; } = "X-Idempotency-Key";

    /// <summary>
    /// Claim type for user ID.
    /// Default: ClaimTypes.NameIdentifier ("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
    /// </summary>
    /// <remarks>
    /// Common alternatives:
    /// - "sub" (OIDC standard)
    /// - ClaimTypes.NameIdentifier (ASP.NET default)
    /// - "http://schemas.microsoft.com/identity/claims/objectidentifier" (Azure AD)
    /// </remarks>
    public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;

    /// <summary>
    /// Claim type for tenant ID.
    /// Default: "tenant_id"
    /// </summary>
    /// <remarks>
    /// Common alternatives:
    /// - "tenant_id" (custom)
    /// - "tid" (Azure AD)
    /// - "http://schemas.microsoft.com/identity/claims/tenantid"
    /// </remarks>
    public string TenantIdClaimType { get; set; } = "tenant_id";

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
}
