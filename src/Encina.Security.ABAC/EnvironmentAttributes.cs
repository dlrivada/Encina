namespace Encina.Security.ABAC;

/// <summary>
/// Well-known attribute identifiers for the environment category used in
/// <see cref="PolicyEvaluationContext.EnvironmentAttributes"/>.
/// </summary>
/// <remarks>
/// XACML 3.0 §B.5 — These constants define standard environment attribute names
/// that <see cref="IAttributeProvider"/> implementations can populate and policy
/// conditions can reference.
/// </remarks>
public static class EnvironmentAttributes
{
    /// <summary>The current UTC date and time.</summary>
    public const string CurrentTime = "currentTime";

    /// <summary>The current day of the week (e.g., "Monday").</summary>
    public const string DayOfWeek = "dayOfWeek";

    /// <summary>Whether the current time falls within business hours.</summary>
    public const string IsBusinessHours = "isBusinessHours";

    /// <summary>The IP address of the requesting client.</summary>
    public const string IpAddress = "ipAddress";

    /// <summary>The User-Agent header of the requesting client.</summary>
    public const string UserAgent = "userAgent";

    /// <summary>The tenant identifier in a multi-tenant application.</summary>
    public const string TenantId = "tenantId";

    /// <summary>The geographic region or data center of the request.</summary>
    public const string Region = "region";

    /// <summary>The HTTP request path.</summary>
    public const string RequestPath = "requestPath";

    /// <summary>The HTTP method (GET, POST, PUT, DELETE, etc.).</summary>
    public const string HttpMethod = "httpMethod";
}
