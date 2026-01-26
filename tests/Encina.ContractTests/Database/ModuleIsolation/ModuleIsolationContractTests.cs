using System.Data;
using System.Data.Common;
using System.Reflection;
using Encina.Modules.Isolation;
using ADOMySQLModules = Encina.ADO.MySQL.Modules;
using ADOPostgreSQLModules = Encina.ADO.PostgreSQL.Modules;
// Type aliases for all provider namespaces
using ADOSqliteModules = Encina.ADO.Sqlite.Modules;
using ADOSqlServerModules = Encina.ADO.SqlServer.Modules;
using DapperMySQLModules = Encina.Dapper.MySQL.Modules;
using DapperPostgreSQLModules = Encina.Dapper.PostgreSQL.Modules;
using DapperSqliteModules = Encina.Dapper.Sqlite.Modules;
using DapperSqlServerModules = Encina.Dapper.SqlServer.Modules;

namespace Encina.ContractTests.Database.ModuleIsolation;

/// <summary>
/// Contract tests ensuring consistent Module Isolation API across all ADO.NET and Dapper providers.
/// </summary>
public class ModuleIsolationContractTests
{
    #region ADO.NET Provider Types

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    public void ADOSqlite_ModuleAwareConnectionFactory_ShouldExist()
    {
        var type = typeof(ADOSqliteModules.ModuleAwareConnectionFactory);
        Assert.NotNull(type);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.True(type.IsPublic);
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    public void ADOSqlServer_ModuleAwareConnectionFactory_ShouldExist()
    {
        var type = typeof(ADOSqlServerModules.ModuleAwareConnectionFactory);
        Assert.NotNull(type);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.True(type.IsPublic);
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    public void ADOPostgreSQL_ModuleAwareConnectionFactory_ShouldExist()
    {
        var type = typeof(ADOPostgreSQLModules.ModuleAwareConnectionFactory);
        Assert.NotNull(type);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.True(type.IsPublic);
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    public void ADOMySQL_ModuleAwareConnectionFactory_ShouldExist()
    {
        var type = typeof(ADOMySQLModules.ModuleAwareConnectionFactory);
        Assert.NotNull(type);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.True(type.IsPublic);
    }

    #endregion

    #region Dapper Provider Types

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    public void DapperSqlite_ModuleAwareConnectionFactory_ShouldExist()
    {
        var type = typeof(DapperSqliteModules.ModuleAwareConnectionFactory);
        Assert.NotNull(type);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.True(type.IsPublic);
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    public void DapperSqlServer_ModuleAwareConnectionFactory_ShouldExist()
    {
        var type = typeof(DapperSqlServerModules.ModuleAwareConnectionFactory);
        Assert.NotNull(type);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.True(type.IsPublic);
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    public void DapperPostgreSQL_ModuleAwareConnectionFactory_ShouldExist()
    {
        var type = typeof(DapperPostgreSQLModules.ModuleAwareConnectionFactory);
        Assert.NotNull(type);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.True(type.IsPublic);
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    public void DapperMySQL_ModuleAwareConnectionFactory_ShouldExist()
    {
        var type = typeof(DapperMySQLModules.ModuleAwareConnectionFactory);
        Assert.NotNull(type);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.True(type.IsPublic);
    }

    #endregion

    #region ADO.NET CreateConnection Method Signature

    [Theory]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    [InlineData(typeof(ADOSqliteModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(ADOSqlServerModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(ADOMySQLModules.ModuleAwareConnectionFactory))]
    public void ADO_ModuleAwareConnectionFactory_ShouldHaveCreateConnectionMethod(Type factoryType)
    {
        var method = factoryType.GetMethod("CreateConnection", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(IDbConnection), method.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    #endregion

    #region Dapper CreateConnection Method Signature

    [Theory]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    [InlineData(typeof(DapperSqliteModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(DapperSqlServerModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(DapperMySQLModules.ModuleAwareConnectionFactory))]
    public void Dapper_ModuleAwareConnectionFactory_ShouldHaveCreateConnectionMethod(Type factoryType)
    {
        var method = factoryType.GetMethod("CreateConnection", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(IDbConnection), method.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Theory]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    [InlineData(typeof(DapperSqliteModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(DapperSqlServerModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(DapperMySQLModules.ModuleAwareConnectionFactory))]
    public void Dapper_ModuleAwareConnectionFactory_ShouldHaveCreateDbConnectionMethod(Type factoryType)
    {
        var method = factoryType.GetMethod("CreateDbConnection", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(DbConnection), method.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    #endregion

    #region Constructor Parameter Consistency

    [Theory]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    [InlineData(typeof(ADOSqliteModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(ADOSqlServerModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(ADOMySQLModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(DapperSqliteModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(DapperSqlServerModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLModules.ModuleAwareConnectionFactory))]
    [InlineData(typeof(DapperMySQLModules.ModuleAwareConnectionFactory))]
    public void AllProviders_ModuleAwareConnectionFactory_ShouldHaveConsistentConstructor(Type factoryType)
    {
        var constructors = factoryType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.Single(constructors);

        var ctor = constructors[0];
        var parameters = ctor.GetParameters();

        Assert.Equal(4, parameters.Length);

        // Parameter 1: Inner connection factory (Func<IDbConnection>)
        // ADO uses "innerConnectionFactory", Dapper uses "innerFactory"
        Assert.True(
            parameters[0].Name == "innerConnectionFactory" || parameters[0].Name == "innerFactory",
            $"Expected 'innerConnectionFactory' or 'innerFactory', got '{parameters[0].Name}'");
        Assert.True(parameters[0].ParameterType.IsGenericType);

        // Parameter 2: Module execution context
        Assert.Equal("moduleContext", parameters[1].Name);
        Assert.Equal(typeof(IModuleExecutionContext), parameters[1].ParameterType);

        // Parameter 3: Schema registry
        Assert.Equal("schemaRegistry", parameters[2].Name);
        Assert.Equal(typeof(IModuleSchemaRegistry), parameters[2].ParameterType);

        // Parameter 4: Options
        Assert.Equal("options", parameters[3].Name);
        Assert.Equal(typeof(ModuleIsolationOptions), parameters[3].ParameterType);
    }

    #endregion

    #region SchemaValidatingConnection Internal Type Verification

    [Theory]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    [InlineData("Encina.ADO.Sqlite", "Encina.ADO.Sqlite.Modules.SchemaValidatingConnection")]
    [InlineData("Encina.ADO.SqlServer", "Encina.ADO.SqlServer.Modules.SchemaValidatingConnection")]
    [InlineData("Encina.ADO.PostgreSQL", "Encina.ADO.PostgreSQL.Modules.SchemaValidatingConnection")]
    [InlineData("Encina.ADO.MySQL", "Encina.ADO.MySQL.Modules.SchemaValidatingConnection")]
    [InlineData("Encina.Dapper.Sqlite", "Encina.Dapper.Sqlite.Modules.SchemaValidatingConnection")]
    [InlineData("Encina.Dapper.SqlServer", "Encina.Dapper.SqlServer.Modules.SchemaValidatingConnection")]
    [InlineData("Encina.Dapper.PostgreSQL", "Encina.Dapper.PostgreSQL.Modules.SchemaValidatingConnection")]
    [InlineData("Encina.Dapper.MySQL", "Encina.Dapper.MySQL.Modules.SchemaValidatingConnection")]
    public void AllProviders_ShouldHaveInternalSchemaValidatingConnection(string assemblyName, string typeName)
    {
        var assembly = Assembly.Load(assemblyName);
        var type = assembly.GetType(typeName);

        Assert.NotNull(type);
        Assert.True(type.IsNotPublic, $"{typeName} should be internal");
        Assert.True(typeof(DbConnection).IsAssignableFrom(type), $"{typeName} should inherit from DbConnection");
    }

    [Theory]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    [InlineData("Encina.ADO.Sqlite", "Encina.ADO.Sqlite.Modules.SchemaValidatingCommand")]
    [InlineData("Encina.ADO.SqlServer", "Encina.ADO.SqlServer.Modules.SchemaValidatingCommand")]
    [InlineData("Encina.ADO.PostgreSQL", "Encina.ADO.PostgreSQL.Modules.SchemaValidatingCommand")]
    [InlineData("Encina.ADO.MySQL", "Encina.ADO.MySQL.Modules.SchemaValidatingCommand")]
    [InlineData("Encina.Dapper.Sqlite", "Encina.Dapper.Sqlite.Modules.SchemaValidatingCommand")]
    [InlineData("Encina.Dapper.SqlServer", "Encina.Dapper.SqlServer.Modules.SchemaValidatingCommand")]
    [InlineData("Encina.Dapper.PostgreSQL", "Encina.Dapper.PostgreSQL.Modules.SchemaValidatingCommand")]
    [InlineData("Encina.Dapper.MySQL", "Encina.Dapper.MySQL.Modules.SchemaValidatingCommand")]
    public void AllProviders_ShouldHaveInternalSchemaValidatingCommand(string assemblyName, string typeName)
    {
        var assembly = Assembly.Load(assemblyName);
        var type = assembly.GetType(typeName);

        Assert.NotNull(type);
        Assert.True(type.IsNotPublic, $"{typeName} should be internal");
        Assert.True(typeof(DbCommand).IsAssignableFrom(type), $"{typeName} should inherit from DbCommand");
    }

    #endregion

    #region Namespace Consistency

    [Theory]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    [InlineData(typeof(ADOSqliteModules.ModuleAwareConnectionFactory), "Encina.ADO.Sqlite.Modules")]
    [InlineData(typeof(ADOSqlServerModules.ModuleAwareConnectionFactory), "Encina.ADO.SqlServer.Modules")]
    [InlineData(typeof(ADOPostgreSQLModules.ModuleAwareConnectionFactory), "Encina.ADO.PostgreSQL.Modules")]
    [InlineData(typeof(ADOMySQLModules.ModuleAwareConnectionFactory), "Encina.ADO.MySQL.Modules")]
    [InlineData(typeof(DapperSqliteModules.ModuleAwareConnectionFactory), "Encina.Dapper.Sqlite.Modules")]
    [InlineData(typeof(DapperSqlServerModules.ModuleAwareConnectionFactory), "Encina.Dapper.SqlServer.Modules")]
    [InlineData(typeof(DapperPostgreSQLModules.ModuleAwareConnectionFactory), "Encina.Dapper.PostgreSQL.Modules")]
    [InlineData(typeof(DapperMySQLModules.ModuleAwareConnectionFactory), "Encina.Dapper.MySQL.Modules")]
    public void AllProviders_ShouldFollowModulesNamespaceConvention(Type factoryType, string expectedNamespace)
    {
        Assert.Equal(expectedNamespace, factoryType.Namespace);
    }

    #endregion

    #region All Providers Present

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    public void AllADOProviders_ShouldHaveModuleIsolationSupport()
    {
        // ADO.NET providers: Sqlite, SqlServer, PostgreSQL, MySQL
        var adoProviderTypes = new[]
        {
            typeof(ADOSqliteModules.ModuleAwareConnectionFactory),
            typeof(ADOSqlServerModules.ModuleAwareConnectionFactory),
            typeof(ADOPostgreSQLModules.ModuleAwareConnectionFactory),
            typeof(ADOMySQLModules.ModuleAwareConnectionFactory),
        };

        Assert.Equal(4, adoProviderTypes.Length);
        Assert.All(adoProviderTypes, type => Assert.NotNull(type));
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Feature", "ModuleIsolation")]
    public void AllDapperProviders_ShouldHaveModuleIsolationSupport()
    {
        // Dapper providers: Sqlite, SqlServer, PostgreSQL, MySQL
        var dapperProviderTypes = new[]
        {
            typeof(DapperSqliteModules.ModuleAwareConnectionFactory),
            typeof(DapperSqlServerModules.ModuleAwareConnectionFactory),
            typeof(DapperPostgreSQLModules.ModuleAwareConnectionFactory),
            typeof(DapperMySQLModules.ModuleAwareConnectionFactory),
        };

        Assert.Equal(4, dapperProviderTypes.Length);
        Assert.All(dapperProviderTypes, type => Assert.NotNull(type));
    }

    #endregion
}
