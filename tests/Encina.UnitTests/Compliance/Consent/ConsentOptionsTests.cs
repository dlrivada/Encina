using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="ConsentOptions"/>.
/// </summary>
public class ConsentOptionsTests
{
    #region Default Values

    [Fact]
    public void Defaults_EnforcementMode_ShouldBeBlock()
    {
        var options = new ConsentOptions();
        options.EnforcementMode.Should().Be(ConsentEnforcementMode.Block);
    }

    [Fact]
    public void Defaults_DefaultExpirationDays_ShouldBe365()
    {
        var options = new ConsentOptions();
        options.DefaultExpirationDays.Should().Be(365);
    }

    [Fact]
    public void Defaults_TrackConsentProof_ShouldBeFalse()
    {
        var options = new ConsentOptions();
        options.TrackConsentProof.Should().BeFalse();
    }

    [Fact]
    public void Defaults_RequireExplicitConsent_ShouldBeTrue()
    {
        var options = new ConsentOptions();
        options.RequireExplicitConsent.Should().BeTrue();
    }

    [Fact]
    public void Defaults_AllowGranularWithdrawal_ShouldBeTrue()
    {
        var options = new ConsentOptions();
        options.AllowGranularWithdrawal.Should().BeTrue();
    }

    [Fact]
    public void Defaults_AddHealthCheck_ShouldBeFalse()
    {
        var options = new ConsentOptions();
        options.AddHealthCheck.Should().BeFalse();
    }

    [Fact]
    public void Defaults_AutoRegisterFromAttributes_ShouldBeTrue()
    {
        var options = new ConsentOptions();
        options.AutoRegisterFromAttributes.Should().BeTrue();
    }

    [Fact]
    public void Defaults_AssembliesToScan_ShouldBeEmpty()
    {
        var options = new ConsentOptions();
        options.AssembliesToScan.Should().BeEmpty();
    }

    [Fact]
    public void Defaults_PurposeDefinitions_ShouldBeEmpty()
    {
        var options = new ConsentOptions();
        options.PurposeDefinitions.Should().BeEmpty();
    }

    [Fact]
    public void Defaults_FailOnUnknownPurpose_ShouldBeFalse()
    {
        var options = new ConsentOptions();
        options.FailOnUnknownPurpose.Should().BeFalse();
    }

    [Fact]
    public void Defaults_DetailedPurposeDefinitions_ShouldBeEmpty()
    {
        var options = new ConsentOptions();
        options.DetailedPurposeDefinitions.Should().BeEmpty();
    }

    #endregion

    #region DefinePurpose

    [Fact]
    public void DefinePurpose_SimplePurpose_ShouldAddToBothCollections()
    {
        // Arrange
        var options = new ConsentOptions();

        // Act
        options.DefinePurpose(ConsentPurposes.Marketing);

        // Assert
        options.PurposeDefinitions.Should().Contain(ConsentPurposes.Marketing);
        options.DetailedPurposeDefinitions.Should().ContainKey(ConsentPurposes.Marketing);
    }

    [Fact]
    public void DefinePurpose_WithConfiguration_ShouldApplySettings()
    {
        // Arrange
        var options = new ConsentOptions();

        // Act
        options.DefinePurpose(ConsentPurposes.Marketing, p =>
        {
            p.Description = "Email marketing";
            p.RequiresExplicitOptIn = true;
            p.CanBeWithdrawnAnytime = true;
            p.DefaultExpirationDays = 180;
        });

        // Assert
        var definition = options.DetailedPurposeDefinitions[ConsentPurposes.Marketing];
        definition.Description.Should().Be("Email marketing");
        definition.RequiresExplicitOptIn.Should().BeTrue();
        definition.CanBeWithdrawnAnytime.Should().BeTrue();
        definition.DefaultExpirationDays.Should().Be(180);
    }

    [Fact]
    public void DefinePurpose_ShouldReturnSameInstance_ForChaining()
    {
        // Arrange
        var options = new ConsentOptions();

        // Act
        var result = options
            .DefinePurpose(ConsentPurposes.Marketing)
            .DefinePurpose(ConsentPurposes.Analytics);

        // Assert
        result.Should().BeSameAs(options);
        options.PurposeDefinitions.Should().HaveCount(2);
    }

    [Fact]
    public void DefinePurpose_NullPurpose_ShouldThrow()
    {
        var options = new ConsentOptions();
        var act = () => options.DefinePurpose(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DefinePurpose_WhitespacePurpose_ShouldThrow()
    {
        var options = new ConsentOptions();
        var act = () => options.DefinePurpose("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DefinePurpose_WithoutConfigure_ShouldUseDefaults()
    {
        // Arrange
        var options = new ConsentOptions();

        // Act
        options.DefinePurpose(ConsentPurposes.Analytics);

        // Assert
        var definition = options.DetailedPurposeDefinitions[ConsentPurposes.Analytics];
        definition.Description.Should().BeNull();
        definition.RequiresExplicitOptIn.Should().BeFalse();
        definition.CanBeWithdrawnAnytime.Should().BeTrue();
        definition.DefaultExpirationDays.Should().BeNull();
    }

    #endregion

    #region PurposeDefinitionEntry Defaults

    [Fact]
    public void PurposeDefinitionEntry_Defaults_ShouldBeCorrect()
    {
        // Arrange & Act
        var entry = new ConsentOptions.PurposeDefinitionEntry();

        // Assert
        entry.Description.Should().BeNull();
        entry.RequiresExplicitOptIn.Should().BeFalse();
        entry.CanBeWithdrawnAnytime.Should().BeTrue();
        entry.DefaultExpirationDays.Should().BeNull();
    }

    #endregion

    #region ConsentEnforcementMode Enum

    [Fact]
    public void ConsentEnforcementMode_ShouldHaveThreeValues()
    {
        Enum.GetValues<ConsentEnforcementMode>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(ConsentEnforcementMode.Block, 0)]
    [InlineData(ConsentEnforcementMode.Warn, 1)]
    [InlineData(ConsentEnforcementMode.Disabled, 2)]
    public void ConsentEnforcementMode_ShouldHaveExpectedIntValues(
        ConsentEnforcementMode mode, int expected)
    {
        ((int)mode).Should().Be(expected);
    }

    #endregion
}
