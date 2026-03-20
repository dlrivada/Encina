using Microsoft.Extensions.Logging;

namespace Encina.Compliance.ProcessorAgreements.Diagnostics;

/// <summary>
/// High-performance structured log messages for the Processor Agreements module.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 9400-9499 range to avoid collisions with other
/// Encina subsystems (DPIA uses 8800-8899, PrivacyByDesign uses 8900-8949,
/// CrossBorderTransfer uses 9000-9099, NIS2 uses 9200-9299).
/// </para>
/// <para>
/// Allocation blocks:
/// <list type="table">
/// <item><term>9400-9409</term><description>Pipeline behavior</description></item>
/// <item><term>9410-9419</term><description>Audit trail</description></item>
/// <item><term>9120-9129</term><description>Processor registry operations (DC 2)</description></item>
/// <item><term>9130-9139</term><description>DPA store operations (DC 2)</description></item>
/// <item><term>9140-9149</term><description>CheckDPAExpirationHandler (DC 6)</description></item>
/// <item><term>9150-9159</term><description>Sub-processor operations and depth tracking (DC 5)</description></item>
/// <item><term>9460-9469</term><description>DPA validation</description></item>
/// <item><term>9470-9489</term><description>Service-level operations (DefaultProcessorService, DefaultDPAService)</description></item>
/// </list>
/// </para>
/// </remarks>
internal static partial class ProcessorAgreementLogMessages
{
    // ========================================================================
    // Pipeline behavior log messages (9400-9409)
    // ========================================================================

    /// <summary>Processor agreement pipeline skipped because enforcement mode is Disabled.</summary>
    [LoggerMessage(
        EventId = 9400,
        Level = LogLevel.Trace,
        Message = "Processor agreement pipeline skipped (enforcement disabled). RequestType={RequestType}")]
    internal static partial void ProcessorPipelineDisabled(this ILogger logger, string requestType);

    /// <summary>Processor agreement pipeline skipped because no [RequiresProcessor] attribute found.</summary>
    [LoggerMessage(
        EventId = 9401,
        Level = LogLevel.Trace,
        Message = "Processor agreement pipeline skipped (no [RequiresProcessor] attribute). RequestType={RequestType}")]
    internal static partial void ProcessorPipelineNoAttribute(this ILogger logger, string requestType);

    /// <summary>Processor agreement pipeline check started.</summary>
    [LoggerMessage(
        EventId = 9402,
        Level = LogLevel.Debug,
        Message = "Processor agreement pipeline check started. RequestType={RequestType}, ProcessorId={ProcessorId}, EnforcementMode={EnforcementMode}")]
    internal static partial void ProcessorPipelineStarted(this ILogger logger, string requestType, string processorId, string enforcementMode);

    /// <summary>Processor agreement pipeline check passed — valid DPA exists.</summary>
    [LoggerMessage(
        EventId = 9403,
        Level = LogLevel.Debug,
        Message = "Processor agreement pipeline check passed. RequestType={RequestType}, ProcessorId={ProcessorId}")]
    internal static partial void ProcessorPipelinePassed(this ILogger logger, string requestType, string processorId);

    /// <summary>Processor agreement pipeline check failed — no valid DPA for processor.</summary>
    [LoggerMessage(
        EventId = 9404,
        Level = LogLevel.Warning,
        Message = "Processor agreement pipeline check failed (no valid DPA). RequestType={RequestType}, ProcessorId={ProcessorId}")]
    internal static partial void ProcessorPipelineNoValidDPA(this ILogger logger, string requestType, string processorId);

    /// <summary>Processor agreement pipeline check failed — detailed validation found issues.</summary>
    [LoggerMessage(
        EventId = 9405,
        Level = LogLevel.Warning,
        Message = "Processor agreement pipeline check failed (validation). RequestType={RequestType}, ProcessorId={ProcessorId}, Reason={Reason}")]
    internal static partial void ProcessorPipelineValidationFailed(this ILogger logger, string requestType, string processorId, string reason);

    /// <summary>Processor agreement pipeline blocked the request in Block enforcement mode.</summary>
    [LoggerMessage(
        EventId = 9406,
        Level = LogLevel.Warning,
        Message = "Processor agreement pipeline blocked request. RequestType={RequestType}, ProcessorId={ProcessorId}, Reason={Reason}")]
    internal static partial void ProcessorPipelineBlocked(this ILogger logger, string requestType, string processorId, string reason);

