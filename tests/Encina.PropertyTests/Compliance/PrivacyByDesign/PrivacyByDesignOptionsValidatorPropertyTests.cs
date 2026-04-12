using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.PrivacyByDesign;

/// <summary>
/// Property-based tests for <see cref="PrivacyByDesignOptionsValidator"/> invariants.
/// </summary>
[Trait("Category", "Property")]
public sealed class PrivacyByDesignOptionsValidatorPropertyTests
{
    private static readonly PrivacyByDesignOptionsValidator Sut = new();

    [Property(MaxTest = 200)]
    public bool ValidThreshold_WithValidEnums_Succeeds(byte rawThreshold)
    {
        var threshold = rawThreshold / 255.0; // [0.0, 1.0]
        var options = new PrivacyByDesignOptions
        {
            MinimizationScoreThreshold = threshold,
            EnforcementMode = PrivacyByDesignEnforcementMode.Warn,
            PrivacyLevel = PrivacyLevel.Standard
        };

        return Sut.Validate(null, options).Succeeded;
    }

    [Property(MaxTest = 200)]
    public bool ThresholdAboveOne_AlwaysFails(PositiveInt extra)
    {
        var options = new PrivacyByDesignOptions
        {
            MinimizationScoreThreshold = 1.0 + extra.Get
        };

        return Sut.Validate(null, options).Failed;
    }

    [Property(MaxTest = 200)]
    public bool ThresholdBelowZero_AlwaysFails(PositiveInt magnitude)
    {
        var options = new PrivacyByDesignOptions
        {
            MinimizationScoreThreshold = -magnitude.Get
        };

        return Sut.Validate(null, options).Failed;
    }

    [Property(MaxTest = 200)]
    public bool InvalidEnforcementMode_AlwaysFails(PositiveInt offset)
    {
        var options = new PrivacyByDesignOptions
        {
            EnforcementMode = (PrivacyByDesignEnforcementMode)(100 + offset.Get)
        };

        return Sut.Validate(null, options).Failed;
    }

    [Property(MaxTest = 200)]
    public bool InvalidPrivacyLevel_AlwaysFails(PositiveInt offset)
    {
        var options = new PrivacyByDesignOptions
        {
            PrivacyLevel = (PrivacyLevel)(100 + offset.Get)
        };

        return Sut.Validate(null, options).Failed;
    }

    [Property(MaxTest = 100)]
    public bool BothEnumsInvalid_ReportsBothFailures(PositiveInt a, PositiveInt b)
    {
        var options = new PrivacyByDesignOptions
        {
            EnforcementMode = (PrivacyByDesignEnforcementMode)(100 + a.Get),
            PrivacyLevel = (PrivacyLevel)(100 + b.Get)
        };

        var result = Sut.Validate(null, options);
        if (!result.Failed) return false;

        var failures = result.Failures!.ToList();
        return failures.Count >= 2
               && failures.Any(f => f.Contains("EnforcementMode", StringComparison.Ordinal))
               && failures.Any(f => f.Contains("PrivacyLevel", StringComparison.Ordinal));
    }

    [Property(MaxTest = 100)]
    public bool PurposeBuilderMissingDescription_AlwaysFails(NonEmptyString name)
    {
        var n = name.Get.Trim();
        if (string.IsNullOrWhiteSpace(n)) return true;

        var options = new PrivacyByDesignOptions();
        options.PurposeBuilders.Add(new PurposeBuilder(n)
        {
            Description = "",
            LegalBasis = "Contract"
        });

        return Sut.Validate(null, options).Failed;
    }

    [Property(MaxTest = 100)]
    public bool PurposeBuilderMissingLegalBasis_AlwaysFails(NonEmptyString name)
    {
        var n = name.Get.Trim();
        if (string.IsNullOrWhiteSpace(n)) return true;

        var options = new PrivacyByDesignOptions();
        options.PurposeBuilders.Add(new PurposeBuilder(n)
        {
            Description = "Valid description",
            LegalBasis = ""
        });

        return Sut.Validate(null, options).Failed;
    }

    [Fact]
    public void DefaultOptions_Succeeds()
    {
        Sut.Validate(null, new PrivacyByDesignOptions()).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Sut.Validate(null, null!));
    }

    [Property(MaxTest = 100)]
    public bool Idempotence_SameInput_SameResult(byte rawThreshold)
    {
        var threshold = rawThreshold / 255.0;
        var options = new PrivacyByDesignOptions
        {
            MinimizationScoreThreshold = threshold
        };

        var first = Sut.Validate(null, options);
        var second = Sut.Validate(null, options);
        return first.Succeeded == second.Succeeded;
    }
}
