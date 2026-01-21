using Microsoft.AspNetCore.Http;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Orchestrates multiple tenant resolvers, executing them in priority order.
/// </summary>
/// <remarks>
/// <para>
/// This class manages a chain of <see cref="ITenantResolver"/> implementations,
/// executing them in ascending priority order until one returns a non-null tenant ID.
/// </para>
/// <para>
/// Lower priority values are executed first. If no resolver returns a tenant ID,
/// <c>null</c> is returned.
/// </para>
/// </remarks>
internal sealed class TenantResolverChain
{
    private readonly List<ITenantResolver> _resolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolverChain"/> class.
    /// </summary>
    /// <param name="resolvers">The tenant resolvers to use.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="resolvers"/> is null.
    /// </exception>
    public TenantResolverChain(IEnumerable<ITenantResolver> resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);

        // Order by priority (lower = higher priority, executed first)
        _resolvers = resolvers.OrderBy(r => r.Priority).ToList();
    }

    /// <summary>
    /// Gets the number of resolvers in the chain.
    /// </summary>
    public int Count => _resolvers.Count;

    /// <summary>
    /// Resolves the tenant identifier by executing resolvers in priority order.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The first non-null tenant ID returned by a resolver, or <c>null</c> if
    /// no resolver could determine the tenant.
    /// </returns>
    public async ValueTask<string?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var resolver in _resolvers)
        {
            var tenantId = await resolver.ResolveAsync(context, cancellationToken);

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId;
            }
        }

        return null;
    }
}
