namespace Encina.Security.ABAC;

/// <summary>
/// Specifies on which authorization effect an obligation or advice should be fulfilled.
/// </summary>
/// <remarks>
/// XACML 3.0 §7.18 — Obligations and advice expressions are conditional on the
/// authorization decision. An obligation with <see cref="Permit"/> is only executed
/// when access is granted; an obligation with <see cref="Deny"/> is only executed
/// when access is refused.
/// </remarks>
public enum FulfillOn
{
    /// <summary>
    /// The obligation or advice should be fulfilled when the decision is <see cref="Effect.Permit"/>.
    /// </summary>
    /// <remarks>Typical use: audit logging of permitted access, resource provisioning, MFA escalation on sensitive resources.</remarks>
    Permit,

    /// <summary>
    /// The obligation or advice should be fulfilled when the decision is <see cref="Effect.Deny"/>.
    /// </summary>
    /// <remarks>Typical use: security alert notifications, access denial logging, user notification of why access was denied.</remarks>
    Deny
}
