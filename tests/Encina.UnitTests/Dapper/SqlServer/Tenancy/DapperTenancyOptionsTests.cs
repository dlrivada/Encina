using Encina.Dapper.SqlServer.Tenancy;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.SqlServer.Tenancy;

/// <summary>
/// Unit tests for <see cref="DapperTenancyOptions"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DapperTenancyOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new DapperTenancyOptions();

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
        // Arrange & Act
        var options = new DapperTenancyOptions { AutoFilterTenantQueries = false };

        // Assert
        options.AutoFilterTenantQueries.ShouldBeFalse();
    }

    [Fact]
    public void AutoAssignTenantId_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new DapperTenancyOptions { AutoAssignTenantId = false };

        // Assert
        options.AutoAssignTenantId.ShouldBeFalse();
    }

    [Fact]
    public void ValidateTenantOnModify_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new DapperTenancyOptions { ValidateTenantOnModify = false };

        // Assert
        options.ValidateTenantOnModify.ShouldBeFalse();
    }

    [Fact]
    public void ThrowOnMissingTenantContext_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new DapperTenancyOptions { ThrowOnMissingTenantContext = false };

        // Assert
        options.ThrowOnMissingTenantContext.ShouldBeFalse();
    }

    [Fact]
    public void TenantColumnName_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new DapperTenancyOptions { TenantColumnName = "OrganizationId" };

        // Assert
        options.TenantColumnName.ShouldBe("OrganizationId");
    }
}
