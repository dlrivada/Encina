using Encina.Compliance.Anonymization;
using Shouldly;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="AnonymizationOptions"/> default values and property behavior.
/// </summary>
public class AnonymizationOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultEnforcementMode_ShouldBeBlock()
    {
        // Act
        var options = new AnonymizationOptions();

        // Assert
        options.EnforcementMode.ShouldBe(AnonymizationEnforcementMode.Block);
    }

    [Fact]
    public void DefaultTrackAuditTrail_ShouldBeTrue()
    {
        // Act
        var options = new AnonymizationOptions();

        // Assert
        options.TrackAuditTrail.ShouldBeTrue();
    }

    [Fact]
    public void DefaultAddHealthCheck_ShouldBeFalse()
    {
        // Act
        var options = new AnonymizationOptions();

        // Assert
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void DefaultAutoRegisterFromAttributes_ShouldBeTrue()
    {
        // Act
        var options = new AnonymizationOptions();

        // Assert
        options.AutoRegisterFromAttributes.ShouldBeTrue();
    }

    [Fact]
    public void DefaultAssembliesToScan_ShouldBeEmpty()
    {
        // Act
        var options = new AnonymizationOptions();

        // Assert
        options.AssembliesToScan.ShouldBeEmpty();
    }

    #endregion

    #region Property Setter Tests

    [Fact]
    public void SetEnforcementMode_ShouldUpdateValue()
    {
        // Arrange
        var options = new AnonymizationOptions();

        // Act
        options.EnforcementMode = AnonymizationEnforcementMode.Warn;

        // Assert
        options.EnforcementMode.ShouldBe(AnonymizationEnforcementMode.Warn);
    }

    [Fact]
    public void SetTrackAuditTrail_ShouldUpdateValue()
    {
        // Arrange
        var options = new AnonymizationOptions();

        // Act
        options.TrackAuditTrail = false;

        // Assert
        options.TrackAuditTrail.ShouldBeFalse();
    }

    [Fact]
    public void SetAddHealthCheck_ShouldUpdateValue()
    {
        // Arrange
        var options = new AnonymizationOptions();

        // Act
        options.AddHealthCheck = true;

        // Assert
        options.AddHealthCheck.ShouldBeTrue();
    }

    [Fact]
    public void SetAutoRegisterFromAttributes_ShouldUpdateValue()
    {
        // Arrange
        var options = new AnonymizationOptions();

        // Act
        options.AutoRegisterFromAttributes = false;

        // Assert
        options.AutoRegisterFromAttributes.ShouldBeFalse();
    }

    [Fact]
    public void AssembliesToScan_AddAssembly_ShouldIncludeAssembly()
    {
        // Arrange
        var options = new AnonymizationOptions();
        var assembly = typeof(AnonymizationOptions).Assembly;

        // Act
        options.AssembliesToScan.Add(assembly);

        // Assert
        options.AssembliesToScan.ShouldHaveSingleItem()
            .Which.ShouldBeSameAs(assembly);
    }

    [Fact]
    public void SetEnforcementMode_ToDisabled_ShouldUpdateValue()
    {
        // Arrange
        var options = new AnonymizationOptions();

        // Act
        options.EnforcementMode = AnonymizationEnforcementMode.Disabled;

        // Assert
        options.EnforcementMode.ShouldBe(AnonymizationEnforcementMode.Disabled);
    }

    #endregion
}
