using System.Security.Claims;
using Encina.Messaging.Health;

namespace Encina.AzureFunctions;

/// <summary>
/// Configuration options for Encina Azure Functions integration.
/// </summary>
/// <remarks>
/// <para>
/// These options control how Encina integrates with Azure Functions,
/// including request context enrichment, error handling, and health checks.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaAzureFunctions(options =>
/// {
///     options.CorrelationIdHeader = "X-Request-ID";
///     options.EnableRequestContextEnrichment = true;
///     options.IncludeExceptionDetailsInResponse = false;
/// });
/// </code>
/// </example>
public sealed class EncinaAzureFunctionsOptions
{
    /// <summary>
    /// Gets or sets whether to automatically enrich the request context
    /// with correlation ID, user ID, and tenant ID from function context.
    /// </summary>
    /// <value>Default: true</value>
    public bool EnableRequestContextEnrichment { get; set; } = true;

    /// <summary>
    /// HTTP header name for correlation ID.
    /// </summary>
    /// <value>Default: "X-Correlation-ID"</value>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// HTTP header name for tenant ID.
    /// </summary>
    /// <value>Default: "X-Tenant-ID"</value>
    public string TenantIdHeader { get; set; } = "X-Tenant-ID";

    /// <summary>
    /// Claim type for user ID extraction from identity.
    /// </summary>
    /// <value>Default: ClaimTypes.NameIdentifier</value>
    /// <remarks>
    /// Common alternatives:
    /// <list type="bullet">
    /// <item><description>"sub" (OIDC standard)</description></item>
    /// <item><description>"http://schemas.microsoft.com/identity/claims/objectidentifier" (Azure AD)</description></item>
    /// </list>
    /// </remarks>
    public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;

    /// <summary>
    /// Claim type for tenant ID extraction from identity.
    /// </summary>
    /// <value>Default: "tenant_id"</value>
    /// <remarks>
    /// Common alternatives:
    /// <list type="bullet">
    /// <item><description>"tid" (Azure AD)</description></item>
    /// <item><description>"http://schemas.microsoft.com/identity/claims/tenantid"</description></item>
    /// </list>
    /// </remarks>
    public string TenantIdClaimType { get; set; } = "tenant_id";

    /// <summary>
    /// Whether to include exception details in error responses.
    /// </summary>
    /// <value>Default: false (production-safe)</value>
    /// <remarks>
    /// Set to true only in development environments. Including exception details
    /// in production responses can expose sensitive information.
    /// </remarks>
    public bool IncludeExceptionDetailsInResponse { get; set; }

    /// <summary>
    /// Configuration options for the Azure Functions health check.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; set; } = new()
    {
        Name = "encina-azure-functions",
        Tags = ["encina", "azure-functions", "ready"]
    };
}
