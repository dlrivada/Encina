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

namespace Encina.PropertyTests.Database.Tenancy;

/// <summary>
/// Property-based tests for Tenancy Options across all providers.
/// Verifies invariants that MUST hold for ALL possible configurations.
/// </summary>
[Trait("Category", "Property")]
public sealed class TenancyOptionsPropertyTests
{
    #region ADO Provider Options Tests

    [Fact]
    public void Property_ADOSqliteOptions_DefaultsAreConsistent()
    {
        // Property: Default options MUST have consistent secure defaults
        var options = new ADOSqliteTenancy.ADOTenancyOptions();

        // Invariants: All security options default to true
        options.AutoFilterTenantQueries.ShouldBeTrue("AutoFilter must default to true for security");
        options.AutoAssignTenantId.ShouldBeTrue("AutoAssign must default to true for convenience");
        options.ValidateTenantOnModify.ShouldBeTrue("Validation must default to true for security");
        options.ThrowOnMissingTenantContext.ShouldBeTrue("Throw must default to true to prevent data leaks");
        options.TenantColumnName.ShouldBe("TenantId", "Default column name must be 'TenantId'");
    }

    [Fact]
    public void Property_ADOSqlServerOptions_DefaultsAreConsistent()
    {
        var options = new ADOSqlServerTenancy.ADOTenancyOptions();

        options.AutoFilterTenantQueries.ShouldBeTrue();
        options.AutoAssignTenantId.ShouldBeTrue();
        options.ValidateTenantOnModify.ShouldBeTrue();
        options.ThrowOnMissingTenantContext.ShouldBeTrue();
        options.TenantColumnName.ShouldBe("TenantId");
    }

    [Fact]
    public void Property_ADOPostgreSQLOptions_DefaultsAreConsistent()
    {
        var options = new ADOPostgreSQLTenancy.ADOTenancyOptions();

        options.AutoFilterTenantQueries.ShouldBeTrue();
        options.AutoAssignTenantId.ShouldBeTrue();
        options.ValidateTenantOnModify.ShouldBeTrue();
        options.ThrowOnMissingTenantContext.ShouldBeTrue();
        options.TenantColumnName.ShouldBe("TenantId");
    }

    [Fact]
    public void Property_ADOMySQLOptions_DefaultsAreConsistent()
    {
        var options = new ADOMySQLTenancy.ADOTenancyOptions();

        options.AutoFilterTenantQueries.ShouldBeTrue();
        options.AutoAssignTenantId.ShouldBeTrue();
        options.ValidateTenantOnModify.ShouldBeTrue();
        options.ThrowOnMissingTenantContext.ShouldBeTrue();
        options.TenantColumnName.ShouldBe("TenantId");
    }

    #endregion

    #region Dapper Provider Options Tests

    [Fact]
    public void Property_DapperSqliteOptions_DefaultsAreConsistent()
    {
        var options = new DapperSqliteTenancy.DapperTenancyOptions();

        options.AutoFilterTenantQueries.ShouldBeTrue();
        options.AutoAssignTenantId.ShouldBeTrue();
        options.ValidateTenantOnModify.ShouldBeTrue();
        options.ThrowOnMissingTenantContext.ShouldBeTrue();
        options.TenantColumnName.ShouldBe("TenantId");
    }

    [Fact]
    public void Property_DapperSqlServerOptions_DefaultsAreConsistent()
    {
        var options = new DapperSqlServerTenancy.DapperTenancyOptions();

        options.AutoFilterTenantQueries.ShouldBeTrue();
        options.AutoAssignTenantId.ShouldBeTrue();
        options.ValidateTenantOnModify.ShouldBeTrue();
        options.ThrowOnMissingTenantContext.ShouldBeTrue();
        options.TenantColumnName.ShouldBe("TenantId");
    }

    [Fact]
    public void Property_DapperPostgreSQLOptions_DefaultsAreConsistent()
    {
        var options = new DapperPostgreSQLTenancy.DapperTenancyOptions();

        options.AutoFilterTenantQueries.ShouldBeTrue();
        options.AutoAssignTenantId.ShouldBeTrue();
        options.ValidateTenantOnModify.ShouldBeTrue();
        options.ThrowOnMissingTenantContext.ShouldBeTrue();
        options.TenantColumnName.ShouldBe("TenantId");
    }

    [Fact]
    public void Property_DapperMySQLOptions_DefaultsAreConsistent()
    {
        var options = new DapperMySQLTenancy.DapperTenancyOptions();

        options.AutoFilterTenantQueries.ShouldBeTrue();
        options.AutoAssignTenantId.ShouldBeTrue();
        options.ValidateTenantOnModify.ShouldBeTrue();
        options.ThrowOnMissingTenantContext.ShouldBeTrue();
        options.TenantColumnName.ShouldBe("TenantId");
    }

    #endregion

    #region EF Core Provider Options Tests

    [Fact]
    public void Property_EfCoreOptions_DefaultsAreConsistent()
    {
        // Property: EF Core tenancy options MUST have secure defaults
        var options = new EfCoreTenancy.EfCoreTenancyOptions();

        // Note: EF Core uses different property names
        options.AutoAssignTenantId.ShouldBeTrue("AutoAssign must default to true for convenience");
        options.ValidateTenantOnSave.ShouldBeTrue("Validation must default to true for security");
        options.UseQueryFilters.ShouldBeTrue("Query filters must default to true for isolation");
        options.ThrowOnMissingTenantContext.ShouldBeTrue("Throw must default to true to prevent data leaks");
    }