    /// <summary>Processor agreement pipeline issued a warning but allowed the request.</summary>
    [LoggerMessage(
        EventId = 9407,
        Level = LogLevel.Warning,
        Message = "Processor agreement pipeline warning (request allowed). RequestType={RequestType}, ProcessorId={ProcessorId}, Reason={Reason}")]
    internal static partial void ProcessorPipelineWarned(this ILogger logger, string requestType, string processorId, string reason);

    /// <summary>Exception occurred in the processor agreement pipeline.</summary>
    [LoggerMessage(
        EventId = 9408,
        Level = LogLevel.Error,
        Message = "Processor agreement pipeline error. RequestType={RequestType}, ProcessorId={ProcessorId}")]
    internal static partial void ProcessorPipelineError(this ILogger logger, string requestType, string processorId, Exception exception);

    // ========================================================================
    // Audit trail log messages (9410-9419)
    // ========================================================================

    /// <summary>Audit entry successfully recorded.</summary>
    [LoggerMessage(
        EventId = 9410,
        Level = LogLevel.Debug,
        Message = "Audit entry recorded. ProcessorId={ProcessorId}, Action={Action}, PerformedBy={PerformedBy}")]
    internal static partial void AuditEntryRecorded(this ILogger logger, string processorId, string action, string performedBy);

    /// <summary>Audit entry recording failed (non-blocking).</summary>
    [LoggerMessage(
        EventId = 9411,
        Level = LogLevel.Warning,
        Message = "Audit entry recording failed (non-blocking). ProcessorId={ProcessorId}, Action={Action}")]
    internal static partial void AuditEntryFailed(this ILogger logger, string processorId, string action, Exception exception);

    // ========================================================================
    // Processor registry operation log messages (9420-9429, DC 2)
    // ========================================================================

    /// <summary>Processor registered successfully.</summary>
    [LoggerMessage(
        EventId = 9420,
        Level = LogLevel.Debug,
        Message = "Processor registered. ProcessorId={ProcessorId}, Name={Name}, Depth={Depth}")]
    internal static partial void ProcessorRegistered(this ILogger logger, string processorId, string name, int depth);

    /// <summary>Processor registration failed.</summary>
    [LoggerMessage(
        EventId = 9421,
        Level = LogLevel.Warning,
        Message = "Processor registration failed. ProcessorId={ProcessorId}, Reason={Reason}")]
    internal static partial void ProcessorRegistrationFailed(this ILogger logger, string processorId, string reason);

    /// <summary>Processor updated successfully.</summary>
    [LoggerMessage(
        EventId = 9422,
        Level = LogLevel.Debug,
        Message = "Processor updated. ProcessorId={ProcessorId}")]
    internal static partial void ProcessorUpdated(this ILogger logger, string processorId);

    /// <summary>Processor update failed.</summary>
    [LoggerMessage(
        EventId = 9423,
        Level = LogLevel.Warning,
        Message = "Processor update failed. ProcessorId={ProcessorId}, Reason={Reason}")]
    internal static partial void ProcessorUpdateFailed(this ILogger logger, string processorId, string reason);

    /// <summary>Processor removed successfully.</summary>
    [LoggerMessage(
        EventId = 9424,
        Level = LogLevel.Debug,
        Message = "Processor removed. ProcessorId={ProcessorId}")]
    internal static partial void ProcessorRemoved(this ILogger logger, string processorId);

    /// <summary>Processor removal failed.</summary>
    [LoggerMessage(
        EventId = 9425,
        Level = LogLevel.Warning,
        Message = "Processor removal failed. ProcessorId={ProcessorId}, Reason={Reason}")]
    internal static partial void ProcessorRemovalFailed(this ILogger logger, string processorId, string reason);

    /// <summary>Sub-processors retrieved for a processor.</summary>
    [LoggerMessage(
        EventId = 9426,
        Level = LogLevel.Debug,
        Message = "Sub-processors retrieved. ProcessorId={ProcessorId}, Count={Count}")]
    internal static partial void SubProcessorsRetrieved(this ILogger logger, string processorId, int count);

