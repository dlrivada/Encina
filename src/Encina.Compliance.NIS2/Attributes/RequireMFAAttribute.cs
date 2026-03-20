namespace Encina.Compliance.NIS2;

/// <summary>
/// Requires multi-factor authentication (MFA) for the decorated request under NIS2 Art. 21(2)(j).
/// </summary>
/// <remarks>
/// <para>
/// When applied to a request class, the <c>NIS2CompliancePipelineBehavior</c> invokes
/// <see cref="Abstractions.IMFAEnforcer.RequireMFAAsync{TRequest}"/> before executing
/// the request handler. If MFA is not enabled for the current user, the behavior either
/// blocks the request or logs a warning, depending on the configured
/// <see cref="Model.NIS2EnforcementMode"/>.
/// </para>
/// <para>
/// Per NIS2 Article 21(2)(j), entities must implement "the use of multi-factor authentication
/// or continuous authentication solutions [...] where appropriate." This attribute identifies
/// which operations require MFA enforcement.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequireMFA(Reason = "Administrative operation requiring elevated authentication")]
/// public sealed record AdminOperationCommand : ICommand&lt;Unit&gt;;
///
/// [RequireMFA]
/// public sealed record DeleteUserCommand(string UserId) : ICommand&lt;Unit&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireMFAAttribute : Attribute
{
    /// <summary>
    /// Gets or sets an optional reason explaining why MFA is required for this operation.
    /// </summary>
    /// <remarks>
    /// Included in error messages and audit trail entries when MFA enforcement is triggered.
    /// </remarks>
    public string? Reason { get; set; }
}
