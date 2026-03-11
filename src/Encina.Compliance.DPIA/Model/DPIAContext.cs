namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Provides contextual information for evaluating DPIA risk criteria at runtime.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DPIAContext"/> is constructed by the DPIA pipeline behavior from the
/// incoming request metadata, processing configuration, and any applicable template.
/// It serves as the input to the <c>IDPIARiskCriteriaEvaluator</c>, which determines
/// whether a DPIA is required for the operation.
/// </para>
/// <para>
/// Per GDPR Article 35(3), a DPIA is required in particular for:
/// </para>
/// <list type="bullet">
/// <item><description>(a) Systematic and extensive evaluation of personal aspects based on automated processing, including profiling.</description></item>
/// <item><description>(b) Processing on a large scale of special categories of data (Article 9) or criminal convictions (Article 10).</description></item>
/// <item><description>(c) Systematic monitoring of a publicly accessible area on a large scale.</description></item>
/// </list>
/// <para>
/// The <see cref="HighRiskTriggers"/> property carries the specific triggers that apply
/// to this processing context, matched against <see cref="Model.HighRiskTriggers"/> constants.
/// </para>
/// </remarks>
public sealed record DPIAContext
{
    /// <summary>
    /// The CLR type of the request being evaluated.
    /// </summary>
    public required Type RequestType { get; init; }

    /// <summary>
    /// The type of processing being performed (e.g., "AutomatedDecisionMaking", "LargeScaleProcessing").
    /// </summary>
    /// <remarks>
    /// Matches against <see cref="DPIATemplate.ProcessingType"/> to select the appropriate
    /// assessment template when available.
    /// </remarks>
    public string? ProcessingType { get; init; }

    /// <summary>
    /// The categories of personal data involved in the processing.
    /// </summary>
    /// <remarks>
    /// Used to determine if special categories (Article 9) or criminal conviction data
    /// (Article 10) are being processed, which triggers mandatory DPIA under Article 35(3)(b).
    /// </remarks>
    public required IReadOnlyList<string> DataCategories { get; init; }

    /// <summary>
    /// The high-risk triggers identified for this processing context.
    /// </summary>
    /// <remarks>
    /// Values should match constants defined in <see cref="Model.HighRiskTriggers"/>.
    /// The presence of multiple triggers increases the likelihood that a DPIA is required.
    /// </remarks>
    public required IReadOnlyList<string> HighRiskTriggers { get; init; }

    /// <summary>
    /// An optional template to guide the assessment for this processing type.
    /// </summary>
    public DPIATemplate? Template { get; init; }

    /// <summary>
    /// Additional metadata for extensibility, allowing custom risk criteria evaluators
    /// to receive domain-specific context.
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
