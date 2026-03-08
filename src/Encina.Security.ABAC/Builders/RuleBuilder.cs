namespace Encina.Security.ABAC.Builders;

/// <summary>
/// Fluent builder for constructing XACML 3.0 <see cref="Rule"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.9 — A Rule is the most granular policy element. It specifies an
/// <see cref="Effect"/> (Permit or Deny) to return when the rule's target matches
/// and its condition evaluates to <c>true</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var rule = new RuleBuilder("allow-finance-read", Effect.Permit)
///     .WithDescription("Allow Finance department to read financial reports")
///     .WithTarget(t => t
///         .AnyOf(any => any
///             .AllOf(all => all
///                 .MatchAttribute(AttributeCategory.Subject, "department", ConditionOperator.Equals, "Finance"))))
///     .WithCondition(ConditionBuilder.Equal(
///         ConditionBuilder.Attribute(AttributeCategory.Action, "name", XACMLDataTypes.String),
///         ConditionBuilder.StringValue("read")))
///     .Build();
/// </code>
/// </example>
public sealed class RuleBuilder
{
    private readonly string _id;
    private readonly Effect _effect;
    private string? _description;
    private Target? _target;
    private Apply? _condition;
    private readonly List<Obligation> _obligations = [];
    private readonly List<AdviceExpression> _advice = [];

    /// <summary>
    /// Initializes a new <see cref="RuleBuilder"/> with the specified rule identifier and effect.
    /// </summary>
    /// <param name="id">The unique rule identifier within its containing policy.</param>
    /// <param name="effect">
    /// The effect to return when the rule applies. Only <see cref="Effect.Permit"/> and
    /// <see cref="Effect.Deny"/> are valid per XACML 3.0 §7.9.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="effect"/> is not <see cref="Effect.Permit"/> or <see cref="Effect.Deny"/>.
    /// </exception>
    public RuleBuilder(string id, Effect effect)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (effect is not (Effect.Permit or Effect.Deny))
        {
            throw new ArgumentOutOfRangeException(
                nameof(effect),
                effect,
                "Rule effect must be Permit or Deny. NotApplicable and Indeterminate are computed by the evaluation engine.");
        }

        _id = id;
        _effect = effect;
    }

    /// <summary>
    /// Sets a human-readable description for this rule.
    /// </summary>
    /// <param name="description">The rule description.</param>
    public RuleBuilder WithDescription(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the target for this rule using a pre-built <see cref="Target"/> instance.
    /// </summary>
    /// <param name="target">The target that determines rule applicability.</param>
    public RuleBuilder WithTarget(Target target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;
        return this;
    }

    /// <summary>
    /// Sets the target for this rule using a <see cref="TargetBuilder"/> delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the <see cref="TargetBuilder"/>.</param>
    public RuleBuilder WithTarget(Action<TargetBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new TargetBuilder();
        configure(builder);
        _target = builder.Build();
        return this;
    }

    /// <summary>
    /// Sets the condition that must evaluate to <c>true</c> for the rule's effect to apply.
    /// </summary>
    /// <param name="condition">An <see cref="Apply"/> expression tree that evaluates to a boolean.</param>
    public RuleBuilder WithCondition(Apply condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        _condition = condition;
        return this;
    }

    /// <summary>
    /// Adds a pre-built obligation to this rule.
    /// </summary>
    /// <param name="obligation">The obligation to add.</param>
    public RuleBuilder AddObligation(Obligation obligation)
    {
        ArgumentNullException.ThrowIfNull(obligation);
        _obligations.Add(obligation);
        return this;
    }

    /// <summary>
    /// Adds an obligation to this rule using a builder delegate.
    /// </summary>
    /// <param name="obligationId">The unique obligation identifier.</param>
    /// <param name="configure">A delegate that configures the <see cref="ObligationBuilder"/>.</param>
    public RuleBuilder AddObligation(string obligationId, Action<ObligationBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(obligationId);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ObligationBuilder(obligationId);
        configure(builder);
        _obligations.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a pre-built advice expression to this rule.
    /// </summary>
    /// <param name="advice">The advice expression to add.</param>
    public RuleBuilder AddAdvice(AdviceExpression advice)
    {
        ArgumentNullException.ThrowIfNull(advice);
        _advice.Add(advice);
        return this;
    }

    /// <summary>
    /// Adds an advice expression to this rule using a builder delegate.
    /// </summary>
    /// <param name="adviceId">The unique advice identifier.</param>
    /// <param name="configure">A delegate that configures the <see cref="AdviceBuilder"/>.</param>
    public RuleBuilder AddAdvice(string adviceId, Action<AdviceBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adviceId);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new AdviceBuilder(adviceId);
        configure(builder);
        _advice.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Builds the <see cref="Rule"/> instance from the accumulated configuration.
    /// </summary>
    /// <returns>An immutable <see cref="Rule"/> record.</returns>
    public Rule Build() => new()
    {
        Id = _id,
        Description = _description,
        Effect = _effect,
        Target = _target,
        Condition = _condition,
        Obligations = _obligations.ToList(),
        Advice = _advice.ToList()
    };
}
