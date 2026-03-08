namespace Encina.Security.ABAC.Builders;

/// <summary>
/// Fluent builder for constructing XACML 3.0 <see cref="Obligation"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.18 — Obligations are mandatory post-decision actions that the PEP
/// <b>must</b> enforce. If an obligation handler fails, the PEP must deny access.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var obligation = new ObligationBuilder("log-access")
///     .OnPermit()
///     .WithAttribute("reason", "Audit trail for financial access")
///     .WithAttribute("timestamp", ConditionBuilder.DateTimeValue(DateTime.UtcNow))
///     .Build();
/// </code>
/// </example>
public sealed class ObligationBuilder
{
    private readonly string _id;
    private FulfillOn _fulfillOn = FulfillOn.Permit;
    private readonly List<AttributeAssignment> _attributeAssignments = [];

    /// <summary>
    /// Initializes a new <see cref="ObligationBuilder"/> with the specified obligation identifier.
    /// </summary>
    /// <param name="id">The unique obligation identifier used for handler matching.</param>
    public ObligationBuilder(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _id = id;
    }

    /// <summary>
    /// Configures this obligation to trigger on <see cref="Effect.Permit"/> decisions.
    /// </summary>
    public ObligationBuilder OnPermit()
    {
        _fulfillOn = FulfillOn.Permit;
        return this;
    }

    /// <summary>
    /// Configures this obligation to trigger on <see cref="Effect.Deny"/> decisions.
    /// </summary>
    public ObligationBuilder OnDeny()
    {
        _fulfillOn = FulfillOn.Deny;
        return this;
    }

    /// <summary>
    /// Adds an attribute assignment with an expression value.
    /// </summary>
    /// <param name="attributeId">The attribute identifier for this assignment.</param>
    /// <param name="value">The expression that produces the attribute value.</param>
    public ObligationBuilder WithAttribute(string attributeId, IExpression value)
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
    public ObligationBuilder WithAttribute(string attributeId, object? value)
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
    public ObligationBuilder WithAttribute(
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
    /// Builds the <see cref="Obligation"/> instance from the accumulated configuration.
    /// </summary>
    /// <returns>An immutable <see cref="Obligation"/> record.</returns>
    public Obligation Build() => new()
    {
        Id = _id,
        FulfillOn = _fulfillOn,
        AttributeAssignments = _attributeAssignments.ToList()
    };
}
