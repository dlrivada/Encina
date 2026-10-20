using Microsoft.Extensions.Logging;

namespace Encina;

/// <summary>
/// Log messages of the request identity model.
/// </summary>
/// <remarks>
/// Event IDs: 162-175 (see <c>EventIdRanges.Core</c>). Messages carry only claim <b>type</b>
/// names, the authentication type, identity kinds, error codes, correlation ids, scope member names
/// and declared service names; never a user id, claim value, role, permission or tenant.
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

    // Level: Information, or Warning when the scope replaces another ambient service identity.
    [LoggerMessage(EventId = 166,
        Message = "Opened a scope as the service identity {ServiceName} (tenant present: {HasTenant}, correlation id {CorrelationId}).")]
    public static partial void ServiceIdentityScopeOpened(ILogger logger, LogLevel level, string serviceName, bool hasTenant, string correlationId);

    [LoggerMessage(EventId = 167, Level = LogLevel.Warning,
        Message = "Refused to open a {RequestedKind} identity scope: {ErrorCode}.")]
    public static partial void IdentityScopeRefused(ILogger logger, string errorCode, IdentityKind requestedKind);

    [LoggerMessage(EventId = 168, Level = LogLevel.Debug,
        Message = "Closed a {Kind} identity scope.")]
    public static partial void IdentityScopeClosed(ILogger logger, IdentityKind kind);

    // Level: Information, or Warning when a user identity opens over an ambient service identity.
    [LoggerMessage(EventId = 169,
        Message = "Opened a scope as a principal mapped to a {Kind} identity (correlation id {CorrelationId}).")]
    public static partial void PrincipalScopeOpened(ILogger logger, LogLevel level, IdentityKind kind, string correlationId);

    [LoggerMessage(EventId = 170, Level = LogLevel.Warning,
        Message = "A {Kind} identity scope ended after an enclosing {ParentKind} scope had already ended; it read as anonymous since then.")]
    public static partial void IdentityScopeOutlivedParent(ILogger logger, IdentityKind kind, IdentityKind parentKind);

    [LoggerMessage(EventId = 171, Level = LogLevel.Information,
        Message = "Restored a {Kind} identity for a deferred message (correlation id {CorrelationId}).")]
    public static partial void IdentityRestored(ILogger logger, IdentityKind kind, string correlationId);

    [LoggerMessage(EventId = 172, Level = LogLevel.Debug,
        Message = "Opened an inbound scope with a {Kind} identity (correlation id {CorrelationId}).")]
    public static partial void InboundScopeOpened(ILogger logger, IdentityKind kind, string correlationId);

    [LoggerMessage(EventId = 173, Level = LogLevel.Information,
        Message = "{Member} opened a {Kind} identity scope with a tenant that differs from the ambient tenant.")]
    public static partial void ScopeTenantChanged(ILogger logger, string member, IdentityKind kind);

    [LoggerMessage(EventId = 174, Level = LogLevel.Warning,
        Message = "{Member} opened a {Kind} identity scope over an inbound request because AllowOverInbound is set (service {ServiceName}).")]
    public static partial void ScopeOpenedOverInbound(ILogger logger, string member, IdentityKind kind, string? serviceName);

    [LoggerMessage(EventId = 175, Level = LogLevel.Information,
        Message = "Application code opened an inbound scope with a {Kind} identity (correlation id {CorrelationId}).")]
    public static partial void InboundScopeOpenedByApplication(ILogger logger, IdentityKind kind, string correlationId);
}
