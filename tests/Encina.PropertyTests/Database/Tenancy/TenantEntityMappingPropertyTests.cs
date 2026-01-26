using Encina.Tenancy;
using Shouldly;
using ADOMySQLTenancy = Encina.ADO.MySQL.Tenancy;
using ADOPostgreSQLTenancy = Encina.ADO.PostgreSQL.Tenancy;
using ADOSqliteTenancy = Encina.ADO.Sqlite.Tenancy;
using ADOSqlServerTenancy = Encina.ADO.SqlServer.Tenancy;
using DapperMySQLTenancy = Encina.Dapper.MySQL.Tenancy;
using DapperPostgreSQLTenancy = Encina.Dapper.PostgreSQL.Tenancy;
using DapperSqliteTenancy = Encina.Dapper.Sqlite.Tenancy;
using DapperSqlServerTenancy = Encina.Dapper.SqlServer.Tenancy;
using MongoDbTenancy = Encina.MongoDB.Tenancy;

namespace Encina.PropertyTests.Database.Tenancy;

/// <summary>
/// Property-based tests for TenantEntityMappingBuilder across all providers.
/// Verifies invariants that MUST hold for ALL entity configurations.
/// </summary>
[Trait("Category", "Property")]
public sealed class TenantEntityMappingPropertyTests
{
    #region ADO Provider Mapping Tests

    [Fact]
    public void Property_ADOSqliteMapping_HasTenantIdAlwaysExcludesFromUpdates()
    {
        // Property: HasTenantId MUST always add property to UpdateExcludedProperties
        var mapping = new ADOSqliteTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build();

        mapping.IsTenantEntity.ShouldBeTrue("Mapping with HasTenantId must be tenant entity");
        mapping.UpdateExcludedProperties.ShouldContain("TenantId", "TenantId must ALWAYS be excluded from updates");
    }

    [Fact]
    public void Property_ADOSqlServerMapping_HasTenantIdAlwaysExcludesFromUpdates()
    {
        var mapping = new ADOSqlServerTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build();

        mapping.IsTenantEntity.ShouldBeTrue();
        mapping.UpdateExcludedProperties.ShouldContain("TenantId");
    }

    [Fact]
    public void Property_ADOPostgreSQLMapping_HasTenantIdAlwaysExcludesFromUpdates()
    {
        var mapping = new ADOPostgreSQLTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build();

        mapping.IsTenantEntity.ShouldBeTrue();
        mapping.UpdateExcludedProperties.ShouldContain("TenantId");
    }

    [Fact]
    public void Property_ADOMySQLMapping_HasTenantIdAlwaysExcludesFromUpdates()
    {
        var mapping = new ADOMySQLTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build();

        mapping.IsTenantEntity.ShouldBeTrue();
        mapping.UpdateExcludedProperties.ShouldContain("TenantId");
    }

    #endregion

    #region Dapper Provider Mapping Tests

    [Fact]
    public void Property_DapperSqliteMapping_HasTenantIdAlwaysExcludesFromUpdates()
    {
        var mapping = new DapperSqliteTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build();

        mapping.IsTenantEntity.ShouldBeTrue();
        mapping.UpdateExcludedProperties.ShouldContain("TenantId");
    }

    [Fact]
    public void Property_DapperSqlServerMapping_HasTenantIdAlwaysExcludesFromUpdates()
    {
        var mapping = new DapperSqlServerTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build();

        mapping.IsTenantEntity.ShouldBeTrue();
        mapping.UpdateExcludedProperties.ShouldContain("TenantId");
    }

    [Fact]
    public void Property_DapperPostgreSQLMapping_HasTenantIdAlwaysExcludesFromUpdates()
    {
        var mapping = new DapperPostgreSQLTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build();

        mapping.IsTenantEntity.ShouldBeTrue();
        mapping.UpdateExcludedProperties.ShouldContain("TenantId");
    }

    [Fact]
    public void Property_DapperMySQLMapping_HasTenantIdAlwaysExcludesFromUpdates()
    {
        var mapping = new DapperMySQLTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build();

        mapping.IsTenantEntity.ShouldBeTrue();
        mapping.UpdateExcludedProperties.ShouldContain("TenantId");
    }

    #endregion

    #region MongoDB Provider Mapping Tests

