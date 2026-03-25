using Encina.Compliance.Anonymization;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Anonymization;

public class AnonymizationOptionsValidatorTests
{
    private readonly AnonymizationOptionsValidator _sut = new();

    [Fact]
    public void Validate_ValidDefaults_ReturnsSuccess()
    {
        var options = new AnonymizationOptions();

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidEnforcementMode_ReturnsFail()
    {
        var options = new AnonymizationOptions { EnforcementMode = (AnonymizationEnforcementMode)99 };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("EnforcementMode");
    }

    [Fact]
    public void Validate_BlockMode_ReturnsSuccess()
    {
        var options = new AnonymizationOptions { EnforcementMode = AnonymizationEnforcementMode.Block };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WarnMode_ReturnsSuccess()
    {
        var options = new AnonymizationOptions { EnforcementMode = AnonymizationEnforcementMode.Warn };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_DisabledMode_ReturnsSuccess()
    {
        var options = new AnonymizationOptions { EnforcementMode = AnonymizationEnforcementMode.Disabled };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _sut.Validate(null, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