    /// <summary>Full sub-processor chain resolved via BFS traversal.</summary>
    [LoggerMessage(
        EventId = 9427,
        Level = LogLevel.Debug,
        Message = "Full sub-processor chain resolved. ProcessorId={ProcessorId}, ChainCount={ChainCount}")]
    internal static partial void SubProcessorChainResolved(this ILogger logger, string processorId, int chainCount);

    // ========================================================================
    // DPA store operation log messages (9430-9439, DC 2)
    // ========================================================================

    /// <summary>DPA added successfully.</summary>
    [LoggerMessage(
        EventId = 9430,
        Level = LogLevel.Debug,
        Message = "DPA added. DPAId={DPAId}, ProcessorId={ProcessorId}, Status={Status}")]
    internal static partial void DPAAdded(this ILogger logger, string dpaId, string processorId, string status);

    /// <summary>DPA addition failed.</summary>
    [LoggerMessage(
        EventId = 9431,
        Level = LogLevel.Warning,
        Message = "DPA addition failed. DPAId={DPAId}, Reason={Reason}")]
    internal static partial void DPAAdditionFailed(this ILogger logger, string dpaId, string reason);

    /// <summary>DPA updated successfully.</summary>
    [LoggerMessage(
        EventId = 9432,
        Level = LogLevel.Debug,
        Message = "DPA updated. DPAId={DPAId}, Status={Status}")]
    internal static partial void DPAUpdated(this ILogger logger, string dpaId, string status);

    /// <summary>DPA update failed.</summary>
    [LoggerMessage(
        EventId = 9433,
        Level = LogLevel.Warning,
        Message = "DPA update failed. DPAId={DPAId}, Reason={Reason}")]
    internal static partial void DPAUpdateFailed(this ILogger logger, string dpaId, string reason);

    /// <summary>Active DPA retrieved for a processor.</summary>
    [LoggerMessage(
        EventId = 9434,
        Level = LogLevel.Debug,
        Message = "Active DPA retrieved. ProcessorId={ProcessorId}, Found={Found}")]
    internal static partial void ActiveDPARetrieved(this ILogger logger, string processorId, bool found);

    /// <summary>Expiring DPAs retrieved.</summary>
    [LoggerMessage(
        EventId = 9435,
        Level = LogLevel.Debug,
        Message = "Expiring DPAs retrieved. Count={Count}, Threshold={Threshold}")]
    internal static partial void ExpiringDPAsRetrieved(this ILogger logger, int count, DateTimeOffset threshold);

    // ========================================================================
    // CheckDPAExpirationHandler log messages (9440-9449, DC 6)
    // ========================================================================

    /// <summary>DPA expiration check started.</summary>
    [LoggerMessage(
        EventId = 9440,
        Level = LogLevel.Debug,
        Message = "DPA expiration check started. WarningDays={WarningDays}, Threshold={Threshold}")]
    internal static partial void ExpirationCheckStarted(this ILogger logger, int warningDays, DateTimeOffset threshold);

    /// <summary>DPA expiration check completed.</summary>
    [LoggerMessage(
        EventId = 9441,
        Level = LogLevel.Information,
        Message = "DPA expiration check completed. ExpiredCount={ExpiredCount}, ExpiringCount={ExpiringCount}")]
    internal static partial void ExpirationCheckCompleted(this ILogger logger, int expiredCount, int expiringCount);

    /// <summary>Error during DPA expiration check.</summary>
    [LoggerMessage(
        EventId = 9442,
        Level = LogLevel.Error,
        Message = "DPA expiration check error. Operation={Operation}, ErrorMessage={ErrorMessage}")]
    internal static partial void ExpirationCheckError(this ILogger logger, string operation, string errorMessage);

    /// <summary>Expired DPA detected — status transitioned and notification published.</summary>
    [LoggerMessage(
        EventId = 9443,
        Level = LogLevel.Warning,
        Message = "DPA expired detected. ProcessorId={ProcessorId}, DPAId={DPAId}, ExpiredAtUtc={ExpiredAtUtc}")]
    internal static partial void DPAExpiredDetected(this ILogger logger, string processorId, string dpaId, DateTimeOffset expiredAtUtc);

