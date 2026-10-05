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
    /// A dispatch tried to run with an identity that would replace the ambient user identity.
    /// </summary>
    public const string ScopeConflict = "encina.identity.scope_conflict";
}
