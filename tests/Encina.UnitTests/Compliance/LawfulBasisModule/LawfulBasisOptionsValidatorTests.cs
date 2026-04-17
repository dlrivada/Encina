using Encina.Compliance.LawfulBasis;
using Microsoft.Extensions.Options;
using GdprLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.UnitTests.Compliance.LawfulBasisModule;

public class LawfulBasisOptionsValidatorTests
{
    private readonly LawfulBasisOptionsValidator _validator = new();

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var result = _validator.Validate(null, new LawfulBasisOptions());
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _validator.Validate(null, null!));
    }

    [Fact]
    public void Validate_InvalidEnforcementMode_Fails()
    {
        var options = new LawfulBasisOptions { EnforcementMode = (LawfulBasisEnforcementMode)999 };
        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("EnforcementMode");
    }

    [Fact]
    public void Validate_InvalidDefaultBasisValue_Fails()
    {
        var options = new LawfulBasisOptions();
        options.DefaultBases[typeof(string)] = (GdprLawfulBasis)999;

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("DefaultBases");
    }

    [Fact]
    public void Validate_ValidDefaultBasis_Succeeds()
    {
        var options = new LawfulBasisOptions();
        options.DefaultBasis<string>(GdprLawfulBasis.Contract);

        var result = _validator.Validate(null, options);
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_AllEnforcementModes_AreValid()
    {
        foreach (var mode in Enum.GetValues<LawfulBasisEnforcementMode>())
        {
            var options = new LawfulBasisOptions { EnforcementMode = mode };
            var result = _validator.Validate(null, options);
            result.Succeeded.ShouldBeTrue($"Mode {mode} should be valid");
        }
    }
}
