using Encina.Messaging.Health;

namespace Encina.AwsLambda;

/// <summary>
/// Configuration options for Encina AWS Lambda integration.
/// </summary>
/// <remarks>
/// <para>
/// These options control how Encina integrates with AWS Lambda functions, including
/// request context enrichment, header extraction, and error response behavior.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaAwsLambda(options =>
/// {
///     options.EnableRequestContextEnrichment = true;
///     options.CorrelationIdHeader = "X-Correlation-ID";
///     options.IncludeExceptionDetailsInResponse = false;
/// });
/// </code>
/// </example>
public sealed class EncinaAwsLambdaOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically enrich the request context
    /// with correlation ID, user ID, and tenant ID from Lambda context and headers.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable automatic context enrichment; otherwise, <c>false</c>.
    /// Default is <c>true</c>.
    /// </value>
    public bool EnableRequestContextEnrichment { get; set; } = true;

    /// <summary>
    /// Gets or sets the HTTP header name used to extract the correlation ID.
    /// </summary>
    /// <value>
    /// The header name for correlation ID. Default is "X-Correlation-ID".
    /// </value>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// Gets or sets the HTTP header name used to extract the tenant ID.
    /// </summary>
    /// <value>
    /// The header name for tenant ID. Default is "X-Tenant-ID".
    /// </value>
    public string TenantIdHeader { get; set; } = "X-Tenant-ID";

    /// <summary>
    /// Gets or sets the claim type used to extract the user ID from JWT claims.
    /// </summary>
    /// <value>
    /// The claim type for user ID. Default is "sub" (standard JWT subject claim).
    /// </value>
    public string UserIdClaimType { get; set; } = "sub";

    /// <summary>
    /// Gets or sets the claim type used to extract the tenant ID from JWT claims.
    /// </summary>
    /// <value>
    /// The claim type for tenant ID. Default is "tenant_id".
    /// </value>
    public string TenantIdClaimType { get; set; } = "tenant_id";

    /// <summary>
    /// Gets or sets a value indicating whether to include exception details in error responses.
    /// </summary>
    /// <value>
    /// <c>true</c> to include exception details (for development); otherwise, <c>false</c>.
    /// Default is <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// <b>Security Warning</b>: Set this to <c>true</c> only in development environments.
    /// Including exception details in production can expose sensitive information.
    /// </para>
    /// </remarks>
    public bool IncludeExceptionDetailsInResponse { get; set; }

    /// <summary>
    /// Gets or sets the provider-specific health check configuration.
    /// </summary>
    /// <value>
    /// The health check options. Default is a new instance of <see cref="ProviderHealthCheckOptions"/>.
    /// </value>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to use API Gateway V2 (HTTP API) format.
    /// </summary>
    /// <value>
    /// <c>true</c> for API Gateway V2 (HTTP API) format; <c>false</c> for V1 (REST API).
    /// Default is <c>false</c>.
    /// </value>
    public bool UseApiGatewayV2Format { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable SQS batch item failure reporting.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable partial batch response for SQS; otherwise, <c>false</c>.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// When enabled, failed messages are reported individually allowing successful messages
    /// to be deleted from the queue while failed ones are retried.
    /// </remarks>
    public bool EnableSqsBatchItemFailures { get; set; } = true;
}
