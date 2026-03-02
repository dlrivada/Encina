namespace Encina.Compliance.Retention.Model;

/// <summary>
/// Summary result of a retention enforcement execution.
/// </summary>
/// <remarks>
/// <para>
/// After the retention enforcement process evaluates expired records, this result
/// summarizes the outcomes: how many records were deleted, retained, failed, or
/// held by legal holds. The individual record-level details are available in
/// <see cref="Details"/>.
/// </para>
/// <para>
/// Per GDPR Article 5(2) (accountability principle), controllers must be able to
/// demonstrate compliance with data protection principles. This result provides
/// an auditable summary of each enforcement execution.
/// </para>
/// </remarks>
public sealed record DeletionResult
{
    /// <summary>
    /// Total number of retention records evaluated during this enforcement cycle.
    /// </summary>
    /// <remarks>
    /// Equal to the sum of <see cref="RecordsDeleted"/>, <see cref="RecordsRetained"/>,
    /// <see cref="RecordsFailed"/>, and <see cref="RecordsUnderHold"/>.
    /// </remarks>
    public required int TotalRecordsEvaluated { get; init; }

    /// <summary>
    /// Number of records successfully deleted during enforcement.
    /// </summary>
    public required int RecordsDeleted { get; init; }

    /// <summary>
    /// Number of records retained (still within retention period or no auto-delete policy).
    /// </summary>
    public required int RecordsRetained { get; init; }

    /// <summary>
    /// Number of records where deletion failed due to errors.
    /// </summary>
    public required int RecordsFailed { get; init; }

    /// <summary>
    /// Number of records skipped because they are under active legal hold.
    /// </summary>
    public required int RecordsUnderHold { get; init; }

    /// <summary>
    /// Per-record details describing the outcome for each evaluated record.
    /// </summary>
    public required IReadOnlyList<DeletionDetail> Details { get; init; }

    /// <summary>
    /// Timestamp when this enforcement execution completed (UTC).
    /// </summary>
    public required DateTimeOffset ExecutedAtUtc { get; init; }
}
