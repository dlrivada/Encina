using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="ConsentOptionsValidator"/>.
/// </summary>
public class ConsentOptionsValidatorTests
{
    private readonly ConsentOptionsValidator _validator = new();

    #region Valid Configurations

    [Fact]
    public void Validate_ValidOptions_ShouldSucceed()
    {
        // Arrange
        var options = new ConsentOptions();
        options.DefinePurpose(ConsentPurposes.Marketing);

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WarnModeNoPurposes_ShouldSucceed()
    {
        // Arrange
        var options = new ConsentOptions
        {
            EnforcementMode = ConsentEnforcementMode.Warn
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_DisabledModeNoPurposes_ShouldSucceed()
    {
        // Arrange
        var options = new ConsentOptions
        {
            EnforcementMode = ConsentEnforcementMode.Disabled
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullExpirationDays_ShouldSucceed()
    {
        // Arrange
        var options = new ConsentOptions
        {
            DefaultExpirationDays = null,
            EnforcementMode = ConsentEnforcementMode.Warn
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region DefaultExpirationDays Validation

    [Fact]
    public void Validate_ZeroExpirationDays_ShouldFail()
    {
        // Arrange
        var options = new ConsentOptions
        {
            DefaultExpirationDays = 0,
            EnforcementMode = ConsentEnforcementMode.Warn
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DefaultExpirationDays");
    }

    [Fact]
    public void Validate_NegativeExpirationDays_ShouldFail()
    {
        // Arrange
        var options = new ConsentOptions
        {
            DefaultExpirationDays = -1,
            EnforcementMode = ConsentEnforcementMode.Warn
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DefaultExpirationDays");
    }

    #endregion

    #region Block Mode + Empty Purposes Validation

    [Fact]
    public void Validate_BlockModeEmptyPurposes_ShouldFail()
    {
        // Arrange
        var options = new ConsentOptions
        {
            EnforcementMode = ConsentEnforcementMode.Block
        };
        // Do not add any purposes

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("PurposeDefinitions");
    }

    [Fact]
    public void Validate_BlockModeWithPurposes_ShouldSucceed()
    {
        // Arrange
        var options = new ConsentOptions
        {
            EnforcementMode = ConsentEnforcementMode.Block
        };
        options.DefinePurpose(ConsentPurposes.Marketing);

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Purpose-Specific Expiration Validation

    [Fact]
    public void Validate_PurposeZeroExpiration_ShouldFail()
    {
        // Arrange
        var options = new ConsentOptions
        {
            EnforcementMode = ConsentEnforcementMode.Warn
        };
        options.DefinePurpose(ConsentPurposes.Marketing, p =>
        {
            p.DefaultExpirationDays = 0;
        });

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("marketing");
    }

    [Fact]
    public void Validate_PurposeNegativeExpiration_ShouldFail()
    {
        // Arrange
        var options = new ConsentOptions
        {
            EnforcementMode = ConsentEnforcementMode.Warn
        };
        options.DefinePurpose(ConsentPurposes.Analytics, p =>
        {
            p.DefaultExpirationDays = -5;
        });

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("analytics");
    }

    [Fact]
    public void Validate_PurposePositiveExpiration_ShouldSucceed()
    {
        // Arrange
        var options = new ConsentOptions
        {
            EnforcementMode = ConsentEnforcementMode.Warn
        };
        options.DefinePurpose(ConsentPurposes.Marketing, p =>
        {
            p.DefaultExpirationDays = 90;
        });

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Null Options

    [Fact]
    public void Validate_NullOptions_ShouldThrow()
    {
        var act = () => _validator.Validate(null, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion
}
