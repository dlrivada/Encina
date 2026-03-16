using Microsoft.Extensions.Logging;

namespace Encina.Compliance.BreachNotification.Diagnostics;

/// <summary>
/// High-performance structured log messages for the Breach Notification module.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 8700-8799 range to avoid collisions with other
/// Encina subsystems (GDPR uses 8100-8199, Consent uses 8200-8299, DSR uses 8300-8399,
/// Anonymization uses 8400-8499, Retention uses 8500-8599, DataResidency uses 8600-8699).
/// </para>
/// <para>
/// Allocation blocks:
/// <list type="table">
/// <item><term>8700-8709</term><description>Pipeline behavior</description></item>
/// <item><term>8710-8719</term><description>Detection engine</description></item>
/// <item><term>8720-8729</term><description>Notification</description></item>
/// <item><term>8730-8739</term><description>Breach lifecycle</description></item>
/// <item><term>8740-8749</term><description>Deadline monitoring</description></item>
/// <item><term>8750-8759</term><description>Health check</description></item>
/// <item><term>8760-8769</term><description>Event stream / audit</description></item>
/// <item><term>8770-8779</term><description>Cache operations</description></item>
/// <item><term>8780-8799</term><description>Service operations</description></item>
/// </list>
/// </para>
/// </remarks>
internal static partial class BreachNotificationLogMessages
{
    // ========================================================================
    // Pipeline behavior log messages (8700-8709)
    // ========================================================================

    /// <summary>Breach detection pipeline skipped because enforcement mode is Disabled.</summary>
    [LoggerMessage(
        EventId = 8700,
        Level = LogLevel.Trace,
        Message = "Breach detection pipeline skipped (enforcement disabled). RequestType={RequestType}")]
    internal static partial void BreachPipelineDisabled(this ILogger logger, string requestType);

    /// <summary>Breach detection pipeline skipped because no [BreachMonitored] attributes found on request type.</summary>
    [LoggerMessage(
        EventId = 8701,
        Level = LogLevel.Trace,
        Message = "Breach detection pipeline skipped (no [BreachMonitored] attributes). RequestType={RequestType}")]
    internal static partial void BreachPipelineNoAttributes(this ILogger logger, string requestType);

    /// <summary>Breach detection pipeline started evaluating a request.</summary>
    [LoggerMessage(
        EventId = 8702,
        Level = LogLevel.Debug,
        Message = "Breach detection pipeline started. RequestType={RequestType}")]
    internal static partial void BreachPipelineStarted(this ILogger logger, string requestType);

    /// <summary>Breach detection pipeline detected a potential breach from the request/response.</summary>
    [LoggerMessage(
        EventId = 8703,
        Level = LogLevel.Warning,
        Message = "Breach detected by pipeline. RequestType={RequestType}, Severity={Severity}, DetectionRule={DetectionRule}")]
    internal static partial void BreachPipelineBreachDetected(this ILogger logger, string requestType, string severity, string detectionRule);

    /// <summary>Breach detection pipeline blocked the request because a breach was detected in Block mode.</summary>
    [LoggerMessage(
        EventId = 8704,
        Level = LogLevel.Warning,
        Message = "Breach detection pipeline blocked request. RequestType={RequestType}, Severity={Severity}, DetectionRule={DetectionRule}")]
    internal static partial void BreachPipelineBreachBlocked(this ILogger logger, string requestType, string severity, string detectionRule);

    /// <summary>Breach detection pipeline issued a warning because a breach was detected in Warn mode.</summary>
    [LoggerMessage(
        EventId = 8705,
        Level = LogLevel.Warning,
        Message = "Breach detection pipeline warning. RequestType={RequestType}, Severity={Severity}, DetectionRule={DetectionRule}")]
    internal static partial void BreachPipelineBreachWarning(this ILogger logger, string requestType, string severity, string detectionRule);

    /// <summary>Breach detection pipeline completed with no breach detected.</summary>
    [LoggerMessage(
        EventId = 8706,
        Level = LogLevel.Debug,
        Message = "Breach detection pipeline completed (no breach). RequestType={RequestType}")]
    internal static partial void BreachPipelineCompleted(this ILogger logger, string requestType);

