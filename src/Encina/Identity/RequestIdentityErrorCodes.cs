namespace Encina;

/// <summary>
/// Error codes of the request identity model (<c>encina.identity.*</c>).
/// </summary>
/// <remarks>
/// Codes are stable string constants suitable for alerting rules, log filters and dashboards.
/// </remarks>
/// <example>
/// <code>
/// var result = await encina.Send(request, explicitContext, ct);
/// result.IfLeft(error =>
/// {
///     if (error.GetCode().Contains(RequestIdentityErrorCodes.ScopeConflict)) { /* identity swap refused */ }
/// });
/// </code>
/// </example>
public static class RequestIdentityErrorCodes
{
    /// <summary>
    /// A dispatch or an identity scope would replace or escalate the caller identity: an explicit
    /// context with a different, stale or unissued identity, or a scope opened over a user or an
    /// inbound request.
    /// </summary>
    public const string ScopeConflict = "encina.identity.scope_conflict";

    /// <summary>
    /// <see cref="IRequestContextScopeFactory.RunAsServiceAsync{T}"/> named a service identity that
    /// is not declared with <c>AddEncinaServiceIdentity</c>.
    /// </summary>
    public const string UnknownServiceIdentity = "encina.identity.unknown_service_identity";

    /// <summary>
    /// A built-in service identity of an Encina package (name prefix <c>encina.</c>) was requested
    /// through the public scope API.
    /// </summary>
    public const string ReservedServiceIdentity = "encina.identity.reserved_service_identity";

    /// <summary>
    /// A scope asked for a tenant that differs from the principal's tenant claim, or an explicit
    /// context changes the tenant of a dispatch that runs for a user.
    /// </summary>
    public const string TenantConflict = "encina.identity.tenant_conflict";

    /// <summary>
    /// An inbound request is refused because its tenant is ambiguous: the tenant header disagrees with
    /// the principal's tenant claim, or the principal's authenticated identities carry different
    /// tenant claims.
    /// </summary>
    public const string InboundTenantConflict = "encina.identity.inbound_tenant_conflict";

    /// <summary>
    /// A principal scope is refused because the principal's authenticated identities carry different
    /// tenant claim values (across the configured tenant claim types, or several values in one
    /// identity), so its tenant is ambiguous.
    /// </summary>
    public const string ConflictingTenantClaims = "encina.identity.conflicting_tenant_claims";

    /// <summary>
    /// A scope is refused because an authenticated principal carries no usable subject: none of the
    /// configured user-id claim types has a non-blank value on any authenticated identity.
    /// </summary>
    public const string MissingSubjectClaim = "encina.identity.missing_subject_claim";

    /// <summary>
    /// A scope is refused because the subject of an authenticated principal is not a valid user id
    /// (surrounding whitespace, or control or format characters; a blank value counts as missing).
    /// </summary>
    public const string MalformedSubjectClaim = "encina.identity.malformed_subject_claim";

    /// <summary>
    /// A scope is refused because the subject of an authenticated principal starts with the reserved
    /// <c>service:</c> prefix, which only Encina service identities may carry.
    /// </summary>
    public const string ReservedSubjectClaim = "encina.identity.reserved_subject_claim";

    /// <summary>
    /// A scope is refused because the authenticated identities of a principal carry different subject
    /// values (across identities, or several values of the winning claim type in one identity), so
    /// the caller is ambiguous.
    /// </summary>
    public const string ConflictingSubjectClaims = "encina.identity.conflicting_subject_claims";

    /// <summary>
    /// The registered <see cref="IRequestContextAccessor"/> is not the default
    /// <see cref="RequestContextAccessor"/>, so an identity scope would write to a store the
    /// dispatcher never reads.
    /// </summary>
    public const string UnsupportedAccessor = "encina.identity.unsupported_accessor";

    /// <summary>
    /// <see cref="IRequestContextScopeFactory.RunRestoredAsync{T}"/> could not rebuild the persisted
    /// identity: an invalid kind, actor, tenant, correlation or causation id, an undefined source, or
    /// an invalid trusted tenant.
    /// </summary>
    public const string InvalidPersistedIdentity = "encina.identity.invalid_persisted_identity";
}
