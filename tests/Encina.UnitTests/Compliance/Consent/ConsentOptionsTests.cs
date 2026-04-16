using Encina.Compliance.Consent;
using Shouldly;

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
        options.EnforcementMode.ShouldBe(ConsentEnforcementMode.Block);
    }

    [Fact]
    public void Defaults_DefaultExpirationDays_ShouldBe365()
    {
        var options = new ConsentOptions();
        options.DefaultExpirationDays.ShouldBe(365);
    }

    [Fact]
    public void Defaults_TrackConsentProof_ShouldBeFalse()
    {
        var options = new ConsentOptions();
        options.TrackConsentProof.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_RequireExplicitConsent_ShouldBeTrue()
    {
        var options = new ConsentOptions();
        options.RequireExplicitConsent.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_AllowGranularWithdrawal_ShouldBeTrue()
    {
        var options = new ConsentOptions();
        options.AllowGranularWithdrawal.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_AddHealthCheck_ShouldBeFalse()
    {
        var options = new ConsentOptions();
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_AutoRegisterFromAttributes_ShouldBeTrue()
    {
        var options = new ConsentOptions();
        options.AutoRegisterFromAttributes.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_AssembliesToScan_ShouldBeEmpty()
    {
        var options = new ConsentOptions();
        options.AssembliesToScan.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_PurposeDefinitions_ShouldBeEmpty()
    {
        var options = new ConsentOptions();
        options.PurposeDefinitions.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_FailOnUnknownPurpose_ShouldBeFalse()
    {
        var options = new ConsentOptions();
        options.FailOnUnknownPurpose.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_DetailedPurposeDefinitions_ShouldBeEmpty()
    {
        var options = new ConsentOptions();
        options.DetailedPurposeDefinitions.ShouldBeEmpty();
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
        options.PurposeDefinitions.ShouldContain(ConsentPurposes.Marketing);
        options.DetailedPurposeDefinitions.ShouldContainKey(ConsentPurposes.Marketing);
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
        definition.Description.ShouldBe("Email marketing");
        definition.RequiresExplicitOptIn.ShouldBeTrue();
        definition.CanBeWithdrawnAnytime.ShouldBeTrue();
        definition.DefaultExpirationDays.ShouldBe(180);
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
        result.ShouldBeSameAs(options);
        options.PurposeDefinitions.Count.ShouldBe(2);
    }

    [Fact]
    public void DefinePurpose_NullPurpose_ShouldThrow()
    {
        var options = new ConsentOptions();
        var act = () => options.DefinePurpose(null!);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void DefinePurpose_WhitespacePurpose_ShouldThrow()
    {
        var options = new ConsentOptions();
        var act = () => options.DefinePurpose("   ");
        Should.Throw<ArgumentException>(act);
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
        definition.Description.ShouldBeNull();
        definition.RequiresExplicitOptIn.ShouldBeFalse();
        definition.CanBeWithdrawnAnytime.ShouldBeTrue();
        definition.DefaultExpirationDays.ShouldBeNull();
    }

    #endregion

    #region PurposeDefinitionEntry Defaults

    [Fact]
    public void PurposeDefinitionEntry_Defaults_ShouldBeCorrect()
    {
        // Arrange & Act
        var entry = new ConsentOptions.PurposeDefinitionEntry();

        // Assert
        entry.Description.ShouldBeNull();
        entry.RequiresExplicitOptIn.ShouldBeFalse();
        entry.CanBeWithdrawnAnytime.ShouldBeTrue();
        entry.DefaultExpirationDays.ShouldBeNull();
    }

    #endregion

    #region ConsentEnforcementMode Enum

    [Fact]
    public void ConsentEnforcementMode_ShouldHaveThreeValues()
    {
        Enum.GetValues<ConsentEnforcementMode>().Count.ShouldBe(3);
    }

    [Theory]
    [InlineData(ConsentEnforcementMode.Block, 0)]
    [InlineData(ConsentEnforcementMode.Warn, 1)]
    [InlineData(ConsentEnforcementMode.Disabled, 2)]
    public void ConsentEnforcementMode_ShouldHaveExpectedIntValues(
        ConsentEnforcementMode mode, int expected)
    {
        ((int)mode).ShouldBe(expected);
    }

    #endregion
}
