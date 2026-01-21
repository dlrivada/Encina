using Encina.Tenancy;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenancyOptions"/>.
/// </summary>
public class TenancyOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void TenancyOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new TenancyOptions();

        // Assert
        options.DefaultStrategy.ShouldBe(TenantIsolationStrategy.SharedSchema);
        options.RequireTenant.ShouldBeFalse();
        options.TenantIdPropertyName.ShouldBe("TenantId");
        options.DefaultConnectionString.ShouldBeNull();
        options.DefaultSchemaName.ShouldBe("dbo");
        options.ValidateTenantOnRequest.ShouldBeFalse();
        options.Tenants.ShouldNotBeNull();
        options.Tenants.ShouldBeEmpty();
    }

    [Fact]
    public void TenancyOptions_CanSetDefaultStrategy()
    {
        // Arrange & Act
        var options = new TenancyOptions
        {
            DefaultStrategy = TenantIsolationStrategy.DatabasePerTenant
        };

        // Assert
        options.DefaultStrategy.ShouldBe(TenantIsolationStrategy.DatabasePerTenant);
    }

    [Fact]
    public void TenancyOptions_CanSetRequireTenant()
    {
        // Arrange & Act
        var options = new TenancyOptions
        {
            RequireTenant = true
        };

        // Assert
        options.RequireTenant.ShouldBeTrue();
    }

    [Fact]
    public void TenancyOptions_CanSetTenantIdPropertyName()
    {
        // Arrange & Act
        var options = new TenancyOptions
        {
            TenantIdPropertyName = "CustomTenantId"
        };

        // Assert
        options.TenantIdPropertyName.ShouldBe("CustomTenantId");
    }

    [Fact]
    public void TenancyOptions_CanSetConnectionStrings()
    {
        // Arrange & Act
        var options = new TenancyOptions
        {
            DefaultConnectionString = "Server=localhost;Database=Test;",
            DefaultSchemaName = "tenant_schema"
        };

        // Assert
        options.DefaultConnectionString.ShouldBe("Server=localhost;Database=Test;");
        options.DefaultSchemaName.ShouldBe("tenant_schema");
    }

    [Fact]
    public void TenancyOptions_CanAddTenants()
    {
        // Arrange
        var tenant1 = new TenantInfo("t1", "Tenant 1", TenantIsolationStrategy.SharedSchema);
        var tenant2 = new TenantInfo("t2", "Tenant 2", TenantIsolationStrategy.DatabasePerTenant, "Server=t2;");

        // Act
        var options = new TenancyOptions();
        options.Tenants.Add(tenant1);
        options.Tenants.Add(tenant2);

        // Assert
        options.Tenants.Count.ShouldBe(2);
        options.Tenants[0].TenantId.ShouldBe("t1");
        options.Tenants[1].TenantId.ShouldBe("t2");
    }

    #endregion
}
