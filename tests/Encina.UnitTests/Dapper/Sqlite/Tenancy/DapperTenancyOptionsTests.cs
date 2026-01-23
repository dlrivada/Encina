using Encina.Dapper.Sqlite.Tenancy;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.Sqlite.Tenancy;

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
        var options = new DapperTenancyOptions { AutoFilterTenantQueries = false };
        options.AutoFilterTenantQueries.ShouldBeFalse();
    }

    [Fact]
    public void AutoAssignTenantId_ShouldBeConfigurable()
    {
        var options = new DapperTenancyOptions { AutoAssignTenantId = false };
        options.AutoAssignTenantId.ShouldBeFalse();
    }

    [Fact]
    public void ValidateTenantOnModify_ShouldBeConfigurable()
    {
        var options = new DapperTenancyOptions { ValidateTenantOnModify = false };
        options.ValidateTenantOnModify.ShouldBeFalse();
    }

    [Fact]
    public void ThrowOnMissingTenantContext_ShouldBeConfigurable()
    {
        var options = new DapperTenancyOptions { ThrowOnMissingTenantContext = false };
        options.ThrowOnMissingTenantContext.ShouldBeFalse();
    }

    [Fact]
    public void TenantColumnName_ShouldBeConfigurable()
    {
        var options = new DapperTenancyOptions { TenantColumnName = "OrganizationId" };
        options.TenantColumnName.ShouldBe("OrganizationId");
    }
}
