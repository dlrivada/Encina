namespace Encina.Security.ABAC;

/// <summary>
/// Explains why a policy or policy set has the effect recorded in its <see cref="PolicyEvaluationTrace"/> node.
/// </summary>
public enum PolicyTraceReason
{
    /// <summary>The policy (set) was enabled and its target matched; its effect is the combined effect of its rules (or children).</summary>
    Evaluated = 0,

    /// <summary>The policy (set) is disabled, so it is not applicable without being evaluated.</summary>
    Disabled = 1,

    /// <summary>The target of the policy (set) did not match the request, so it is not applicable.</summary>
    TargetNotMatched = 2,

    /// <summary>The target of the policy (set) could not be evaluated, so its effect is indeterminate.</summary>
    TargetIndeterminate = 3,

    /// <summary>
    /// The policy or condition was not evaluated: an earlier requirement already decided the request
    /// (a short-circuited condition), or the required policy does not exist or could not be read.
    /// The node's effect is <see cref="Effect.NotApplicable"/>, or <see cref="Effect.Indeterminate"/>
    /// when the policy could not be read.
    /// </summary>
    NotEvaluated = 4
}
