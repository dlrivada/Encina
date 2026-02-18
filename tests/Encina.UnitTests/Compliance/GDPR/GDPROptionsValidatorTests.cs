using Encina.Compliance.GDPR;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="GDPROptionsValidator"/> (internal).
/// </summary>
public class GDPROptionsValidatorTests
{
    private readonly GDPROptionsValidator _sut = new();

    [Fact]
    public void Validate_EnforceMode_WithAllRequired_ShouldSucceed()
    {
        // Arrange
        var options = new GDPROptions
        {
            EnforcementMode = GDPREnforcementMode.Enforce,
            ControllerName = "Acme Corp",
            ControllerEmail = "privacy@acme.com"
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_EnforceMode_MissingControllerName_ShouldFail()
    {
        // Arrange
        var options = new GDPROptions
        {
            EnforcementMode = GDPREnforcementMode.Enforce,
            ControllerEmail = "privacy@acme.com"
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ControllerName");
    }

    [Fact]
    public void Validate_EnforceMode_MissingControllerEmail_ShouldFail()
    {
        // Arrange
        var options = new GDPROptions
        {
            EnforcementMode = GDPREnforcementMode.Enforce,
            ControllerName = "Acme Corp"
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ControllerEmail");
    }

    [Fact]
    public void Validate_WarnOnlyMode_MissingFields_ShouldSucceed()
    {
        // Arrange
        var options = new GDPROptions
        {
            EnforcementMode = GDPREnforcementMode.WarnOnly
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_DPO_WithEmptyName_ShouldFail()
    {
        // Arrange
        var options = new GDPROptions
        {
            EnforcementMode = GDPREnforcementMode.WarnOnly,
            DataProtectionOfficer = new DataProtectionOfficer("", "dpo@acme.com")
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DataProtectionOfficer.Name");
    }

    [Fact]
    public void Validate_DPO_WithEmptyEmail_ShouldFail()
    {
        // Arrange
        var options = new GDPROptions
        {
            EnforcementMode = GDPREnforcementMode.WarnOnly,
            DataProtectionOfficer = new DataProtectionOfficer("Jane Doe", "")
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DataProtectionOfficer.Email");
    }

    [Fact]
    public void Validate_DPO_Valid_ShouldSucceed()
    {
        // Arrange
        var options = new GDPROptions
        {
            EnforcementMode = GDPREnforcementMode.WarnOnly,
            DataProtectionOfficer = new DataProtectionOfficer("Jane Doe", "dpo@acme.com")
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _sut.Validate(null, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }
}
