namespace Encina.Caching;

/// <summary>
/// The user segment of a <c>VaryByUser</c> cache key.
/// </summary>
/// <remarks>
/// A <c>VaryByUser</c> entry is private to one authenticated user. A request whose identity is not an
/// authenticated <see cref="IdentityKind.User"/> (anonymous, a service, or a context with no identity)
/// has no user to key on, so it must never read or write such an entry: sharing one key across every
/// non-user caller would disclose one user's response to another. <see cref="QueryCachingPipelineBehavior{TRequest, TResponse}"/>
/// bypasses the cache for those requests, and the key generators refuse to build a key for them.
/// </remarks>
internal static class CacheUserIdentity
{
    /// <summary>
    /// Determines whether <paramref name="context"/> carries an authenticated user identity.
    /// </summary>
    internal static bool IsUser(IRequestContext context) =>
        context.Identity is { Kind: IdentityKind.User, UserId: not null };

    /// <summary>
    /// Gets the identity kind of <paramref name="context"/>, reading a missing identity as anonymous.
    /// </summary>
    internal static IdentityKind KindOf(IRequestContext context) =>
        context.Identity?.Kind ?? IdentityKind.Anonymous;

    /// <summary>
    /// Returns the user id of <paramref name="context"/> for a <c>VaryByUser</c> key.
    /// </summary>
    /// <exception cref="InvalidOperationException">The identity is not an authenticated user.</exception>
    internal static string RequireUserId(IRequestContext context) =>
        IsUser(context)
            ? context.Identity.UserId!
            : throw new InvalidOperationException(
                $"A VaryByUser cache key requires an authenticated user identity, but the request identity is {KindOf(context)}. Bypass the cache for this request instead of sharing one key across callers.");
}
