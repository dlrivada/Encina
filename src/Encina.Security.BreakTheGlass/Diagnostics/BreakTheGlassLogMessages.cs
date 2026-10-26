using Microsoft.Extensions.Logging;

namespace Encina.Security.BreakTheGlass.Diagnostics;

/// <summary>
/// Structured log messages of the break-the-glass gate, expiry sweeper, purge and start-up checks.
/// </summary>
/// <remarks>
/// <para>Event IDs: 9750-9764 (see <c>EventIdRanges.SecurityBreakTheGlass</c>).</para>
/// <para>
/// Arguments are counts, error codes, the scope name and an environment name only: never a user id,
/// a subject id, a justification, an elevation payload or an <see cref="EncinaError"/> message. An
/// exception reaches a logger through <c>ForLogging()</c>.
/// </para>
/// </remarks>
internal static partial class BreakTheGlassLogMessages
{
    // ── Gate (9750) ──────────────────────────────────────────────────

    [LoggerMessage(EventId = 9750, Level = LogLevel.Warning,
        Message = "Break-the-glass elevation lookup failed ({ErrorCode}); the original denial is kept")]
    internal static partial void AccessLookupFailed(ILogger logger, string errorCode);

    // ── Expiry sweeper (9751-9755) ───────────────────────────────────

    [LoggerMessage(EventId = 9751, Level = LogLevel.Debug,
        Message = "Break-the-glass expiry sweep completed: {Recorded} expiries recorded")]
    internal static partial void ExpirySweepCompleted(ILogger logger, int recorded);

    [LoggerMessage(EventId = 9752, Level = LogLevel.Error,
        Message = "Break-the-glass expiry sweep failed ({ErrorCode}); the cycle is not marked completed and is retried")]
    internal static partial void ExpirySweepFailed(ILogger logger, string errorCode);

    [LoggerMessage(EventId = 9753, Level = LogLevel.Error,
        Message = "Break-the-glass expiry audit entry could not be written ({ErrorCode}); the expiry is retried next cycle")]
    internal static partial void ExpiryAuditFailed(ILogger logger, string errorCode);

    [LoggerMessage(EventId = 9754, Level = LogLevel.Error,
        Message = "Break-the-glass expiry sweep threw an exception; the cycle is not marked completed and is retried")]
    internal static partial void ExpirySweepThrew(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9755, Level = LogLevel.Information,
        Message = "Break-the-glass expiry recorded for an elevation of scope {Scope}")]
    internal static partial void ExpiryRecorded(ILogger logger, string scope);

    // ── Purge (9756-9758) ────────────────────────────────────────────

    [LoggerMessage(EventId = 9756, Level = LogLevel.Information,
        Message = "Break-the-glass purge deleted {Count} reviewed elevations older than {RetentionDays} days")]
    internal static partial void PurgeCompleted(ILogger logger, int count, int retentionDays);

    [LoggerMessage(EventId = 9757, Level = LogLevel.Error,
        Message = "Break-the-glass purge failed ({ErrorCode}); it is retried at the next interval")]
    internal static partial void PurgeFailed(ILogger logger, string errorCode);

    [LoggerMessage(EventId = 9758, Level = LogLevel.Error,
        Message = "Break-the-glass purge threw an exception; it is retried at the next interval")]
    internal static partial void PurgeThrew(ILogger logger, Exception exception);

    // ── Start-up checks (9759-9763) ──────────────────────────────────

    [LoggerMessage(EventId = 9759, Level = LogLevel.Warning,
        Message = "The in-memory break-the-glass store is in use in environment {Environment}: elevations are lost on restart and are not shared between hosts")]
    internal static partial void InMemoryStoreInUse(ILogger logger, string environment);

    [LoggerMessage(EventId = 9760, Level = LogLevel.Warning,
        Message = "The in-memory operation audit store is in use: break-the-glass evidence is lost on restart")]
    internal static partial void InMemoryAuditStoreInUse(ILogger logger);

    [LoggerMessage(EventId = 9761, Level = LogLevel.Critical,
        Message = "Break-the-glass is enabled but no IOperationAuditStore is registered: the evidence of every emergency access needs one")]
    internal static partial void AuditStoreMissing(ILogger logger);

    [LoggerMessage(EventId = 9762, Level = LogLevel.Warning,
        Message = "BreakTheGlassOptions.RequireTenant is false although tenancy is registered; AllowMissingTenantRequirement is set, so elevations may be opened without a tenant")]
    internal static partial void TenantRequirementOptedOut(ILogger logger);

    [LoggerMessage(EventId = 9763, Level = LogLevel.Critical,
        Message = "Break-the-glass start-up validation failed with {FailureCount} problems")]
    internal static partial void StartupValidationFailed(ILogger logger, int failureCount);

    // ── Purge disabled (9764) ────────────────────────────────────────

    [LoggerMessage(EventId = 9764, Level = LogLevel.Information,
        Message = "Break-the-glass purge is disabled (EnableAutoPurge = false); reviewed elevations are kept until it is enabled")]
    internal static partial void PurgeDisabled(ILogger logger);
}
