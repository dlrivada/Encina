using LanguageExt;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// Persists the decision records the Policy Enforcement Point produces.
/// </summary>
/// <remarks>
/// <para>
/// The Policy Enforcement Point awaits <see cref="RecordAsync"/> before the protected handler runs
/// (write-ahead). A <c>Left</c> result, an exception or a timeout means the record was not written;
/// depending on <see cref="ABACDecisionAuditOptions.FailureMode"/> the request is then denied or
/// proceeds with a logged failure.
/// </para>
/// <para>
/// An implementation must be safe to share across requests, must not link the write to the client's
/// cancellation (a disconnect must not erase the evidence of a denied attempt) and must never put an
/// error or exception message in the returned error.
/// </para>
/// </remarks>
public interface IABACDecisionRecorder
{
    /// <summary>
    /// Writes one decision record.
    /// </summary>
    /// <param name="record">The record to write.</param>
    /// <param name="cancellationToken">A token the write is bounded by; the Policy Enforcement Point passes none.</param>
    /// <returns><c>Right</c> when the record was written; otherwise <c>Left</c> with the failure.</returns>
    ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ABACDecisionRecord record,
        CancellationToken cancellationToken = default);
}
