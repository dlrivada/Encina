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
            DefaultReviewPeriod = TimeSpan.FromDays(days.Get),
            EnableExpirationMonitoring = false
        };

        var result = validator.Validate(null, options);
        return result.Succeeded;
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
