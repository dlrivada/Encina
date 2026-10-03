namespace Encina.Security.ABAC.Enforcement;

/// <summary>
/// The verdict of the requirements a request declares through <see cref="RequirePolicyAttribute"/>
/// and <see cref="RequireConditionAttribute"/>.
/// </summary>
internal enum RequirementVerdictKind
{
    /// <summary>Every requirement passed.</summary>
    Permit,

    /// <summary>A required policy returned Deny or NotApplicable, so its group did not pass.</summary>
    PolicyDenied,

    /// <summary>A required policy is neither a top-level policy set nor a standalone policy in the store.</summary>
    PolicyNotFound,

    /// <summary>A required policy or condition could not be evaluated.</summary>
    Indeterminate,

    /// <summary>A required condition evaluated to <c>false</c>.</summary>
    ConditionNotMet
}

/// <summary>The outcome of one <see cref="RequireConditionAttribute"/> expression.</summary>
internal enum ConditionOutcome
{
    /// <summary>The expression evaluated to <c>true</c>.</summary>
    True,

    /// <summary>The expression evaluated to <c>false</c>.</summary>
    False,

    /// <summary>The expression could not be compiled or threw during evaluation.</summary>
    Error
}

/// <summary>
/// The outcome of one <see cref="RequirePolicyAttribute"/>: the effect of the named policy, or
/// <c>null</c> when the policy is not in the store.
/// </summary>
/// <param name="PolicyId">The required policy name.</param>
/// <param name="AllMustPass">The <see cref="RequirePolicyAttribute.AllMustPass"/> value of the attribute.</param>
/// <param name="Effect">The effect of the named policy, or <c>null</c> when it was not found.</param>
internal readonly record struct RequiredPolicyOutcome(string PolicyId, bool AllMustPass, Effect? Effect);

/// <summary>
/// The combination rule of the Policy Enforcement Point for <see cref="RequirePolicyAttribute"/>
/// and <see cref="RequireConditionAttribute"/> (#1634).
/// </summary>
/// <remarks>
/// <para>
/// A request is permitted only when every present requirement passes: every policy with
/// <see cref="RequirePolicyAttribute.AllMustPass"/> = <c>true</c> returns Permit, at least one policy
/// with <see cref="RequirePolicyAttribute.AllMustPass"/> = <c>false</c> returns Permit (when there is
/// any), and every condition is <c>true</c>.
/// </para>
/// <para>
/// When the request is not permitted, the verdict names the reason in this order: a missing policy;
/// then the policy groups (a group that must all pass and has a Deny or NotApplicable is a denial even
/// if another member is Indeterminate; a group where one must pass and none did is Indeterminate when
/// a member is Indeterminate, a denial otherwise); then the first condition that is not <c>true</c>
/// (<c>false</c> is a denial, an error is Indeterminate).
/// </para>
/// </remarks>
internal static class ABACRequirementCombiner
{
    /// <summary>
    /// Combines policy outcomes and condition outcomes with AND. This is the verdict the Policy
    /// Enforcement Point enforces: <see cref="ABACRequirementEvaluator"/> calls it with every policy
    /// outcome and the condition outcomes it evaluated (none when the policies did not pass).
    /// </summary>
    public static RequirementVerdictKind Combine(
        IReadOnlyList<RequiredPolicyOutcome> policies,
        IReadOnlyList<ConditionOutcome> conditions)
    {
        var policyVerdict = CombinePolicies(policies);
        return policyVerdict == RequirementVerdictKind.Permit
            ? CombineConditions(conditions)
            : policyVerdict;
    }

    /// <summary>Combines the outcomes of the required policies.</summary>
    public static RequirementVerdictKind CombinePolicies(IReadOnlyList<RequiredPolicyOutcome> policies)
    {
        if (policies.Any(policy => policy.Effect is null))
        {
            return RequirementVerdictKind.PolicyNotFound;
        }

        var allMustPass = GroupVerdict(EffectsOf(policies, allMustPass: true), requireAll: true);
        return allMustPass == RequirementVerdictKind.Permit
            ? GroupVerdict(EffectsOf(policies, allMustPass: false), requireAll: false)
            : allMustPass;
    }

    /// <summary>
    /// Combines condition outcomes in declaration order; the first outcome that is not
    /// <see cref="ConditionOutcome.True"/> decides.
    /// </summary>
    public static RequirementVerdictKind CombineConditions(IReadOnlyList<ConditionOutcome> conditions)
    {
        foreach (var condition in conditions)
        {
            if (condition != ConditionOutcome.True)
            {
                return condition == ConditionOutcome.False
                    ? RequirementVerdictKind.ConditionNotMet
                    : RequirementVerdictKind.Indeterminate;
            }
        }

        return RequirementVerdictKind.Permit;
    }

    private static List<Effect> EffectsOf(IReadOnlyList<RequiredPolicyOutcome> policies, bool allMustPass) =>
        policies
            .Where(policy => policy.AllMustPass == allMustPass)
            .Select(policy => policy.Effect.GetValueOrDefault(Effect.Indeterminate))
            .ToList();

    private static RequirementVerdictKind GroupVerdict(List<Effect> effects, bool requireAll)
    {
        var passed = effects.Count == 0
            || (requireAll ? effects.TrueForAll(effect => effect == Effect.Permit) : effects.Contains(Effect.Permit));

        if (passed)
        {
            return RequirementVerdictKind.Permit;
        }

        return requireAll
            ? DefiniteFailureFirst(effects)
            : IndeterminateFirst(effects);
    }

    private static RequirementVerdictKind DefiniteFailureFirst(List<Effect> effects) =>
        effects.Exists(effect => effect is Effect.Deny or Effect.NotApplicable)
            ? RequirementVerdictKind.PolicyDenied
            : RequirementVerdictKind.Indeterminate;

    private static RequirementVerdictKind IndeterminateFirst(List<Effect> effects) =>
        effects.Contains(Effect.Indeterminate)
            ? RequirementVerdictKind.Indeterminate
            : RequirementVerdictKind.PolicyDenied;
}
