using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.ProcessorAgreements.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina Processor Agreements compliance observability.
/// </summary>
/// <remarks>
/// <para>
/// Follows the established Encina compliance observability pattern with:
/// <list type="bullet">
/// <item><description><see cref="ActivitySource"/> for OpenTelemetry distributed tracing.</description></item>
/// <item><description><see cref="Meter"/> for OpenTelemetry metrics (counters, histograms).</description></item>
/// </list>
/// </para>
/// <para>
/// Metric names follow the <c>processor_agreement.*</c> prefix convention.
/// Tag names follow the <c>processor_agreement.*</c> prefix convention.
/// </para>
/// </remarks>
internal static class ProcessorAgreementDiagnostics
{
    internal const string SourceName = "Encina.Compliance.ProcessorAgreements";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ========================================================================
    // Pipeline counters
    // ========================================================================

    /// <summary>Total number of processor agreement pipeline checks executed.</summary>
    internal static readonly Counter<long> PipelineCheckTotal =
        Meter.CreateCounter<long>("processor_agreement.pipeline.checks.total",
            description: "Total number of processor agreement pipeline compliance checks.");

    /// <summary>Number of pipeline checks that passed (valid DPA exists).</summary>
    internal static readonly Counter<long> PipelineCheckPassed =
        Meter.CreateCounter<long>("processor_agreement.pipeline.checks.passed",
            description: "Number of processor agreement pipeline checks that passed.");

    /// <summary>Number of pipeline checks that failed (no/expired/incomplete DPA).</summary>
    internal static readonly Counter<long> PipelineCheckFailed =
        Meter.CreateCounter<long>("processor_agreement.pipeline.checks.failed",
            description: "Number of processor agreement pipeline checks that failed.");

    /// <summary>Number of requests skipped (no [RequiresProcessor] attribute).</summary>
    internal static readonly Counter<long> PipelineCheckSkipped =
        Meter.CreateCounter<long>("processor_agreement.pipeline.checks.skipped",
            description: "Number of requests skipped (no [RequiresProcessor] attribute).");

    // ========================================================================
    // Store operation counters (DC 2: separate registry vs DPA)
    // ========================================================================

    /// <summary>Total number of processor registry operations (register, update, remove, query).</summary>
    internal static readonly Counter<long> RegistryOperationTotal =
        Meter.CreateCounter<long>("processor_agreement.registry.operations.total",
            description: "Total number of processor registry operations.");

    /// <summary>Total number of DPA store operations (add, update, query).</summary>
    internal static readonly Counter<long> DPAOperationTotal =
        Meter.CreateCounter<long>("processor_agreement.dpa.operations.total",
            description: "Total number of DPA store operations.");

    /// <summary>Total number of audit trail entries recorded.</summary>
    internal static readonly Counter<long> AuditEntryTotal =
        Meter.CreateCounter<long>("processor_agreement.audit.entries.total",
            description: "Total number of audit trail entries recorded.");

    /// <summary>Total number of DPA expiration check cycles.</summary>
    internal static readonly Counter<long> ExpirationCheckTotal =
        Meter.CreateCounter<long>("processor_agreement.expiration_check.total",
            description: "Total number of DPA expiration check cycles.");

    /// <summary>Number of sub-processor registrations that exceeded the maximum depth (DC 5).</summary>
    internal static readonly Counter<long> SubProcessorDepthExceededTotal =
        Meter.CreateCounter<long>("processor_agreement.sub_processor.depth_exceeded.total",
            description: "Number of sub-processor registrations that exceeded the configured maximum depth.");

    // ========================================================================
    // Histograms
    // ========================================================================

    /// <summary>Duration of processor agreement pipeline check in milliseconds.</summary>
    internal static readonly Histogram<double> PipelineCheckDuration =
        Meter.CreateHistogram<double>("processor_agreement.pipeline.check.duration",
            unit: "ms",
            description: "Duration of processor agreement pipeline compliance check in milliseconds.");

