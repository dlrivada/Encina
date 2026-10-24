using System.Collections.Frozen;

namespace Encina;

/// <summary>
/// Decides which authorization denials an <see cref="IAccessElevationGate"/> may override.
/// </summary>
/// <remarks>
/// <para>
/// Only definite verdicts about an authenticated caller are overridable. Unauthenticated callers,
/// indeterminate and failed evaluations, failed obligations and a missing required policy are never
/// overridable: they are not a decision that the caller lacks permission.
/// </para>
/// <para>
/// The match is by <b>exact</b> code, never by the <c>encina.authorization.</c> prefix: the
/// overridable ABAC codes and the never-overridable <c>abac_obligation_failed</c> and
/// <c>abac_policy_not_found</c> share that prefix.
/// </para>
/// </remarks>
public static class AccessElevationDenials
{
    // The ABAC codes are literals because core cannot reference Encina.Security.ABAC.
    private static readonly FrozenSet<string> Overridable = new[]
    {
        EncinaErrorCodes.AuthorizationForbidden,
        EncinaErrorCodes.AuthorizationPolicyFailed,
        EncinaErrorCodes.AuthorizationResourceDenied,
        EncinaErrorCodes.AuthorizationInsufficientRoles,
        EncinaErrorCodes.AuthorizationPermissionDenied,
        EncinaErrorCodes.AuthorizationClaimMissing,
        EncinaErrorCodes.AuthorizationNotOwner,
        "encina.authorization.abac_access_denied",
        "encina.authorization.abac_condition_not_met"
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Determines whether a denial is a definite denial that an elevation may override.
    /// </summary>
    /// <param name="denial">The denial the gate is about to return.</param>
    /// <returns>
    /// <see langword="true"/> when the error code is exactly one of the definite-denial codes;
    /// <see langword="false"/> for every other code, including an error without a code.
    /// </returns>
    public static bool IsOverridable(EncinaError denial) =>
        denial.GetCode().Match(Some: code => Overridable.Contains(code), None: () => false);
}
