using Encina.Security.ABAC;
using Encina.Security.ABAC.Enforcement;

using FsCheck.Xunit;

namespace Encina.PropertyTests.Security.ABAC;

/// <summary>
/// Property-based tests for the combination rule of <see cref="RequirePolicyAttribute"/> and
/// <see cref="RequireConditionAttribute"/> (#1634): a request is permitted if and only if every
/// AND-group policy permits, the OR group is empty or has a permit, every required policy exists,
/// and every condition is true.
/// </summary>
public sealed class ABACRequirementCombinerPropertyTests
{
    // 0..3 map to the four effects; 4 means the policy was not found.
    private static RequiredPolicyOutcome ToOutcome((bool AllMustPass, byte Code) policy, int index) =>
        new($"policy-{index}", policy.AllMustPass, (policy.Code % 5) switch
        {
            0 => Effect.Permit,
            1 => Effect.Deny,
            2 => Effect.NotApplicable,
            3 => Effect.Indeterminate,
            _ => null
        });

    private static ConditionOutcome ToCondition(byte code) => (ConditionOutcome)(code % 3);

    [Property(MaxTest = 500)]
    public bool Combine_PermitsIffEveryRequirementPasses((bool AllMustPass, byte Code)[] policies, byte[] conditions)
    {
        var outcomes = (policies ?? []).Select(ToOutcome).ToList();
        var conditionOutcomes = (conditions ?? []).Select(ToCondition).ToList();

        var andGroup = outcomes.Where(p => p.AllMustPass).ToList();
        var orGroup = outcomes.Where(p => !p.AllMustPass).ToList();

        var expected = outcomes.TrueForAll(p => p.Effect is not null)
            && andGroup.TrueForAll(p => p.Effect == Effect.Permit)
            && (orGroup.Count == 0 || orGroup.Exists(p => p.Effect == Effect.Permit))
            && conditionOutcomes.TrueForAll(c => c == ConditionOutcome.True);

        var actual = ABACRequirementCombiner.Combine(outcomes, conditionOutcomes) == RequirementVerdictKind.Permit;

        return actual == expected;
    }

    [Property(MaxTest = 200)]
    public bool Combine_AMissingPolicyAlwaysReportsPolicyNotFound((bool AllMustPass, byte Code)[] policies, byte[] conditions, bool allMustPass)
    {
        var outcomes = (policies ?? []).Select(ToOutcome).ToList();
        outcomes.Add(new RequiredPolicyOutcome("missing", allMustPass, null));

        return ABACRequirementCombiner.Combine(outcomes, (conditions ?? []).Select(ToCondition).ToList())
            == RequirementVerdictKind.PolicyNotFound;
    }

    [Property(MaxTest = 200)]
    public bool CombineConditions_TheFirstConditionThatIsNotTrueDecides(byte[] conditions)
    {
        var outcomes = (conditions ?? []).Select(ToCondition).ToList();
        var first = outcomes.FindIndex(c => c != ConditionOutcome.True);

        var expected = first < 0
            ? RequirementVerdictKind.Permit
            : outcomes[first] == ConditionOutcome.False
                ? RequirementVerdictKind.ConditionNotMet
                : RequirementVerdictKind.Indeterminate;

        return ABACRequirementCombiner.CombineConditions(outcomes) == expected;
    }
}
