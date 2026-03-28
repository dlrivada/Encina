using Encina.Tenancy;
using Encina.Testing.Shouldly;
using Shouldly;
using ADOMySQLTenancy = Encina.ADO.MySQL.Tenancy;
using ADOPostgreSQLTenancy = Encina.ADO.PostgreSQL.Tenancy;
using ADOSqlServerTenancy = Encina.ADO.SqlServer.Tenancy;
using DapperMySQLTenancy = Encina.Dapper.MySQL.Tenancy;
using DapperPostgreSQLTenancy = Encina.Dapper.PostgreSQL.Tenancy;
using DapperSqlServerTenancy = Encina.Dapper.SqlServer.Tenancy;
using EfCoreTenancy = Encina.EntityFrameworkCore.Tenancy;
using MongoDbTenancy = Encina.MongoDB.Tenancy;

namespace Encina.ContractTests.Database.Tenancy;

/// <summary>
/// Contract tests verifying that all Tenancy implementations follow the same interface contracts.
/// These tests ensure behavioral consistency across all providers.
/// </summary>
[Trait("Category", "Contract")]
public sealed class TenancyContractTests
{
    #region ITenantEntityMapping Contract Tests

    [Fact]
    public void Contract_AllProviders_TenantEntityMapping_ImplementCorrectInterface()
    {
        // Contract: All TenantEntityMapping implementations must implement ITenantEntityMapping interface from their provider
        var ADOSqlServerMapping = new ADOSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        var adoPostgresMapping = new ADOPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        var adoMySQLMapping = new ADOMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        var DapperSqlServerMapping = new DapperSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        var dapperPostgresMapping = new DapperPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        var dapperMySQLMapping = new DapperMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();

        // All must implement their respective ITenantEntityMapping interfaces
        ADOSqlServerMapping.ShouldBeAssignableTo<ADOSqlServerTenancy.ITenantEntityMapping<ContractTestOrder, Guid>>();
        adoPostgresMapping.ShouldBeAssignableTo<ADOPostgreSQLTenancy.ITenantEntityMapping<ContractTestOrder, Guid>>();
        adoMySQLMapping.ShouldBeAssignableTo<ADOMySQLTenancy.ITenantEntityMapping<ContractTestOrder, Guid>>();
        DapperSqlServerMapping.ShouldBeAssignableTo<DapperSqlServerTenancy.ITenantEntityMapping<ContractTestOrder, Guid>>();
        dapperPostgresMapping.ShouldBeAssignableTo<DapperPostgreSQLTenancy.ITenantEntityMapping<ContractTestOrder, Guid>>();
        dapperMySQLMapping.ShouldBeAssignableTo<DapperMySQLTenancy.ITenantEntityMapping<ContractTestOrder, Guid>>();
    }

    [Fact]
    public void Contract_ADO_TenantEntityMapping_HaveConsistentBehavior()
    {
        // Contract: All ADO providers MUST have identical mapping behavior
        var testOrder = new ContractTestOrder
        {
            Id = Guid.NewGuid(),
            TenantId = "test-tenant-123",
            Amount = 100m
        };

        var ADOSqlServer = new ADOSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        var adoSqlServer = new ADOSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        var adoPostgres = new ADOPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        var adoMySQL = new ADOMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();

        // Verify consistent behavior
        ADOSqlServer.IsTenantEntity.ShouldBeTrue("ADO.SqlServer");
        adoSqlServer.IsTenantEntity.ShouldBeTrue("ADO.SqlServer");
        adoPostgres.IsTenantEntity.ShouldBeTrue("ADO.PostgreSQL");
        adoMySQL.IsTenantEntity.ShouldBeTrue("ADO.MySQL");

        ADOSqlServer.GetTenantId(testOrder).ShouldBe("test-tenant-123");
        adoSqlServer.GetTenantId(testOrder).ShouldBe("test-tenant-123");
        adoPostgres.GetTenantId(testOrder).ShouldBe("test-tenant-123");
        adoMySQL.GetTenantId(testOrder).ShouldBe("test-tenant-123");
    }