    /// <summary>Exception occurred in the breach detection pipeline.</summary>
    [LoggerMessage(
        EventId = 8707,
        Level = LogLevel.Error,
        Message = "Breach detection pipeline error. RequestType={RequestType}")]
    internal static partial void BreachPipelineError(this ILogger logger, string requestType, Exception exception);

    // ========================================================================
    // Detection engine log messages (8710-8719)
    // ========================================================================

    /// <summary>Breach detection started for a security event.</summary>
    [LoggerMessage(
        EventId = 8710,
        Level = LogLevel.Debug,
        Message = "Breach detection started. EventType={EventType}, Source={Source}, RuleCount={RuleCount}")]
    internal static partial void BreachDetectionStarted(this ILogger logger, string eventType, string source, int ruleCount);

    /// <summary>A detection rule matched and identified a potential breach.</summary>
    [LoggerMessage(
        EventId = 8711,
        Level = LogLevel.Information,
        Message = "Breach detection rule matched. RuleName={RuleName}, Severity={Severity}, Description={Description}")]
    internal static partial void BreachDetectionRuleMatched(this ILogger logger, string ruleName, string severity, string description);

    /// <summary>A detection rule did not match the security event.</summary>
    [LoggerMessage(
        EventId = 8712,
        Level = LogLevel.Trace,
        Message = "Breach detection rule no match. RuleName={RuleName}")]
    internal static partial void BreachDetectionRuleNoMatch(this ILogger logger, string ruleName);

    /// <summary>Breach detection completed for a security event.</summary>
    [LoggerMessage(
        EventId = 8713,
        Level = LogLevel.Debug,
        Message = "Breach detection completed. EventType={EventType}, BreachesFound={BreachesFound}")]
    internal static partial void BreachDetectionCompleted(this ILogger logger, string eventType, int breachesFound);

    /// <summary>A detection rule evaluation failed with an error.</summary>
    [LoggerMessage(
        EventId = 8714,
        Level = LogLevel.Warning,
        Message = "Breach detection rule evaluation failed. RuleName={RuleName}")]
    internal static partial void BreachDetectionRuleFailed(this ILogger logger, string ruleName, Exception exception);

    // ========================================================================
    // Notification log messages (8720-8729)
    // ========================================================================

    /// <summary>Authority notification process started for a breach.</summary>
    [LoggerMessage(
        EventId = 8720,
        Level = LogLevel.Information,
        Message = "Authority notification started. BreachId={BreachId}, Severity={Severity}")]
    internal static partial void AuthorityNotificationStarted(this ILogger logger, string breachId, string severity);

    /// <summary>Authority notification sent successfully for a breach.</summary>
    [LoggerMessage(
        EventId = 8721,
        Level = LogLevel.Information,
        Message = "Authority notification sent. BreachId={BreachId}, TimeToNotificationHours={TimeToNotificationHours}")]
    internal static partial void AuthorityNotificationSent(this ILogger logger, string breachId, double timeToNotificationHours);

    /// <summary>Authority notification failed for a breach.</summary>
    [LoggerMessage(
        EventId = 8722,
        Level = LogLevel.Error,
        Message = "Authority notification failed. BreachId={BreachId}, ErrorMessage={ErrorMessage}")]
    internal static partial void AuthorityNotificationFailed(this ILogger logger, string breachId, string errorMessage);

    /// <summary>Data subject notification process started for a breach.</summary>
    [LoggerMessage(
        EventId = 8723,
        Level = LogLevel.Information,
        Message = "Subject notification started. BreachId={BreachId}, SubjectCount={SubjectCount}")]
    internal static partial void SubjectNotificationStarted(this ILogger logger, string breachId, int subjectCount);

    /// <summary>Data subject notification sent successfully for a breach.</summary>
    [LoggerMessage(
        EventId = 8724,
        Level = LogLevel.Information,
        Message = "Subject notification sent. BreachId={BreachId}, SubjectCount={SubjectCount}")]
    internal static partial void SubjectNotificationSent(this ILogger logger, string breachId, int subjectCount);

    /// <summary>Data subject notification failed for a breach.</summary>
    [LoggerMessage(
        EventId = 8725,
        Level = LogLevel.Error,
        Message = "Subject notification failed. BreachId={BreachId}, ErrorMessage={ErrorMessage}")]
    internal static partial void SubjectNotificationFailed(this ILogger logger, string breachId, string errorMessage);

