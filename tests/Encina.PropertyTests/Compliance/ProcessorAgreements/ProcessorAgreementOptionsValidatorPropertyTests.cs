using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.ProcessorAgreements;

/// <summary>
/// Property-based tests for <see cref="ProcessorAgreementOptionsValidator"/> verifying
/// that validation invariants hold across random inputs.
/// </summary>
public class ProcessorAgreementOptionsValidatorPropertyTests
{
    private readonly ProcessorAgreementOptionsValidator _validator = new();

    [Property(MaxTest = 50)]
    public bool ValidOptions_AlwaysSucceeds(PositiveInt maxDepth, PositiveInt warningDays)
    {
        var clampedDepth = Math.Clamp(maxDepth.Get, 1, 10);

        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Warn,
            MaxSubProcessorDepth = clampedDepth,
            ExpirationWarningDays = warningDays.Get,
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.FromMinutes(15)
        };

        var result = _validator.Validate(null, options);
        return result.Succeeded;
    }

    [Property(MaxTest = 50)]
    public bool ZeroOrNegativeWarningDays_AlwaysFails(NegativeInt warningDays)
    {
        var options = new ProcessorAgreementOptions
        {
            ExpirationWarningDays = warningDays.Get
        };

        var result = _validator.Validate(null, options);
        return result.Failed;
    }

    [Property(MaxTest = 50)]
    public bool DepthOutOfRange_AlwaysFails(PositiveInt depthOffset)
    {
        var options = new ProcessorAgreementOptions
        {
            MaxSubProcessorDepth = 11 + depthOffset.Get
        };

        var result = _validator.Validate(null, options);
        return result.Failed;
    }

    [Property(MaxTest = 50)]
    public bool ZeroExpirationCheckInterval_WhenMonitoringEnabled_AlwaysFails(PositiveInt warningDays)
    {
        var options = new ProcessorAgreementOptions
        {
            ExpirationWarningDays = warningDays.Get,
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.Zero
        };

        var result = _validator.Validate(null, options);
        return result.Failed;
    }
}
