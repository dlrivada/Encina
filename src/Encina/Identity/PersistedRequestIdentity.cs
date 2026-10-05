namespace Encina;

/// <summary>
/// The persisted form of a request identity, stored with a deferred message so the originating
/// actor can be rebuilt at dispatch (SPEC-002 REQ-015 / DEC-011).
/// </summary>
/// <param name="Kind">The kind of the originating caller.</param>
/// <param name="ActorId">
/// The actor: the user id for <see cref="IdentityKind.User"/>, the declared service name for
/// <see cref="IdentityKind.Service"/>, and <see langword="null"/> for <see cref="IdentityKind.Anonymous"/>.
/// </param>
/// <param name="TenantId">The tenant the message belongs to, if any.</param>
/// <param name="CorrelationId">The correlation id of the originating request.</param>
/// <param name="CausationId">The id of the message or request that caused this one, if any.</param>
/// <remarks>
/// Roles, permissions and the principal are never persisted: a stored row is never a source of
/// authority. A rebuilt user identity therefore carries no roles and no permissions.
/// </remarks>
/// <example>
/// <code>
/// var persisted = context.Identity.ToPersisted(context.TenantId, context.CorrelationId, causationId: null);
/// // store persisted.Kind, persisted.ActorId, persisted.TenantId, persisted.CorrelationId with the message
/// </code>
/// </example>
public sealed record PersistedRequestIdentity(
    IdentityKind Kind,
    string? ActorId,
    string? TenantId,
    string CorrelationId,
    string? CausationId)
{
    /// <summary>
    /// Returns the identity kind only; the actor id is never printed, so a persisted identity cannot
    /// leak a user id into logs or diagnostics.
    /// </summary>
    /// <returns>A string such as <c>PersistedRequestIdentity { Kind = User }</c>.</returns>
    public override string ToString() => $"PersistedRequestIdentity {{ Kind = {Kind} }}";
}
