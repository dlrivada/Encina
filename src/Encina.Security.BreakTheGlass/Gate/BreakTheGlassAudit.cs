using Encina.Security.Audit;
using LanguageExt;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Builds and writes the operation-audit entries of the break-the-glass lifecycle.
/// </summary>
/// <remarks>
/// An entry names the elevation id as its entity id and carries only fixed keys in its metadata:
/// never the subject id, the justification, the review note or an error message.
/// </remarks>
internal static class BreakTheGlassAudit
{
    public const string EntityType = "BreakTheGlassElevation";
    public const string ActionOpened = "break_glass.opened";
    public const string ActionOpenFailed = "break_glass.open_failed";
    public const string ActionAccess = "break_glass.access";
    public const string ActionRevoked = "break_glass.revoked";
    public const string ActionReviewed = "break_glass.reviewed";
    public const string ActionExpired = "break_glass.expired";

    public static OperationAuditEntry Create(
        string action,
        AuditOutcome outcome,
        Guid elevationId,
        string correlationId,
        string? userId,
        string? tenantId,
        DateTimeOffset nowUtc,
        IReadOnlyDictionary<string, object?> metadata) => new()
        {
            Id = Guid.CreateVersion7(nowUtc),
            CorrelationId = correlationId,
            UserId = userId,
            TenantId = tenantId,
            Action = action,
            EntityType = EntityType,
            EntityId = elevationId.ToString("D"),
            Outcome = outcome,
            TimestampUtc = nowUtc.UtcDateTime,
            StartedAtUtc = nowUtc,
            CompletedAtUtc = nowUtc,
            Metadata = metadata
        };

    // Writes the entry; a Left (or a store that throws) becomes break_glass.audit_failed so the caller fails closed.
    public static async ValueTask<Either<EncinaError, Unit>> WriteAsync(
        IOperationAuditStore store, OperationAuditEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var result = await store.RecordAsync(entry, cancellationToken).ConfigureAwait(false);
            return result.MapLeft(_ => BreakTheGlassErrors.AuditFailed());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return BreakTheGlassErrors.AuditFailed();
        }
    }
}
