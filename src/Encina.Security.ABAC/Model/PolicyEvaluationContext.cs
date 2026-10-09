namespace Encina.Security.ABAC;

/// <summary>
/// Represents the attribute context used during XACML policy evaluation, containing
/// all resolved attributes organized by XACML 3.0 attribute category and attribute identifier.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.2 — The evaluation context carries all attribute bags needed for
/// policy evaluation. Attributes are organized by the four standard categories:
/// <see cref="SubjectAttributes"/>, <see cref="ResourceAttributes"/>,
/// <see cref="EnvironmentAttributes"/>, and <see cref="ActionAttributes"/>.
/// </para>
/// <para>
/// Each category maps an attribute identifier (for example <c>"department"</c>) to the
/// <see cref="AttributeBag"/> of that attribute, so every attribute keeps its own multi-valued
/// bag as required by the XACML specification. An <see cref="AttributeDesignator"/> selects the
/// bag stored under its <see cref="AttributeDesignator.Category"/> and
/// <see cref="AttributeDesignator.AttributeId"/>, keeping only the values whose
/// <see cref="AttributeValue.DataType"/> equals <see cref="AttributeDesignator.DataType"/>
/// (XACML 3.0 §7.3.5); a value of one attribute never satisfies a designator for another.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = new PolicyEvaluationContext
/// {
///     SubjectAttributes = new Dictionary&lt;string, AttributeBag&gt;
///     {
///         ["role"] = AttributeBag.Of(
///             new AttributeValue { DataType = XACMLDataTypes.String, Value = "admin" })
///     },
///     ResourceAttributes = new Dictionary&lt;string, AttributeBag&gt;
///     {
///         ["classification"] = AttributeBag.Of(
///             new AttributeValue { DataType = XACMLDataTypes.String, Value = "financial-report" })
///     },
///     EnvironmentAttributes = new Dictionary&lt;string, AttributeBag&gt;(),
///     ActionAttributes = new Dictionary&lt;string, AttributeBag&gt;
///     {
///         ["name"] = AttributeBag.Of(
///             new AttributeValue { DataType = XACMLDataTypes.String, Value = "read" })
///     },
///     RequestType = typeof(GetFinancialReportQuery)
/// };
/// </code>
/// </example>
public sealed record PolicyEvaluationContext
{
    /// <summary>
    /// Attributes describing the subject (user or service) making the access request,
    /// keyed by attribute identifier.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §B.2 — Includes attributes such as user ID, roles, department,
    /// clearance level, and group memberships.
    /// </remarks>
    public required IReadOnlyDictionary<string, AttributeBag> SubjectAttributes { get; init; }

    /// <summary>
    /// Attributes describing the resource being accessed, keyed by attribute identifier.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §B.3 — Includes attributes such as resource type, classification,
    /// owner, and sensitivity label.
    /// </remarks>
    public required IReadOnlyDictionary<string, AttributeBag> ResourceAttributes { get; init; }

    /// <summary>
    /// Attributes describing the current environmental conditions, keyed by attribute identifier.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §B.5 — Includes attributes such as current time, IP address,
    /// business hours, and tenant ID.
    /// </remarks>
    public required IReadOnlyDictionary<string, AttributeBag> EnvironmentAttributes { get; init; }

    /// <summary>
    /// Attributes describing the action being performed on the resource, keyed by attribute identifier.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §B.4 — Includes attributes such as action name (read, write, delete),
    /// HTTP method, and operation type. <see cref="AttributeContextBuilder"/> stores the
    /// request type name under <c>"name"</c>.
    /// </remarks>
    public required IReadOnlyDictionary<string, AttributeBag> ActionAttributes { get; init; }

    /// <summary>
    /// The type of the Encina request being evaluated (e.g., the command or query type).
    /// </summary>
    /// <remarks>
    /// Used by the PEP to correlate the ABAC evaluation with the CQRS pipeline request.
    /// </remarks>
    public required Type RequestType { get; init; }

    /// <summary>
    /// Whether to include advice expressions in the decision response.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>. Set to <c>false</c> to exclude advice from the response
    /// for performance optimization when advice is not needed.
    /// </remarks>
    public bool IncludeAdvice { get; init; } = true;

    /// <summary>
    /// Whether the decision carries the evaluation trace (<see cref="PolicyDecision.EvaluatedPolicies"/>
    /// and <see cref="PolicyDecision.RuleId"/>).
    /// </summary>
    /// <remarks>
    /// Defaults to <c>false</c>. When <c>false</c> the Policy Decision Point builds no trace and
    /// allocates nothing for it; the decision audit sets it when it records decisions.
    /// </remarks>
    public bool IncludeEvaluationTrace { get; init; }

    /// <summary>
    /// The maximum number of trace nodes (policies and policy sets) one evaluation records.
    /// </summary>
    /// <remarks>
    /// Defaults to 64. Only used when <see cref="IncludeEvaluationTrace"/> is <c>true</c>; a value
    /// below one records no node. Nodes past the limit are dropped, not summarized.
    /// </remarks>
    public int MaxTraceEntries { get; init; } = 64;
}
