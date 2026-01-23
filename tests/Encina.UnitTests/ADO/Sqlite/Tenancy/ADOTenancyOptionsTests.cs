using Encina.ADO.Sqlite.Tenancy;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.Sqlite.Tenancy;

/// <summary>
/// Unit tests for <see cref="ADOTenancyOptions"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ADOTenancyOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new ADOTenancyOptions();

        // Assert
        options.AutoFilterTenantQueries.ShouldBeTrue();
        options.AutoAssignTenantId.ShouldBeTrue();
        options.ValidateTenantOnModify.ShouldBeTrue();
        options.ThrowOnMissingTenantContext.ShouldBeTrue();
        options.TenantColumnName.ShouldBe("TenantId");
    }

    [Fact]
    public void AutoFilterTenantQueries_ShouldBeConfigurable()
    {
        // Arrange
        var options = new ADOTenancyOptions();

        // Act
        options.AutoFilterTenantQueries = false;

        // Assert
        options.AutoFilterTenantQueries.ShouldBeFalse();
    }

    [Fact]
    public void AutoAssignTenantId_ShouldBeConfigurable()
    {
        // Arrange
        var options = new ADOTenancyOptions();

        // Act
        options.AutoAssignTenantId = false;

        // Assert
        options.AutoAssignTenantId.ShouldBeFalse();
    }

    [Fact]
    public void ValidateTenantOnModify_ShouldBeConfigurable()
    {
        // Arrange
        var options = new ADOTenancyOptions();

        // Act
        options.ValidateTenantOnModify = false;

        // Assert
        options.ValidateTenantOnModify.ShouldBeFalse();
    }

    [Fact]
    public void ThrowOnMissingTenantContext_ShouldBeConfigurable()
    {
        // Arrange
        var options = new ADOTenancyOptions();

        // Act
        options.ThrowOnMissingTenantContext = false;

        // Assert
        options.ThrowOnMissingTenantContext.ShouldBeFalse();
    }

    [Fact]
    public void TenantColumnName_ShouldBeConfigurable()
    {
        // Arrange
        var options = new ADOTenancyOptions();

        // Act
        options.TenantColumnName = "OrganizationId";

        // Assert
        options.TenantColumnName.ShouldBe("OrganizationId");
    }
}