    [Fact]
    public void Property_MongoDBMapping_HasTenantIdSetsIsTenantEntity()
    {
        // Property: HasTenantId MUST set IsTenantEntity to true
        var mapping = new MongoDbTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapField(o => o.CustomerId)
            .Build();

        mapping.IsTenantEntity.ShouldBeTrue("Mapping with HasTenantId must be tenant entity");
        mapping.TenantFieldName.ShouldBe("TenantId");
    }

    [Fact]
    public void Property_MongoDBMapping_GetTenantId_ReturnsCorrectValue()
    {
        var mapping = new MongoDbTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .Build();

        var order = new TenantTestOrder { TenantId = "tenant-mongo" };
        mapping.GetTenantId(order).ShouldBe("tenant-mongo");
    }

    [Fact]
    public void Property_MongoDBMapping_SetTenantId_SetsCorrectValue()
    {
        var mapping = new MongoDbTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .Build();

        var order = new TenantTestOrder();
        mapping.SetTenantId(order, "tenant-new");
        order.TenantId.ShouldBe("tenant-new");
    }

    #endregion

    #region Invariant Tests

    [Fact]
    public void Property_WithoutHasTenantId_IsTenantEntityIsFalse()
    {
        // Property: Without HasTenantId, IsTenantEntity must ALWAYS be false
        var adoSqlite = new ADOSqliteTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.CustomerId).Build();

        var dapperSqlite = new DapperSqliteTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.CustomerId).Build();

        adoSqlite.IsTenantEntity.ShouldBeFalse("Without HasTenantId, IsTenantEntity must be false");
        adoSqlite.TenantColumnName.ShouldBeNull("Without HasTenantId, TenantColumnName must be null");
        dapperSqlite.IsTenantEntity.ShouldBeFalse();
        dapperSqlite.TenantColumnName.ShouldBeNull();
    }

    [Theory]
    [InlineData("TenantId")]
    [InlineData("OrganizationId")]
    [InlineData("company_id")]
    public void Property_CustomTenantColumnName_IsRespected(string customColumnName)
    {
        // Property: Custom tenant column name MUST be stored correctly
        var mapping = new ADOSqliteTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId, customColumnName)
            .Build();

        mapping.TenantColumnName.ShouldBe(customColumnName);
    }

    [Fact]
    public void Property_GetTenantId_ReturnsCorrectValue()
    {
        // Property: GetTenantId must ALWAYS return the actual property value
        var mapping = new ADOSqliteTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .Build();

        var testTenantIds = new[] { "tenant-1", "tenant-abc", "org-123", "", null };

        foreach (var tenantId in testTenantIds)
        {
            var order = new TenantTestOrder { TenantId = tenantId! };
            mapping.GetTenantId(order).ShouldBe(tenantId);
        }
    }

    [Fact]
    public void Property_SetTenantId_SetsCorrectValue()
    {
        // Property: SetTenantId must ALWAYS set the property correctly
        var mapping = new ADOSqliteTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .Build();

        var testTenantIds = new[] { "tenant-1", "tenant-xyz", "org-999" };

        foreach (var tenantId in testTenantIds)
        {
            var order = new TenantTestOrder();
            mapping.SetTenantId(order, tenantId);
            order.TenantId.ShouldBe(tenantId);
        }
    }

    [Fact]
    public void Property_ColumnMappingsIncludeTenantId()
    {
        // Property: When HasTenantId is called, ColumnMappings must include the tenant column
        var mapping = new ADOSqliteTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .MapProperty(o => o.Total)
            .Build();

        mapping.ColumnMappings.ShouldContainKey("TenantId");
        mapping.ColumnMappings.ShouldContainKey("Id");
        mapping.ColumnMappings.ShouldContainKey("CustomerId");
        mapping.ColumnMappings.ShouldContainKey("Total");
    }

    [Fact]
    public void Property_BuildWithoutTableName_ThrowsInvalidOperationException()
    {
        // Property: Build without ToTable MUST throw InvalidOperationException
        var builder = new ADOSqliteTenancy.TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId);

        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Table name must be specified");
    }

    #endregion
}

/// <summary>
/// Shared test entity for property tests.
/// </summary>
public sealed class TenantTestOrder : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
