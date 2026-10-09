using LanguageExt;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// The recorder registered until the operation-audit-store recorder exists: it writes nothing and
/// always fails, so enabling the decision audit before a durable recorder is available fails closed
/// (or logs, in <see cref="ABACDecisionAuditFailureMode.BestEffort"/>) instead of silently dropping
/// the evidence.
/// </summary>
/// <remarks>
/// <see cref="ABACDecisionAuditOptions.Enabled"/> is <c>false</c> by default, so this recorder is
/// never called unless an application opts in.
/// </remarks>
internal sealed class UnavailableABACDecisionRecorder : IABACDecisionRecorder
{
    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ABACDecisionRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(ABACErrors.DecisionAuditStoreUnavailable());
    }
}
