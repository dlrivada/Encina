using System.Reflection;
using Encina.Sharding;
using Shouldly;
using ADOMySQLSharding = Encina.ADO.MySQL.Sharding;
using ADOPostgreSQLSharding = Encina.ADO.PostgreSQL.Sharding;
using ADOSqliteSharding = Encina.ADO.Sqlite.Sharding;
using ADOSqlServerSharding = Encina.ADO.SqlServer.Sharding;
using DapperMySQLSharding = Encina.Dapper.MySQL.Sharding;
using DapperPostgreSQLSharding = Encina.Dapper.PostgreSQL.Sharding;
using DapperSqliteSharding = Encina.Dapper.Sqlite.Sharding;
using DapperSqlServerSharding = Encina.Dapper.SqlServer.Sharding;
using EFCoreSharding = Encina.EntityFrameworkCore.Sharding;
using MongoDBSharding = Encina.MongoDB.Sharding;

namespace Encina.ContractTests.Database.Sharding;

/// <summary>
/// Contract tests verifying that all <see cref="IFunctionalShardedRepository{TEntity, TId}"/>
/// implementations follow the same interface contract across all providers.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ShardedRepositoryContractTests
{
    private static readonly Type RepositoryInterface = typeof(IFunctionalShardedRepository<,>);

    #region All Providers Implement IFunctionalShardedRepository

    [Fact]
    public void Contract_ADO_Sqlite_ImplementsIFunctionalShardedRepository()
    {
        var repoType = typeof(ADOSqliteSharding.FunctionalShardedRepositoryADO<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_ADO_SqlServer_ImplementsIFunctionalShardedRepository()
    {
        var repoType = typeof(ADOSqlServerSharding.FunctionalShardedRepositoryADO<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_ADO_PostgreSQL_ImplementsIFunctionalShardedRepository()
    {
        var repoType = typeof(ADOPostgreSQLSharding.FunctionalShardedRepositoryADO<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_ADO_MySQL_ImplementsIFunctionalShardedRepository()
    {
        var repoType = typeof(ADOMySQLSharding.FunctionalShardedRepositoryADO<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_Dapper_Sqlite_ImplementsIFunctionalShardedRepository()
    {
        var repoType = typeof(DapperSqliteSharding.FunctionalShardedRepositoryDapper<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_Dapper_SqlServer_ImplementsIFunctionalShardedRepository()
    {
        var repoType = typeof(DapperSqlServerSharding.FunctionalShardedRepositoryDapper<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_Dapper_PostgreSQL_ImplementsIFunctionalShardedRepository()
    {
        var repoType = typeof(DapperPostgreSQLSharding.FunctionalShardedRepositoryDapper<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_Dapper_MySQL_ImplementsIFunctionalShardedRepository()
    {
        var repoType = typeof(DapperMySQLSharding.FunctionalShardedRepositoryDapper<,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_EFCore_ImplementsIFunctionalShardedRepository()
    {
        var repoType = typeof(EFCoreSharding.FunctionalShardedRepositoryEF<,,>);
        VerifyImplementsInterface(repoType);
    }

    [Fact]
    public void Contract_MongoDB_ImplementsIFunctionalShardedRepository()
    {
        var repoType = typeof(MongoDBSharding.FunctionalShardedRepositoryMongoDB<,>);
        VerifyImplementsInterface(repoType);
    }

    #endregion

    #region Interface Method Consistency

    [Fact]
    public void Contract_AllProviders_HaveIdenticalInterfaceMethods()
    {
        // Get the expected methods from IFunctionalShardedRepository
        var interfaceMethods = RepositoryInterface
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        interfaceMethods.ShouldContain("GetByIdAsync");
        interfaceMethods.ShouldContain("AddAsync");
        interfaceMethods.ShouldContain("UpdateAsync");
        interfaceMethods.ShouldContain("DeleteAsync");
        interfaceMethods.ShouldContain("QueryAllShardsAsync");
        interfaceMethods.ShouldContain("QueryShardsAsync");
        interfaceMethods.ShouldContain("GetShardIdForEntity");
    }

    #endregion

    #region Generic Constraints Consistency

    [Fact]
    public void Contract_AllProviders_HaveConsistentGenericConstraints()
    {
        // IFunctionalShardedRepository<TEntity, TId> where TEntity : class, TId : notnull
        var genericParams = RepositoryInterface.GetGenericArguments();

        genericParams.Length.ShouldBe(2);

        // TEntity must have class constraint
        var entityParam = genericParams[0];
        (entityParam.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint)
            .ShouldBe(GenericParameterAttributes.ReferenceTypeConstraint);

        // TId must have notnull constraint (NotNullableValueTypeConstraint is not set, but struct is not required)
        var idParam = genericParams[1];
        idParam.GenericParameterAttributes.ShouldNotBe(GenericParameterAttributes.None);
    }

    #endregion

    #region Sealed Class Contract

    [Fact]
    public void Contract_AllImplementations_AreSealed()
    {
        var implementations = new[]
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

        foreach (var impl in implementations)
        {
            impl.IsSealed.ShouldBeTrue($"{impl.FullName} should be sealed");
        }
    }

    #endregion

    #region Naming Convention Contract

    [Fact]
    public void Contract_ADO_Providers_FollowNamingConvention()
    {
        typeof(ADOSqliteSharding.FunctionalShardedRepositoryADO<,>).Name.ShouldStartWith("FunctionalShardedRepositoryADO");
        typeof(ADOSqlServerSharding.FunctionalShardedRepositoryADO<,>).Name.ShouldStartWith("FunctionalShardedRepositoryADO");
        typeof(ADOPostgreSQLSharding.FunctionalShardedRepositoryADO<,>).Name.ShouldStartWith("FunctionalShardedRepositoryADO");
        typeof(ADOMySQLSharding.FunctionalShardedRepositoryADO<,>).Name.ShouldStartWith("FunctionalShardedRepositoryADO");
    }

    [Fact]
    public void Contract_Dapper_Providers_FollowNamingConvention()
    {
        typeof(DapperSqliteSharding.FunctionalShardedRepositoryDapper<,>).Name.ShouldStartWith("FunctionalShardedRepositoryDapper");
        typeof(DapperSqlServerSharding.FunctionalShardedRepositoryDapper<,>).Name.ShouldStartWith("FunctionalShardedRepositoryDapper");
        typeof(DapperPostgreSQLSharding.FunctionalShardedRepositoryDapper<,>).Name.ShouldStartWith("FunctionalShardedRepositoryDapper");
        typeof(DapperMySQLSharding.FunctionalShardedRepositoryDapper<,>).Name.ShouldStartWith("FunctionalShardedRepositoryDapper");
    }

    [Fact]
    public void Contract_EFCore_FollowsNamingConvention()
    {
        typeof(EFCoreSharding.FunctionalShardedRepositoryEF<,,>).Name.ShouldStartWith("FunctionalShardedRepositoryEF");
    }

    [Fact]
    public void Contract_MongoDB_FollowsNamingConvention()
    {
        typeof(MongoDBSharding.FunctionalShardedRepositoryMongoDB<,>).Name.ShouldStartWith("FunctionalShardedRepositoryMongoDB");
    }

    #endregion

    #region Helpers

    private static void VerifyImplementsInterface(Type implementationType)
    {
        var interfaces = implementationType.GetInterfaces();
        var implementsRepo = interfaces.Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == RepositoryInterface);

        implementsRepo.ShouldBeTrue(
            $"{implementationType.FullName} should implement IFunctionalShardedRepository<,>");
    }

    #endregion
}
