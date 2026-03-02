using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DataResidencyOptionsValidatorTests
{
    private readonly DataResidencyOptionsValidator _validator = new();

    [Fact]
    public void Validate_DefaultOptions_ShouldSucceed()
    {
        // Arrange
        var options = new DataResidencyOptions();

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_BlockModeWithDefaultRegion_ShouldSucceed()
    {
        // Arrange
        var options = new DataResidencyOptions
        {
            EnforcementMode = DataResidencyEnforcementMode.Block,
            DefaultRegion = RegionRegistry.DE
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_BlockModeWithoutDefaultRegion_ShouldFail()
    {
        // Arrange
        var options = new DataResidencyOptions
        {
            EnforcementMode = DataResidencyEnforcementMode.Block,
            DefaultRegion = null
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_WarnModeWithoutDefaultRegion_ShouldSucceed()
    {
        // Arrange
        var options = new DataResidencyOptions
        {
            EnforcementMode = DataResidencyEnforcementMode.Warn,
            DefaultRegion = null
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_DisabledMode_ShouldAlwaysSucceed()
    {
        // Arrange
        var options = new DataResidencyOptions
        {
            EnforcementMode = DataResidencyEnforcementMode.Disabled,
            DefaultRegion = null
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_InvalidEnforcementMode_ShouldFail()
    {
        // Arrange
        var options = new DataResidencyOptions
        {
            EnforcementMode = (DataResidencyEnforcementMode)999
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
    }
}
