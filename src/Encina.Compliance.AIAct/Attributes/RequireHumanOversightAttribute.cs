namespace Encina.Compliance.AIAct.Attributes;

/// <summary>
/// Marks a request type as requiring human oversight before the AI system's
/// output can be acted upon, per Article 14 of the EU AI Act.
/// </summary>
/// <remarks>
/// <para>
/// Article 14(1) requires high-risk AI systems to be designed and developed in such
/// a way that they can be effectively overseen by natural persons during the period
/// in which they are in use.
/// </para>
/// <para>
/// When the <c>AIActCompliancePipelineBehavior</c> encounters a request decorated with
/// this attribute and enforcement mode is <see cref="Model.AIActEnforcementMode.Block"/>,
/// it will block the request unless a corresponding
/// <see cref="Model.HumanDecisionRecord"/> exists (verified via
/// <see cref="Abstractions.IHumanOversightEnforcer.HasHumanApprovalAsync"/>).
/// </para>
/// <para>
/// This attribute is complementary to <see cref="HighRiskAIAttribute"/>. All high-risk
/// systems implicitly require human oversight; this attribute allows explicit human
/// oversight requirements on requests that may not be classified as high-risk but still
/// warrant human review.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequireHumanOversight(Reason = "Loan approval decisions must be reviewed by a human officer")]
/// public sealed record ApproveLoanCommand(string ApplicationId) : ICommand&lt;LoanDecision&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireHumanOversightAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the reason why human oversight is required for this request.
    /// </summary>
    /// <remarks>
    /// Documenting the reason supports compliance evidence and is included in
    /// <see cref="Notifications.HumanOversightRequiredNotification"/> when the
    /// pipeline behavior publishes oversight notifications.
    /// </remarks>
    public required string Reason { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the AI system associated with this oversight requirement.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the pipeline behavior resolves the system ID from a co-located
    /// <see cref="HighRiskAIAttribute"/> or from the request type name.
    /// </remarks>
    public string? SystemId { get; set; }
}