    /// <summary>Data subject notification exempted per Art. 34(3) for a breach.</summary>
    [LoggerMessage(
        EventId = 8726,
        Level = LogLevel.Information,
        Message = "Subject notification exempted. BreachId={BreachId}, Exemption={Exemption}")]
    internal static partial void SubjectNotificationExempted(this ILogger logger, string breachId, string exemption);

    // ========================================================================
    // Breach lifecycle log messages (8730-8739)
    // ========================================================================

    /// <summary>A new breach was recorded via the event-sourced aggregate.</summary>
    [LoggerMessage(
        EventId = 8730,
        Level = LogLevel.Information,
        Message = "Breach recorded. BreachId={BreachId}, Severity={Severity}, Nature={Nature}")]
    internal static partial void BreachRecorded(this ILogger logger, string breachId, string severity, string nature);

    /// <summary>A breach aggregate status was updated via an event.</summary>
    [LoggerMessage(
        EventId = 8731,
        Level = LogLevel.Information,
        Message = "Breach status updated. BreachId={BreachId}, NewStatus={NewStatus}")]
    internal static partial void BreachStatusUpdated(this ILogger logger, string breachId, string newStatus);

    /// <summary>A phased report was added to a breach aggregate per Art. 33(4).</summary>
    [LoggerMessage(
        EventId = 8732,
        Level = LogLevel.Information,
        Message = "Phased report added. BreachId={BreachId}, ReportNumber={ReportNumber}")]
    internal static partial void PhasedReportAdded(this ILogger logger, string breachId, int reportNumber);

    /// <summary>A breach was resolved.</summary>
    [LoggerMessage(
        EventId = 8733,
        Level = LogLevel.Information,
        Message = "Breach resolved. BreachId={BreachId}, Severity={Severity}, ResolutionSummary={ResolutionSummary}")]
    internal static partial void BreachResolved(this ILogger logger, string breachId, string severity, string? resolutionSummary);

    /// <summary>A breach was closed.</summary>
    [LoggerMessage(
        EventId = 8734,
        Level = LogLevel.Information,
        Message = "Breach closed. BreachId={BreachId}")]
    internal static partial void BreachClosed(this ILogger logger, string breachId);

    /// <summary>A breach was contained.</summary>
    [LoggerMessage(
        EventId = 8735,
        Level = LogLevel.Information,
        Message = "Breach contained. BreachId={BreachId}")]
    internal static partial void BreachContained(this ILogger logger, string breachId);

    /// <summary>A breach was assessed and its severity potentially updated.</summary>
    [LoggerMessage(
        EventId = 8736,
        Level = LogLevel.Information,
        Message = "Breach assessed. BreachId={BreachId}, UpdatedSeverity={UpdatedSeverity}")]
    internal static partial void BreachAssessed(this ILogger logger, string breachId, string updatedSeverity);

    // ========================================================================
    // Deadline monitoring log messages (8740-8749)
    // ========================================================================

    /// <summary>Deadline monitoring check started.</summary>
    [LoggerMessage(
        EventId = 8740,
        Level = LogLevel.Debug,
        Message = "Breach deadline check started")]
    internal static partial void DeadlineCheckStarted(this ILogger logger);

    /// <summary>Breach approaching its 72-hour notification deadline.</summary>
    [LoggerMessage(
        EventId = 8741,
        Level = LogLevel.Warning,
        Message = "Breach deadline approaching. BreachId={BreachId}, RemainingHours={RemainingHours}")]
    internal static partial void DeadlineWarning(this ILogger logger, string breachId, double remainingHours);

    /// <summary>Breach has exceeded its 72-hour notification deadline without authority notification.</summary>
    [LoggerMessage(
        EventId = 8742,
        Level = LogLevel.Error,
        Message = "Breach deadline overdue. BreachId={BreachId}, OverdueHours={OverdueHours}")]
    internal static partial void DeadlineOverdue(this ILogger logger, string breachId, double overdueHours);

    /// <summary>Deadline monitoring check completed.</summary>
    [LoggerMessage(
        EventId = 8743,
        Level = LogLevel.Debug,
        Message = "Breach deadline check completed. ApproachingCount={ApproachingCount}, OverdueCount={OverdueCount}")]
    internal static partial void DeadlineCheckCompleted(this ILogger logger, int approachingCount, int overdueCount);

