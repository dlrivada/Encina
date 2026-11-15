using System.Text.Json;
using Encina.Tenancy;
using Microsoft.Extensions.Configuration;

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

    #region Secret Protection Tests

    private const string SecretConnectionString = "Server=db;User Id=sa;Password=secret;";

    private static TenantInfo CreateTenantWithSecret() =>
        new("t1", "Acme", TenantIsolationStrategy.DatabasePerTenant, SecretConnectionString, "acme_schema");

    [Fact]
    public void ToString_WithConnectionString_MasksTheConnectionString()
    {
        // Arrange
        var tenant = CreateTenantWithSecret();

        // Act
        var text = tenant.ToString();

        // Assert
        text.ShouldNotContain("secret");
        text.ShouldNotContain("Server=db");
        text.ShouldNotContain("Password");
        text.ShouldContain("ConnectionString = ***");
        text.ShouldBe(
            "TenantInfo { TenantId = t1, Name = Acme, Strategy = DatabasePerTenant, SchemaName = acme_schema, ConnectionString = *** }");
    }

    [Fact]
    public void StringInterpolation_WithConnectionString_MasksTheConnectionString()
    {
        // Arrange
        var tenant = CreateTenantWithSecret();

        // Act
        var text = $"Resolved {tenant}";

        // Assert
        text.ShouldNotContain("secret");
        text.ShouldNotContain("Server=db");
        text.ShouldContain("ConnectionString = ***");
    }

    [Fact]
    public void ToString_WithoutConnectionString_OmitsTheConnectionString()
    {
        // Arrange
        var tenant = new TenantInfo("t2", "Contoso", TenantIsolationStrategy.SchemaPerTenant, SchemaName: "contoso");

        // Act
        var text = tenant.ToString();

        // Assert
        text.ShouldBe("TenantInfo { TenantId = t2, Name = Contoso, Strategy = SchemaPerTenant, SchemaName = contoso }");
        text.ShouldNotContain("ConnectionString");
    }

    [Fact]
    public void JsonSerialize_WithConnectionString_ExcludesTheConnectionString()
    {
        // Arrange
        var tenant = CreateTenantWithSecret();

        // Act
        var json = JsonSerializer.Serialize(tenant);

        // Assert
        json.ShouldNotContain("ConnectionString", Case.Insensitive);
        json.ShouldNotContain("Password");
        json.ShouldNotContain("secret");
        json.ShouldContain("\"TenantId\":\"t1\"");
    }

    [Fact]
    public void JsonRoundTrip_WithConnectionString_LosesTheConnectionString()
    {
        // Arrange
        var tenant = CreateTenantWithSecret();
        var json = JsonSerializer.Serialize(tenant);

        // Act
        var restored = JsonSerializer.Deserialize<TenantInfo>(json);

        // Assert
        restored.ShouldNotBeNull();
        restored.ConnectionString.ShouldBeNull();
        restored.TenantId.ShouldBe("t1");
        restored.Name.ShouldBe("Acme");
        restored.Strategy.ShouldBe(TenantIsolationStrategy.DatabasePerTenant);
        restored.SchemaName.ShouldBe("acme_schema");
    }

    [Fact]
    public void ConfigurationBinding_TenancyOptionsTenants_StillBindsTheConnectionString()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tenancy:Tenants:0:TenantId"] = "t1",
                ["Tenancy:Tenants:0:Name"] = "Acme",
                ["Tenancy:Tenants:0:Strategy"] = "DatabasePerTenant",
                ["Tenancy:Tenants:0:ConnectionString"] = SecretConnectionString,
            })
            .Build();
        var options = new TenancyOptions();

        // Act
        configuration.GetSection("Tenancy").Bind(options);

        // Assert
        options.Tenants.Count.ShouldBe(1);
        options.Tenants[0].TenantId.ShouldBe("t1");
        options.Tenants[0].Strategy.ShouldBe(TenantIsolationStrategy.DatabasePerTenant);
        options.Tenants[0].ConnectionString.ShouldBe(SecretConnectionString);
    }

    #endregion
}
