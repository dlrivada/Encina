using Microsoft.Extensions.Logging;

namespace Encina;

/// <summary>
/// Log messages of the request identity model.
/// </summary>
/// <remarks>
/// Event IDs: 162-165 (see <c>EventIdRanges.Core</c>). Messages carry only claim <b>type</b>
/// names, the authentication type and identity kinds; never a user id, claim value, role or tenant.
/// </remarks>
internal static partial class RequestIdentityLog
{
    [LoggerMessage(EventId = 162, Level = LogLevel.Warning,
        Message = "An authenticated principal (authentication type {AuthenticationType}) carries none of the user-id claim types {ClaimTypes}; it maps to the anonymous identity.")]
    public static partial void AuthenticatedPrincipalWithoutSubject(ILogger logger, string? authenticationType, string claimTypes);

    [LoggerMessage(EventId = 163, Level = LogLevel.Warning,
        Message = "The user-id claim {ClaimType} carries a reserved 'service:' subject or a malformed value; the principal maps to the anonymous identity.")]
    public static partial void ReservedServiceSubjectRejected(ILogger logger, string claimType);

    [LoggerMessage(EventId = 164, Level = LogLevel.Warning,
        Message = "The authenticated identities of one principal disagree on the {ClaimKind}; the principal maps to the anonymous identity.")]
    public static partial void ConflictingAuthenticatedIdentities(ILogger logger, string claimKind);

    [LoggerMessage(EventId = 165, Level = LogLevel.Warning,
        Message = "An explicit request context carries a {ExplicitKind} identity that differs from the ambient {AmbientKind} identity; the dispatch is {Outcome}.")]
    public static partial void ExplicitContextIdentityConflict(ILogger logger, IdentityKind explicitKind, IdentityKind ambientKind, string outcome);
}
