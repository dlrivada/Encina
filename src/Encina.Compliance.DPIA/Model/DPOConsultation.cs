namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Records the Data Protection Officer's consultation on a DPIA assessment.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 35(2) states: "The controller shall seek the advice of the data protection
/// officer, where designated, when carrying out a data protection impact assessment."
/// </para>
/// <para>
/// This record captures the full consultation lifecycle: when advice was requested,
/// when the DPO responded, their decision, and any conditions or comments attached
/// to the decision. The DPO's role is advisory (Article 39(1)(c)), but their input
/// is a mandatory part of the DPIA process.
/// </para>
/// </remarks>
public sealed record DPOConsultation
{
    /// <summary>
    /// Unique identifier for this consultation record.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Full name of the Data Protection Officer consulted.
    /// </summary>
    /// <remarks>
    /// Required by Article 37(7) to be communicated to the supervisory authority.
    /// </remarks>
    public required string DPOName { get; init; }

    /// <summary>
    /// Email address of the Data Protection Officer.
    /// </summary>
    public required string DPOEmail { get; init; }

    /// <summary>
    /// The UTC timestamp when the consultation was requested.
    /// </summary>
    public required DateTimeOffset RequestedAtUtc { get; init; }

    /// <summary>
    /// The UTC timestamp when the DPO responded, or <see langword="null"/> if still pending.
    /// </summary>
    public DateTimeOffset? RespondedAtUtc { get; init; }

    /// <summary>
    /// The DPO's decision on the assessment.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="DPOConsultationDecision.Pending"/> until the DPO responds.
    /// </remarks>
    public required DPOConsultationDecision Decision { get; init; }

    /// <summary>
    /// Conditions that must be met before processing can proceed.
    /// </summary>
    /// <remarks>
    /// Only applicable when <see cref="Decision"/> is
    /// <see cref="DPOConsultationDecision.ConditionallyApproved"/>.
    /// </remarks>
    public string? Conditions { get; init; }

    /// <summary>
    /// Additional comments or observations from the DPO.
    /// </summary>
    public string? Comments { get; init; }
}
