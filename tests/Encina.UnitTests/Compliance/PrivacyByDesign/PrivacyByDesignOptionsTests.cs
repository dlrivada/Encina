#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="PrivacyByDesignOptions"/>.
/// </summary>
public class PrivacyByDesignOptionsTests
{
    #region Default Values

    [Fact]
    public void DefaultEnforcementMode_ShouldBeWarn()
    {
        // Act
        var options = new PrivacyByDesignOptions();

        // Assert
        options.EnforcementMode.Should().Be(PrivacyByDesignEnforcementMode.Warn);
    }

    [Fact]
    public void DefaultMinimizationScoreThreshold_ShouldBeZero()
    {
        // Act
        var options = new PrivacyByDesignOptions();

        // Assert
        options.MinimizationScoreThreshold.Should().Be(0.0);
    }

    [Fact]
    public void DefaultPrivacyLevel_ShouldBeStandard()
    {
        // Act
        var options = new PrivacyByDesignOptions();

        // Assert
        options.PrivacyLevel.Should().Be(PrivacyLevel.Standard);
    }

    [Fact]
    public void DefaultTrackAuditTrail_ShouldBeTrue()
    {
        // Act
        var options = new PrivacyByDesignOptions();

        // Assert
        options.TrackAuditTrail.Should().BeTrue();
    }

    [Fact]
    public void DefaultAddHealthCheck_ShouldBeFalse()
    {
        // Act
        var options = new PrivacyByDesignOptions();

        // Assert
        options.AddHealthCheck.Should().BeFalse();
    }

    #endregion

    #region BlockOnViolation

    [Fact]
    public void BlockOnViolation_WhenSetToTrue_ShouldSetEnforcementModeToBlock()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();

        // Act
        options.BlockOnViolation = true;

        // Assert
        options.EnforcementMode.Should().Be(PrivacyByDesignEnforcementMode.Block);
    }

    [Fact]
    public void BlockOnViolation_WhenEnforcementModeIsBlock_ShouldReturnTrue()
    {
        // Arrange
        var options = new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Block
        };

        // Assert
        options.BlockOnViolation.Should().BeTrue();
    }

    [Fact]
    public void BlockOnViolation_WhenEnforcementModeIsWarn_ShouldReturnFalse()
    {
        // Arrange
        var options = new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Warn
        };

        // Assert
        options.BlockOnViolation.Should().BeFalse();
    }

    [Fact]
    public void BlockOnViolation_WhenSetToFalse_ShouldNotChangeEnforcementMode()
    {
        // Arrange
        var options = new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Block
        };

        // Act
        options.BlockOnViolation = false;

        // Assert — setting false does NOT change EnforcementMode away from Block
        options.EnforcementMode.Should().Be(PrivacyByDesignEnforcementMode.Block);
    }

    #endregion

    #region AddPurpose

    [Fact]
    public void AddPurpose_ShouldAddBuilderToPurposeBuilders()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();

        // Act
        options.AddPurpose("Order Processing", purpose =>
        {
            purpose.Description = "Processing personal data for order fulfillment.";
            purpose.LegalBasis = "Contract";
        });

        // Assert
        options.PurposeBuilders.Should().HaveCount(1);
        options.PurposeBuilders[0].Name.Should().Be("Order Processing");
        options.PurposeBuilders[0].Description.Should().Be("Processing personal data for order fulfillment.");
        options.PurposeBuilders[0].LegalBasis.Should().Be("Contract");
        options.PurposeBuilders[0].ModuleId.Should().BeNull();
    }

    [Fact]
    public void AddPurpose_WithModuleId_ShouldSetModuleId()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();

        // Act
        options.AddPurpose("Marketing Analytics", "marketing", purpose =>
        {
            purpose.Description = "Processing data for marketing analytics.";
            purpose.LegalBasis = "Consent";
        });

        // Assert
        options.PurposeBuilders.Should().HaveCount(1);
        options.PurposeBuilders[0].Name.Should().Be("Marketing Analytics");
        options.PurposeBuilders[0].ModuleId.Should().Be("marketing");
    }

    [Fact]
    public void AddPurpose_ShouldReturnOptionsForChaining()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();

        // Act
        var result = options.AddPurpose("Purpose 1", purpose =>
        {
            purpose.Description = "Desc 1";
            purpose.LegalBasis = "Contract";
        });

        // Assert
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void AddPurpose_WithModuleId_ShouldReturnOptionsForChaining()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();

        // Act
        var result = options.AddPurpose("Purpose 1", "module-a", purpose =>
        {
            purpose.Description = "Desc 1";
            purpose.LegalBasis = "Contract";
        });

        // Assert
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void AddPurpose_MultiplePurposes_ShouldAddAllViaChaining()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();

        // Act
        options
            .AddPurpose("Purpose 1", purpose =>
            {
                purpose.Description = "Desc 1";
                purpose.LegalBasis = "Contract";
            })
            .AddPurpose("Purpose 2", "module-a", purpose =>
            {
                purpose.Description = "Desc 2";
                purpose.LegalBasis = "Consent";
            });

        // Assert
        options.PurposeBuilders.Should().HaveCount(2);
        options.PurposeBuilders[0].Name.Should().Be("Purpose 1");
        options.PurposeBuilders[1].Name.Should().Be("Purpose 2");
        options.PurposeBuilders[1].ModuleId.Should().Be("module-a");
    }

    [Fact]
    public void AddPurpose_NullName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();

        // Act
        var act = () => options.AddPurpose(null!, _ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void AddPurpose_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();

        // Act
        var act = () => options.AddPurpose("Test", (Action<PurposeBuilder>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("configure");
    }

    [Fact]
    public void AddPurpose_WithModuleId_NullName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();

        // Act
        var act = () => options.AddPurpose(null!, "module", _ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void AddPurpose_WithModuleId_NullModuleId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();

        // Act
        var act = () => options.AddPurpose("Test", null!, _ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("moduleId");
    }

    [Fact]
    public void AddPurpose_WithModuleId_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();

        // Act
        var act = () => options.AddPurpose("Test", "module", (Action<PurposeBuilder>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("configure");
    }

    #endregion
}
