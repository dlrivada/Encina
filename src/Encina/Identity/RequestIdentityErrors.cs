namespace Encina;

/// <summary>
/// Factory methods for request identity errors. Messages are fixed text: they never carry a user
/// id, claim value, role or tenant.
/// </summary>
/// <example>
/// <code>
/// return RequestIdentityErrors.ScopeConflict(IdentityKind.User, IdentityKind.Service);
/// </code>
/// </example>
public static class RequestIdentityErrors
{
    /// <summary>
    /// Creates the error returned when a dispatch would replace the ambient identity.
    /// </summary>
    /// <param name="ambientKind">The kind of the ambient identity.</param>
    /// <param name="requestedKind">The kind of the identity the dispatch asked for.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="RequestIdentityErrorCodes.ScopeConflict"/>.</returns>
    public static EncinaError ScopeConflict(IdentityKind ambientKind, IdentityKind requestedKind)
        => EncinaErrors.Create(
            RequestIdentityErrorCodes.ScopeConflict,
            "The requested identity would replace the ambient caller identity; a dispatch cannot swap the identity of the request it runs in.",
            details: new Dictionary<string, object?>
            {
                ["ambientKind"] = ambientKind.ToString(),
                ["requestedKind"] = requestedKind.ToString()
            });
}
