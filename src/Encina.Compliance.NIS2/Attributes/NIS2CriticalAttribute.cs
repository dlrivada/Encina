namespace Encina.Compliance.NIS2;

/// <summary>
/// Marks a request as a critical infrastructure operation under NIS2 (Art. 21).
/// </summary>
/// <remarks>
/// <para>
/// When applied to a request class, the <c>NIS2CompliancePipelineBehavior</c> records
/// enhanced observability data (activity spans, metrics) for the request execution.
/// This enables monitoring of critical operations that are subject to NIS2 oversight.
/// </para>
/// <para>
/// Per NIS2 Article 21(1), entities must take appropriate measures to manage risks to
/// the security of network and information systems used for their operations. Marking
/// operations as critical helps identify which request paths require the highest level
/// of security monitoring and compliance enforcement.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [NIS2Critical(Description = "Core payment processing — critical infrastructure")]
/// public sealed record ProcessPaymentCommand(decimal Amount) : ICommand&lt;PaymentResult&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class NIS2CriticalAttribute : Attribute
{
    /// <summary>
    /// Gets or sets an optional description explaining why this operation is considered critical.
    /// </summary>
    /// <remarks>
    /// Included in observability data and audit trail entries for traceability.
    /// </remarks>
    public string? Description { get; set; }
}
