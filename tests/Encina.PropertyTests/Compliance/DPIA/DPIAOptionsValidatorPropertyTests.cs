using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.DPIA;

/// <summary>
/// Property-based tests for <see cref="DPIAOptionsValidator"/>.
/// </summary>
[Trait("Category", "Property")]
public sealed class DPIAOptionsValidatorPropertyTests
{
    [Property(MaxTest = 50)]
    public bool Validate_ValidOptions_AlwaysSucceeds(PositiveInt days)
    {
        var validator = new DPIAOptionsValidator();
        var options = new DPIAOptions
        {
            EnforcementMode = DPIAEnforcementMode.Block,
            DefaultReviewPeriod = TimeSpan.FromDays(days.Get)
        };

        var result = validator.Validate(null, options);
        return result.Succeeded;
    }

    [Property(MaxTest = 100)]
    public bool Validate_IntervalWithinPeriodicTimerRange_AlwaysSucceeds(PositiveInt milliseconds)
    {
        // PositiveInt tops at int.MaxValue ms (about 24.8 days), inside the 1 ms .. uint.MaxValue - 1 ms range
        var validator = new DPIAOptionsValidator();
        var options = new DPIAOptions { ExpirationCheckInterval = TimeSpan.FromMilliseconds(milliseconds.Get) };

        return validator.Validate(null, options).Succeeded;
    }

    [Property(MaxTest = 100)]
    public bool Validate_NonPositiveInterval_AlwaysFails(NonNegativeInt milliseconds)
    {
        var validator = new DPIAOptionsValidator();
        var options = new DPIAOptions { ExpirationCheckInterval = TimeSpan.FromMilliseconds(-milliseconds.Get) };

        return validator.Validate(null, options).Failed;
    }

    [Property(MaxTest = 100)]
    public bool Validate_IntervalAbovePeriodicTimerMaximum_AlwaysFails(PositiveInt extraMilliseconds)
    {
        var validator = new DPIAOptionsValidator();
        var options = new DPIAOptions
        {
            ExpirationCheckInterval = TimeSpan.FromMilliseconds(uint.MaxValue - 1)
                + TimeSpan.FromMilliseconds(extraMilliseconds.Get)
        };

        return validator.Validate(null, options).Failed;
    }

    [Property(MaxTest = 50)]
    public bool Validate_NegativeReviewPeriod_AlwaysFails(PositiveInt days)
    {
        var validator = new DPIAOptionsValidator();
        var options = new DPIAOptions
        {
            EnforcementMode = DPIAEnforcementMode.Block,
            DefaultReviewPeriod = TimeSpan.FromDays(-days.Get)
        };

        var result = validator.Validate(null, options);
        return result.Failed;
    }

    [Property(MaxTest = 50)]
    public bool Validate_InvalidEmail_AlwaysFails(NonEmptyString localPart)
    {
        // Email without @ should fail
        var email = localPart.Get.Replace("@", "");
        if (string.IsNullOrEmpty(email)) return true; // skip empty

        var validator = new DPIAOptionsValidator();
        var options = new DPIAOptions
        {
            EnforcementMode = DPIAEnforcementMode.Block,
            DefaultReviewPeriod = TimeSpan.FromDays(365),
            DPOEmail = email
        };

        var result = validator.Validate(null, options);
        return result.Failed;
    }
}
