using System.Security.Claims;

namespace Encina;

/// <summary>
/// Maps a <see cref="ClaimsPrincipal"/> to a <see cref="RequestIdentity"/> with the one claim map of
/// <see cref="RequestIdentityOptions"/>.
/// </summary>
/// <remarks>
/// Internal on purpose: the only callers are Encina's identity entry points, which validate and
/// log every identity they open. Applications customise the mapping through
/// <see cref="RequestIdentityOptions"/>; there is no public factory to call or replace.
/// </remarks>
internal interface IRequestIdentityFactory
{
    /// <summary>
    /// Maps <paramref name="principal"/> to an identity. A null or unauthenticated principal, an
    /// authenticated principal without a user-id claim, a reserved or malformed user id, and
    /// conflicting authenticated identities all map to <see cref="RequestIdentity.Anonymous"/>.
    /// </summary>
    /// <param name="principal">The principal to map.</param>
    /// <param name="issuer">The scope that issues the identity, or <see langword="null"/>.</param>
    /// <returns>The identity, stamped with <paramref name="issuer"/> and the configured per-token claim types.</returns>
    RequestIdentity Create(ClaimsPrincipal? principal, IdentityIssuer? issuer = null);

    /// <summary>
    /// Resolves the tenant id from the authenticated identities of <paramref name="principal"/>, or
    /// <see langword="null"/> when there is none or the identities disagree.
    /// </summary>
    string? ResolveTenantId(ClaimsPrincipal? principal);
}
