using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.Anonymization.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina Anonymization observability.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="ActivitySource"/> (<c>Encina.Compliance.Anonymization</c>)
/// for fine-grained trace filtering, and a dedicated <see cref="Meter"/> for metric aggregation.
/// </para>
/// <para>
/// All counters use tag-based dimensions (<c>anon.technique</c>, <c>anon.outcome</c>, <c>anon.field_name</c>)
/// to enable flexible dashboards without creating separate counters per outcome.
/// </para>
/// </remarks>
internal static class AnonymizationDiagnostics
{
    internal const string SourceName = "Encina.Compliance.Anonymization";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ---- Tag constants ----

    internal const string TagOutcome = "anon.outcome";
    internal const string TagTechnique = "anon.technique";
    internal const string TagFieldName = "anon.field_name";
    internal const string TagRequestType = "anon.request_type";
    internal const string TagResponseType = "anon.response_type";
    internal const string TagEnforcementMode = "anon.enforcement_mode";
    internal const string TagFailureReason = "anon.failure_reason";
    internal const string TagTransformationType = "anon.transformation_type";
    internal const string TagAlgorithm = "anon.algorithm";
    internal const string TagKeyId = "anon.key_id";
    internal const string TagTokenFormat = "anon.token_format";

    // ---- Counters ----

    /// <summary>
    /// Total anonymization pipeline executions, tagged with <c>anon.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> PipelineExecutionsTotal =
        Meter.CreateCounter<long>("anon.pipeline.executions.total",
            description: "Total number of anonymization pipeline executions.");

    /// <summary>
    /// Total field transformations, tagged with <c>anon.technique</c> and <c>anon.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> FieldTransformationsTotal =
        Meter.CreateCounter<long>("anon.field.transformations.total",
            description: "Total number of field-level anonymization transformations.");

    /// <summary>
    /// Total pseudonymization operations, tagged with <c>anon.algorithm</c> and <c>anon.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> PseudonymizationOperationsTotal =
        Meter.CreateCounter<long>("anon.pseudonymization.operations.total",
            description: "Total number of pseudonymization operations.");

    /// <summary>
    /// Total tokenization operations, tagged with <c>anon.token_format</c> and <c>anon.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> TokenizationOperationsTotal =
        Meter.CreateCounter<long>("anon.tokenization.operations.total",
            description: "Total number of tokenization operations.");

    /// <summary>
    /// Total key rotation operations, tagged with <c>anon.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> KeyRotationsTotal =
        Meter.CreateCounter<long>("anon.key.rotations.total",
            description: "Total number of cryptographic key rotation operations.");

    /// <summary>
    /// Total risk assessments, tagged with <c>anon.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> RiskAssessmentsTotal =
        Meter.CreateCounter<long>("anon.risk.assessments.total",
            description: "Total number of re-identification risk assessments.");

    // ---- Histograms ----

    /// <summary>
    /// Duration of anonymization pipeline execution in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> PipelineDuration =
        Meter.CreateHistogram<double>("anon.pipeline.duration",
            unit: "ms",
            description: "Duration of anonymization pipeline execution in milliseconds.");

    /// <summary>
    /// Duration of anonymization technique application in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> AnonymizationDuration =
        Meter.CreateHistogram<double>("anon.anonymization.duration",
            unit: "ms",
            description: "Duration of anonymization technique application in milliseconds.");

    /// <summary>
    /// Duration of pseudonymization operations in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> PseudonymizationDuration =
        Meter.CreateHistogram<double>("anon.pseudonymization.duration",
            unit: "ms",
            description: "Duration of pseudonymization operations in milliseconds.");

    /// <summary>
    /// Duration of tokenization operations in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> TokenizationDuration =
        Meter.CreateHistogram<double>("anon.tokenization.duration",
            unit: "ms",
            description: "Duration of tokenization operations in milliseconds.");

    /// <summary>
    /// Duration of risk assessment operations in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> RiskAssessmentDuration =
        Meter.CreateHistogram<double>("anon.risk.assessment.duration",
            unit: "ms",
            description: "Duration of re-identification risk assessment operations in milliseconds.");

    // ---- Activity helpers ----