    /// <summary>Expiring DPA detected — warning notification published.</summary>
    [LoggerMessage(
        EventId = 9444,
        Level = LogLevel.Warning,
        Message = "DPA expiring detected. ProcessorId={ProcessorId}, DPAId={DPAId}, DaysUntilExpiration={DaysUntilExpiration}")]
    internal static partial void DPAExpiringDetected(this ILogger logger, string processorId, string dpaId, int daysUntilExpiration);

    // ========================================================================
    // Sub-processor operations and depth tracking (9450-9459, DC 5)
    // ========================================================================

    /// <summary>Sub-processor registered with parent relationship.</summary>
    [LoggerMessage(
        EventId = 9450,
        Level = LogLevel.Information,
        Message = "Sub-processor registered. SubProcessorId={SubProcessorId}, ParentId={ParentId}, Depth={Depth}")]
    internal static partial void SubProcessorRegistered(this ILogger logger, string subProcessorId, string parentId, int depth);

    /// <summary>Sub-processor registration rejected — maximum depth exceeded (DC 5).</summary>
    [LoggerMessage(
        EventId = 9451,
        Level = LogLevel.Warning,
        Message = "Sub-processor depth exceeded. ProcessorId={ProcessorId}, RequestedDepth={RequestedDepth}, MaxDepth={MaxDepth}")]
    internal static partial void SubProcessorDepthExceeded(this ILogger logger, string processorId, int requestedDepth, int maxDepth);

    /// <summary>Sub-processor registration rejected — inconsistent depth with parent.</summary>
    [LoggerMessage(
        EventId = 9452,
        Level = LogLevel.Warning,
        Message = "Sub-processor depth inconsistent. SubProcessorId={SubProcessorId}, ParentId={ParentId}, ExpectedDepth={ExpectedDepth}, ActualDepth={ActualDepth}")]
    internal static partial void SubProcessorDepthInconsistent(this ILogger logger, string subProcessorId, string parentId, int expectedDepth, int actualDepth);

    // ========================================================================
    // DPA validation log messages (9460-9469)
    // ========================================================================

    /// <summary>DPA validation started for a processor.</summary>
    [LoggerMessage(
        EventId = 9460,
        Level = LogLevel.Debug,
        Message = "DPA validation started. ProcessorId={ProcessorId}")]
    internal static partial void ValidationStarted(this ILogger logger, string processorId);

    /// <summary>DPA validation passed — processor has a valid, active DPA.</summary>
    [LoggerMessage(
        EventId = 9461,
        Level = LogLevel.Debug,
        Message = "DPA validation passed. ProcessorId={ProcessorId}")]
    internal static partial void ValidationPassed(this ILogger logger, string processorId);

    /// <summary>DPA validation failed — compliance issue detected.</summary>
    [LoggerMessage(
        EventId = 9462,
        Level = LogLevel.Warning,
        Message = "DPA validation failed. ProcessorId={ProcessorId}, Reason={Reason}")]
    internal static partial void ValidationFailed(this ILogger logger, string processorId, string reason);

    /// <summary>DPA validation error — store operation failed.</summary>
    [LoggerMessage(
        EventId = 9463,
        Level = LogLevel.Error,
        Message = "DPA validation error. ProcessorId={ProcessorId}")]
    internal static partial void ValidationError(this ILogger logger, string processorId, Exception exception);

    // ========================================================================
    // Service-level operations log messages (9470-9489)
    // ========================================================================

    /// <summary>Processor service operation started.</summary>
    [LoggerMessage(
        EventId = 9470,
        Level = LogLevel.Debug,
        Message = "Processor service operation started. Operation={Operation}, ProcessorId={ProcessorId}")]
    internal static partial void ProcessorServiceOperationStarted(this ILogger logger, string operation, string processorId);

    /// <summary>Processor service operation completed successfully.</summary>
    [LoggerMessage(
        EventId = 9471,
        Level = LogLevel.Debug,
        Message = "Processor service operation completed. Operation={Operation}, ProcessorId={ProcessorId}")]
    internal static partial void ProcessorServiceOperationCompleted(this ILogger logger, string operation, string processorId);

    /// <summary>Processor service operation failed.</summary>
    [LoggerMessage(
        EventId = 9472,
        Level = LogLevel.Warning,
        Message = "Processor service operation failed. Operation={Operation}, ProcessorId={ProcessorId}, Reason={Reason}")]
    internal static partial void ProcessorServiceOperationFailed(this ILogger logger, string operation, string processorId, string reason);

