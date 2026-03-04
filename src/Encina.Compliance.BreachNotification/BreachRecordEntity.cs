namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Persistence entity for <see cref="Model.BreachRecord"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a breach record,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Model.BreachRecord.Status"/> (<see cref="Model.BreachStatus"/>) is stored
/// as <see cref="StatusValue"/> (<see cref="int"/>) for cross-provider compatibility.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.BreachRecord.Severity"/> (<see cref="Model.BreachSeverity"/>) is stored
/// as <see cref="SeverityValue"/> (<see cref="int"/>) for cross-provider compatibility.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.BreachRecord.SubjectNotificationExemption"/> (<see cref="Model.SubjectNotificationExemption"/>)
/// is stored as <see cref="SubjectNotificationExemptionValue"/> (<see cref="int"/>).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.BreachRecord.CategoriesOfDataAffected"/> (<see cref="System.Collections.Generic.IReadOnlyList{T}"/>)
/// is stored as <see cref="CategoriesOfDataAffected"/> (<see cref="string"/>) in JSON format.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="BreachRecordMapper"/> to convert between this entity and
/// <see cref="Model.BreachRecord"/>.
/// </para>
/// </remarks>
public sealed class BreachRecordEntity
{
    /// <summary>
    /// Unique identifier for this breach record.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Description of the nature of the personal data breach.
    /// </summary>
    public required string Nature { get; set; }

    /// <summary>
    /// Approximate number of data subjects affected by the breach.
    /// </summary>
    public required int ApproximateSubjectsAffected { get; set; }

    /// <summary>
    /// Categories of personal data affected, stored as a JSON array string.
    /// </summary>
    /// <remarks>
    /// Serialized representation of the <see cref="Model.BreachRecord.CategoriesOfDataAffected"/> list.
    /// Example: <c>["names","email addresses","financial data"]</c>.
    /// </remarks>
    public required string CategoriesOfDataAffected { get; set; }

    /// <summary>
    /// Name and contact details of the DPO or other contact point.
    /// </summary>
    public required string DPOContactDetails { get; set; }

    /// <summary>
    /// Description of the likely consequences of the breach.
    /// </summary>
    public required string LikelyConsequences { get; set; }

    /// <summary>
    /// Description of the measures taken or proposed to address the breach.
    /// </summary>
    public required string MeasuresTaken { get; set; }

    /// <summary>
    /// Timestamp when the breach was detected (UTC).
    /// </summary>
    /// <remarks>
    /// An INDEX should be created on this column for efficient time-range queries.
    /// </remarks>
    public DateTimeOffset DetectedAtUtc { get; set; }

    /// <summary>
    /// Deadline for notifying the supervisory authority (UTC).
    /// </summary>
    public DateTimeOffset NotificationDeadlineUtc { get; set; }

    /// <summary>
    /// Timestamp when the supervisory authority was notified (UTC).
    /// </summary>
    public DateTimeOffset? NotifiedAuthorityAtUtc { get; set; }

    /// <summary>
    /// Timestamp when data subjects were notified (UTC).
    /// </summary>
    public DateTimeOffset? NotifiedSubjectsAtUtc { get; set; }

    /// <summary>
    /// Integer value of the <see cref="Model.BreachSeverity"/> enum.
    /// </summary>
    /// <remarks>
    /// Values: Low=0, Medium=1, High=2, Critical=3.
    /// </remarks>
    public required int SeverityValue { get; set; }

    /// <summary>
    /// Integer value of the <see cref="Model.BreachStatus"/> enum.
    /// </summary>
    /// <remarks>
    /// Values: Detected=0, Investigating=1, AuthorityNotified=2, SubjectsNotified=3, Resolved=4, Closed=5.
    /// An INDEX should be created on this column for efficient status-based queries.
    /// </remarks>
    public required int StatusValue { get; set; }

    /// <summary>
    /// Reason for delaying notification beyond the 72-hour deadline.
    /// </summary>
    public string? DelayReason { get; set; }

    /// <summary>
    /// Integer value of the <see cref="Model.SubjectNotificationExemption"/> enum.
    /// </summary>
    /// <remarks>
    /// Values: None=0, EncryptionProtected=1, MitigatingMeasures=2, DisproportionateEffort=3.
    /// </remarks>
    public required int SubjectNotificationExemptionValue { get; set; }

    /// <summary>
    /// Timestamp when the breach was resolved (UTC).
    /// </summary>
    public DateTimeOffset? ResolvedAtUtc { get; set; }

    /// <summary>
    /// Summary of the resolution measures and outcomes.
    /// </summary>
    public string? ResolutionSummary { get; set; }
}
