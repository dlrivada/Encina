namespace Encina.Security.ABAC.Builders;

/// <summary>
/// Fluent builder for constructing XACML 3.0 <see cref="PolicySet"/> instances with
/// support for recursive nesting of policy sets.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.11 — A PolicySet is the top-level authorization unit that groups
/// <see cref="Policy"/> and nested <see cref="PolicySet"/> elements under a single
/// combining algorithm. PolicySets enable multi-level authorization architectures
/// (e.g., organization → department → application).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var policySet = new PolicySetBuilder("organization-policies")
///     .WithDescription("Top-level organizational access policies")
///     .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
///     .AddPolicy("finance-policy", policy => policy
///         .AddRule("allow-read", Effect.Permit, rule => rule
///             .WithCondition(ConditionBuilder.Equal(
///                 ConditionBuilder.Attribute(AttributeCategory.Action, "name", XACMLDataTypes.String),
///                 ConditionBuilder.StringValue("read")))))
///     .AddPolicySet("department-policies", nested => nested
///         .AddPolicy("hr-policy", policy => policy
///             .AddRule("deny-external", Effect.Deny, _ => { })))
///     .Build();
/// </code>
/// </example>
public sealed class PolicySetBuilder
{
    private readonly string _id;
    private string? _version;
    private string? _description;
    private Target? _target;
    private CombiningAlgorithmId _algorithm = CombiningAlgorithmId.DenyOverrides;
    private readonly List<Policy> _policies = [];
    private readonly List<PolicySet> _policySets = [];
    private readonly List<Obligation> _obligations = [];
    private readonly List<AdviceExpression> _advice = [];
    private bool _isEnabled = true;
    private int _priority;

    /// <summary>
    /// Initializes a new <see cref="PolicySetBuilder"/> with the specified policy set identifier.
    /// </summary>
    /// <param name="id">The unique policy set identifier.</param>
    public PolicySetBuilder(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _id = id;
    }

    /// <summary>
    /// Sets the version identifier for this policy set.
    /// </summary>
    /// <param name="version">The version string (e.g., <c>"1.0"</c>, <c>"2.1.3"</c>).</param>
    public PolicySetBuilder WithVersion(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        _version = version;
        return this;
    }

    /// <summary>
    /// Sets a human-readable description for this policy set.
    /// </summary>
    /// <param name="description">The policy set description.</param>
    public PolicySetBuilder WithDescription(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the target for this policy set using a pre-built <see cref="Target"/> instance.
    /// </summary>
    /// <param name="target">The target that determines policy set applicability.</param>
    public PolicySetBuilder WithTarget(Target target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;
        return this;
    }

    /// <summary>
    /// Sets the target for this policy set using a <see cref="TargetBuilder"/> delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the <see cref="TargetBuilder"/>.</param>
    public PolicySetBuilder WithTarget(Action<TargetBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new TargetBuilder();
        configure(builder);
        _target = builder.Build();
        return this;
    }

    /// <summary>
    /// Adds a pre-built policy to this policy set.
    /// </summary>
    /// <param name="policy">The policy to add.</param>
    public PolicySetBuilder AddPolicy(Policy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _policies.Add(policy);
        return this;
    }

    /// <summary>
    /// Adds a policy to this policy set using a builder delegate.
    /// </summary>
    /// <param name="policyId">The unique policy identifier.</param>
    /// <param name="configure">A delegate that configures the <see cref="PolicyBuilder"/>.</param>
    public PolicySetBuilder AddPolicy(string policyId, Action<PolicyBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new PolicyBuilder(policyId);
        configure(builder);
        _policies.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a pre-built nested policy set.
    /// </summary>
    /// <param name="policySet">The nested policy set to add.</param>
    public PolicySetBuilder AddPolicySet(PolicySet policySet)
    {
        ArgumentNullException.ThrowIfNull(policySet);
        _policySets.Add(policySet);
        return this;
    }

    /// <summary>
    /// Adds a nested policy set using a builder delegate (recursive nesting).
    /// </summary>
    /// <param name="policySetId">The unique policy set identifier.</param>
    /// <param name="configure">A delegate that configures the nested <see cref="PolicySetBuilder"/>.</param>
    public PolicySetBuilder AddPolicySet(string policySetId, Action<PolicySetBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policySetId);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new PolicySetBuilder(policySetId);
        configure(builder);
        _policySets.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Sets the combining algorithm for this policy set.
    /// </summary>
    /// <param name="algorithm">The combining algorithm identifier. Defaults to <see cref="CombiningAlgorithmId.DenyOverrides"/>.</param>
    public PolicySetBuilder WithAlgorithm(CombiningAlgorithmId algorithm)
    {
        _algorithm = algorithm;
        return this;
    }

    /// <summary>
    /// Adds a pre-built obligation to this policy set.
    /// </summary>
    /// <param name="obligation">The obligation to add.</param>
    public PolicySetBuilder AddObligation(Obligation obligation)
    {
        ArgumentNullException.ThrowIfNull(obligation);
        _obligations.Add(obligation);
        return this;
    }

    /// <summary>
    /// Adds an obligation to this policy set using a builder delegate.
    /// </summary>
    /// <param name="obligationId">The unique obligation identifier.</param>
    /// <param name="configure">A delegate that configures the <see cref="ObligationBuilder"/>.</param>
    public PolicySetBuilder AddObligation(string obligationId, Action<ObligationBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(obligationId);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ObligationBuilder(obligationId);
        configure(builder);
        _obligations.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a pre-built advice expression to this policy set.
    /// </summary>
    /// <param name="advice">The advice expression to add.</param>
    public PolicySetBuilder AddAdvice(AdviceExpression advice)
    {
        ArgumentNullException.ThrowIfNull(advice);
        _advice.Add(advice);
        return this;
    }

    /// <summary>
    /// Adds an advice expression to this policy set using a builder delegate.
    /// </summary>
    /// <param name="adviceId">The unique advice identifier.</param>
    /// <param name="configure">A delegate that configures the <see cref="AdviceBuilder"/>.</param>
    public PolicySetBuilder AddAdvice(string adviceId, Action<AdviceBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adviceId);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new AdviceBuilder(adviceId);
        configure(builder);
        _advice.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Sets the evaluation priority for this policy set (lower values = higher priority).
    /// </summary>
    /// <param name="priority">The priority value.</param>
    public PolicySetBuilder WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// Marks this policy set as disabled. Disabled policy sets produce <see cref="Effect.NotApplicable"/>.
    /// </summary>
    public PolicySetBuilder Disabled()
    {
        _isEnabled = false;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="PolicySet"/> instance from the accumulated configuration.
    /// </summary>
    /// <returns>An immutable <see cref="PolicySet"/> record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when neither policies nor nested policy sets have been added.
    /// </exception>
    public PolicySet Build()
    {
        if (_policies.Count == 0 && _policySets.Count == 0)
        {
            throw new InvalidOperationException(
                $"PolicySet '{_id}' must contain at least one policy or nested policy set. " +
                "Use AddPolicy() or AddPolicySet() to add children.");
        }

        return new PolicySet
        {
            Id = _id,
            Version = _version,
            Description = _description,
            Target = _target,
            Policies = _policies.ToList(),
            PolicySets = _policySets.ToList(),
            Algorithm = _algorithm,
            Obligations = _obligations.ToList(),
            Advice = _advice.ToList(),
            IsEnabled = _isEnabled,
            Priority = _priority
        };
    }
}
