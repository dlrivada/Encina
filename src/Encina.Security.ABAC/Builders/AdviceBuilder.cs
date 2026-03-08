namespace Encina.Security.ABAC.Builders;

/// <summary>
/// Fluent builder for constructing XACML 3.0 <see cref="AdviceExpression"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.18 — Advice expressions are optional post-decision recommendations that
/// the PEP <b>may</b> choose to act on. Unlike obligations, advice can be ignored without
/// affecting the authorization decision.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var advice = new AdviceBuilder("notify-user")
///     .OnDeny()
///     .WithAttribute("message", "Contact your manager to request access.")
///     .WithAttribute("reason", ConditionBuilder.StringValue("Insufficient clearance"))
///     .Build();
/// </code>
/// </example>
public sealed class AdviceBuilder
{
    private readonly string _id;
    private FulfillOn _appliesTo = FulfillOn.Permit;
    private readonly List<AttributeAssignment> _attributeAssignments = [];

    /// <summary>
    /// Initializes a new <see cref="AdviceBuilder"/> with the specified advice identifier.
    /// </summary>
    /// <param name="id">The unique advice identifier used for handler matching.</param>
    public AdviceBuilder(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _id = id;
    }

    /// <summary>
    /// Configures this advice to apply on <see cref="Effect.Permit"/> decisions.
    /// </summary>
    public AdviceBuilder OnPermit()
    {
        _appliesTo = FulfillOn.Permit;
        return this;
    }

    /// <summary>
    /// Configures this advice to apply on <see cref="Effect.Deny"/> decisions.
    /// </summary>
    public AdviceBuilder OnDeny()
    {
        _appliesTo = FulfillOn.Deny;
        return this;
    }

    /// <summary>
    /// Adds an attribute assignment with an expression value.
    /// </summary>
    /// <param name="attributeId">The attribute identifier for this assignment.</param>
    /// <param name="value">The expression that produces the attribute value.</param>
    public AdviceBuilder WithAttribute(string attributeId, IExpression value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeId);
        ArgumentNullException.ThrowIfNull(value);

        _attributeAssignments.Add(new AttributeAssignment
        {
            AttributeId = attributeId,
            Value = value
        });
        return this;
    }

    /// <summary>
    /// Adds an attribute assignment with a literal object value (convenience overload).
    /// The value is wrapped in a string-typed <see cref="AttributeValue"/>.
    /// </summary>
    /// <param name="attributeId">The attribute identifier for this assignment.</param>
    /// <param name="value">The literal value (converted to string).</param>
    public AdviceBuilder WithAttribute(string attributeId, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeId);

        _attributeAssignments.Add(new AttributeAssignment
        {
            AttributeId = attributeId,
            Value = new AttributeValue
            {
                DataType = XACMLDataTypes.String,
                Value = value?.ToString()
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a category-scoped attribute assignment with an expression value.
    /// </summary>
    /// <param name="attributeId">The attribute identifier for this assignment.</param>
    /// <param name="category">The attribute category scope.</param>
    /// <param name="value">The expression that produces the attribute value.</param>
    public AdviceBuilder WithAttribute(
        string attributeId,
        AttributeCategory category,
        IExpression value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeId);
        ArgumentNullException.ThrowIfNull(value);

        _attributeAssignments.Add(new AttributeAssignment
        {
            AttributeId = attributeId,
            Category = category,
            Value = value
        });
        return this;
    }

    /// <summary>
    /// Builds the <see cref="AdviceExpression"/> instance from the accumulated configuration.
    /// </summary>
    /// <returns>An immutable <see cref="AdviceExpression"/> record.</returns>
    public AdviceExpression Build() => new()
    {
        Id = _id,
        AppliesTo = _appliesTo,
        AttributeAssignments = _attributeAssignments.ToList()
    };
}
