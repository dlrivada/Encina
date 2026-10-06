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
    /// Determines whether <paramref name="identity"/> is an authenticated user identity.
    /// </summary>
    internal static bool IsUser([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] RequestIdentity? identity) =>
        identity is { Kind: IdentityKind.User, UserId: not null };

    /// <summary>
    /// Returns the user id of <paramref name="context"/> for a <c>VaryByUser</c> key, reading the
    /// identity once (it reads anonymous as soon as its scope ends).
    /// </summary>
    /// <exception cref="InvalidOperationException">The identity is not an authenticated user.</exception>
    internal static string RequireUserId(IRequestContext context)
    {
        var identity = context.Identity;
        return IsUser(identity)
            ? identity.UserId!
            : throw new InvalidOperationException(
                $"A VaryByUser cache key requires an authenticated user identity, but the request identity is {identity?.Kind ?? IdentityKind.Anonymous}. Bypass the cache for this request instead of sharing one key across callers.");
    }
}