    /// <summary>Duration of processor registry operations in milliseconds.</summary>
    internal static readonly Histogram<double> RegistryOperationDuration =
        Meter.CreateHistogram<double>("processor_agreement.registry.operation.duration",
            unit: "ms",
            description: "Duration of processor registry operations in milliseconds.");

    /// <summary>Duration of DPA store operations in milliseconds.</summary>
    internal static readonly Histogram<double> DPAOperationDuration =
        Meter.CreateHistogram<double>("processor_agreement.dpa.operation.duration",
            unit: "ms",
            description: "Duration of DPA store operations in milliseconds.");

    /// <summary>Duration of DPA expiration check cycles in milliseconds.</summary>
    internal static readonly Histogram<double> ExpirationCheckDuration =
        Meter.CreateHistogram<double>("processor_agreement.expiration_check.duration",
            unit: "ms",
            description: "Duration of DPA expiration check cycles in milliseconds.");

    // ========================================================================
    // Tag names
    // ========================================================================

    internal const string TagOutcome = "processor_agreement.outcome";
    internal const string TagRequestType = "processor_agreement.request_type";
    internal const string TagProcessorId = "processor_agreement.processor_id";
    internal const string TagFailureReason = "processor_agreement.failure_reason";
    internal const string TagEnforcementMode = "processor_agreement.enforcement_mode";
    internal const string TagOperation = "processor_agreement.operation";
    internal const string TagDPAId = "processor_agreement.dpa_id";
    internal const string TagDepth = "processor_agreement.depth";

    // ========================================================================
    // Pipeline activity helpers
    // ========================================================================

    /// <summary>
    /// Starts an activity for a processor agreement pipeline check.
    /// </summary>
    /// <param name="requestTypeName">The request type being checked.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartPipelineCheck(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ProcessorAgreement.PipelineCheck", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

    /// <summary>Records a passed outcome on the activity.</summary>
    internal static void RecordPassed(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "passed");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>Records a failed outcome on the activity.</summary>
    internal static void RecordFailed(Activity? activity, string failureReason)
    {
        activity?.SetTag(TagOutcome, "failed");
        activity?.SetTag(TagFailureReason, failureReason);
        activity?.SetStatus(ActivityStatusCode.Error, failureReason);
    }

    /// <summary>Records a warned outcome on the activity (Warn enforcement mode).</summary>
    internal static void RecordWarned(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "warned");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    // ========================================================================
    // Registry operation activity helpers (DC 2)
    // ========================================================================

