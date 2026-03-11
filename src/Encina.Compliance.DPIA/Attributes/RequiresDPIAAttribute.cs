namespace Encina.Compliance.DPIA;

/// <summary>
/// Declares that a request type requires a Data Protection Impact Assessment
/// before it can be executed.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used by the <c>DPIAPipelineBehavior</c> in the Encina request pipeline.
/// When a request decorated with this attribute is dispatched, the pipeline verifies that a
/// current, approved DPIA assessment exists before the handler executes.
/// </para>
/// <para>
/// Per GDPR Article 35(1), a DPIA is required "where a type of processing [...] is likely
/// to result in a high risk to the rights and freedoms of natural persons." This attribute
/// provides declarative DPIA enforcement at the request type level.
/// </para>
/// <para>
/// The behavior depends on the configured <see cref="Model.DPIAEnforcementMode"/>:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Model.DPIAEnforcementMode.Block"/>: Blocks execution if no current assessment exists.</description></item>
/// <item><description><see cref="Model.DPIAEnforcementMode.Warn"/>: Logs a warning but allows execution to proceed.</description></item>
/// <item><description><see cref="Model.DPIAEnforcementMode.Disabled"/>: No enforcement (attribute is ignored).</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Basic usage — processing type and reason auto-detected from context
/// [RequiresDPIA]
/// public sealed record ProcessBiometricDataCommand : ICommand&lt;Unit&gt;;
///
/// // With explicit processing type and reason
/// [RequiresDPIA(
///     ProcessingType = "AutomatedDecisionMaking",
///     Reason = "Credit scoring uses automated profiling with legal effects per Article 22")]
/// public sealed record CreditScoringCommand(string CustomerId) : ICommand&lt;CreditScore&gt;;
///
/// // With review requirement disabled (assessment approved once, not periodically reviewed)
/// [RequiresDPIA(ReviewRequired = false)]
/// public sealed record AnalyticsAggregationQuery : IQuery&lt;AggregatedReport&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequiresDPIAAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the type of processing this request performs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When specified, this value is used to match the request to a <see cref="Model.DPIATemplate"/>
    /// via <see cref="IDPIATemplateProvider"/>. It is also stored in the <see cref="Model.DPIAAssessment"/>
    /// for categorization and reporting.
    /// </para>
    /// <para>
    /// Common values match those defined in <see cref="Model.HighRiskTriggers"/>
    /// (e.g., "AutomatedDecisionMaking", "LargeScaleProcessing", "SystematicProfiling").
    /// </para>
    /// <para>
    /// When <see langword="null"/>, the pipeline behavior infers the processing type
    /// from the request's context and metadata.
    /// </para>
    /// </remarks>
    public string? ProcessingType { get; set; }

    /// <summary>
    /// Gets or sets the reason or justification for requiring a DPIA on this request type.
    /// </summary>
    /// <remarks>
    /// Provides context for the DPIA requirement, useful for assessment documentation
    /// and audit trail entries. When <see langword="null"/>, a default reason based on
    /// the processing type and high-risk triggers is generated.
    /// </remarks>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether periodic review of the DPIA assessment
    /// is required for this request type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to <see langword="true"/>. Per GDPR Article 35(11), the controller must
    /// review the assessment "at least when there is a change of the risk represented
    /// by processing operations."
    /// </para>
    /// <para>
    /// When <see langword="true"/>, the pipeline behavior checks both the assessment status
    /// and its <see cref="Model.DPIAAssessment.NextReviewAtUtc"/> date. When <see langword="false"/>,
    /// only the status is checked (the assessment does not expire).
    /// </para>
    /// <para>
    /// Set to <see langword="false"/> only for processing operations where the risk profile
    /// is stable and unlikely to change (e.g., aggregated analytics with no personal data exposure).
    /// </para>
    /// </remarks>
    public bool ReviewRequired { get; set; } = true;
}