    /// <summary>
    /// Starts a new <c>Anonymization.Pipeline</c> activity for a pipeline behavior execution.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type triggering the pipeline.</param>
    /// <param name="responseTypeName">The name of the response type being transformed.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartPipelineExecution(string requestTypeName, string responseTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Anonymization.Pipeline", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        activity?.SetTag(TagResponseType, responseTypeName);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>Anonymization.Anonymize</c> activity for an anonymization technique application.
    /// </summary>
    /// <param name="fieldName">The name of the field being anonymized.</param>
    /// <param name="technique">The anonymization technique being applied.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartAnonymization(string fieldName, string technique)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Anonymization.Anonymize", ActivityKind.Internal);
        activity?.SetTag(TagFieldName, fieldName);
        activity?.SetTag(TagTechnique, technique);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>Anonymization.Pseudonymize</c> activity for a pseudonymization operation.
    /// </summary>
    /// <param name="algorithm">The pseudonymization algorithm being used.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartPseudonymization(string algorithm)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Anonymization.Pseudonymize", ActivityKind.Internal);
        activity?.SetTag(TagAlgorithm, algorithm);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>Anonymization.Tokenize</c> activity for a tokenization operation.
    /// </summary>
    /// <param name="tokenFormat">The token format being used.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartTokenization(string tokenFormat)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Anonymization.Tokenize", ActivityKind.Internal);
        activity?.SetTag(TagTokenFormat, tokenFormat);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>Anonymization.KeyRotation</c> activity for a key rotation operation.
    /// </summary>
    /// <param name="keyId">The identifier of the key being rotated.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartKeyRotation(string keyId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Anonymization.KeyRotation", ActivityKind.Internal);
        activity?.SetTag(TagKeyId, keyId);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>Anonymization.RiskAssessment</c> activity for a risk assessment operation.
    /// </summary>
    /// <param name="datasetSize">The number of records in the dataset being assessed.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartRiskAssessment(int datasetSize)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Anonymization.RiskAssessment", ActivityKind.Internal);
        activity?.SetTag("anon.dataset_size", datasetSize);
        return activity;
    }

    // ---- Outcome recorders ----

    /// <summary>
    /// Records a successful completion on an activity.
    /// </summary>
    /// <param name="activity">The activity to complete (may be <c>null</c>).</param>
    /// <param name="fieldsTransformed">Number of fields successfully transformed.</param>
    internal static void RecordCompleted(Activity? activity, int fieldsTransformed)
    {
        activity?.SetTag(TagOutcome, "completed");
        activity?.SetTag("anon.fields_transformed", fieldsTransformed);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records a successful completion on an activity without a field count.
    /// </summary>
    /// <param name="activity">The activity to complete (may be <c>null</c>).</param>
    internal static void RecordCompleted(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "completed");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records a failure on an activity.
    /// </summary>
    /// <param name="activity">The activity to mark as failed (may be <c>null</c>).</param>
    /// <param name="reason">The failure reason.</param>
    internal static void RecordFailed(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "failed");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    /// <summary>
    /// Records a skipped outcome on an activity (no attributes found or enforcement disabled).
    /// </summary>
    /// <param name="activity">The activity to mark as skipped (may be <c>null</c>).</param>
    internal static void RecordSkipped(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "skipped");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records a blocked outcome on an activity (transformation failure in Block mode).
    /// </summary>
    /// <param name="activity">The activity to mark as blocked (may be <c>null</c>).</param>
    /// <param name="fieldName">The field that failed transformation.</param>
    internal static void RecordBlocked(Activity? activity, string fieldName)
    {
        activity?.SetTag(TagOutcome, "blocked");
        activity?.SetTag(TagFieldName, fieldName);
        activity?.SetStatus(ActivityStatusCode.Error, "Transformation failed in Block mode");
    }

    /// <summary>
    /// Records a warned outcome on an activity (transformation failure in Warn mode).
    /// </summary>
    /// <param name="activity">The activity to mark as warned (may be <c>null</c>).</param>
    /// <param name="fieldName">The field that failed transformation.</param>
    internal static void RecordWarned(Activity? activity, string fieldName)
    {
        activity?.SetTag(TagOutcome, "warned");
        activity?.SetTag(TagFieldName, fieldName);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
