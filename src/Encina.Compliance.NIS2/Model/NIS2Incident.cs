namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Represents a cybersecurity incident subject to NIS2 notification obligations (Art. 23).
/// </summary>
/// <remarks>
/// <para>
/// Per NIS2 Article 23(1), essential and important entities must notify their CSIRT or
/// competent authority of any significant incident without undue delay. Article 23(3)
/// defines a "significant incident" as one that:
/// </para>
/// <list type="bullet">
/// <item><description>Has caused or is capable of causing severe operational disruption of the services
/// or financial loss for the entity concerned.</description></item>
/// <item><description>Has affected or is capable of affecting other natural or legal persons by causing
/// considerable material or non-material damage.</description></item>
/// </list>
/// <para>
/// The notification timeline under Art. 23(4) requires:
/// </para>
/// <list type="number">
/// <item><description>Early warning within 24 hours (<see cref="EarlyWarningDeadlineUtc"/>).</description></item>
/// <item><description>Incident notification within 72 hours (<see cref="IncidentNotificationDeadlineUtc"/>).</description></item>
/// <item><description>Final report within 1 month of the incident notification (<see cref="FinalReportDeadlineUtc"/>).</description></item>
/// </list>
/// </remarks>
public sealed record NIS2Incident
{
    /// <summary>
    /// Unique identifier for this incident.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Description of the incident, including its nature and scope.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Assessed severity of the incident.
    /// </summary>
    public required NIS2IncidentSeverity Severity { get; init; }

    /// <summary>
    /// Timestamp when the incident was detected or the entity became aware of it (UTC).
    /// </summary>
    /// <remarks>
    /// This is the starting point for all notification deadlines under Art. 23(4).
    /// The "awareness" moment is when the entity has a reasonable degree of certainty
    /// that a significant incident has occurred.
    /// </remarks>
    public required DateTimeOffset DetectedAtUtc { get; init; }

    /// <summary>
    /// Whether this incident meets the "significant incident" threshold under Art. 23(3).
    /// </summary>
    /// <remarks>
    /// Only significant incidents trigger the mandatory notification obligations
    /// under Art. 23(4). Non-significant incidents should still be documented
    /// per Art. 21(2)(b) (incident handling measure).
    /// </remarks>
    public required bool IsSignificant { get; init; }

    /// <summary>
    /// Services affected by this incident.
    /// </summary>
    /// <remarks>
    /// Identifies which services or operations are impacted, helping assess
    /// the scope and cross-border impact of the incident (Art. 23(4)(a)).
    /// </remarks>
    public required IReadOnlyList<string> AffectedServices { get; init; }

    /// <summary>
    /// Initial assessment of the incident's impact and scope.
    /// </summary>
    /// <remarks>
    /// Per Art. 23(4)(b), the incident notification must include an initial assessment
    /// of the significant incident, including its severity and impact.
    /// </remarks>
    public required string InitialAssessment { get; init; }

    /// <summary>
    /// Timestamp when the early warning was submitted to the CSIRT or competent authority (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> until the early warning has been submitted. Per Art. 23(4)(a),
    /// the early warning must be submitted within 24 hours of becoming aware of the incident.
    /// </remarks>
    public DateTimeOffset? EarlyWarningAtUtc { get; init; }

    /// <summary>
    /// Timestamp when the incident notification was submitted (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> until the incident notification has been submitted. Per Art. 23(4)(b),
    /// the notification must be submitted within 72 hours of becoming aware of the incident.
    /// </remarks>
    public DateTimeOffset? IncidentNotificationAtUtc { get; init; }

    /// <summary>
    /// Timestamp when the final report was submitted (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> until the final report has been submitted. Per Art. 23(4)(d),
    /// the final report must be submitted within one month of the incident notification.
    /// </remarks>
    public DateTimeOffset? FinalReportAtUtc { get; init; }

    /// <summary>
    /// Deadline for submitting the early warning (24 hours from detection).
    /// </summary>
    /// <remarks>Per Art. 23(4)(a).</remarks>
    public DateTimeOffset EarlyWarningDeadlineUtc => DetectedAtUtc.AddHours(24);

    /// <summary>
    /// Deadline for submitting the incident notification (72 hours from detection).
    /// </summary>
    /// <remarks>Per Art. 23(4)(b).</remarks>
    public DateTimeOffset IncidentNotificationDeadlineUtc => DetectedAtUtc.AddHours(72);

    /// <summary>
    /// Deadline for submitting the final report (1 month from incident notification submission).
    /// </summary>
    /// <remarks>
    /// Per Art. 23(4)(d). Returns <c>null</c> if the incident notification has not yet been submitted,
    /// as the final report deadline is calculated from the notification submission date.
    /// </remarks>
    public DateTimeOffset? FinalReportDeadlineUtc => IncidentNotificationAtUtc?.AddMonths(1);

    /// <summary>
    /// Creates a new NIS2 incident with a generated unique identifier.
    /// </summary>
    /// <param name="description">Description of the incident.</param>
    /// <param name="severity">Assessed severity.</param>
    /// <param name="detectedAtUtc">Timestamp when the incident was detected.</param>
    /// <param name="isSignificant">Whether the incident is significant under Art. 23(3).</param>
    /// <param name="affectedServices">Services affected by the incident.</param>
    /// <param name="initialAssessment">Initial impact assessment.</param>
    /// <returns>A new <see cref="NIS2Incident"/> with notification deadlines computed from the detection timestamp.</returns>
    public static NIS2Incident Create(
        string description,
        NIS2IncidentSeverity severity,
        DateTimeOffset detectedAtUtc,
        bool isSignificant,
        IReadOnlyList<string> affectedServices,
        string initialAssessment) =>
        new()
        {
            Id = Guid.NewGuid(),
            Description = description,
            Severity = severity,
            DetectedAtUtc = detectedAtUtc,
            IsSignificant = isSignificant,
            AffectedServices = affectedServices,
            InitialAssessment = initialAssessment
        };
}