    /// <summary>Deadline monitor service started.</summary>
    [LoggerMessage(
        EventId = 8744,
        Level = LogLevel.Information,
        Message = "Breach deadline monitor service started. Interval={Interval}")]
    internal static partial void DeadlineMonitorStarted(this ILogger logger, TimeSpan interval);

    /// <summary>Deadline monitor service disabled via configuration.</summary>
    [LoggerMessage(
        EventId = 8745,
        Level = LogLevel.Information,
        Message = "Breach deadline monitor service disabled via configuration")]
    internal static partial void DeadlineMonitorDisabled(this ILogger logger);

    /// <summary>Deadline monitor cycle failed with an error.</summary>
    [LoggerMessage(
        EventId = 8746,
        Level = LogLevel.Error,
        Message = "Breach deadline monitor cycle failed")]
    internal static partial void DeadlineMonitorCycleFailed(this ILogger logger, Exception exception);

    /// <summary>Deadline monitor cycle cancelled due to service shutdown.</summary>
    [LoggerMessage(
        EventId = 8747,
        Level = LogLevel.Information,
        Message = "Breach deadline monitor cycle cancelled due to service shutdown")]
    internal static partial void DeadlineMonitorCycleCancelled(this ILogger logger);

    // ========================================================================
    // Health check log messages (8750-8759)
    // ========================================================================

    /// <summary>Breach notification health check completed.</summary>
    [LoggerMessage(
        EventId = 8750,
        Level = LogLevel.Debug,
        Message = "Breach notification health check completed. Status={Status}, ServicesVerified={ServicesVerified}")]
    internal static partial void HealthCheckCompleted(this ILogger logger, string status, int servicesVerified);

    /// <summary>Breach notification health check degraded — infrastructure warnings detected.</summary>
    [LoggerMessage(
        EventId = 8751,
        Level = LogLevel.Warning,
        Message = "Breach notification health check degraded. WarningCount={WarningCount}, Details={Details}")]
    internal static partial void HealthCheckDegraded(this ILogger logger, int warningCount, string details);

    // ========================================================================
    // Event stream / audit log messages (8760-8769)
    // ========================================================================

    /// <summary>Aggregate loaded from event stream.</summary>
    [LoggerMessage(
        EventId = 8760,
        Level = LogLevel.Debug,
        Message = "Breach aggregate loaded from event stream. BreachId={BreachId}, Version={Version}")]
    internal static partial void AggregateLoaded(this ILogger logger, string breachId, int version);

    /// <summary>Aggregate saved to event stream with new events.</summary>
    [LoggerMessage(
        EventId = 8761,
        Level = LogLevel.Debug,
        Message = "Breach aggregate saved to event stream. BreachId={BreachId}, NewEventCount={NewEventCount}")]
    internal static partial void AggregateSaved(this ILogger logger, string breachId, int newEventCount);

    /// <summary>Aggregate not found in event stream (no events for the given ID).</summary>
    [LoggerMessage(
        EventId = 8762,
        Level = LogLevel.Debug,
        Message = "Breach aggregate not found. BreachId={BreachId}")]
    internal static partial void AggregateNotFound(this ILogger logger, string breachId);

    /// <summary>Event stream audit trail queried for a specific breach.</summary>
    [LoggerMessage(
        EventId = 8763,
        Level = LogLevel.Debug,
        Message = "Breach event stream queried. BreachId={BreachId}, EventCount={EventCount}")]
    internal static partial void EventStreamQueried(this ILogger logger, string breachId, int eventCount);

    // ========================================================================
    // Cache operations log messages (8770-8779)
    // ========================================================================

    /// <summary>Breach read model cache hit during a query operation.</summary>
    [LoggerMessage(
        EventId = 8770,
        Level = LogLevel.Debug,
        Message = "Breach cache hit. CacheKey={CacheKey}")]
    internal static partial void BreachCacheHit(this ILogger logger, string cacheKey);

    /// <summary>Breach read model cache miss during a query operation.</summary>
    [LoggerMessage(
        EventId = 8771,
        Level = LogLevel.Debug,
        Message = "Breach cache miss. CacheKey={CacheKey}")]
    internal static partial void BreachCacheMiss(this ILogger logger, string cacheKey);