    [Fact]
    public void Contract_Dapper_TenantEntityMapping_HaveConsistentBehavior()
    {
        // Contract: All Dapper providers MUST have identical mapping behavior
        var testOrder = new ContractTestOrder
        {
            Id = Guid.NewGuid(),
            TenantId = "test-tenant-123",
            Amount = 100m
        };

        var DapperSqlServer = new DapperSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        var dapperSqlServer = new DapperSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        var dapperPostgres = new DapperPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        var dapperMySQL = new DapperMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build().ShouldBeSuccess();

        // Verify consistent behavior
        DapperSqlServer.IsTenantEntity.ShouldBeTrue("Dapper.SqlServer");
        dapperSqlServer.IsTenantEntity.ShouldBeTrue("Dapper.SqlServer");
        dapperPostgres.IsTenantEntity.ShouldBeTrue("Dapper.PostgreSQL");
        dapperMySQL.IsTenantEntity.ShouldBeTrue("Dapper.MySQL");

        DapperSqlServer.GetTenantId(testOrder).ShouldBe("test-tenant-123");
        dapperSqlServer.GetTenantId(testOrder).ShouldBe("test-tenant-123");
        dapperPostgres.GetTenantId(testOrder).ShouldBe("test-tenant-123");
        dapperMySQL.GetTenantId(testOrder).ShouldBe("test-tenant-123");
    }

    [Fact]
    public void Contract_AllProviders_TenantEntityMapping_SetTenantIdWorks()
    {
        // Contract: SetTenantId must work identically across all providers
        var order1 = new ContractTestOrder();
        var order2 = new ContractTestOrder();
        var order3 = new ContractTestOrder();
        var order4 = new ContractTestOrder();
        var order5 = new ContractTestOrder();
        var order6 = new ContractTestOrder();

        new ADOSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build().ShouldBeSuccess()
            .SetTenantId(order1, "tenant-1");

        new ADOPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build().ShouldBeSuccess()
            .SetTenantId(order2, "tenant-2");

        new ADOMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build().ShouldBeSuccess()
            .SetTenantId(order3, "tenant-3");

        new DapperSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build().ShouldBeSuccess()
            .SetTenantId(order4, "tenant-4");

        new DapperPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build().ShouldBeSuccess()
            .SetTenantId(order5, "tenant-5");

        new DapperMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build().ShouldBeSuccess()
            .SetTenantId(order6, "tenant-6");

        order1.TenantId.ShouldBe("tenant-1", "ADO.SqlServer");
        order2.TenantId.ShouldBe("tenant-2", "ADO.PostgreSQL");
        order3.TenantId.ShouldBe("tenant-3", "ADO.MySQL");
        order4.TenantId.ShouldBe("tenant-4", "Dapper.SqlServer");
        order5.TenantId.ShouldBe("tenant-5", "Dapper.PostgreSQL");
        order6.TenantId.ShouldBe("tenant-6", "Dapper.MySQL");
    }

    [Fact]
    public void Contract_AllProviders_TenantEntityMapping_ExcludeTenantIdFromUpdates()
    {
        // Contract: TenantId MUST always be excluded from updates for security
        new ADOSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build().ShouldBeSuccess()
            .UpdateExcludedProperties.ShouldContain("TenantId", "ADO.SqlServer");

        new ADOPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build().ShouldBeSuccess()
            .UpdateExcludedProperties.ShouldContain("TenantId", "ADO.PostgreSQL");

        new ADOMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build().ShouldBeSuccess()
            .UpdateExcludedProperties.ShouldContain("TenantId", "ADO.MySQL");

        new DapperSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build().ShouldBeSuccess()
            .UpdateExcludedProperties.ShouldContain("TenantId", "Dapper.SqlServer");

        new DapperPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build().ShouldBeSuccess()
            .UpdateExcludedProperties.ShouldContain("TenantId", "Dapper.PostgreSQL");

        new DapperMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build().ShouldBeSuccess()
            .UpdateExcludedProperties.ShouldContain("TenantId", "Dapper.MySQL");

    }

    #endregion

    #region TenancyOptions Contract Tests

    [Fact]
    public void Contract_AllADOProviders_HaveIdenticalOptionDefaults()
    {
        // Contract: All ADO providers must have identical default option values
        var ADOSqlServer = new ADOSqlServerTenancy.ADOTenancyOptions();
        var adoPostgres = new ADOPostgreSQLTenancy.ADOTenancyOptions();
        var adoMySQL = new ADOMySQLTenancy.ADOTenancyOptions();

        // All must have same AutoFilterTenantQueries default
        ADOSqlServer.AutoFilterTenantQueries.ShouldBe(adoPostgres.AutoFilterTenantQueries);
        adoPostgres.AutoFilterTenantQueries.ShouldBe(adoMySQL.AutoFilterTenantQueries);

        // All must have same TenantColumnName default
        ADOSqlServer.TenantColumnName.ShouldBe(adoPostgres.TenantColumnName);
        adoPostgres.TenantColumnName.ShouldBe(adoMySQL.TenantColumnName);
    }