    /// <summary>Processor service cache hit.</summary>
    [LoggerMessage(
        EventId = 9473,
        Level = LogLevel.Trace,
        Message = "Processor cache hit. CacheKey={CacheKey}")]
    internal static partial void ProcessorCacheHit(this ILogger logger, string cacheKey);

    /// <summary>Processor service cache miss — querying read model repository.</summary>
    [LoggerMessage(
        EventId = 9474,
        Level = LogLevel.Trace,
        Message = "Processor cache miss. CacheKey={CacheKey}")]
    internal static partial void ProcessorCacheMiss(this ILogger logger, string cacheKey);

    /// <summary>DPA service operation started.</summary>
    [LoggerMessage(
        EventId = 9475,
        Level = LogLevel.Debug,
        Message = "DPA service operation started. Operation={Operation}, DPAId={DPAId}")]
    internal static partial void DPAServiceOperationStarted(this ILogger logger, string operation, string dpaId);

    /// <summary>DPA service operation completed successfully.</summary>
    [LoggerMessage(
        EventId = 9476,
        Level = LogLevel.Debug,
        Message = "DPA service operation completed. Operation={Operation}, DPAId={DPAId}")]
    internal static partial void DPAServiceOperationCompleted(this ILogger logger, string operation, string dpaId);

    /// <summary>DPA service operation failed.</summary>
    [LoggerMessage(
        EventId = 9477,
        Level = LogLevel.Warning,
        Message = "DPA service operation failed. Operation={Operation}, DPAId={DPAId}, Reason={Reason}")]
    internal static partial void DPAServiceOperationFailed(this ILogger logger, string operation, string dpaId, string reason);

    /// <summary>DPA service cache hit.</summary>
    [LoggerMessage(
        EventId = 9478,
        Level = LogLevel.Trace,
        Message = "DPA cache hit. CacheKey={CacheKey}")]
    internal static partial void DPACacheHit(this ILogger logger, string cacheKey);

    /// <summary>DPA service cache miss — querying read model repository.</summary>
    [LoggerMessage(
        EventId = 9479,
        Level = LogLevel.Trace,
        Message = "DPA cache miss. CacheKey={CacheKey}")]
    internal static partial void DPACacheMiss(this ILogger logger, string cacheKey);

    /// <summary>DPA cache invalidated after mutation.</summary>
    [LoggerMessage(
        EventId = 9480,
        Level = LogLevel.Trace,
        Message = "DPA cache invalidated. DPAId={DPAId}, ProcessorId={ProcessorId}")]
    internal static partial void DPACacheInvalidated(this ILogger logger, string dpaId, string processorId);

    /// <summary>Processor cache invalidated after mutation.</summary>
    [LoggerMessage(
        EventId = 9481,
        Level = LogLevel.Trace,
        Message = "Processor cache invalidated. ProcessorId={ProcessorId}")]
    internal static partial void ProcessorCacheInvalidated(this ILogger logger, string processorId);

    /// <summary>DPA amend operation completed.</summary>
    [LoggerMessage(
        EventId = 9482,
        Level = LogLevel.Information,
        Message = "DPA amended. DPAId={DPAId}, Reason={Reason}")]
    internal static partial void DPAAmended(this ILogger logger, string dpaId, string reason);

    /// <summary>DPA audit recorded via event-sourced aggregate.</summary>
    [LoggerMessage(
        EventId = 9483,
        Level = LogLevel.Information,
        Message = "DPA audited. DPAId={DPAId}, AuditorId={AuditorId}")]
    internal static partial void DPAAudited(this ILogger logger, string dpaId, string auditorId);

    /// <summary>DPA renewed with new expiration date.</summary>
    [LoggerMessage(
        EventId = 9484,
        Level = LogLevel.Information,
        Message = "DPA renewed. DPAId={DPAId}, NewExpiresAtUtc={NewExpiresAtUtc}")]
    internal static partial void DPARenewed(this ILogger logger, string dpaId, DateTimeOffset newExpiresAtUtc);

    /// <summary>DPA terminated.</summary>
    [LoggerMessage(
        EventId = 9485,
        Level = LogLevel.Warning,
        Message = "DPA terminated. DPAId={DPAId}, Reason={Reason}")]
    internal static partial void DPATerminated(this ILogger logger, string dpaId, string reason);
}