    /// <summary>
    /// Starts an activity for a processor registry operation.
    /// </summary>
    /// <param name="operation">The operation name (e.g., "Register", "Update", "Remove").</param>
    /// <param name="processorId">The processor identifier.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartRegistryOperation(string operation, string processorId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ProcessorAgreement.Registry." + operation, ActivityKind.Internal);
        activity?.SetTag(TagOperation, operation);
        activity?.SetTag(TagProcessorId, processorId);
        return activity;
    }

    // ========================================================================
    // DPA operation activity helpers (DC 2)
    // ========================================================================

    /// <summary>
    /// Starts an activity for a DPA store operation.
    /// </summary>
    /// <param name="operation">The operation name (e.g., "Add", "UpdateStatus", "GetActive").</param>
    /// <param name="dpaId">The DPA identifier, or <c>null</c> for query operations.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartDPAOperation(string operation, string? dpaId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ProcessorAgreement.DPA." + operation, ActivityKind.Internal);
        activity?.SetTag(TagOperation, operation);
        if (dpaId is not null)
        {
            activity?.SetTag(TagDPAId, dpaId);
        }

        return activity;
    }

    // ========================================================================
    // Expiration check activity helper (DC 6)
    // ========================================================================

    /// <summary>
    /// Starts an activity for a DPA expiration check cycle (scheduled command handler).
    /// </summary>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartExpirationCheck()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity("ProcessorAgreement.ExpirationCheck", ActivityKind.Internal);
    }

    // ========================================================================
    // Audit record activity helper
    // ========================================================================

    /// <summary>
    /// Starts an activity for recording an audit trail entry.
    /// </summary>
    /// <param name="processorId">The processor identifier.</param>
    /// <param name="action">The audit action being recorded.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartAuditRecord(string processorId, string action)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ProcessorAgreement.Audit.Record", ActivityKind.Internal);
        activity?.SetTag(TagProcessorId, processorId);
        activity?.SetTag(TagOperation, action);
        return activity;
    }

    // ========================================================================
    // Service-level counters (Marten event-sourced services)
    // ========================================================================

    /// <summary>Total number of processor service operations (register, update, remove, query).</summary>
    internal static readonly Counter<long> ProcessorServiceOperationTotal =
        Meter.CreateCounter<long>("processor_agreement.processor_service.operations.total",
            description: "Total number of processor service operations.");

    /// <summary>Total number of DPA service operations (execute, amend, audit, renew, terminate, query).</summary>
    internal static readonly Counter<long> DPAServiceOperationTotal =
        Meter.CreateCounter<long>("processor_agreement.dpa_service.operations.total",
            description: "Total number of DPA service operations.");

    /// <summary>Total number of cache hits for processor/DPA lookups.</summary>
    internal static readonly Counter<long> CacheHitTotal =
        Meter.CreateCounter<long>("processor_agreement.cache.hits.total",
            description: "Total number of cache hits for processor and DPA lookups.");

    /// <summary>Total number of cache misses for processor/DPA lookups.</summary>
    internal static readonly Counter<long> CacheMissTotal =
        Meter.CreateCounter<long>("processor_agreement.cache.misses.total",
            description: "Total number of cache misses for processor and DPA lookups.");

    /// <summary>Duration of processor service operations in milliseconds.</summary>
    internal static readonly Histogram<double> ProcessorServiceOperationDuration =
        Meter.CreateHistogram<double>("processor_agreement.processor_service.operation.duration",
            unit: "ms",
            description: "Duration of processor service operations in milliseconds.");

    /// <summary>Duration of DPA service operations in milliseconds.</summary>
    internal static readonly Histogram<double> DPAServiceOperationDuration =
        Meter.CreateHistogram<double>("processor_agreement.dpa_service.operation.duration",
            unit: "ms",
            description: "Duration of DPA service operations in milliseconds.");

    // ========================================================================
    // Service-level activity helpers
    // ========================================================================

    /// <summary>
    /// Starts an activity for a processor service operation.
    /// </summary>
    /// <param name="operation">The operation name (e.g., "RegisterProcessor", "UpdateProcessor").</param>
    /// <param name="processorId">The processor identifier, or <c>null</c> for list operations.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartProcessorServiceOperation(string operation, string? processorId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ProcessorAgreement.ProcessorService." + operation, ActivityKind.Internal);
        activity?.SetTag(TagOperation, operation);
        if (processorId is not null)
        {
            activity?.SetTag(TagProcessorId, processorId);
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for a DPA service operation.
    /// </summary>
    /// <param name="operation">The operation name (e.g., "ExecuteDPA", "AmendDPA").</param>
    /// <param name="dpaId">The DPA identifier, or <c>null</c> for list/validation operations.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartDPAServiceOperation(string operation, string? dpaId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ProcessorAgreement.DPAService." + operation, ActivityKind.Internal);
        activity?.SetTag(TagOperation, operation);
        if (dpaId is not null)
        {
            activity?.SetTag(TagDPAId, dpaId);
        }

        return activity;
    }

    // ========================================================================
    // Shared activity outcome helpers
    // ========================================================================

    /// <summary>Records a completed operation outcome on the activity.</summary>
    internal static void RecordCompleted(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "completed");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>Records an error outcome on the activity.</summary>
    internal static void RecordError(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "error");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }
}
