namespace Encina;

/// <summary>
/// Factory methods for request identity errors. Messages are fixed text: they never carry a user
/// id, claim value, role or tenant (only a declared service name, which is not a secret).
/// </summary>
/// <example>
/// <code>
/// return RequestIdentityErrors.ScopeConflict(IdentityKind.User, IdentityKind.Service);
/// </code>
/// </example>
public static class RequestIdentityErrors
{
    /// <summary>
    /// Creates the error returned when a dispatch or a scope would replace the ambient identity.
    /// </summary>
    /// <param name="ambientKind">The kind of the ambient identity.</param>
    /// <param name="requestedKind">The kind of the identity the dispatch or scope asked for.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="RequestIdentityErrorCodes.ScopeConflict"/>.</returns>
    public static EncinaError ScopeConflict(IdentityKind ambientKind, IdentityKind requestedKind)
        => EncinaErrors.Create(
            RequestIdentityErrorCodes.ScopeConflict,
            "The requested identity would replace the ambient caller identity; a dispatch cannot swap the identity of the request it runs in.",
            details: Kinds(ambientKind, requestedKind));

    /// <summary>
    /// Creates the error returned when a scope names a service identity that is not declared.
    /// </summary>
    /// <param name="serviceName">The requested service name.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="RequestIdentityErrorCodes.UnknownServiceIdentity"/>.</returns>
    public static EncinaError UnknownServiceIdentity(string serviceName)
        => EncinaErrors.Create(
            RequestIdentityErrorCodes.UnknownServiceIdentity,
            "The service identity is not declared; declare it at startup with AddEncinaServiceIdentity.",
            details: new Dictionary<string, object?> { ["serviceName"] = serviceName });

    /// <summary>
    /// Creates the error returned when the public scope API names a built-in service identity.
    /// </summary>
    /// <param name="serviceName">The requested service name.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="RequestIdentityErrorCodes.ReservedServiceIdentity"/>.</returns>
    public static EncinaError ReservedServiceIdentity(string serviceName)
        => EncinaErrors.Create(
            RequestIdentityErrorCodes.ReservedServiceIdentity,
            "Service identities with the 'encina.' prefix are built into Encina packages and cannot be opened by application code.",
            details: new Dictionary<string, object?> { ["serviceName"] = serviceName });

    /// <summary>
    /// Creates the error returned when a scope or an explicit context changes a tenant it may not change.
    /// </summary>
    /// <param name="ambientKind">The kind of the ambient identity, or of the principal of the scope.</param>
    /// <param name="requestedKind">The kind of the requested identity.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="RequestIdentityErrorCodes.TenantConflict"/>.</returns>
    public static EncinaError TenantConflict(IdentityKind ambientKind, IdentityKind requestedKind)
        => EncinaErrors.Create(
            RequestIdentityErrorCodes.TenantConflict,
            "The requested tenant differs from the tenant of the caller; a cross-tenant operation opens its own identity scope.",
            details: Kinds(ambientKind, requestedKind));

    /// <summary>
    /// Creates the error returned when the registered accessor is not the default one.
    /// </summary>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="RequestIdentityErrorCodes.UnsupportedAccessor"/>.</returns>
    public static EncinaError UnsupportedAccessor()
        => EncinaErrors.Create(RequestIdentityErrorCodes.UnsupportedAccessor, UnsupportedAccessorMessage);

    /// <summary>
    /// Creates the error returned when a persisted identity cannot be rebuilt.
    /// </summary>
    /// <param name="reason">Which part of the persisted identity failed validation (fixed text, never a value).</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="RequestIdentityErrorCodes.InvalidPersistedIdentity"/>.</returns>
    public static EncinaError InvalidPersistedIdentity(string reason)
        => EncinaErrors.Create(
            RequestIdentityErrorCodes.InvalidPersistedIdentity,
            "The persisted identity of the message cannot be rebuilt; the message must not run.",
            details: new Dictionary<string, object?> { ["reason"] = reason });

    /// <summary>
    /// The message of <see cref="UnsupportedAccessor"/> and of the startup validation failure.
    /// </summary>
    internal const string UnsupportedAccessorMessage =
        "The registered IRequestContextAccessor is not Encina's RequestContextAccessor. Identity scopes and the dispatcher share the default accessor; remove the custom IRequestContextAccessor registration.";

    private static Dictionary<string, object?> Kinds(IdentityKind ambientKind, IdentityKind requestedKind) => new()
    {
        ["ambientKind"] = ambientKind.ToString(),
        ["requestedKind"] = requestedKind.ToString()
    };
}
