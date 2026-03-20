using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Model;

using FluentAssertions;

using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.NIS2;

/// <summary>
/// Unit tests for <see cref="NIS2OptionsValidator"/>.
/// </summary>
public class NIS2OptionsValidatorTests
{
    private readonly NIS2OptionsValidator _sut = new();

    #region ValidOptions

    [Fact]
    public void ValidOptions_ShouldReturnSuccess()
    {
        // Arrange
        var options = new NIS2Options
        {
            Sector = NIS2Sector.Energy,
            EntityType = NIS2EntityType.Essential,
            IncidentNotificationHours = 24,
            CompetentAuthority = "authority@test.eu",
            EnforceEncryption = false // No encrypted categories configured
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    #endregion

    #region InvalidEnforcementMode

    [Fact]
    public void InvalidEnforcementMode_ShouldFail()
    {
        // Arrange
        var options = new NIS2Options
        {
            Sector = NIS2Sector.Energy,
            EntityType = NIS2EntityType.Essential,
            EnforcementMode = (NIS2EnforcementMode)99,
            IncidentNotificationHours = 24,
            CompetentAuthority = "authority@test.eu"
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("EnforcementMode");
    }

    #endregion

    #region InvalidEntityType

    [Fact]
    public void InvalidEntityType_ShouldFail()
    {
        // Arrange
        var options = new NIS2Options
        {
            Sector = NIS2Sector.Energy,
            EntityType = (NIS2EntityType)99,
            IncidentNotificationHours = 24,
            CompetentAuthority = "authority@test.eu"
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("EntityType");
    }

    #endregion

    #region InvalidSector

    [Fact]
    public void InvalidSector_ShouldFail()
    {
        // Arrange
        var options = new NIS2Options
        {
            Sector = (NIS2Sector)99,
            EntityType = NIS2EntityType.Essential,
            IncidentNotificationHours = 24,
            CompetentAuthority = "authority@test.eu"
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Sector");
    }

    #endregion

    #region NonPositiveIncidentHours

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveIncidentHours_ShouldFail(int hours)
    {
        // Arrange
        var options = new NIS2Options
        {
            Sector = NIS2Sector.Energy,
            EntityType = NIS2EntityType.Essential,
            IncidentNotificationHours = hours,
            CompetentAuthority = "authority@test.eu"
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("IncidentNotificationHours");
    }

    #endregion

    #region EssentialEntity_MissingAuthority

    [Fact]
    public void EssentialEntity_MissingAuthority_ShouldFail()
    {
        // Arrange
        var options = new NIS2Options
        {
            Sector = NIS2Sector.Energy,
            EntityType = NIS2EntityType.Essential,
            IncidentNotificationHours = 24,
            CompetentAuthority = null
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("CompetentAuthority");
    }

    #endregion

    #region ImportantEntity_MissingAuthority

    [Fact]
    public void ImportantEntity_MissingAuthority_ShouldSucceed()
    {
        // Arrange
        var options = new NIS2Options
        {
            Sector = NIS2Sector.Energy,
            EntityType = NIS2EntityType.Important,
            IncidentNotificationHours = 24,
            CompetentAuthority = null,
            EnforceEncryption = false
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    #endregion

    #region EncryptionCoherence

    [Fact]
    public void EnforceEncryption_WithoutCategories_ShouldFail()
    {
        // Arrange — EnforceEncryption is true but no categories or endpoints configured
        var options = new NIS2Options
        {
            Sector = NIS2Sector.Energy,
            EntityType = NIS2EntityType.Essential,
            IncidentNotificationHours = 24,
            CompetentAuthority = "authority@test.eu",
            EnforceEncryption = true
            // No EncryptedDataCategories or EncryptedEndpoints
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("EnforceEncryption");
    }

    [Fact]
    public void EnforceEncryption_WithCategories_ShouldSucceed()
    {
        // Arrange
        var options = new NIS2Options
        {
            Sector = NIS2Sector.Energy,
            EntityType = NIS2EntityType.Essential,
            IncidentNotificationHours = 24,
            CompetentAuthority = "authority@test.eu",
            EnforceEncryption = true
        };
        options.EncryptedDataCategories.Add("PII");

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    #endregion

    #region ExternalCallTimeout

    [Fact]
    public void ZeroExternalCallTimeout_ShouldFail()
    {
        // Arrange
        var options = new NIS2Options
        {
            Sector = NIS2Sector.Energy,
            EntityType = NIS2EntityType.Essential,
            IncidentNotificationHours = 24,
            CompetentAuthority = "authority@test.eu",
            EnforceEncryption = false,
            ExternalCallTimeout = TimeSpan.Zero
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ExternalCallTimeout");
    }

    #endregion

    #region SupplierWithEmptyName

    [Fact]
    public void SupplierWithEmptyName_ShouldFail()
    {
        // Arrange
        var options = new NIS2Options
        {
            Sector = NIS2Sector.Energy,
            EntityType = NIS2EntityType.Essential,
            IncidentNotificationHours = 24,
            CompetentAuthority = "authority@test.eu"
        };
        options.AddSupplier("test-supplier", s => s.Name = "");

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("test-supplier");
    }

    #endregion

    #region SupplierWithInvalidRiskLevel

    [Fact]
    public void SupplierWithInvalidRiskLevel_ShouldFail()
    {
        // Arrange
        var options = new NIS2Options
        {
            Sector = NIS2Sector.Energy,
            EntityType = NIS2EntityType.Essential,
            IncidentNotificationHours = 24,
            CompetentAuthority = "authority@test.eu"
        };
        options.AddSupplier("test-supplier", s =>
        {
            s.Name = "Valid Name";
            s.RiskLevel = (SupplierRiskLevel)99;
        });

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("RiskLevel");
    }

    #endregion
}
