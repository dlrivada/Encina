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
    /// Maps <paramref name="principal"/> to an identity. A null or unauthenticated principal maps to
    /// <see cref="RequestIdentity.Anonymous"/>. An authenticated principal whose subject
    /// <see cref="CheckSubject"/> refuses also maps to <see cref="RequestIdentity.Anonymous"/> here, as a
    /// last-resort floor: the scope factory refuses such a principal before it calls this method, so the
    /// floor is never the identity of an opened scope.
    /// </summary>
    /// <param name="principal">The principal to map.</param>
    /// <param name="issuer">The scope that issues the identity, or <see langword="null"/>.</param>
    /// <returns>The identity, stamped with <paramref name="issuer"/> and the configured per-token claim types.</returns>
    RequestIdentity Create(ClaimsPrincipal? principal, IdentityIssuer? issuer = null);

    /// <summary>
    /// Checks the subject of the authenticated identities of <paramref name="principal"/> and tells apart
    /// the four causes that refuse a scope (fail closed): no subject claim, a malformed subject, a reserved
    /// <c>service:</c> subject and conflicting subjects. Logs Warning 162, 163 or 164 (claim types and the
    /// authentication type only, never a value) once per refused principal.
    /// </summary>
    /// <param name="principal">The principal to inspect.</param>
    /// <returns>
    /// The refusal with the code of the cause (<see cref="RequestIdentityErrorCodes.MissingSubjectClaim"/>,
    /// <see cref="RequestIdentityErrorCodes.MalformedSubjectClaim"/>,
    /// <see cref="RequestIdentityErrorCodes.ReservedSubjectClaim"/> or
    /// <see cref="RequestIdentityErrorCodes.ConflictingSubjectClaims"/>); <see langword="null"/> when the
    /// subject is usable or the principal is null or unauthenticated (the anonymous case).
    /// </returns>
    EncinaError? CheckSubject(ClaimsPrincipal? principal);

    /// <summary>
    /// Resolves the tenant id from the authenticated identities of <paramref name="principal"/> and
    /// tells apart "no tenant claim" from "conflicting tenant claims".
    /// </summary>
    /// <param name="principal">The principal to inspect.</param>
    /// <param name="tenantId">The tenant claim value, or <see langword="null"/> when there is none or the identities disagree.</param>
    /// <returns><see langword="false"/> when the authenticated identities carry different tenant claims; otherwise <see langword="true"/>.</returns>
    bool TryResolveTenantId(ClaimsPrincipal? principal, out string? tenantId);
}
