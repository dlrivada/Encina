using Microsoft.AspNetCore.Http;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Defines a strategy for resolving tenant identifiers from HTTP requests.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to create custom tenant resolution strategies.
/// Multiple resolvers can be registered and are executed in priority order
/// until one returns a non-null tenant ID.
/// </para>
/// <para><b>Built-in Resolvers:</b></para>
/// <list type="bullet">
/// <item><see cref="HeaderTenantResolver"/> - Reads from HTTP header (e.g., X-Tenant-ID)</item>
/// <item><see cref="ClaimTenantResolver"/> - Reads from user claims</item>
/// <item><see cref="SubdomainTenantResolver"/> - Extracts from subdomain (e.g., acme.example.com)</item>
/// <item><see cref="RouteTenantResolver"/> - Extracts from route parameters</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class ApiKeyTenantResolver : ITenantResolver
/// {
///     public int Priority => 100;
///
///     public ValueTask&lt;string?&gt; ResolveAsync(HttpContext context, CancellationToken cancellationToken)
///     {
///         if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
///         {
///             // Look up tenant by API key
///             return new ValueTask&lt;string?&gt;(LookupTenantByApiKey(apiKey!));
///         }
///         return new ValueTask&lt;string?&gt;((string?)null);
///     }
/// }
/// </code>
/// </example>
public interface ITenantResolver
{
    /// <summary>
    /// Gets the priority of this resolver in the chain.
    /// </summary>
    /// <value>
    /// Lower values indicate higher priority. Resolvers with priority 0-99
    /// are executed before the built-in resolvers (100-199).
    /// </value>
    /// <remarks>
    /// <para><b>Recommended Priority Ranges:</b></para>
    /// <list type="bullet">
    /// <item>0-99: High priority custom resolvers</item>
    /// <item>100-199: Built-in resolvers (Header=100, Claim=110, Route=120, Subdomain=130)</item>
    /// <item>200+: Low priority fallback resolvers</item>
    /// </list>
    /// </remarks>
    int Priority { get; }

    /// <summary>
    /// Attempts to resolve a tenant identifier from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context containing the request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The resolved tenant ID, or <c>null</c> if this resolver cannot determine the tenant.
    /// </returns>
    ValueTask<string?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default);
}