    #endregion

    #region MongoDB Provider Options Tests

    [Fact]
    public void Property_MongoDbOptions_DefaultsAreConsistent()
    {
        // Property: MongoDB tenancy options MUST have secure defaults
        var options = new MongoDbTenancy.MongoDbTenancyOptions();

        options.AutoFilterTenantQueries.ShouldBeTrue("AutoFilter must default to true for security");
        options.AutoAssignTenantId.ShouldBeTrue("AutoAssign must default to true for convenience");
        options.ValidateTenantOnModify.ShouldBeTrue("Validation must default to true for security");
        options.ThrowOnMissingTenantContext.ShouldBeTrue("Throw must default to true to prevent data leaks");
        options.TenantFieldName.ShouldBe("TenantId", "Default field name must be 'TenantId'");
    }

    #endregion

    #region Cross-Provider Consistency Tests

    [Fact]
    public void Property_AllADODapperProviders_HaveIdenticalDefaults()
    {
        // Property: All ADO/Dapper provider options MUST have identical defaults for consistency
        var adoSqlite = new ADOSqliteTenancy.ADOTenancyOptions();
        var adoSqlServer = new ADOSqlServerTenancy.ADOTenancyOptions();
        var adoPostgres = new ADOPostgreSQLTenancy.ADOTenancyOptions();
        var adoMySQL = new ADOMySQLTenancy.ADOTenancyOptions();
        var dapperSqlite = new DapperSqliteTenancy.DapperTenancyOptions();
        var dapperSqlServer = new DapperSqlServerTenancy.DapperTenancyOptions();
        var dapperPostgres = new DapperPostgreSQLTenancy.DapperTenancyOptions();
        var dapperMySQL = new DapperMySQLTenancy.DapperTenancyOptions();

        // All must have same AutoFilterTenantQueries default
        adoSqlite.AutoFilterTenantQueries.ShouldBe(adoSqlServer.AutoFilterTenantQueries);
        adoSqlServer.AutoFilterTenantQueries.ShouldBe(adoPostgres.AutoFilterTenantQueries);
        adoPostgres.AutoFilterTenantQueries.ShouldBe(adoMySQL.AutoFilterTenantQueries);
        adoMySQL.AutoFilterTenantQueries.ShouldBe(dapperSqlite.AutoFilterTenantQueries);
        dapperSqlite.AutoFilterTenantQueries.ShouldBe(dapperSqlServer.AutoFilterTenantQueries);
        dapperSqlServer.AutoFilterTenantQueries.ShouldBe(dapperPostgres.AutoFilterTenantQueries);
        dapperPostgres.AutoFilterTenantQueries.ShouldBe(dapperMySQL.AutoFilterTenantQueries);

        // All must have same TenantColumnName default
        adoSqlite.TenantColumnName.ShouldBe(adoSqlServer.TenantColumnName);
        adoSqlServer.TenantColumnName.ShouldBe(adoPostgres.TenantColumnName);
        adoPostgres.TenantColumnName.ShouldBe(adoMySQL.TenantColumnName);
        adoMySQL.TenantColumnName.ShouldBe(dapperSqlite.TenantColumnName);
        dapperSqlite.TenantColumnName.ShouldBe(dapperSqlServer.TenantColumnName);
        dapperSqlServer.TenantColumnName.ShouldBe(dapperPostgres.TenantColumnName);
        dapperPostgres.TenantColumnName.ShouldBe(dapperMySQL.TenantColumnName);
    }

    [Theory]
    [InlineData("TenantId")]
    [InlineData("OrganizationId")]
    [InlineData("CompanyId")]
    [InlineData("tenant_id")]
    [InlineData("org_id")]
    public void Property_TenantColumnName_CanBeCustomized(string customColumnName)
    {
        // Property: TenantColumnName must be customizable to any valid SQL identifier
        var adoOptions = new ADOSqliteTenancy.ADOTenancyOptions { TenantColumnName = customColumnName };
        var dapperOptions = new DapperSqliteTenancy.DapperTenancyOptions { TenantColumnName = customColumnName };

        adoOptions.TenantColumnName.ShouldBe(customColumnName);
        dapperOptions.TenantColumnName.ShouldBe(customColumnName);
    }

    [Fact]
    public void Property_AllBooleanOptions_CanBeToggled()
    {
        // Property: All boolean options must be independently toggleable
        var options = new ADOSqliteTenancy.ADOTenancyOptions
        {
            AutoFilterTenantQueries = false,
            AutoAssignTenantId = false,
            ValidateTenantOnModify = false,
            ThrowOnMissingTenantContext = false
        };

        options.AutoFilterTenantQueries.ShouldBeFalse();
        options.AutoAssignTenantId.ShouldBeFalse();
        options.ValidateTenantOnModify.ShouldBeFalse();
        options.ThrowOnMissingTenantContext.ShouldBeFalse();

        // Toggle back
        options.AutoFilterTenantQueries = true;
        options.AutoAssignTenantId = true;
        options.ValidateTenantOnModify = true;
        options.ThrowOnMissingTenantContext = true;

        options.AutoFilterTenantQueries.ShouldBeTrue();
        options.AutoAssignTenantId.ShouldBeTrue();
        options.ValidateTenantOnModify.ShouldBeTrue();
        options.ThrowOnMissingTenantContext.ShouldBeTrue();
    }

    #endregion
}
