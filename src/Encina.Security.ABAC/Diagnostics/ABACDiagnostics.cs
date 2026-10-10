using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Security.ABAC.DecisionAudit;

namespace Encina.Security.ABAC.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina ABAC observability.
/// </summary>
internal static class ABACDiagnostics
{
    internal const string SourceName = "Encina.Security.ABAC";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ── Counters ────────────────────────────────────────────────────

    internal static readonly Counter<long> EvaluationTotal =
        Meter.CreateCounter<long>("abac.evaluation.total",
            description: "Total number of ABAC policy evaluations.");

    internal static readonly Counter<long> EvaluationPermitted =
        Meter.CreateCounter<long>("abac.evaluation.permitted",
            description: "Number of ABAC evaluations that resulted in Permit.");

    internal static readonly Counter<long> EvaluationDenied =
        Meter.CreateCounter<long>("abac.evaluation.denied",
            description: "Number of ABAC evaluations that resulted in Deny.");

    internal static readonly Counter<long> ObligationExecuted =
        Meter.CreateCounter<long>("abac.obligation.executed",
            description: "Total number of obligations executed.");

    internal static readonly Counter<long> EvaluationIndeterminate =
        Meter.CreateCounter<long>("abac.evaluation.indeterminate",
            description: "Number of ABAC evaluations that resulted in Indeterminate.");

    internal static readonly Counter<long> ObligationFailed =
        Meter.CreateCounter<long>("abac.obligation.failed",
            description: "Number of obligation executions that failed.");

    internal static readonly Counter<long> ObligationNoHandler =
        Meter.CreateCounter<long>("abac.obligation.no_handler",
            description: "Number of obligations with no registered handler.");

    internal static readonly Counter<long> AdviceExecuted =
        Meter.CreateCounter<long>("abac.advice.executed",
            description: "Total number of advice expressions executed.");

    // ── Histograms ──────────────────────────────────────────────────

    internal static readonly Histogram<double> EvaluationDuration =
        Meter.CreateHistogram<double>("abac.evaluation.duration",
            unit: "ms",
            description: "Duration of ABAC policy evaluations in milliseconds.");

    internal static readonly Histogram<double> ObligationDuration =
        Meter.CreateHistogram<double>("abac.obligation.duration",
            unit: "ms",
            description: "Duration of individual obligation executions in milliseconds.");

    // ── PAP Counters ─────────────────────────────────────────────────

    internal static readonly Counter<long> PapLoadTotal =
        Meter.CreateCounter<long>("abac.pap.load.total",
            description: "Total number of PAP load (read) operations.");

    internal static readonly Counter<long> PapSaveTotal =
        Meter.CreateCounter<long>("abac.pap.save.total",
            description: "Total number of PAP save (write) operations.");

    internal static readonly Counter<long> PapDeleteTotal =
        Meter.CreateCounter<long>("abac.pap.delete.total",
            description: "Total number of PAP delete operations.");

    internal static readonly Counter<long> PapOperationFailed =
        Meter.CreateCounter<long>("abac.pap.operation.failed",
            description: "Total number of PAP operations that failed.");

    internal static readonly Counter<long> PapSerializeTotal =
        Meter.CreateCounter<long>("abac.pap.serialize.total",
            description: "Total number of policy serialization/deserialization operations.");

    // ── PAP Histograms ───────────────────────────────────────────────

    internal static readonly Histogram<double> PapOperationDuration =
        Meter.CreateHistogram<double>("abac.pap.operation.duration",
            unit: "ms",
            description: "Duration of PAP store operations in milliseconds.");

    internal static readonly Histogram<int> PapJsonSize =
        Meter.CreateHistogram<int>("abac.pap.json.size",
            unit: "By",
            description: "Size of serialized policy JSON in bytes.");

    // ── XACML XML Serialization Counters ────────────────────────────

    internal static readonly Counter<long> XacmlXmlSerializeTotal =
        Meter.CreateCounter<long>("abac.pap.serialize.xacml_xml.total",
            description: "Total number of XACML XML serialization operations.");

    internal static readonly Counter<long> XacmlXmlDeserializeTotal =
        Meter.CreateCounter<long>("abac.pap.deserialize.xacml_xml.total",
            description: "Total number of XACML XML deserialization operations.");

    internal static readonly Counter<long> XacmlXmlErrorTotal =
        Meter.CreateCounter<long>("abac.pap.xacml_xml.errors",
            description: "Total number of XACML XML serialization/deserialization errors.");

    // ── XACML XML Histograms ─────────────────────────────────────────

    internal static readonly Histogram<long> XacmlXmlSizeBytes =
        Meter.CreateHistogram<long>("abac.pap.xacml_xml.size_bytes",
            unit: "By",
            description: "Size of serialized XACML XML documents in bytes.");

    // ── Decision Audit Instruments (#751) ───────────────────────────
    // Tags carry enumeration names, error codes and exception type names only: never a subject,
    // resource, tenant or attribute value and never an error or exception message (SPEC-002 REQ-062).

    internal static readonly Counter<long> DecisionAuditRecorded =
        Meter.CreateCounter<long>("abac.decision_audit.recorded",
            description: "Number of ABAC decision records written to the audit trail.");

