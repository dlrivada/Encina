using System.Reflection;
using Encina.DomainModeling.Sharding;
using Shouldly;
using ADOSqliteSharding = Encina.ADO.Sqlite.Sharding;
using ADOSqlServerSharding = Encina.ADO.SqlServer.Sharding;
using ADOPostgreSQLSharding = Encina.ADO.PostgreSQL.Sharding;
using ADOMySQLSharding = Encina.ADO.MySQL.Sharding;
using DapperSqliteSharding = Encina.Dapper.Sqlite.Sharding;
using DapperSqlServerSharding = Encina.Dapper.SqlServer.Sharding;
using DapperPostgreSQLSharding = Encina.Dapper.PostgreSQL.Sharding;
using DapperMySQLSharding = Encina.Dapper.MySQL.Sharding;
using EFCoreSharding = Encina.EntityFrameworkCore.Sharding;
using MongoDBSharding = Encina.MongoDB.Sharding;

namespace Encina.ContractTests.Database.Sharding;

/// <summary>
/// Contract tests verifying that all 13 providers implement
/// <see cref="IShardedSpecificationSupport{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ShardedSpecificationSupportContractTests
{
    private static readonly Type SpecSupportInterface = typeof(IShardedSpecificationSupport<,>);

    #region All Providers Implement IShardedSpecificationSupport

    [Fact]
    public void Contract_ADO_Sqlite_ImplementsIShardedSpecificationSupport()
    {
        var repoType = typeof(ADOSqliteSharding.FunctionalShardedRepositoryADO<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_ADO_SqlServer_ImplementsIShardedSpecificationSupport()
    {
        var repoType = typeof(ADOSqlServerSharding.FunctionalShardedRepositoryADO<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_ADO_PostgreSQL_ImplementsIShardedSpecificationSupport()
    {
        var repoType = typeof(ADOPostgreSQLSharding.FunctionalShardedRepositoryADO<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_ADO_MySQL_ImplementsIShardedSpecificationSupport()
    {
        var repoType = typeof(ADOMySQLSharding.FunctionalShardedRepositoryADO<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_Dapper_Sqlite_ImplementsIShardedSpecificationSupport()
    {
        var repoType = typeof(DapperSqliteSharding.FunctionalShardedRepositoryDapper<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_Dapper_SqlServer_ImplementsIShardedSpecificationSupport()
    {
        var repoType = typeof(DapperSqlServerSharding.FunctionalShardedRepositoryDapper<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_Dapper_PostgreSQL_ImplementsIShardedSpecificationSupport()
    {
        var repoType = typeof(DapperPostgreSQLSharding.FunctionalShardedRepositoryDapper<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_Dapper_MySQL_ImplementsIShardedSpecificationSupport()
    {
        var repoType = typeof(DapperMySQLSharding.FunctionalShardedRepositoryDapper<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_EFCore_ImplementsIShardedSpecificationSupport()
    {
        var repoType = typeof(EFCoreSharding.FunctionalShardedRepositoryEF<,,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_MongoDB_ImplementsIShardedSpecificationSupport()
    {
        var repoType = typeof(MongoDBSharding.FunctionalShardedRepositoryMongoDB<,>);
        VerifyImplementsInterface(repoType);
    }

    #endregion

    #region Interface Method Verification

    [Fact]
    public void Contract_Interface_HasQueryAllShardsAsyncMethod()
    {
        var methods = SpecSupportInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var methodNames = methods.Select(m => m.Name).ToList();
        methodNames.ShouldContain("QueryAllShardsAsync");
    }

    [Fact]
    public void Contract_Interface_HasQueryAllShardsPagedAsyncMethod()
    {
        var methods = SpecSupportInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var methodNames = methods.Select(m => m.Name).ToList();
        // QueryAllShardsPagedAsync is a separate overload
        methods.Any(m => m.Name == "QueryAllShardsPagedAsync").ShouldBeTrue(
            "IShardedSpecificationSupport should have QueryAllShardsPagedAsync method");
    }

    [Fact]
    public void Contract_Interface_HasCountAllShardsAsyncMethod()
    {
        var methods = SpecSupportInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        methods.Any(m => m.Name == "CountAllShardsAsync").ShouldBeTrue(
            "IShardedSpecificationSupport should have CountAllShardsAsync method");
    }

    [Fact]
    public void Contract_Interface_HasQueryShardsAsyncMethod()
    {
        var methods = SpecSupportInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        methods.Any(m => m.Name == "QueryShardsAsync").ShouldBeTrue(
            "IShardedSpecificationSupport should have QueryShardsAsync method");
    }

    #endregion

    #region Helpers

    private static void VerifyImplementsInterface(Type repoType)
    {
        var interfaces = repoType.GetInterfaces();
        var implementsInterface = interfaces.Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == SpecSupportInterface);

        implementsInterface.ShouldBeTrue(
            $"{repoType.Name} should implement IShardedSpecificationSupport<,>");
    }

    #endregion
}
