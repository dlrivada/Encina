using Encina.Compliance.Anonymization;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="AnonymizationOptionsValidator"/> to verify null parameter handling.
/// </summary>
public class AnonymizationOptionsValidatorGuardTests
{
    #region Validate Guards

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var sut = new AnonymizationOptionsValidator();

        var act = () => sut.Validate(null, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Validate_ValidOptions_ReturnsSuccess()
    {
        var sut = new AnonymizationOptionsValidator();
        var options = new AnonymizationOptions();

        var result = sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_InvalidEnforcementMode_ReturnsFailure()
    {
        var sut = new AnonymizationOptionsValidator();
        var options = new AnonymizationOptions
        {
            EnforcementMode = (AnonymizationEnforcementMode)999
        };

        var result = sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
    }

    #endregion
}
