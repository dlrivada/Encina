namespace Encina.Security.ABAC.Builders;

/// <summary>
/// Fluent builder for constructing XACML 3.0 <see cref="Target"/> instances with
/// the triple-nesting structure: Target → AnyOf (OR) → AllOf (AND) → Match.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.6 — A Target determines whether a policy, policy set, or rule applies
/// to a given access request. The Target contains <see cref="AnyOf"/> elements (all must match — AND),
/// each <see cref="AnyOf"/> contains <see cref="AllOf"/> elements (any can match — OR),
/// and each <see cref="AllOf"/> contains <see cref="Match"/> elements (all must match — AND).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var target = new TargetBuilder()
///     .AnyOf(any => any
///         .AllOf(all => all
///             .MatchAttribute(AttributeCategory.Subject, "department", ConditionOperator.Equals, "Finance")))
///     .AnyOf(any => any
///         .AllOf(all => all
///             .MatchAttribute(AttributeCategory.Resource, "classification", ConditionOperator.Equals, "Confidential")))
///     .Build();
/// </code>
/// </example>
public sealed class TargetBuilder
{
    private readonly List<AnyOf> _anyOfElements = [];

    /// <summary>
    /// Adds an <see cref="AnyOf"/> element to the target (all AnyOf elements must match — logical AND).
    /// </summary>
    /// <param name="configure">A delegate that configures the <see cref="AnyOfBuilder"/>.</param>
    public TargetBuilder AnyOf(Action<AnyOfBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new AnyOfBuilder();
        configure(builder);
        _anyOfElements.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Builds the <see cref="Target"/> instance from the accumulated configuration.
    /// </summary>
    /// <returns>An immutable <see cref="Target"/> record.</returns>
    /// <remarks>
    /// An empty <see cref="Target.AnyOfElements"/> list is valid per XACML 3.0 §7.6 —
    /// it means the target matches all requests (unconditional applicability).
    /// </remarks>
    public Target Build() => new()
    {
        AnyOfElements = _anyOfElements.ToList()
    };
}

/// <summary>
/// Fluent builder for constructing XACML 3.0 <see cref="AnyOf"/> elements within a <see cref="Target"/>.
/// </summary>
/// <remarks>
/// XACML 3.0 §7.6 — An AnyOf matches if <em>any</em> of its AllOf child elements matches (logical OR).
/// </remarks>
public sealed class AnyOfBuilder
{
    private readonly List<AllOf> _allOfElements = [];

    /// <summary>
    /// Adds an <see cref="AllOf"/> element to the AnyOf (any AllOf can match — logical OR).
    /// </summary>
    /// <param name="configure">A delegate that configures the <see cref="AllOfBuilder"/>.</param>
    public AnyOfBuilder AllOf(Action<AllOfBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new AllOfBuilder();
        configure(builder);
        _allOfElements.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Builds the <see cref="AnyOf"/> instance from the accumulated configuration.
    /// </summary>
    internal ABAC.AnyOf Build() => new()
    {
        AllOfElements = _allOfElements.ToList()
    };
}

/// <summary>
/// Fluent builder for constructing XACML 3.0 <see cref="AllOf"/> elements within an <see cref="AnyOf"/>.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.6 — An AllOf matches if <em>all</em> of its Match child elements match (logical AND).
/// </para>
/// </remarks>
public sealed class AllOfBuilder
{
    private readonly List<Match> _matches = [];

    /// <summary>
    /// Adds a match element using an explicit function ID, attribute designator, and literal value.
    /// </summary>
    /// <param name="functionId">The XACML function identifier for the comparison (e.g., <c>"string-equal"</c>).</param>
    /// <param name="designator">The attribute designator that resolves the value from the request context.</param>
    /// <param name="value">The literal attribute value to compare against.</param>
    public AllOfBuilder Match(
        string functionId,
        AttributeDesignator designator,
        AttributeValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionId);
        ArgumentNullException.ThrowIfNull(designator);
        ArgumentNullException.ThrowIfNull(value);

        _matches.Add(new Match
        {
            FunctionId = functionId,
            AttributeDesignator = designator,
            AttributeValue = value
        });
        return this;
    }

    /// <summary>
    /// Adds a match element using category, attribute ID, data type, operator, and value.
    /// The XACML function ID is resolved from the operator and data type.
    /// </summary>
    /// <param name="category">The attribute category (Subject, Resource, Action, Environment).</param>
    /// <param name="attributeId">The attribute identifier within the category.</param>
    /// <param name="dataType">The XACML data type URI (e.g., <see cref="XACMLDataTypes.String"/>).</param>
    /// <param name="op">The comparison operator.</param>
    /// <param name="value">The literal value to compare against.</param>
    public AllOfBuilder Match(
        AttributeCategory category,
        string attributeId,
        string dataType,
        ConditionOperator op,
        object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataType);

        var functionId = ConditionBuilder.MapOperatorToFunctionId(op, dataType);

        _matches.Add(new Match
        {
            FunctionId = functionId,
            AttributeDesignator = new AttributeDesignator
            {
                Category = category,
                AttributeId = attributeId,
                DataType = dataType
            },
            AttributeValue = new AttributeValue
            {
                DataType = dataType,
                Value = value
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a match element with automatic data type inference from the runtime value type.
    /// </summary>
    /// <param name="category">The attribute category (Subject, Resource, Action, Environment).</param>
    /// <param name="attributeId">The attribute identifier within the category.</param>
    /// <param name="op">The comparison operator.</param>
    /// <param name="value">The literal value to compare against. The XACML data type is inferred from the runtime type.</param>
    public AllOfBuilder MatchAttribute(
        AttributeCategory category,
        string attributeId,
        ConditionOperator op,
        object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeId);

        var dataType = ConditionBuilder.InferDataType(value);
        return Match(category, attributeId, dataType, op, value);
    }

    /// <summary>
    /// Builds the <see cref="AllOf"/> instance from the accumulated configuration.
    /// </summary>
    internal ABAC.AllOf Build() => new()
    {
        Matches = _matches.ToList()
    };
}
