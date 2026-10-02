using Encina.Diagnostics;
using Encina.Security.Audit;

using LanguageExt;

using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.Auditing;

/// <summary>
/// Shared audit-entry recording for the audited secret decorators (reader, writer, rotator).
/// Audit failures are logged and never propagate to the secret operation.
/// </summary>
internal static class SecretAuditRecorder
{
    /// <summary>
    /// Builds and records one audit entry for a secret operation.
    /// </summary>
    /// <typeparam name="TRight">The success type of the audited operation's result.</typeparam>
    /// <param name="auditStore">The store the entry is recorded in.</param>
    /// <param name="requestContextAccessor">Accessor read at the moment the entry is recorded.</param>
    /// <param name="logger">The logger of the calling decorator.</param>
    /// <param name="action">The audit action name (for example <c>SecretAccess</c>).</param>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="result">
    /// The operation result used to derive the failure message, or <c>null</c> when none is recorded.
    /// </param>
    /// <param name="startedAt">When the operation started.</param>
    /// <param name="completedAt">When the operation completed.</param>
    /// <param name="cancellationToken">A token to cancel the audit write.</param>
    internal static async ValueTask RecordAsync<TRight>(
        IAuditStore auditStore,
        IRequestContextAccessor requestContextAccessor,
        ILogger logger,
        string action,
        string secretName,
        bool isSuccess,
        Either<EncinaError, TRight>? result,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            var errorMessage = ErrorMessageOf(isSuccess, result);
            var requestContext = requestContextAccessor.RequestContext;

            var entry = BuildEntry(
                requestContext, action, secretName, isSuccess, errorMessage, startedAt, completedAt);

            var auditResult = await auditStore.RecordAsync(entry, cancellationToken).ConfigureAwait(false);
            auditResult.Match(
                Right: _ => Log.AuditEntryRecorded(logger, secretName),
                Left: _ => Log.AuditEntryFailed(logger, secretName, new InvalidOperationException("Audit store returned error")));
        }
        catch (Exception ex)
        {
            // Audit failures must never block secret operations
            Log.AuditEntryFailed(logger, secretName, ex.ForLogging());
        }
    }

    private static string? ErrorMessageOf<TRight>(bool isSuccess, Either<EncinaError, TRight>? result)
    {
        if (isSuccess || !result.HasValue)
        {
            return null;
        }

        return result.Value.MatchUnsafe(Right: _ => (string?)null, Left: e => e.GetCode().IfNone("encina.unknown"));
    }

    private static AuditEntry BuildEntry(
        IRequestContext? requestContext,
        string action,
        string secretName,
        bool isSuccess,
        string? errorMessage,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt)
    {
        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = CorrelationIdOf(requestContext),
            UserId = requestContext?.UserId,
            TenantId = requestContext?.TenantId,
            Action = action,
            EntityType = "Secret",
            EntityId = secretName,
            Outcome = isSuccess ? AuditOutcome.Success : AuditOutcome.Failure,
            ErrorMessage = errorMessage,
            TimestampUtc = completedAt.UtcDateTime,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            Metadata = MetadataOf(secretName, isSuccess)
        };
    }

    private static string CorrelationIdOf(IRequestContext? requestContext) =>
        requestContext?.CorrelationId ?? Guid.NewGuid().ToString();

    private static Dictionary<string, object?> MetadataOf(string secretName, bool isSuccess) =>
        new()
        {
            ["secretName"] = secretName,
            ["result"] = isSuccess ? "success" : "failure"
        };
}