    [Fact]
    public void Contract_AllDapperProviders_HaveIdenticalOptionDefaults()
    {
        // Contract: All Dapper providers must have identical default option values
        var DapperSqlServer = new DapperSqlServerTenancy.DapperTenancyOptions();
        var dapperPostgres = new DapperPostgreSQLTenancy.DapperTenancyOptions();
        var dapperMySQL = new DapperMySQLTenancy.DapperTenancyOptions();

        // All must have same AutoFilterTenantQueries default
        DapperSqlServer.AutoFilterTenantQueries.ShouldBe(dapperPostgres.AutoFilterTenantQueries);
        dapperPostgres.AutoFilterTenantQueries.ShouldBe(dapperMySQL.AutoFilterTenantQueries);

        // All must have same TenantColumnName default
        DapperSqlServer.TenantColumnName.ShouldBe(dapperPostgres.TenantColumnName);
        dapperPostgres.TenantColumnName.ShouldBe(dapperMySQL.TenantColumnName);
    }

    [Fact]
    public void Contract_ADOAndDapper_HaveEquivalentOptionDefaults()
    {
        // Contract: ADO and Dapper options must have equivalent security defaults
        var adoOptions = new ADOSqlServerTenancy.ADOTenancyOptions();
        var dapperOptions = new DapperSqlServerTenancy.DapperTenancyOptions();

        adoOptions.AutoFilterTenantQueries.ShouldBe(dapperOptions.AutoFilterTenantQueries, "AutoFilterTenantQueries");
        adoOptions.AutoAssignTenantId.ShouldBe(dapperOptions.AutoAssignTenantId, "AutoAssignTenantId");
        adoOptions.ValidateTenantOnModify.ShouldBe(dapperOptions.ValidateTenantOnModify, "ValidateTenantOnModify");
        adoOptions.ThrowOnMissingTenantContext.ShouldBe(dapperOptions.ThrowOnMissingTenantContext, "ThrowOnMissingTenantContext");
        adoOptions.TenantColumnName.ShouldBe(dapperOptions.TenantColumnName, "TenantColumnName");
    }

    [Fact]
    public void Contract_AllProviders_SecurityDefaultsAreEnabled()
    {
        // Contract: All security-related options MUST default to true
        var ADOSqlServer = new ADOSqlServerTenancy.ADOTenancyOptions();
        var DapperSqlServer = new DapperSqlServerTenancy.DapperTenancyOptions();

        // Security defaults
        ADOSqlServer.AutoFilterTenantQueries.ShouldBeTrue("AutoFilterTenantQueries must be true by default for security");
        ADOSqlServer.ValidateTenantOnModify.ShouldBeTrue("ValidateTenantOnModify must be true by default for security");
        ADOSqlServer.ThrowOnMissingTenantContext.ShouldBeTrue("ThrowOnMissingTenantContext must be true to prevent data leaks");

        DapperSqlServer.AutoFilterTenantQueries.ShouldBeTrue("AutoFilterTenantQueries must be true by default for security");
        DapperSqlServer.ValidateTenantOnModify.ShouldBeTrue("ValidateTenantOnModify must be true by default for security");
        DapperSqlServer.ThrowOnMissingTenantContext.ShouldBeTrue("ThrowOnMissingTenantContext must be true to prevent data leaks");
    }

    #endregion

    #region Non-Tenant Entity Contract Tests

    [Fact]
    public void Contract_AllProviders_NonTenantEntity_HasCorrectFlags()
    {
        // Contract: Entities without HasTenantId must have IsTenantEntity = false
        new ADOSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build().ShouldBeSuccess()
            .IsTenantEntity.ShouldBeFalse("ADO.SqlServer");

        new ADOPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build().ShouldBeSuccess()
            .IsTenantEntity.ShouldBeFalse("ADO.PostgreSQL");

        new DapperSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build().ShouldBeSuccess()
            .IsTenantEntity.ShouldBeFalse("Dapper.SqlServer");

        new DapperPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build().ShouldBeSuccess()
            .IsTenantEntity.ShouldBeFalse("Dapper.PostgreSQL");
    }

    [Fact]
    public void Contract_AllProviders_NonTenantEntity_GetTenantIdReturnsNull()
    {
        // Contract: GetTenantId on non-tenant entity must return null
        var order = new ContractTestOrder { TenantId = "some-tenant" };

        new ADOSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build().ShouldBeSuccess()
            .GetTenantId(order).ShouldBeNull("ADO.SqlServer");

        new DapperSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build().ShouldBeSuccess()
            .GetTenantId(order).ShouldBeNull("Dapper.SqlServer");
    }

    [Fact]
    public void Contract_AllProviders_NonTenantEntity_SetTenantIdThrows()
    {
        // Contract: SetTenantId on non-tenant entity must throw InvalidOperationException
        var order = new ContractTestOrder();

        var ADOSqlServer = new ADOSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        Should.Throw<InvalidOperationException>(() => ADOSqlServer.SetTenantId(order, "tenant"));

        var DapperSqlServer = new DapperSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build().ShouldBeSuccess();
        Should.Throw<InvalidOperationException>(() => DapperSqlServer.SetTenantId(order, "tenant"));
    }

