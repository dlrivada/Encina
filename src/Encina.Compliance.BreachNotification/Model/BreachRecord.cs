namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Tracks a personal data breach throughout its lifecycle, from detection through
/// authority notification, data subject notification, and resolution.
/// </summary>
/// <remarks>
/// <para>
/// A breach record captures all information required by GDPR Article 33(3) for
/// supervisory authority notification, including the nature of the breach, the
/// approximate number of data subjects affected, the categories of data involved,
/// the DPO contact details, the likely consequences, and the measures taken to
/// address the breach.
/// </para>
/// <para>
/// Per GDPR Article 33(1), the controller must notify the supervisory authority
/// "without undue delay and, where feasible, not later than 72 hours after having
/// become aware of it." The <see cref="NotificationDeadlineUtc"/> property enforces
/// this 72-hour window from the moment of detection.
/// </para>
/// <para>
/// Per GDPR Article 33(4), "where, and in so far as, it is not possible to provide
/// the information at the same time, the information may be provided in phases without
/// undue further delay." The <see cref="PhasedReports"/> collection supports this
/// progressive disclosure requirement.
/// </para>
/// <para>
/// Per GDPR Article 33(5), the controller must document the facts relating to
/// the personal data breach, its effects and the remedial action taken. This record
/// serves as that documentation and should be retained for supervisory authority review.
/// </para>
/// </remarks>
public sealed record BreachRecord
{
    /// <summary>
    /// Unique identifier for this breach record.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Description of the nature of the personal data breach.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(3)(a), this must include "where possible, the categories and approximate
    /// number of data subjects concerned and the categories and approximate number of
    /// personal data records concerned."
    /// </remarks>
    public required string Nature { get; init; }

    /// <summary>
    /// Approximate number of data subjects affected by the breach.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(3)(a), this is an approximate figure. If the exact number is unknown
    /// at the time of the initial report, it can be updated in a phased report (Art. 33(4)).
    /// </remarks>
    public required int ApproximateSubjectsAffected { get; init; }

    /// <summary>
    /// Categories of personal data affected by the breach.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(3)(a), examples include: names, email addresses, financial data,
    /// health records, identification numbers. Each category should be a descriptive string.
    /// </remarks>
    public required IReadOnlyList<string> CategoriesOfDataAffected { get; init; }

    /// <summary>
    /// Name and contact details of the data protection officer (DPO) or other contact point.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(3)(b), the notification must communicate "the name and contact details
    /// of the data protection officer or other contact point where more information can be obtained."
    /// </remarks>
    public required string DPOContactDetails { get; init; }

    /// <summary>
    /// Description of the likely consequences of the personal data breach.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(3)(c), the notification must describe the likely consequences
    /// (e.g., identity theft, financial loss, discrimination, reputational damage).
    /// </remarks>
    public required string LikelyConsequences { get; init; }

    /// <summary>
    /// Description of the measures taken or proposed to address the breach.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(3)(d), the notification must describe "the measures taken or proposed
    /// to be taken by the controller to address the personal data breach, including,
    /// where appropriate, measures to mitigate its possible adverse effects."
    /// </remarks>
    public required string MeasuresTaken { get; init; }

    /// <summary>
    /// Timestamp when the breach was detected or when the controller became aware of it (UTC).
    /// </summary>
    /// <remarks>
    /// The 72-hour notification deadline starts from this timestamp per Art. 33(1).
    /// The "awareness" moment is when the controller has a reasonable degree of certainty
    /// that a security incident has occurred leading to personal data being compromised
    /// (EDPB Guidelines 9/2022, Section 2.3).
    /// </remarks>
    public required DateTimeOffset DetectedAtUtc { get; init; }

    /// <summary>
    /// Deadline for notifying the supervisory authority (UTC).
    /// </summary>
    /// <remarks>
    /// Calculated as <see cref="DetectedAtUtc"/> + 72 hours per Art. 33(1).
    /// If the deadline passes without notification, the controller must provide
    /// reasons for the delay per Art. 33(1).
    /// </remarks>
    public required DateTimeOffset NotificationDeadlineUtc { get; init; }

