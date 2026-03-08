namespace Encina.Security.ABAC;

/// <summary>
/// Builds a <see cref="PolicyEvaluationContext"/> from raw attribute dictionaries,
/// wrapping values in <see cref="AttributeBag"/> instances for XACML bag function compatibility.
/// </summary>
/// <remarks>
/// <para>
/// The builder converts the key-value attribute dictionaries returned by
/// <see cref="IAttributeProvider"/> into the XACML-compatible <see cref="AttributeBag"/>
/// structure expected by the <see cref="IPolicyDecisionPoint"/>.
/// </para>
/// <para>
/// Each attribute value is wrapped in an <see cref="AttributeValue"/> with an automatically
/// inferred data type. Collections are expanded into multi-valued bags per XACML 3.0 §7.3.2.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = AttributeContextBuilder.Build(
///     subjectAttributes: new Dictionary&lt;string, object&gt; { ["department"] = "Finance" },
///     resourceAttributes: new Dictionary&lt;string, object&gt; { ["classification"] = "confidential" },
///     environmentAttributes: new Dictionary&lt;string, object&gt; { ["currentTime"] = DateTime.UtcNow },
///     requestType: typeof(GetReportQuery),
///     includeAdvice: true);
/// </code>
/// </example>
public static class AttributeContextBuilder
{
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
            SubjectAttributes = ToBag(subjectAttributes),
            ResourceAttributes = ToBag(resourceAttributes),
            EnvironmentAttributes = ToBag(environmentAttributes),
            ActionAttributes = CreateActionBag(requestType),
            RequestType = requestType,
            IncludeAdvice = includeAdvice
        };
    }

    /// <summary>
    /// Converts an attribute dictionary to an <see cref="AttributeBag"/>.
    /// </summary>
    /// <param name="attributes">The attribute key-value pairs to convert.</param>
    /// <returns>An <see cref="AttributeBag"/> containing the converted values.</returns>
    /// <remarks>
    /// Each entry produces one <see cref="AttributeValue"/> with an inferred data type.
    /// If the dictionary is empty, <see cref="AttributeBag.Empty"/> is returned.
    /// </remarks>
    public static AttributeBag ToBag(IReadOnlyDictionary<string, object> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        if (attributes.Count == 0)
        {
            return AttributeBag.Empty;
        }

        var values = new List<AttributeValue>(attributes.Count);

        foreach (var kvp in attributes)
        {
            values.Add(new AttributeValue
            {
                DataType = InferDataType(kvp.Value),
                Value = kvp.Value
            });
        }

        return AttributeBag.FromValues(values);
    }

    // ── Private Helpers ─────────────────────────────────────────────

    private static AttributeBag CreateActionBag(Type requestType)
    {
        return AttributeBag.Of(new AttributeValue
        {
            DataType = XACMLDataTypes.String,
            Value = requestType.Name
        });
    }

    private static string InferDataType(object? value) => value switch
    {
        string => XACMLDataTypes.String,
        int or long => XACMLDataTypes.Integer,
        bool => XACMLDataTypes.Boolean,
        double or float or decimal => XACMLDataTypes.Double,
        DateTime or DateTimeOffset => XACMLDataTypes.DateTime,
        Uri => XACMLDataTypes.AnyURI,
        _ => XACMLDataTypes.String
    };
}
