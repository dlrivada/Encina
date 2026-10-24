using Microsoft.AspNetCore.Http;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Defines a strategy for resolving tenant identifiers from HTTP requests.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to create custom tenant resolution strategies.
/// Multiple resolvers can be registered; authoritative resolvers (see <see cref="IsAuthoritative"/>)
/// run first, and a client-supplied value that disagrees with the verified tenant is rejected.
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
///     public bool IsAuthoritative => false; // the API key is client supplied
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
    /// <item>100-199: Built-in resolvers (Header=100, Claim=110, Route=120, Subdomain=130). The priority orders resolvers only within their group (authoritative or not); a verified claim always wins over a header.</item>
    /// <item>200+: Low priority fallback resolvers</item>
    /// </list>
    /// </remarks>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this resolver reads a value the server has verified,
    /// such as the claim of an authenticated principal.
    /// </summary>
    /// <value>
    /// <c>true</c> for a verified source; <c>false</c> for a value the client controls
    /// (a header, a route value, the host name).
    /// </value>
    /// <remarks>
    /// <para>
    /// An authoritative value always wins: authoritative resolvers run first (ordered by
    /// <see cref="Priority"/> among themselves) and the first non-empty value is the tenant. Every
    /// other resolver still runs, and a non-empty value that differs from the authoritative tenant
    /// (ordinal comparison) is a tenant mismatch: the middleware answers 403 Forbidden.
    /// </para>
    /// <para>
    /// With no authoritative value, the first non-empty value in <see cref="Priority"/> order is the tenant.
    /// </para>
    /// </remarks>
    bool IsAuthoritative { get; }

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