    /// <summary>Breach read model cache entry invalidated.</summary>
    [LoggerMessage(
        EventId = 8772,
        Level = LogLevel.Debug,
        Message = "Breach cache invalidated. CacheKey={CacheKey}")]
    internal static partial void BreachCacheInvalidated(this ILogger logger, string cacheKey);

    /// <summary>Breach read model cache set (populated after miss).</summary>
    [LoggerMessage(
        EventId = 8773,
        Level = LogLevel.Debug,
        Message = "Breach cache set. CacheKey={CacheKey}")]
    internal static partial void BreachCacheSet(this ILogger logger, string cacheKey);

    // ========================================================================
    // Service operations log messages (8780-8799)
    // ========================================================================

    /// <summary>Breach service operation failed with an error.</summary>
    [LoggerMessage(
        EventId = 8780,
        Level = LogLevel.Error,
        Message = "Breach service operation failed. Operation={Operation}")]
    internal static partial void BreachServiceError(this ILogger logger, string operation, Exception exception);

    /// <summary>Invalid state transition attempted on a breach aggregate.</summary>
    [LoggerMessage(
        EventId = 8781,
        Level = LogLevel.Warning,
        Message = "Invalid breach state transition. BreachId={BreachId}, Operation={Operation}")]
    internal static partial void BreachInvalidStateTransition(this ILogger logger, string breachId, string operation, Exception exception);

    /// <summary>Breach successfully assessed via service.</summary>
    [LoggerMessage(
        EventId = 8783,
        Level = LogLevel.Information,
        Message = "Breach assessed via service. BreachId={BreachId}, UpdatedSeverity={UpdatedSeverity}")]
    internal static partial void BreachAssessedService(this ILogger logger, string breachId, string updatedSeverity);

    /// <summary>Breach reported to DPA via service.</summary>
    [LoggerMessage(
        EventId = 8784,
        Level = LogLevel.Information,
        Message = "Breach reported to DPA via service. BreachId={BreachId}, Authority={Authority}")]
    internal static partial void BreachReportedToDPAService(this ILogger logger, string breachId, string authority);

    /// <summary>Breach subjects notified via service.</summary>
    [LoggerMessage(
        EventId = 8785,
        Level = LogLevel.Information,
        Message = "Breach subjects notified via service. BreachId={BreachId}, SubjectCount={SubjectCount}")]
    internal static partial void BreachSubjectsNotifiedService(this ILogger logger, string breachId, int subjectCount);

    /// <summary>Breach phased report added via service.</summary>
    [LoggerMessage(
        EventId = 8786,
        Level = LogLevel.Information,
        Message = "Breach phased report added via service. BreachId={BreachId}")]
    internal static partial void BreachPhasedReportAddedService(this ILogger logger, string breachId);

    /// <summary>Breach contained via service.</summary>
    [LoggerMessage(
        EventId = 8787,
        Level = LogLevel.Information,
        Message = "Breach contained via service. BreachId={BreachId}")]
    internal static partial void BreachContainedService(this ILogger logger, string breachId);

    /// <summary>Breach closed via service.</summary>
    [LoggerMessage(
        EventId = 8788,
        Level = LogLevel.Information,
        Message = "Breach closed via service. BreachId={BreachId}")]
    internal static partial void BreachClosedService(this ILogger logger, string breachId);

    /// <summary>Breach resolved via service.</summary>
    [LoggerMessage(
        EventId = 8789,
        Level = LogLevel.Information,
        Message = "Breach resolved via service. BreachId={BreachId}")]
    internal static partial void BreachResolvedService(this ILogger logger, string breachId);

    /// <summary>Breach service operation started.</summary>
    [LoggerMessage(
        EventId = 8790,
        Level = LogLevel.Debug,
        Message = "Breach service operation started. Operation={Operation}, BreachId={BreachId}")]
    internal static partial void BreachServiceOperationStarted(this ILogger logger, string operation, string? breachId);

    /// <summary>Breach service operation completed successfully.</summary>
    [LoggerMessage(
        EventId = 8791,
        Level = LogLevel.Debug,
        Message = "Breach service operation completed. Operation={Operation}, BreachId={BreachId}")]
    internal static partial void BreachServiceOperationCompleted(this ILogger logger, string operation, string? breachId);
}
