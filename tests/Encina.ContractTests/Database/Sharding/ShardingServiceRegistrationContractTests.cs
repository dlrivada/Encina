using System.Reflection;

using Encina.Sharding;
using Encina.Sharding.Routing;

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
/// Contract tests verifying that all 13 providers have sharding service registration
/// extension methods in their Sharding namespace following consistent naming conventions.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ShardingServiceRegistrationContractTests
{
    #region All Providers Have ShardingServiceCollectionExtensions

    [Fact]
    public void Contract_ADO_Sqlite_HasShardingServiceCollectionExtensions()
    {
        VerifyExtensionClassExists(typeof(ADOSqliteSharding.FunctionalShardedRepositoryADO<,>).Assembly, "Encina.ADO.Sqlite.Sharding");
    }

    [Fact]
    public void Contract_ADO_SqlServer_HasShardingServiceCollectionExtensions()
    {
        VerifyExtensionClassExists(typeof(ADOSqlServerSharding.FunctionalShardedRepositoryADO<,>).Assembly, "Encina.ADO.SqlServer.Sharding");
    }

    [Fact]
    public void Contract_ADO_PostgreSQL_HasShardingServiceCollectionExtensions()
    {
        VerifyExtensionClassExists(typeof(ADOPostgreSQLSharding.FunctionalShardedRepositoryADO<,>).Assembly, "Encina.ADO.PostgreSQL.Sharding");
    }

    [Fact]
    public void Contract_ADO_MySQL_HasShardingServiceCollectionExtensions()
    {
        VerifyExtensionClassExists(typeof(ADOMySQLSharding.FunctionalShardedRepositoryADO<,>).Assembly, "Encina.ADO.MySQL.Sharding");
    }

    [Fact]
    public void Contract_Dapper_Sqlite_HasShardingServiceCollectionExtensions()
    {
        VerifyExtensionClassExists(typeof(DapperSqliteSharding.FunctionalShardedRepositoryDapper<,>).Assembly, "Encina.Dapper.Sqlite.Sharding");
    }

    [Fact]
    public void Contract_Dapper_SqlServer_HasShardingServiceCollectionExtensions()
    {
        VerifyExtensionClassExists(typeof(DapperSqlServerSharding.FunctionalShardedRepositoryDapper<,>).Assembly, "Encina.Dapper.SqlServer.Sharding");
    }

    [Fact]
    public void Contract_Dapper_PostgreSQL_HasShardingServiceCollectionExtensions()
    {
        VerifyExtensionClassExists(typeof(DapperPostgreSQLSharding.FunctionalShardedRepositoryDapper<,>).Assembly, "Encina.Dapper.PostgreSQL.Sharding");
    }

    [Fact]
    public void Contract_Dapper_MySQL_HasShardingServiceCollectionExtensions()
    {
        VerifyExtensionClassExists(typeof(DapperMySQLSharding.FunctionalShardedRepositoryDapper<,>).Assembly, "Encina.Dapper.MySQL.Sharding");
    }

    [Fact]
    public void Contract_EFCore_HasShardingServiceCollectionExtensions()
    {
        VerifyExtensionClassExists(typeof(EFCoreSharding.FunctionalShardedRepositoryEF<,,>).Assembly, "Encina.EntityFrameworkCore.Sharding");
    }

    [Fact]
    public void Contract_MongoDB_HasShardingServiceCollectionExtensions()
    {
        VerifyExtensionClassExists(typeof(MongoDBSharding.FunctionalShardedRepositoryMongoDB<,>).Assembly, "Encina.MongoDB.Sharding");
    }

    #endregion

    #region Provider Namespace Consistency

    [Fact]
    public void Contract_AllProviders_HaveShardingNamespace()
    {
        var expectedNamespaces = new[]
        {
            "Encina.ADO.Sqlite.Sharding",
            "Encina.ADO.SqlServer.Sharding",
            "Encina.ADO.PostgreSQL.Sharding",
            "Encina.ADO.MySQL.Sharding",
            "Encina.Dapper.Sqlite.Sharding",
            "Encina.Dapper.SqlServer.Sharding",
            "Encina.Dapper.PostgreSQL.Sharding",
            "Encina.Dapper.MySQL.Sharding",
            "Encina.EntityFrameworkCore.Sharding",
            "Encina.MongoDB.Sharding"
        };

        var repoTypes = new[]
        {
            typeof(ADOSqliteSharding.FunctionalShardedRepositoryADO<,>),
            typeof(ADOSqlServerSharding.FunctionalShardedRepositoryADO<,>),
            typeof(ADOPostgreSQLSharding.FunctionalShardedRepositoryADO<,>),
            typeof(ADOMySQLSharding.FunctionalShardedRepositoryADO<,>),
            typeof(DapperSqliteSharding.FunctionalShardedRepositoryDapper<,>),
            typeof(DapperSqlServerSharding.FunctionalShardedRepositoryDapper<,>),
            typeof(DapperPostgreSQLSharding.FunctionalShardedRepositoryDapper<,>),
            typeof(DapperMySQLSharding.FunctionalShardedRepositoryDapper<,>),
            typeof(EFCoreSharding.FunctionalShardedRepositoryEF<,,>),
            typeof(MongoDBSharding.FunctionalShardedRepositoryMongoDB<,>)
        };

        for (var i = 0; i < repoTypes.Length; i++)
        {
            repoTypes[i].Namespace.ShouldBe(expectedNamespaces[i],
                $"Repository type {repoTypes[i].Name} should be in namespace {expectedNamespaces[i]}");
        }
    }

    #endregion

    #region Core Sharding Types Available

    [Fact]
    public void Contract_CoreShardingTypes_ArePublic()
    {
        typeof(IShardRouter).IsPublic.ShouldBeTrue("IShardRouter should be public");
        typeof(ShardTopology).IsPublic.ShouldBeTrue("ShardTopology should be public");
        typeof(ShardInfo).IsPublic.ShouldBeTrue("ShardInfo should be public");
        typeof(IFunctionalShardedRepository<,>).IsPublic.ShouldBeTrue("IFunctionalShardedRepository should be public");
        typeof(IShardable).IsPublic.ShouldBeTrue("IShardable should be public");
        typeof(ShardKeyAttribute).IsPublic.ShouldBeTrue("ShardKeyAttribute should be public");
    }

    [Fact]
    public void Contract_CoreRoutingTypes_ArePublic()
    {
        typeof(HashShardRouter).IsPublic.ShouldBeTrue("HashShardRouter should be public");
        typeof(RangeShardRouter).IsPublic.ShouldBeTrue("RangeShardRouter should be public");
        typeof(DirectoryShardRouter).IsPublic.ShouldBeTrue("DirectoryShardRouter should be public");
        typeof(GeoShardRouter).IsPublic.ShouldBeTrue("GeoShardRouter should be public");
    }

    #endregion

    #region Helpers

    private static void VerifyExtensionClassExists(Assembly assembly, string expectedNamespace)
    {
        var extensionClass = assembly.GetTypes()
            .FirstOrDefault(t =>
                t.Namespace == expectedNamespace &&
                t.Name.Contains("ShardingServiceCollectionExtensions", StringComparison.Ordinal));

        extensionClass.ShouldNotBeNull(
            $"Assembly {assembly.GetName().Name} should contain ShardingServiceCollectionExtensions in namespace {expectedNamespace}");

        extensionClass.IsAbstract.ShouldBeTrue($"Extension class {extensionClass.FullName} should be static (abstract + sealed)");
        extensionClass.IsSealed.ShouldBeTrue($"Extension class {extensionClass.FullName} should be static (abstract + sealed)");
    }

    #endregion
}
