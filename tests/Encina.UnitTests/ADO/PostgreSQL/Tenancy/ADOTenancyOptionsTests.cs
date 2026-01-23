using Encina.ADO.PostgreSQL.Tenancy;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.PostgreSQL.Tenancy;

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
        var options = new ADOTenancyOptions { AutoFilterTenantQueries = false };
        options.AutoFilterTenantQueries.ShouldBeFalse();
    }

    [Fact]
    public void AutoAssignTenantId_ShouldBeConfigurable()
    {
        var options = new ADOTenancyOptions { AutoAssignTenantId = false };
        options.AutoAssignTenantId.ShouldBeFalse();
    }

    [Fact]
    public void ValidateTenantOnModify_ShouldBeConfigurable()
    {
        var options = new ADOTenancyOptions { ValidateTenantOnModify = false };
        options.ValidateTenantOnModify.ShouldBeFalse();
    }

    [Fact]
    public void ThrowOnMissingTenantContext_ShouldBeConfigurable()
    {
        var options = new ADOTenancyOptions { ThrowOnMissingTenantContext = false };
        options.ThrowOnMissingTenantContext.ShouldBeFalse();
    }

    [Fact]
    public void TenantColumnName_ShouldBeConfigurable()
    {
        var options = new ADOTenancyOptions { TenantColumnName = "OrganizationId" };
        options.TenantColumnName.ShouldBe("OrganizationId");
    }
}
