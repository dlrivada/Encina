namespace Encina.Compliance.Retention.Model;

/// <summary>
/// Describes the outcome of a retention enforcement action on a single data record.
/// </summary>
/// <remarks>
/// <para>
/// During retention enforcement, each expired or eligible record is evaluated
/// individually. This detail record captures the entity identifier, data category,
/// the outcome of the evaluation, and the reason if the record was not deleted.
/// </para>
/// <para>
/// Deletion details are collected in <see cref="DeletionResult.Details"/> to provide
/// a complete audit trail of each enforcement cycle.
/// </para>
/// </remarks>
public sealed record DeletionDetail
{
    /// <summary>
    /// Identifier of the data entity that was evaluated.
    /// </summary>
    public required string EntityId { get; init; }

    /// <summary>
    /// The data category of the evaluated record.
    /// </summary>
    public required string DataCategory { get; init; }

    /// <summary>
    /// The outcome of the enforcement action for this record.
    /// </summary>
    public required DeletionOutcome Outcome { get; init; }

    /// <summary>
    /// Human-readable reason explaining the outcome.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for successful deletions. Populated when a record is retained,
    /// held, failed, or skipped — for example: "Under active legal hold: Case #2024-456"
    /// or "Deletion failed: connection timeout".
    /// </remarks>
    public string? Reason { get; init; }
}
