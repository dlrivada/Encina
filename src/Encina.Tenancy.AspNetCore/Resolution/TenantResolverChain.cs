using Microsoft.AspNetCore.Http;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Orchestrates multiple tenant resolvers, executing them in priority order.
/// </summary>
/// <remarks>
/// <para>
/// Authoritative resolvers (<see cref="ITenantResolver.IsAuthoritative"/>) run first, ordered by
/// priority among themselves; the first non-empty value is the tenant. Every non-authoritative
/// resolver still runs, and a non-empty value that differs (ordinal comparison) from the
/// authoritative tenant is a mismatch.
/// </para>
/// <para>
/// With no authoritative value, the first non-empty value of the non-authoritative resolvers, in
/// ascending priority order, is the tenant. If no resolver returns a value the outcome has no tenant.
/// </para>
/// </remarks>
internal sealed class TenantResolverChain
{
    private readonly List<ITenantResolver> _authoritative;
    private readonly List<ITenantResolver> _others;

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

        // Order by priority (lower = higher priority, executed first) within each group
        var ordered = resolvers.OrderBy(r => r.Priority).ToList();
        _authoritative = ordered.Where(r => r.IsAuthoritative).ToList();
        _others = ordered.Where(r => !r.IsAuthoritative).ToList();
    }

    /// <summary>
    /// Gets the number of resolvers in the chain.
    /// </summary>
    public int Count => _authoritative.Count + _others.Count;

    /// <summary>
    /// Resolves the tenant identifier: the authoritative value first, then the others, flagging a mismatch.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The tenant (or <see cref="TenantResolution.None"/>), or a mismatch when a non-authoritative
    /// resolver disagrees with the authoritative tenant.
    /// </returns>
    public async ValueTask<TenantResolution> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var (tenantId, authoritativeName) = await FirstValueAsync(_authoritative, context, cancellationToken);

        return authoritativeName is null
            ? await ResolveWithoutAuthoritativeAsync(context, cancellationToken)
            : await CompareOthersAsync(tenantId!, authoritativeName, context, cancellationToken);
    }

    private async ValueTask<TenantResolution> ResolveWithoutAuthoritativeAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var (tenantId, _) = await FirstValueAsync(_others, context, cancellationToken);
        return tenantId is null ? TenantResolution.None : new TenantResolution(tenantId, null, null);
    }

    private async ValueTask<TenantResolution> CompareOthersAsync(
        string tenantId,
        string authoritativeName,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        foreach (var resolver in _others)
        {
            var value = await resolver.ResolveAsync(context, cancellationToken);

            if (!string.IsNullOrWhiteSpace(value) && !string.Equals(value, tenantId, StringComparison.Ordinal))
            {
                return new TenantResolution(null, authoritativeName, resolver.GetType().Name);
            }
        }

        return new TenantResolution(tenantId, authoritativeName, null);
    }

    private static async ValueTask<(string? TenantId, string? ResolverName)> FirstValueAsync(
        List<ITenantResolver> resolvers,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        foreach (var resolver in resolvers)
        {
            var tenantId = await resolver.ResolveAsync(context, cancellationToken);

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return (tenantId, resolver.GetType().Name);
            }
        }

        return (null, null);
    }
}
