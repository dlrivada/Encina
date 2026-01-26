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
        var adoSqliteMapping = new ADOSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();
        var adoPostgresMapping = new ADOPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();
        var adoMySQLMapping = new ADOMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();
        var dapperSqliteMapping = new DapperSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();
        var dapperPostgresMapping = new DapperPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();
        var dapperMySQLMapping = new DapperMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();

        // All must implement their respective ITenantEntityMapping interfaces
        adoSqliteMapping.ShouldBeAssignableTo<ADOSqliteTenancy.ITenantEntityMapping<ContractTestOrder, Guid>>();
        adoPostgresMapping.ShouldBeAssignableTo<ADOPostgreSQLTenancy.ITenantEntityMapping<ContractTestOrder, Guid>>();
        adoMySQLMapping.ShouldBeAssignableTo<ADOMySQLTenancy.ITenantEntityMapping<ContractTestOrder, Guid>>();
        dapperSqliteMapping.ShouldBeAssignableTo<DapperSqliteTenancy.ITenantEntityMapping<ContractTestOrder, Guid>>();
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

        var adoSqlite = new ADOSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();
        var adoSqlServer = new ADOSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();
        var adoPostgres = new ADOPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();
        var adoMySQL = new ADOMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();

        // Verify consistent behavior
        adoSqlite.IsTenantEntity.ShouldBeTrue("ADO.SQLite");
        adoSqlServer.IsTenantEntity.ShouldBeTrue("ADO.SqlServer");
        adoPostgres.IsTenantEntity.ShouldBeTrue("ADO.PostgreSQL");
        adoMySQL.IsTenantEntity.ShouldBeTrue("ADO.MySQL");

        adoSqlite.GetTenantId(testOrder).ShouldBe("test-tenant-123");
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

        var dapperSqlite = new DapperSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();
        var dapperSqlServer = new DapperSqlServerTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();
        var dapperPostgres = new DapperPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();
        var dapperMySQL = new DapperMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).MapProperty(o => o.Amount).Build();

        // Verify consistent behavior
        dapperSqlite.IsTenantEntity.ShouldBeTrue("Dapper.SQLite");
        dapperSqlServer.IsTenantEntity.ShouldBeTrue("Dapper.SqlServer");
        dapperPostgres.IsTenantEntity.ShouldBeTrue("Dapper.PostgreSQL");
        dapperMySQL.IsTenantEntity.ShouldBeTrue("Dapper.MySQL");

        dapperSqlite.GetTenantId(testOrder).ShouldBe("test-tenant-123");
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

        new ADOSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build()
            .SetTenantId(order1, "tenant-1");

        new ADOPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build()
            .SetTenantId(order2, "tenant-2");

        new ADOMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build()
            .SetTenantId(order3, "tenant-3");

        new DapperSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build()
            .SetTenantId(order4, "tenant-4");

        new DapperPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build()
            .SetTenantId(order5, "tenant-5");

        new DapperMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build()
            .SetTenantId(order6, "tenant-6");

        order1.TenantId.ShouldBe("tenant-1", "ADO.SQLite");
        order2.TenantId.ShouldBe("tenant-2", "ADO.PostgreSQL");
        order3.TenantId.ShouldBe("tenant-3", "ADO.MySQL");
        order4.TenantId.ShouldBe("tenant-4", "Dapper.SQLite");
        order5.TenantId.ShouldBe("tenant-5", "Dapper.PostgreSQL");
        order6.TenantId.ShouldBe("tenant-6", "Dapper.MySQL");
    }

    [Fact]
    public void Contract_AllProviders_TenantEntityMapping_ExcludeTenantIdFromUpdates()
    {
        // Contract: TenantId MUST always be excluded from updates for security
        new ADOSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build()
            .UpdateExcludedProperties.ShouldContain("TenantId", "ADO.SQLite");

        new ADOPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build()
            .UpdateExcludedProperties.ShouldContain("TenantId", "ADO.PostgreSQL");

        new ADOMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build()
            .UpdateExcludedProperties.ShouldContain("TenantId", "ADO.MySQL");

        new DapperSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build()
            .UpdateExcludedProperties.ShouldContain("TenantId", "Dapper.SQLite");

        new DapperPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build()
            .UpdateExcludedProperties.ShouldContain("TenantId", "Dapper.PostgreSQL");

        new DapperMySQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).HasTenantId(o => o.TenantId).Build()
            .UpdateExcludedProperties.ShouldContain("TenantId", "Dapper.MySQL");

    }

    #endregion

    #region TenancyOptions Contract Tests

    [Fact]
    public void Contract_AllADOProviders_HaveIdenticalOptionDefaults()
    {
        // Contract: All ADO providers must have identical default option values
        var adoSqlite = new ADOSqliteTenancy.ADOTenancyOptions();
        var adoPostgres = new ADOPostgreSQLTenancy.ADOTenancyOptions();
        var adoMySQL = new ADOMySQLTenancy.ADOTenancyOptions();

        // All must have same AutoFilterTenantQueries default
        adoSqlite.AutoFilterTenantQueries.ShouldBe(adoPostgres.AutoFilterTenantQueries);
        adoPostgres.AutoFilterTenantQueries.ShouldBe(adoMySQL.AutoFilterTenantQueries);

        // All must have same TenantColumnName default
        adoSqlite.TenantColumnName.ShouldBe(adoPostgres.TenantColumnName);
        adoPostgres.TenantColumnName.ShouldBe(adoMySQL.TenantColumnName);
    }

    [Fact]
    public void Contract_AllDapperProviders_HaveIdenticalOptionDefaults()
    {
        // Contract: All Dapper providers must have identical default option values
        var dapperSqlite = new DapperSqliteTenancy.DapperTenancyOptions();
        var dapperPostgres = new DapperPostgreSQLTenancy.DapperTenancyOptions();
        var dapperMySQL = new DapperMySQLTenancy.DapperTenancyOptions();

        // All must have same AutoFilterTenantQueries default
        dapperSqlite.AutoFilterTenantQueries.ShouldBe(dapperPostgres.AutoFilterTenantQueries);
        dapperPostgres.AutoFilterTenantQueries.ShouldBe(dapperMySQL.AutoFilterTenantQueries);

        // All must have same TenantColumnName default
        dapperSqlite.TenantColumnName.ShouldBe(dapperPostgres.TenantColumnName);
        dapperPostgres.TenantColumnName.ShouldBe(dapperMySQL.TenantColumnName);
    }

    [Fact]
    public void Contract_ADOAndDapper_HaveEquivalentOptionDefaults()
    {
        // Contract: ADO and Dapper options must have equivalent security defaults
        var adoOptions = new ADOSqliteTenancy.ADOTenancyOptions();
        var dapperOptions = new DapperSqliteTenancy.DapperTenancyOptions();

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
        var adoSqlite = new ADOSqliteTenancy.ADOTenancyOptions();
        var dapperSqlite = new DapperSqliteTenancy.DapperTenancyOptions();

        // Security defaults
        adoSqlite.AutoFilterTenantQueries.ShouldBeTrue("AutoFilterTenantQueries must be true by default for security");
        adoSqlite.ValidateTenantOnModify.ShouldBeTrue("ValidateTenantOnModify must be true by default for security");
        adoSqlite.ThrowOnMissingTenantContext.ShouldBeTrue("ThrowOnMissingTenantContext must be true to prevent data leaks");

        dapperSqlite.AutoFilterTenantQueries.ShouldBeTrue("AutoFilterTenantQueries must be true by default for security");
        dapperSqlite.ValidateTenantOnModify.ShouldBeTrue("ValidateTenantOnModify must be true by default for security");
        dapperSqlite.ThrowOnMissingTenantContext.ShouldBeTrue("ThrowOnMissingTenantContext must be true to prevent data leaks");
    }

    #endregion

    #region Non-Tenant Entity Contract Tests

    [Fact]
    public void Contract_AllProviders_NonTenantEntity_HasCorrectFlags()
    {
        // Contract: Entities without HasTenantId must have IsTenantEntity = false
        new ADOSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build()
            .IsTenantEntity.ShouldBeFalse("ADO.SQLite");

        new ADOPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build()
            .IsTenantEntity.ShouldBeFalse("ADO.PostgreSQL");

        new DapperSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build()
            .IsTenantEntity.ShouldBeFalse("Dapper.SQLite");

        new DapperPostgreSQLTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build()
            .IsTenantEntity.ShouldBeFalse("Dapper.PostgreSQL");
    }

    [Fact]
    public void Contract_AllProviders_NonTenantEntity_GetTenantIdReturnsNull()
    {
        // Contract: GetTenantId on non-tenant entity must return null
        var order = new ContractTestOrder { TenantId = "some-tenant" };

        new ADOSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build()
            .GetTenantId(order).ShouldBeNull("ADO.SQLite");

        new DapperSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build()
            .GetTenantId(order).ShouldBeNull("Dapper.SQLite");
    }

    [Fact]
    public void Contract_AllProviders_NonTenantEntity_SetTenantIdThrows()
    {
        // Contract: SetTenantId on non-tenant entity must throw InvalidOperationException
        var order = new ContractTestOrder();

        var adoSqlite = new ADOSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build();
        Should.Throw<InvalidOperationException>(() => adoSqlite.SetTenantId(order, "tenant"));

        var dapperSqlite = new DapperSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
            .ToTable("Orders").HasId(o => o.Id).MapProperty(o => o.Amount).Build();
        Should.Throw<InvalidOperationException>(() => dapperSqlite.SetTenantId(order, "tenant"));
    }

    #endregion

    #region Validation Contract Tests

    [Fact]
    public void Contract_AllProviders_RequireTableName()
    {
        // Contract: Build without ToTable must throw InvalidOperationException
        Should.Throw<InvalidOperationException>(() =>
            new ADOSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
                .HasId(o => o.Id)
                .HasTenantId(o => o.TenantId)
                .Build())
            .Message.ShouldContain("Table name");

        Should.Throw<InvalidOperationException>(() =>
            new DapperSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
                .HasId(o => o.Id)
                .HasTenantId(o => o.TenantId)
                .Build())
            .Message.ShouldContain("Table name");
    }

    [Fact]
    public void Contract_AllProviders_RequirePrimaryKey()
    {
        // Contract: Build without HasId must throw InvalidOperationException
        Should.Throw<InvalidOperationException>(() =>
            new ADOSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
                .ToTable("Orders")
                .HasTenantId(o => o.TenantId)
                .Build())
            .Message.ShouldContain("Primary key");

        Should.Throw<InvalidOperationException>(() =>
            new DapperSqliteTenancy.TenantEntityMappingBuilder<ContractTestOrder, Guid>()
                .ToTable("Orders")
                .HasTenantId(o => o.TenantId)
                .Build())
            .Message.ShouldContain("Primary key");
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
            .Build();

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
            .Build();

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