    /// <summary>
    /// Timestamp when the supervisory authority was notified (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> until the authority has been notified. Set when the notification
    /// is successfully delivered via <c>IBreachNotifier.NotifyAuthorityAsync</c>.
    /// </remarks>
    public DateTimeOffset? NotifiedAuthorityAtUtc { get; init; }

    /// <summary>
    /// Timestamp when data subjects were notified (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> until subjects have been notified or an exemption applies.
    /// Per Art. 34(1), required only when the breach "is likely to result in a high risk
    /// to the rights and freedoms of natural persons."
    /// </remarks>
    public DateTimeOffset? NotifiedSubjectsAtUtc { get; init; }

    /// <summary>
    /// Assessed severity of the breach.
    /// </summary>
    public required BreachSeverity Severity { get; init; }

    /// <summary>
    /// Current lifecycle status of the breach.
    /// </summary>
    public required BreachStatus Status { get; init; }

    /// <summary>
    /// Collection of phased reports submitted for this breach.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(4), information may be provided in phases without undue further delay
    /// when it is not possible to provide all information at the same time.
    /// </remarks>
    public IReadOnlyList<PhasedReport> PhasedReports { get; init; } = [];

    /// <summary>
    /// Reason for delaying notification beyond the 72-hour deadline.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(1), "where the notification to the supervisory authority is not made
    /// within 72 hours, it shall be accompanied by reasons for the delay."
    /// <c>null</c> when notification was made within the deadline.
    /// </remarks>
    public string? DelayReason { get; init; }

    /// <summary>
    /// Applicable exemption from notifying data subjects.
    /// </summary>
    /// <remarks>
    /// Per Art. 34(3), one of the exemption conditions may apply, removing the obligation
    /// to notify data subjects individually. Defaults to <see cref="SubjectNotificationExemption.None"/>.
    /// </remarks>
    public SubjectNotificationExemption SubjectNotificationExemption { get; init; }

    /// <summary>
    /// Timestamp when the breach was resolved (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> until the breach is resolved. Set when the controller determines
    /// that all remedial actions have been completed and the risk has been mitigated.
    /// </remarks>
    public DateTimeOffset? ResolvedAtUtc { get; init; }

    /// <summary>
    /// Summary of the resolution measures and outcomes.
    /// </summary>
    /// <remarks>
    /// <c>null</c> until the breach is resolved. Per Art. 33(5), this contributes to
    /// the documentation of remedial actions taken.
    /// </remarks>
    public string? ResolutionSummary { get; init; }

    /// <summary>
    /// Creates a new breach record with a generated unique identifier and
    /// the 72-hour notification deadline calculated from the detection timestamp.
    /// </summary>
    /// <param name="nature">Description of the nature of the breach.</param>
    /// <param name="approximateSubjectsAffected">Approximate number of data subjects affected.</param>
    /// <param name="categoriesOfDataAffected">Categories of personal data affected.</param>
    /// <param name="dpoContactDetails">DPO or contact point details.</param>
    /// <param name="likelyConsequences">Description of likely consequences.</param>
    /// <param name="measuresTaken">Description of measures taken or proposed.</param>
    /// <param name="detectedAtUtc">Timestamp when the breach was detected.</param>
    /// <param name="severity">Assessed severity of the breach.</param>
    /// <returns>A new <see cref="BreachRecord"/> with <see cref="BreachStatus.Detected"/> status
    /// and a 72-hour notification deadline.</returns>
    public static BreachRecord Create(
        string nature,
        int approximateSubjectsAffected,
        IReadOnlyList<string> categoriesOfDataAffected,
        string dpoContactDetails,
        string likelyConsequences,
        string measuresTaken,
        DateTimeOffset detectedAtUtc,
        BreachSeverity severity) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Nature = nature,
            ApproximateSubjectsAffected = approximateSubjectsAffected,
            CategoriesOfDataAffected = categoriesOfDataAffected,
            DPOContactDetails = dpoContactDetails,
            LikelyConsequences = likelyConsequences,
            MeasuresTaken = measuresTaken,
            DetectedAtUtc = detectedAtUtc,
            NotificationDeadlineUtc = detectedAtUtc.AddHours(72),
            Severity = severity,
            Status = BreachStatus.Detected,
            SubjectNotificationExemption = SubjectNotificationExemption.None
        };
}
