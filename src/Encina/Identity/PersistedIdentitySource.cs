namespace Encina;

/// <summary>
/// Where a deferred-message dispatcher read the message whose identity it restores with
/// <see cref="IRequestContextScopeFactory.RunRestoredAsync{T}"/>.
/// </summary>
/// <remarks>
/// There is no zero value on purpose: every dispatcher states the source, and
/// <c>default(PersistedIdentitySource)</c> is refused with
/// <see cref="RequestIdentityErrorCodes.InvalidPersistedIdentity"/>.
/// </remarks>
/// <example>
/// <code>
/// // An inbox fed by an external broker: the row is untrusted, so the message runs anonymous.
/// await scopes.RunRestoredAsync(persisted, PersistedIdentitySource.External, work, configuredTenantId: inboxTenant, ct);
/// </code>
/// </example>
public enum PersistedIdentitySource
{
    /// <summary>
    /// The message was written inside the trust boundary (outbox, scheduled messages, saga state):
    /// the persisted actor and tenant are restored.
    /// </summary>
    Internal = 1,

    /// <summary>
    /// The message crossed the trust boundary (an inbox fed by an external broker): the identity is
    /// restored as anonymous with an inbound origin, the persisted tenant is ignored, and only the
    /// dispatcher's trusted tenant is used.
    /// </summary>
    External = 2
}
