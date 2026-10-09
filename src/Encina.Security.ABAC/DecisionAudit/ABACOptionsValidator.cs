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

        var failures = new List<string>();
        var audit = options.DecisionAudit;

        if (audit.WriteTimeout <= TimeSpan.Zero || audit.WriteTimeout > MaxWriteTimeout)
        {
            failures.Add("ABACOptions.DecisionAudit.WriteTimeout must be greater than zero and at most int.MaxValue milliseconds.");
        }

        if (audit.MaxTraceEntries < 1)
        {
            failures.Add("ABACOptions.DecisionAudit.MaxTraceEntries must be at least 1.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