    #endregion

    #region Validation Contract Tests

    [Fact]
    public void Contract_AllProviders_RequireTableName()
    {
        // Contract: Build without ToTable must return an error
        new ADOSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .Build()
            .ShouldBeErrorContaining("Table name");

        new DapperSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .Build()
            .ShouldBeErrorContaining("Table name");
    }

    [Fact]
    public void Contract_AllProviders_RequirePrimaryKey()
    {
        // Contract: Build without HasId must return an error
        new ADOSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders")
            .HasTenantId(o => o.TenantId)
            .Build()
            .ShouldBeErrorContaining("Primary key");

        new DapperSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders")
            .HasTenantId(o => o.TenantId)
            .Build()
            .ShouldBeErrorContaining("Primary key");
    }

    #endregion

    #region MongoDB Contract Tests

    [Fact]
    public void Contract_MongoDB_TenantEntityMapping_HasCorrectBehavior()
    {
        // Contract: MongoDB mapping MUST have consistent behavior with other providers
        var testOrder = new ContractTestOrder
        {
            Id = Guid.NewGuid(),
            TenantId = "test-tenant-mongo",
            Amount = 100m
        };

        var mongoMapping = new MongoDbTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapField(o => o.Amount)
            .Build()
            .ShouldBeSuccess();

        mongoMapping.IsTenantEntity.ShouldBeTrue("MongoDB");
        mongoMapping.GetTenantId(testOrder).ShouldBe("test-tenant-mongo");
        mongoMapping.TenantFieldName.ShouldBe("TenantId");
    }

    [Fact]
    public void Contract_MongoDB_TenantEntityMapping_SetTenantIdWorks()
    {
        // Contract: MongoDB SetTenantId must work like other providers
        var order = new ContractTestOrder();

        var mongoMapping = new MongoDbTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .Build()
            .ShouldBeSuccess();

        mongoMapping.SetTenantId(order, "tenant-mongo");
        order.TenantId.ShouldBe("tenant-mongo");
    }

    [Fact]
    public void Contract_MongoDB_NonTenantEntity_HasCorrectFlags()
    {
        // Contract: MongoDB non-tenant entity must have IsTenantEntity = false
        new MongoDbTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .MapField(o => o.Amount)
            .Build()
            .ShouldBeSuccess()
            .IsTenantEntity.ShouldBeFalse("MongoDB");
    }

    [Fact]
    public void Contract_MongoDbOptions_HaveSecureDefaults()
    {
        // Contract: MongoDB tenancy options must have secure defaults
        var options = new MongoDbTenancy.MongoDbTenancyOptions();

        options.AutoFilterTenantQueries.ShouldBeTrue("AutoFilterTenantQueries must be true by default");
        options.AutoAssignTenantId.ShouldBeTrue("AutoAssignTenantId must be true by default");
        options.ValidateTenantOnModify.ShouldBeTrue("ValidateTenantOnModify must be true by default");
        options.ThrowOnMissingTenantContext.ShouldBeTrue("ThrowOnMissingTenantContext must be true by default");
    }

    #endregion

    #region EF Core Contract Tests

    [Fact]
    public void Contract_EfCoreOptions_HaveSecureDefaults()
    {
        // Contract: EF Core tenancy options must have secure defaults
        var options = new EfCoreTenancy.EfCoreTenancyOptions();

        options.AutoAssignTenantId.ShouldBeTrue("AutoAssignTenantId must be true by default");
        options.ValidateTenantOnSave.ShouldBeTrue("ValidateTenantOnSave must be true by default");
        options.UseQueryFilters.ShouldBeTrue("UseQueryFilters must be true by default");
        options.ThrowOnMissingTenantContext.ShouldBeTrue("ThrowOnMissingTenantContext must be true by default");
    }

    [Fact]
    public void Contract_EfCoreOptions_CanDisableAllOptions()
    {
        // Contract: All EF Core options must be independently toggleable
        var options = new EfCoreTenancy.EfCoreTenancyOptions
        {
            AutoAssignTenantId = false,
            ValidateTenantOnSave = false,
            UseQueryFilters = false,
            ThrowOnMissingTenantContext = false
        };

        options.AutoAssignTenantId.ShouldBeFalse();
        options.ValidateTenantOnSave.ShouldBeFalse();
        options.UseQueryFilters.ShouldBeFalse();
        options.ThrowOnMissingTenantContext.ShouldBeFalse();
    }

    #endregion
}

/// <summary>
/// Test entity for contract tests.
/// </summary>
public sealed class ContractTestOrder : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