    internal static readonly Counter<long> DecisionAuditFailed =
        Meter.CreateCounter<long>("abac.decision_audit.failed",
            description: "Number of ABAC decision records that could not be written to the audit trail.");

    internal static readonly Histogram<double> DecisionAuditDuration =
        Meter.CreateHistogram<double>("abac.decision_audit.duration",
            unit: "ms",
            description: "Duration of building and writing one ABAC decision record in milliseconds.");

    // ── Tag Names ───────────────────────────────────────────────────

    internal const string TagRequestType = "abac.request_type";
    internal const string TagEffect = "abac.effect";
    internal const string TagPolicyId = "abac.policy_id";
    internal const string TagEnforcementMode = "abac.enforcement_mode";
    internal const string TagObligationId = "abac.obligation_id";
    internal const string TagAdviceId = "abac.advice_id";

    // Decision audit tags
    internal const string TagDecisionId = "abac.decision_id";
    internal const string TagOutcome = "abac.outcome";
    internal const string TagFailureMode = "abac.failure_mode";
    internal const string TagErrorType = "error.type";

    // PAP-specific tags
    internal const string TagOperation = "abac.operation";
    internal const string TagEntityType = "abac.entity_type";
    internal const string TagStatus = "abac.status";
    internal const string TagJsonSize = "abac.json_size";

    // ── Activity Helpers ────────────────────────────────────────────

    internal static Activity? StartEvaluation(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ABAC.Evaluate", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

    internal static void RecordPermitted(Activity? activity, string? policyId)
    {
        activity?.SetTag(TagEffect, "permit");
        activity?.SetTag(TagPolicyId, policyId);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    internal static void RecordDenied(Activity? activity, string? policyId, string reason)
    {
        activity?.SetTag(TagEffect, "deny");
        activity?.SetTag(TagPolicyId, policyId);
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    internal static void RecordIndeterminate(Activity? activity, string reason)
    {
        activity?.SetTag(TagEffect, "indeterminate");
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    // ── Decision Audit Activity Helpers ─────────────────────────────

    // Started while ABAC.Evaluate is the current activity, so the span is its child.
    internal static Activity? StartDecisionAuditRecord()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity("ABAC.DecisionAudit.Record", ActivityKind.Internal);
    }

    internal static void SetDecisionId(Activity? evaluation, Guid decisionId) =>
        evaluation?.SetTag(TagDecisionId, decisionId.ToString("D"));

    // failure is null when the record was written, otherwise an error code or an exception type name.
    internal static void RecordDecisionAudit(
        Activity? span,
        long startTimestamp,
        string outcome,
        ABACEnforcementMode enforcementMode,
        ABACDecisionAuditFailureMode failureMode,
        string? failure)
    {
        DecisionAuditDuration.Record(Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);

        if (failure is null)
        {
            DecisionAuditRecorded.Add(1,
                new KeyValuePair<string, object?>(TagOutcome, outcome),
                new KeyValuePair<string, object?>(TagEnforcementMode, enforcementMode.ToString()));
            span?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        var failureModeName = failureMode.ToString();
        DecisionAuditFailed.Add(1,
            new KeyValuePair<string, object?>(TagFailureMode, failureModeName),
            new KeyValuePair<string, object?>(TagErrorType, failure));
        span?.SetTag(TagFailureMode, failureModeName);
        span?.SetTag(TagErrorType, failure);
        span?.SetStatus(ActivityStatusCode.Error, failure);
    }

    // ── PAP Activity Helpers ────────────────────────────────────────

    internal static Activity? StartPapLoad(string entityType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ABAC.PAP.Load", ActivityKind.Internal);
        activity?.SetTag(TagOperation, "load");
        activity?.SetTag(TagEntityType, entityType);
        return activity;
    }

    internal static Activity? StartPapSave(string entityType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ABAC.PAP.Save", ActivityKind.Internal);
        activity?.SetTag(TagOperation, "save");
        activity?.SetTag(TagEntityType, entityType);
        return activity;
    }

    internal static Activity? StartPapDelete(string entityType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ABAC.PAP.Delete", ActivityKind.Internal);
        activity?.SetTag(TagOperation, "delete");
        activity?.SetTag(TagEntityType, entityType);
        return activity;
    }

    internal static Activity? StartPapSerialize(string entityType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ABAC.PAP.Serialize", ActivityKind.Internal);
        activity?.SetTag(TagOperation, "serialize");
        activity?.SetTag(TagEntityType, entityType);
        return activity;
    }

    internal static void RecordPapSuccess(Activity? activity)
    {
        activity?.SetTag(TagStatus, "success");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    internal static void RecordPapFailure(Activity? activity, string reason)
    {
        activity?.SetTag(TagStatus, "failure");
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    // ── XACML XML Activity Helpers ──────────────────────────────────

    internal static Activity? StartXacmlXmlSerialize(string entityType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ABAC.PAP.XacmlXml.Serialize", ActivityKind.Internal);
        activity?.SetTag(TagOperation, "serialize");
        activity?.SetTag(TagEntityType, entityType);
        return activity;
    }

    internal static Activity? StartXacmlXmlDeserialize(string entityType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ABAC.PAP.XacmlXml.Deserialize", ActivityKind.Internal);
        activity?.SetTag(TagOperation, "deserialize");
        activity?.SetTag(TagEntityType, entityType);
        return activity;
    }
}
