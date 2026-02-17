using System.Reflection;
using Encina.Sharding.Data;
using Shouldly;
using ADOMySQLSharding = Encina.ADO.MySQL.Sharding;
using ADOPostgreSQLSharding = Encina.ADO.PostgreSQL.Sharding;
using ADOSqliteSharding = Encina.ADO.Sqlite.Sharding;
using ADOSqlServerSharding = Encina.ADO.SqlServer.Sharding;
using DapperMySQLSharding = Encina.Dapper.MySQL.Sharding;
using DapperPostgreSQLSharding = Encina.Dapper.PostgreSQL.Sharding;
using DapperSqliteSharding = Encina.Dapper.Sqlite.Sharding;
using DapperSqlServerSharding = Encina.Dapper.SqlServer.Sharding;

namespace Encina.ContractTests.Database.Sharding;

/// <summary>
/// Contract tests verifying that all ADO.NET and Dapper providers implement
/// <see cref="IShardedReadWriteConnectionFactory"/> and
/// <see cref="IShardedReadWriteConnectionFactory{TConnection}"/> consistently.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ShardedReadWriteConnectionFactoryContractTests
{
    private static readonly Type BaseFactoryInterface = typeof(IShardedReadWriteConnectionFactory);
    private static readonly Type GenericFactoryInterface = typeof(IShardedReadWriteConnectionFactory<>);

    #region ADO Providers Implement IShardedReadWriteConnectionFactory

    [Fact]
    public void Contract_ADO_Sqlite_ImplementsIShardedReadWriteConnectionFactory()
    {
        VerifyImplementsBaseFactory(typeof(ADOSqliteSharding.ShardedReadWriteConnectionFactory));
    }

    [Fact]
    public void Contract_ADO_SqlServer_ImplementsIShardedReadWriteConnectionFactory()
    {
        VerifyImplementsBaseFactory(typeof(ADOSqlServerSharding.ShardedReadWriteConnectionFactory));
    }

    [Fact]
    public void Contract_ADO_PostgreSQL_ImplementsIShardedReadWriteConnectionFactory()
    {
        VerifyImplementsBaseFactory(typeof(ADOPostgreSQLSharding.ShardedReadWriteConnectionFactory));
    }

    [Fact]
    public void Contract_ADO_MySQL_ImplementsIShardedReadWriteConnectionFactory()
    {
        VerifyImplementsBaseFactory(typeof(ADOMySQLSharding.ShardedReadWriteConnectionFactory));
    }

    #endregion

    #region Dapper Providers Implement IShardedReadWriteConnectionFactory

    [Fact]
    public void Contract_Dapper_Sqlite_ImplementsIShardedReadWriteConnectionFactory()
    {
        VerifyImplementsBaseFactory(typeof(DapperSqliteSharding.ShardedReadWriteConnectionFactory));
    }

    [Fact]
    public void Contract_Dapper_SqlServer_ImplementsIShardedReadWriteConnectionFactory()
    {
        VerifyImplementsBaseFactory(typeof(DapperSqlServerSharding.ShardedReadWriteConnectionFactory));
    }

    [Fact]
    public void Contract_Dapper_PostgreSQL_ImplementsIShardedReadWriteConnectionFactory()
    {
        VerifyImplementsBaseFactory(typeof(DapperPostgreSQLSharding.ShardedReadWriteConnectionFactory));
    }

    [Fact]
    public void Contract_Dapper_MySQL_ImplementsIShardedReadWriteConnectionFactory()
    {
        VerifyImplementsBaseFactory(typeof(DapperMySQLSharding.ShardedReadWriteConnectionFactory));
    }

    #endregion

    #region ADO Providers Implement Generic IShardedReadWriteConnectionFactory<TConnection>

    [Fact]
    public void Contract_ADO_Sqlite_ImplementsGenericShardedReadWriteConnectionFactory()
    {
        VerifyImplementsGenericFactory(typeof(ADOSqliteSharding.ShardedReadWriteConnectionFactory));
    }

    [Fact]
    public void Contract_ADO_SqlServer_ImplementsGenericShardedReadWriteConnectionFactory()
    {
        VerifyImplementsGenericFactory(typeof(ADOSqlServerSharding.ShardedReadWriteConnectionFactory));
    }

    [Fact]
    public void Contract_ADO_PostgreSQL_ImplementsGenericShardedReadWriteConnectionFactory()
    {
        VerifyImplementsGenericFactory(typeof(ADOPostgreSQLSharding.ShardedReadWriteConnectionFactory));
    }

    [Fact]
    public void Contract_ADO_MySQL_ImplementsGenericShardedReadWriteConnectionFactory()
    {
        VerifyImplementsGenericFactory(typeof(ADOMySQLSharding.ShardedReadWriteConnectionFactory));
    }

    #endregion

    #region Dapper Providers Do NOT Implement Generic (use IDbConnection directly)

    [Fact]
    public void Contract_Dapper_Providers_DoNotImplementGenericFactory()
    {
        // Dapper works with IDbConnection, so Dapper providers only implement the non-generic interface
        var dapperFactories = new[]
        {
            typeof(DapperSqliteSharding.ShardedReadWriteConnectionFactory),
            typeof(DapperSqlServerSharding.ShardedReadWriteConnectionFactory),
            typeof(DapperPostgreSQLSharding.ShardedReadWriteConnectionFactory),
            typeof(DapperMySQLSharding.ShardedReadWriteConnectionFactory),
        };

        foreach (var factory in dapperFactories)
        {
            var implementsGeneric = factory.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == GenericFactoryInterface);

            implementsGeneric
                .ShouldBeFalse($"{factory.FullName} is a Dapper provider and should NOT implement {GenericFactoryInterface.Name}");
        }
    }

    #endregion

    #region Interface Method Consistency

    [Fact]
    public void Contract_IShardedReadWriteConnectionFactory_HasExpectedMethods()
    {
        var methods = BaseFactoryInterface
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        methods.ShouldContain("GetReadConnectionAsync");
        methods.ShouldContain("GetWriteConnectionAsync");
        methods.ShouldContain("GetConnectionAsync");
        methods.ShouldContain("GetAllReadConnectionsAsync");
        methods.ShouldContain("GetAllWriteConnectionsAsync");
    }

    #endregion

    #region All ReadWrite Factories Are Sealed

    [Fact]
    public void Contract_AllReadWriteConnectionFactories_AreSealed()
    {
        var factories = new[]
        {
            typeof(ADOSqliteSharding.ShardedReadWriteConnectionFactory),
            typeof(ADOSqlServerSharding.ShardedReadWriteConnectionFactory),
            typeof(ADOPostgreSQLSharding.ShardedReadWriteConnectionFactory),
            typeof(ADOMySQLSharding.ShardedReadWriteConnectionFactory),
            typeof(DapperSqliteSharding.ShardedReadWriteConnectionFactory),
            typeof(DapperSqlServerSharding.ShardedReadWriteConnectionFactory),
            typeof(DapperPostgreSQLSharding.ShardedReadWriteConnectionFactory),
            typeof(DapperMySQLSharding.ShardedReadWriteConnectionFactory),
        };

        foreach (var factory in factories)
        {
            factory.IsSealed.ShouldBeTrue($"{factory.FullName} should be sealed");
        }
    }

    #endregion

    #region Helper Methods

    private static void VerifyImplementsBaseFactory(Type type)
    {
        BaseFactoryInterface.IsAssignableFrom(type)
            .ShouldBeTrue($"{type.FullName} should implement {BaseFactoryInterface.Name}");
    }

    private static void VerifyImplementsGenericFactory(Type type)
    {
        var implementsGeneric = type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == GenericFactoryInterface);

        implementsGeneric
            .ShouldBeTrue($"{type.FullName} should implement {GenericFactoryInterface.Name}");
    }

    #endregion
}
