using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Model;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="AIActOptionsValidator"/>.
/// </summary>
public class AIActOptionsValidatorTests
{
    private readonly AIActOptionsValidator _sut = new();

    [Fact]
    public void Validate_DefaultOptions_ShouldSucceed()
    {
        // Arrange
        var options = new AIActOptions();

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_AssembliesToScanWithAutoRegisterDisabled_ShouldFail()
    {
        // Arrange
        var options = new AIActOptions
        {
            AutoRegisterFromAttributes = false,
            EnforcementMode = AIActEnforcementMode.Block
        };
        options.AssembliesToScan.Add(typeof(AIActOptionsValidatorTests).Assembly);

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_BlockModeWithAutoRegisterAndNoAssemblies_ShouldSucceed()
    {
        // Arrange — this is a warning-only case; system falls back to entry assembly
        var options = new AIActOptions
        {
            EnforcementMode = AIActEnforcementMode.Block,
            AutoRegisterFromAttributes = true
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_DisabledMode_WithAssemblies_ShouldSucceed()
    {
        // Arrange — disabled mode ignores everything
        var options = new AIActOptions
        {
            EnforcementMode = AIActEnforcementMode.Disabled,
            AutoRegisterFromAttributes = false
        };
        options.AssembliesToScan.Add(typeof(AIActOptionsValidatorTests).Assembly);

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }
}
