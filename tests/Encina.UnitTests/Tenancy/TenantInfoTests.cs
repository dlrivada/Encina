using Encina.Tenancy;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantInfo"/>.
/// </summary>
public class TenantInfoTests
{
    #region Constructor Tests

    [Fact]
    public void TenantInfo_Constructor_SetsRequiredProperties()
    {
        // Arrange & Act
        var tenant = new TenantInfo("tenant-123", "Test Tenant", TenantIsolationStrategy.SharedSchema);

        // Assert
        tenant.TenantId.ShouldBe("tenant-123");
        tenant.Name.ShouldBe("Test Tenant");
        tenant.Strategy.ShouldBe(TenantIsolationStrategy.SharedSchema);
        tenant.ConnectionString.ShouldBeNull();
        tenant.SchemaName.ShouldBeNull();
    }

    [Fact]
    public void TenantInfo_Constructor_SetsOptionalProperties()
    {
        // Arrange & Act
        var tenant = new TenantInfo(
            "tenant-456",
            "Enterprise Tenant",
            TenantIsolationStrategy.DatabasePerTenant,
            ConnectionString: "Server=enterprise;Database=EnterpriseDb;",
            SchemaName: "enterprise_schema");

        // Assert
        tenant.TenantId.ShouldBe("tenant-456");
        tenant.Name.ShouldBe("Enterprise Tenant");
        tenant.Strategy.ShouldBe(TenantIsolationStrategy.DatabasePerTenant);
        tenant.ConnectionString.ShouldBe("Server=enterprise;Database=EnterpriseDb;");
        tenant.SchemaName.ShouldBe("enterprise_schema");
    }

    #endregion

    #region HasDedicatedDatabase Tests

    [Fact]
    public void HasDedicatedDatabase_WhenDatabasePerTenantWithConnectionString_ReturnsTrue()
    {
        // Arrange
        var tenant = new TenantInfo(
            "t1", "Tenant",
            TenantIsolationStrategy.DatabasePerTenant,
            ConnectionString: "Server=t1;");

        // Act & Assert
        tenant.HasDedicatedDatabase.ShouldBeTrue();
    }

    [Fact]
    public void HasDedicatedDatabase_WhenDatabasePerTenantWithoutConnectionString_ReturnsFalse()
    {
        // Arrange
        var tenant = new TenantInfo("t1", "Tenant", TenantIsolationStrategy.DatabasePerTenant);

        // Act & Assert
        tenant.HasDedicatedDatabase.ShouldBeFalse();
    }

    [Fact]
    public void HasDedicatedDatabase_WhenSharedSchema_ReturnsFalse()
    {
        // Arrange
        var tenant = new TenantInfo("t1", "Tenant", TenantIsolationStrategy.SharedSchema);

        // Act & Assert
        tenant.HasDedicatedDatabase.ShouldBeFalse();
    }

    [Fact]
    public void HasDedicatedDatabase_WhenSchemaPerTenant_ReturnsFalse()
    {
        // Arrange
        var tenant = new TenantInfo(
            "t1", "Tenant",
            TenantIsolationStrategy.SchemaPerTenant,
            ConnectionString: "Server=shared;"); // Connection string doesn't matter for schema strategy

        // Act & Assert
        tenant.HasDedicatedDatabase.ShouldBeFalse();
    }

    #endregion

    #region HasDedicatedSchema Tests

    [Fact]
    public void HasDedicatedSchema_WhenSchemaPerTenantWithSchemaName_ReturnsTrue()
    {
        // Arrange
        var tenant = new TenantInfo(
            "t1", "Tenant",
            TenantIsolationStrategy.SchemaPerTenant,
            SchemaName: "tenant_schema");

        // Act & Assert
        tenant.HasDedicatedSchema.ShouldBeTrue();
    }

    [Fact]
    public void HasDedicatedSchema_WhenSchemaPerTenantWithoutSchemaName_ReturnsFalse()
    {
        // Arrange
        var tenant = new TenantInfo("t1", "Tenant", TenantIsolationStrategy.SchemaPerTenant);

        // Act & Assert
        tenant.HasDedicatedSchema.ShouldBeFalse();
    }

    [Fact]
    public void HasDedicatedSchema_WhenSharedSchema_ReturnsFalse()
    {
        // Arrange
        var tenant = new TenantInfo("t1", "Tenant", TenantIsolationStrategy.SharedSchema);

        // Act & Assert
        tenant.HasDedicatedSchema.ShouldBeFalse();
    }

    [Fact]
    public void HasDedicatedSchema_WhenDatabasePerTenant_ReturnsFalse()
    {
        // Arrange
        var tenant = new TenantInfo(
            "t1", "Tenant",
            TenantIsolationStrategy.DatabasePerTenant,
            SchemaName: "some_schema"); // Schema name doesn't apply

        // Act & Assert
        tenant.HasDedicatedSchema.ShouldBeFalse();
    }

    #endregion

    #region UsesSharedSchema Tests

    [Fact]
    public void UsesSharedSchema_WhenSharedSchema_ReturnsTrue()
    {
        // Arrange
        var tenant = new TenantInfo("t1", "Tenant", TenantIsolationStrategy.SharedSchema);

        // Act & Assert
        tenant.UsesSharedSchema.ShouldBeTrue();
    }

    [Fact]
    public void UsesSharedSchema_WhenSchemaPerTenant_ReturnsFalse()
    {
        // Arrange
        var tenant = new TenantInfo("t1", "Tenant", TenantIsolationStrategy.SchemaPerTenant);

        // Act & Assert
        tenant.UsesSharedSchema.ShouldBeFalse();
    }

    [Fact]
    public void UsesSharedSchema_WhenDatabasePerTenant_ReturnsFalse()
    {
        // Arrange
        var tenant = new TenantInfo("t1", "Tenant", TenantIsolationStrategy.DatabasePerTenant);

        // Act & Assert
        tenant.UsesSharedSchema.ShouldBeFalse();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void TenantInfo_Equality_WhenSameValues_AreEqual()
    {
        // Arrange
        var tenant1 = new TenantInfo("t1", "Tenant", TenantIsolationStrategy.SharedSchema);
        var tenant2 = new TenantInfo("t1", "Tenant", TenantIsolationStrategy.SharedSchema);

        // Act & Assert
        tenant1.ShouldBe(tenant2);
        (tenant1 == tenant2).ShouldBeTrue();
    }

    [Fact]
    public void TenantInfo_Equality_WhenDifferentId_AreNotEqual()
    {
        // Arrange
        var tenant1 = new TenantInfo("t1", "Tenant", TenantIsolationStrategy.SharedSchema);
        var tenant2 = new TenantInfo("t2", "Tenant", TenantIsolationStrategy.SharedSchema);

        // Act & Assert
        tenant1.ShouldNotBe(tenant2);
        (tenant1 != tenant2).ShouldBeTrue();
    }

    #endregion
}
