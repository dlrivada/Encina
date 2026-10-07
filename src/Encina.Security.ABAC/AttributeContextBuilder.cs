namespace Encina.Security.ABAC;

/// <summary>
/// Builds a <see cref="PolicyEvaluationContext"/> from raw attribute dictionaries, keeping one
/// <see cref="AttributeBag"/> per attribute identifier for XACML designator selection.
/// </summary>
/// <remarks>
/// <para>
/// The builder converts the key-value attribute dictionaries returned by
/// <see cref="IAttributeProvider"/> into the per-attribute <see cref="AttributeBag"/>
/// dictionaries expected by the <see cref="IPolicyDecisionPoint"/>. Each dictionary key is the
/// attribute identifier an <see cref="AttributeDesignator.AttributeId"/> names (#1983).
/// </para>
/// <para>
/// Each attribute value is wrapped in an <see cref="AttributeValue"/> with an automatically
/// inferred data type and stored as a single-value bag under its key. A collection value is
/// stored as one value; it is not expanded into a multi-valued bag.
/// </para>
/// <para>
/// The action category holds one attribute, <c>"name"</c>, with the request type name: the
/// same value the EEL variable <c>action.name</c> of <see cref="RequireConditionAttribute"/> reads.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = AttributeContextBuilder.Build(
///     subjectAttributes: new Dictionary&lt;string, object&gt; { ["department"] = "Finance" },
///     resourceAttributes: new Dictionary&lt;string, object&gt; { ["classification"] = "confidential" },
///     environmentAttributes: new Dictionary&lt;string, object&gt; { ["currentTime"] = timeProvider.GetUtcNow() },
///     requestType: typeof(GetReportQuery),
///     includeAdvice: true);
///
/// // context.SubjectAttributes["department"] is a bag with the single string value "Finance".
/// </code>
/// </example>
public static class AttributeContextBuilder
{
    /// <summary>
    /// The attribute identifier of the action category that holds the request type name; the EEL
    /// variable <c>action</c> of <see cref="Enforcement.ABACRequirementEvaluator"/> uses the same id.
    /// </summary>
    internal const string ActionNameAttributeId = "name";

    /// <summary>
    /// Builds a <see cref="PolicyEvaluationContext"/> from attribute dictionaries.
    /// </summary>
    /// <param name="subjectAttributes">Subject (user) attributes from <see cref="IAttributeProvider"/>.</param>
    /// <param name="resourceAttributes">Resource attributes from <see cref="IAttributeProvider"/>.</param>
    /// <param name="environmentAttributes">Environment attributes from <see cref="IAttributeProvider"/>.</param>
    /// <param name="requestType">The type of the request being evaluated.</param>
    /// <param name="includeAdvice">Whether to include advice expressions in evaluation results.</param>
    /// <returns>A fully populated <see cref="PolicyEvaluationContext"/>.</returns>
    public static PolicyEvaluationContext Build(
        IReadOnlyDictionary<string, object> subjectAttributes,
        IReadOnlyDictionary<string, object> resourceAttributes,
        IReadOnlyDictionary<string, object> environmentAttributes,
        Type requestType,
        bool includeAdvice = true)
    {
        ArgumentNullException.ThrowIfNull(subjectAttributes);
        ArgumentNullException.ThrowIfNull(resourceAttributes);
        ArgumentNullException.ThrowIfNull(environmentAttributes);
        ArgumentNullException.ThrowIfNull(requestType);

        return new PolicyEvaluationContext
        {
            SubjectAttributes = ToAttributeBags(subjectAttributes),
            ResourceAttributes = ToAttributeBags(resourceAttributes),
            EnvironmentAttributes = ToAttributeBags(environmentAttributes),
            ActionAttributes = CreateActionAttributes(requestType),
            RequestType = requestType,
            IncludeAdvice = includeAdvice
        };
    }

    /// <summary>
    /// Converts an attribute dictionary to one <see cref="AttributeBag"/> per attribute identifier.
    /// </summary>
    /// <param name="attributes">The attribute key-value pairs to convert; each key is an attribute identifier.</param>
    /// <returns>
    /// A read-only dictionary with the same keys (compared ordinally), each mapped to a
    /// single-value <see cref="AttributeBag"/> with the value and its inferred data type.
    /// </returns>
    public static IReadOnlyDictionary<string, AttributeBag> ToAttributeBags(IReadOnlyDictionary<string, object> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        var bags = new Dictionary<string, AttributeBag>(attributes.Count, StringComparer.Ordinal);

        foreach (var (attributeId, value) in attributes)
        {
            bags[attributeId] = AttributeBag.Of(new AttributeValue
            {
                DataType = InferDataType(value),
                Value = value
            });
        }

        return bags.AsReadOnly();
    }

    // ── Private Helpers ─────────────────────────────────────────────

    private static System.Collections.ObjectModel.ReadOnlyDictionary<string, AttributeBag> CreateActionAttributes(Type requestType) =>
        new Dictionary<string, AttributeBag>(StringComparer.Ordinal)
        {
            [ActionNameAttributeId] = AttributeBag.Of(new AttributeValue
            {
                DataType = XACMLDataTypes.String,
                Value = requestType.Name
            })
        }.AsReadOnly();

    // crap-exempt: single-question switch — maps a CLR value to its XACML data type identifier.
    private static string InferDataType(object? value) => value switch
    {
        string => XACMLDataTypes.String,
        sbyte or byte or short or ushort or int or uint or long or ulong => XACMLDataTypes.Integer,
        bool => XACMLDataTypes.Boolean,
        double or float or decimal => XACMLDataTypes.Double,
        DateTime or DateTimeOffset => XACMLDataTypes.DateTime,
        DateOnly => XACMLDataTypes.Date,
        TimeSpan => XACMLDataTypes.Time,
        Uri => XACMLDataTypes.AnyURI,
        _ => XACMLDataTypes.String
    };
}
