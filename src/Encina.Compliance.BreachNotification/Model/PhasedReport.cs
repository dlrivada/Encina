namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Represents a phased report submitted for a breach, allowing progressive disclosure
/// of breach information as it becomes available.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 33(4), "where, and in so far as, it is not possible to provide
/// the information at the same time, the information may be provided in phases without
/// undue further delay." This supports scenarios where the full scope of a breach is
/// not immediately known at the time of initial notification.
/// </para>
/// <para>
/// Each phased report has an incrementing <see cref="ReportNumber"/> (starting at 1 for
/// the initial report) and contains the additional information discovered since the
/// previous report. The collection of all phased reports for a breach provides the
/// complete picture as it was progressively disclosed to the supervisory authority.
/// </para>
/// </remarks>
public sealed record PhasedReport
{
    /// <summary>
    /// Unique identifier for this phased report.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Identifier of the breach this report belongs to.
    /// </summary>
    public required string BreachId { get; init; }

    /// <summary>
    /// Sequential report number, starting at 1 for the initial report.
    /// </summary>
    /// <remarks>
    /// Report numbers increment with each additional submission for the same breach.
    /// Report 1 is the initial notification; subsequent reports provide supplementary
    /// information per Art. 33(4).
    /// </remarks>
    public required int ReportNumber { get; init; }

    /// <summary>
    /// Content of the phased report, describing newly discovered information.
    /// </summary>
    /// <remarks>
    /// Should include any updates to the Art. 33(3) required fields: nature of breach,
    /// approximate subjects affected, data categories, likely consequences, or measures taken.
    /// </remarks>
    public required string Content { get; init; }

    /// <summary>
    /// Timestamp when this phased report was submitted (UTC).
    /// </summary>
    public required DateTimeOffset SubmittedAtUtc { get; init; }

    /// <summary>
    /// Identifier of the user who submitted this phased report.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for system-generated reports (e.g., automated updates from
    /// the detection engine).
    /// </remarks>
    public string? SubmittedByUserId { get; init; }

    /// <summary>
    /// Creates a new phased report with a generated unique identifier.
    /// </summary>
    /// <param name="breachId">Identifier of the breach.</param>
    /// <param name="reportNumber">Sequential report number.</param>
    /// <param name="content">Content of the report.</param>
    /// <param name="submittedAtUtc">Timestamp when the report was submitted.</param>
    /// <param name="submittedByUserId">Identifier of the submitting user, if applicable.</param>
    /// <returns>A new <see cref="PhasedReport"/> with a generated GUID identifier.</returns>
    public static PhasedReport Create(
        string breachId,
        int reportNumber,
        string content,
        DateTimeOffset submittedAtUtc,
        string? submittedByUserId = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            BreachId = breachId,
            ReportNumber = reportNumber,
            Content = content,
            SubmittedAtUtc = submittedAtUtc,
            SubmittedByUserId = submittedByUserId
        };
}
