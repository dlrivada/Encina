namespace Encina.Security.ABAC.Builders;

/// <summary>
/// Fluent builder for constructing XACML 3.0 <see cref="Policy"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.10 — A Policy groups related <see cref="Rule"/> elements under a common
/// <see cref="Target"/> and combines their individual effects using a specified combining algorithm.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var policy = new PolicyBuilder("finance-access-policy")
///     .WithDescription("Controls access to financial resources")
///     .ForResourceType&lt;FinancialReport&gt;()
///     .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
///     .AddRule("allow-read", Effect.Permit, rule => rule
///         .WithCondition(ConditionBuilder.Equal(
///             ConditionBuilder.Attribute(AttributeCategory.Action, "name", XACMLDataTypes.String),
///             ConditionBuilder.StringValue("read"))))
///     .AddObligation("audit-log", ob => ob
///         .OnPermit()
///         .WithAttribute("action", "Financial resource accessed"))
///     .Build();
/// </code>
/// </example>
public sealed class PolicyBuilder
{
    private readonly string _id;
    private string? _version;
    private string? _description;
    private Target? _target;
    private CombiningAlgorithmId _algorithm = CombiningAlgorithmId.DenyOverrides;
    private readonly List<Rule> _rules = [];
    private readonly List<Obligation> _obligations = [];
    private readonly List<AdviceExpression> _advice = [];
    private readonly List<VariableDefinition> _variableDefinitions = [];
    private bool _isEnabled = true;
    private int _priority;

    /// <summary>
    /// Initializes a new <see cref="PolicyBuilder"/> with the specified policy identifier.
    /// </summary>
    /// <param name="id">The unique policy identifier.</param>
    public PolicyBuilder(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _id = id;
    }

    /// <summary>
    /// Sets the version identifier for this policy.
    /// </summary>
    /// <param name="version">The version string (e.g., <c>"1.0"</c>, <c>"2.1.3"</c>).</param>
    public PolicyBuilder WithVersion(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        _version = version;
        return this;
    }

