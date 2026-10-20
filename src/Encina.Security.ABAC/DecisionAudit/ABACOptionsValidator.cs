using Microsoft.Extensions.Options;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// Validates the decision audit bounds of <see cref="ABACOptions"/> when the application starts:
/// a write timeout that cannot bound a write, or a trace limit that cannot hold one node, fails the
/// start instead of failing every audited request.
/// </summary>
internal sealed class ABACOptionsValidator : IValidateOptions<ABACOptions>
{
    // The largest timeout a CancellationTokenSource accepts (int.MaxValue milliseconds).
    private static readonly TimeSpan MaxWriteTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ABACOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var audit = options.DecisionAudit;

        (bool Valid, string Message)[] rules =
        [
            (audit.WriteTimeout > TimeSpan.Zero && audit.WriteTimeout <= MaxWriteTimeout,
                "ABACOptions.DecisionAudit.WriteTimeout must be greater than zero and at most int.MaxValue milliseconds."),
            (audit.HealthFailureWindow > TimeSpan.Zero,
                "ABACOptions.DecisionAudit.HealthFailureWindow must be greater than zero."),
            (audit.MaxTraceEntries >= 1,
                "ABACOptions.DecisionAudit.MaxTraceEntries must be at least 1."),
            ((audit.Outcomes & ~ABACDecisionAuditOutcomes.All) == 0,
                "ABACOptions.DecisionAudit.Outcomes contains a value that is not an ABACDecisionAuditOutcomes flag."),
            (!(audit.Enabled && options.EnforcementMode == ABACEnforcementMode.Disabled),
                "ABACOptions.DecisionAudit.Enabled cannot be combined with EnforcementMode.Disabled: a disabled enforcement point evaluates and records nothing."),
            (Enum.IsDefined(audit.FailureMode),
                "ABACOptions.DecisionAudit.FailureMode is not a defined ABACDecisionAuditFailureMode value.")
        ];

        var failures = rules.Where(rule => !rule.Valid).Select(rule => rule.Message).ToList();
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
