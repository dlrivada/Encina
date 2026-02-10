using System.Data;
using System.Reflection;
using Encina.Sharding.Data;
using Shouldly;
using ADOMySQLSharding = Encina.ADO.MySQL.Sharding;
using ADOPostgreSQLSharding = Encina.ADO.PostgreSQL.Sharding;
using ADOSqliteSharding = Encina.ADO.Sqlite.Sharding;
using ADOSqlServerSharding = Encina.ADO.SqlServer.Sharding;

namespace Encina.ContractTests.Database.Sharding;

/// <summary>
/// Contract tests verifying that all ADO.NET providers implement
/// <see cref="IShardedConnectionFactory"/> and <see cref="IShardedConnectionFactory{TConnection}"/> consistently.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ShardedConnectionFactoryContractTests
{
    private static readonly Type BaseFactoryInterface = typeof(IShardedConnectionFactory);
    private static readonly Type GenericFactoryInterface = typeof(IShardedConnectionFactory<>);

    #region All ADO Providers Implement IShardedConnectionFactory

    [Fact]
    public void Contract_ADO_Sqlite_ImplementsIShardedConnectionFactory()
    {
        var factoryType = typeof(ADOSqliteSharding.ShardedConnectionFactory);
        VerifyImplementsBaseFactory(factoryType);
    }

    [Fact]
    public void Contract_ADO_SqlServer_ImplementsIShardedConnectionFactory()
    {
        var factoryType = typeof(ADOSqlServerSharding.ShardedConnectionFactory);
        VerifyImplementsBaseFactory(factoryType);
    }

    [Fact]
    public void Contract_ADO_PostgreSQL_ImplementsIShardedConnectionFactory()
    {
        var factoryType = typeof(ADOPostgreSQLSharding.ShardedConnectionFactory);
        VerifyImplementsBaseFactory(factoryType);
    }

    [Fact]
    public void Contract_ADO_MySQL_ImplementsIShardedConnectionFactory()
    {
        var factoryType = typeof(ADOMySQLSharding.ShardedConnectionFactory);
        VerifyImplementsBaseFactory(factoryType);
    }

    #endregion

    #region All ADO Providers Implement Generic IShardedConnectionFactory<TConnection>

    [Fact]
    public void Contract_ADO_Sqlite_ImplementsGenericShardedConnectionFactory()
    {
        var factoryType = typeof(ADOSqliteSharding.ShardedConnectionFactory);
        VerifyImplementsGenericFactory(factoryType);
    }

    [Fact]
    public void Contract_ADO_SqlServer_ImplementsGenericShardedConnectionFactory()
    {
        var factoryType = typeof(ADOSqlServerSharding.ShardedConnectionFactory);
        VerifyImplementsGenericFactory(factoryType);
    }

    [Fact]
    public void Contract_ADO_PostgreSQL_ImplementsGenericShardedConnectionFactory()
    {
        var factoryType = typeof(ADOPostgreSQLSharding.ShardedConnectionFactory);
        VerifyImplementsGenericFactory(factoryType);
    }

    [Fact]
    public void Contract_ADO_MySQL_ImplementsGenericShardedConnectionFactory()
    {
        var factoryType = typeof(ADOMySQLSharding.ShardedConnectionFactory);
        VerifyImplementsGenericFactory(factoryType);
    }

    #endregion

    #region Interface Method Consistency

    [Fact]
    public void Contract_IShardedConnectionFactory_HasExpectedMethods()
    {
        var methods = BaseFactoryInterface
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        methods.ShouldContain("GetConnectionAsync");
        methods.ShouldContain("GetAllConnectionsAsync");
        methods.ShouldContain("GetConnectionForEntityAsync");
    }

    #endregion

    #region Sealed Class Contract

    [Fact]
    public void Contract_AllConnectionFactories_AreSealed()
    {
        var factories = new[]
        {
            typeof(ADOSqliteSharding.ShardedConnectionFactory),
            typeof(ADOSqlServerSharding.ShardedConnectionFactory),
            typeof(ADOPostgreSQLSharding.ShardedConnectionFactory),
            typeof(ADOMySQLSharding.ShardedConnectionFactory)
        };

        foreach (var factory in factories)
        {
            factory.IsSealed.ShouldBeTrue($"{factory.FullName} should be sealed");
        }
    }

    #endregion

    #region Consistent Naming

    [Fact]
    public void Contract_AllProviders_UseShardedConnectionFactoryName()
    {
        typeof(ADOSqliteSharding.ShardedConnectionFactory).Name.ShouldBe("ShardedConnectionFactory");
        typeof(ADOSqlServerSharding.ShardedConnectionFactory).Name.ShouldBe("ShardedConnectionFactory");
        typeof(ADOPostgreSQLSharding.ShardedConnectionFactory).Name.ShouldBe("ShardedConnectionFactory");
        typeof(ADOMySQLSharding.ShardedConnectionFactory).Name.ShouldBe("ShardedConnectionFactory");
    }

    #endregion

    #region Helpers

    private static void VerifyImplementsBaseFactory(Type factoryType)
    {
        BaseFactoryInterface.IsAssignableFrom(factoryType).ShouldBeTrue(
            $"{factoryType.FullName} should implement IShardedConnectionFactory");
    }

    private static void VerifyImplementsGenericFactory(Type factoryType)
    {
        var interfaces = factoryType.GetInterfaces();
        var implementsGeneric = interfaces.Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == GenericFactoryInterface);

        implementsGeneric.ShouldBeTrue(
            $"{factoryType.FullName} should implement IShardedConnectionFactory<TConnection>");
    }

    #endregion
}