    /// <summary>
    /// Sets a human-readable description for this policy.
    /// </summary>
    /// <param name="description">The policy description.</param>
    public PolicyBuilder WithDescription(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the target for this policy using a pre-built <see cref="Target"/> instance.
    /// </summary>
    /// <param name="target">The target that determines policy applicability.</param>
    public PolicyBuilder WithTarget(Target target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;
        return this;
    }

    /// <summary>
    /// Sets the target for this policy using a <see cref="TargetBuilder"/> delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the <see cref="TargetBuilder"/>.</param>
    public PolicyBuilder WithTarget(Action<TargetBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new TargetBuilder();
        configure(builder);
        _target = builder.Build();
        return this;
    }

    /// <summary>
    /// Configures a target that matches requests for the specified resource type.
    /// </summary>
    /// <typeparam name="T">The resource type to match against.</typeparam>
    /// <returns>This builder for fluent chaining.</returns>
    /// <remarks>
    /// Creates a Target with a single Match: <c>resource.resourceType == typeof(T).Name</c>
    /// using <c>string-equal</c>.
    /// </remarks>
    public PolicyBuilder ForResourceType<T>()
    {
        var targetBuilder = new TargetBuilder();
        targetBuilder.AnyOf(any => any
            .AllOf(all => all
                .Match(
                    AttributeCategory.Resource,
                    "resourceType",
                    XACMLDataTypes.String,
                    ConditionOperator.Equals,
                    typeof(T).Name)));

        _target = targetBuilder.Build();
        return this;
    }

    /// <summary>
    /// Adds a pre-built rule to this policy.
    /// </summary>
    /// <param name="rule">The rule to add.</param>
    public PolicyBuilder AddRule(Rule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules.Add(rule);
        return this;
    }

    /// <summary>
    /// Adds a rule to this policy using a builder delegate.
    /// </summary>
    /// <param name="ruleId">The unique rule identifier within this policy.</param>
    /// <param name="effect">The effect to return when the rule applies (Permit or Deny).</param>
    /// <param name="configure">A delegate that configures the <see cref="RuleBuilder"/>.</param>
    public PolicyBuilder AddRule(string ruleId, Effect effect, Action<RuleBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new RuleBuilder(ruleId, effect);
        configure(builder);
        _rules.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Sets the combining algorithm for this policy.
    /// </summary>
    /// <param name="algorithm">The combining algorithm identifier. Defaults to <see cref="CombiningAlgorithmId.DenyOverrides"/>.</param>
    public PolicyBuilder WithAlgorithm(CombiningAlgorithmId algorithm)
    {
        _algorithm = algorithm;
        return this;
    }

    /// <summary>
    /// Adds a pre-built obligation to this policy.
    /// </summary>
    /// <param name="obligation">The obligation to add.</param>
    public PolicyBuilder AddObligation(Obligation obligation)
    {
        ArgumentNullException.ThrowIfNull(obligation);
        _obligations.Add(obligation);
        return this;
    }

    /// <summary>
    /// Adds an obligation to this policy using a builder delegate.
    /// </summary>
    /// <param name="obligationId">The unique obligation identifier.</param>
    /// <param name="configure">A delegate that configures the <see cref="ObligationBuilder"/>.</param>
    public PolicyBuilder AddObligation(string obligationId, Action<ObligationBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(obligationId);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ObligationBuilder(obligationId);
        configure(builder);
        _obligations.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a pre-built advice expression to this policy.
    /// </summary>
    /// <param name="advice">The advice expression to add.</param>
    public PolicyBuilder AddAdvice(AdviceExpression advice)
    {
        ArgumentNullException.ThrowIfNull(advice);
        _advice.Add(advice);
        return this;
    }

    /// <summary>
    /// Adds an advice expression to this policy using a builder delegate.
    /// </summary>
    /// <param name="adviceId">The unique advice identifier.</param>
    /// <param name="configure">A delegate that configures the <see cref="AdviceBuilder"/>.</param>
    public PolicyBuilder AddAdvice(string adviceId, Action<AdviceBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adviceId);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new AdviceBuilder(adviceId);
        configure(builder);
        _advice.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Defines a reusable sub-expression variable scoped to this policy.
    /// </summary>
    /// <param name="variableId">The unique variable identifier within this policy.</param>
    /// <param name="expression">The expression that computes the variable's value.</param>
    public PolicyBuilder DefineVariable(string variableId, IExpression expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);
        ArgumentNullException.ThrowIfNull(expression);

        _variableDefinitions.Add(new VariableDefinition
        {
            VariableId = variableId,
            Expression = expression
        });
        return this;
    }

    /// <summary>
    /// Sets the evaluation priority for this policy (lower values = higher priority).
    /// </summary>
    /// <param name="priority">The priority value.</param>
    public PolicyBuilder WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// Marks this policy as disabled. Disabled policies produce <see cref="Effect.NotApplicable"/>.
    /// </summary>
    public PolicyBuilder Disabled()
    {
        _isEnabled = false;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="Policy"/> instance from the accumulated configuration.
    /// </summary>
    /// <returns>An immutable <see cref="Policy"/> record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no rules have been added to the policy.
    /// </exception>
    public Policy Build()
    {
        if (_rules.Count == 0)
        {
            throw new InvalidOperationException(
                $"Policy '{_id}' must contain at least one rule. Use AddRule() to add rules.");
        }

        return new Policy
        {
            Id = _id,
            Version = _version,
            Description = _description,
            Target = _target,
            Rules = _rules.ToList(),
            Algorithm = _algorithm,
            Obligations = _obligations.ToList(),
            Advice = _advice.ToList(),
            VariableDefinitions = _variableDefinitions.ToList(),
            IsEnabled = _isEnabled,
            Priority = _priority
        };
    }
}
