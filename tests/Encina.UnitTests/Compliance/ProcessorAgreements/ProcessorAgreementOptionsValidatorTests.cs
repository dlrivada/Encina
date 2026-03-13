#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

public class ProcessorAgreementOptionsValidatorTests
{
    private readonly ProcessorAgreementOptionsValidator _validator = new();

    #region ValidDefaults

    [Fact]
    public void Validate_ValidDefaults_ReturnsSuccess()
    {
        // Arrange
        var options = new ProcessorAgreementOptions();

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region EnforcementMode

    [Fact]
    public void Validate_UndefinedEnforcementMode_ReturnsFail()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = (ProcessorAgreementEnforcementMode)99
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region MaxSubProcessorDepth

    [Fact]
    public void Validate_MaxDepthZero_ReturnsFail()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            MaxSubProcessorDepth = 0
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_MaxDepth11_ReturnsFail()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            MaxSubProcessorDepth = 11
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_MaxDepthBoundary1_ReturnsSuccess()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            MaxSubProcessorDepth = 1
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_MaxDepthBoundary10_ReturnsSuccess()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            MaxSubProcessorDepth = 10
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region ExpirationMonitoring

    [Fact]
    public void Validate_EnableMonitoringZeroInterval_ReturnsFail()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.Zero
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_DisableMonitoringZeroInterval_ReturnsSuccess()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnableExpirationMonitoring = false,
            ExpirationCheckInterval = TimeSpan.Zero
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region ExpirationWarningDays

    [Fact]
    public void Validate_NegativeWarningDays_ReturnsFail()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            ExpirationWarningDays = -1
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_ZeroWarningDays_ReturnsFail()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            ExpirationWarningDays = 0
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region NullOptions

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _validator.Validate(null, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region MultipleFailures

    [Fact]
    public void Validate_MultipleFailures_ReportsAll()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = (ProcessorAgreementEnforcementMode)99,
            MaxSubProcessorDepth = 0,
            ExpirationWarningDays = -5
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().NotBeNullOrEmpty();
    }

    #endregion
}
