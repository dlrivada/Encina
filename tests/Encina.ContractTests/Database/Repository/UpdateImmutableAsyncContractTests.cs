using Encina.DomainModeling;
using LanguageExt;
using Shouldly;
using ADOMySQLRepo = Encina.ADO.MySQL.Repository;
using ADOPostgreSQLRepo = Encina.ADO.PostgreSQL.Repository;
using ADOSqliteRepo = Encina.ADO.Sqlite.Repository;
using ADOSqlServerRepo = Encina.ADO.SqlServer.Repository;
using DapperMySQLRepo = Encina.Dapper.MySQL.Repository;
using DapperPostgreSQLRepo = Encina.Dapper.PostgreSQL.Repository;
using DapperSqliteRepo = Encina.Dapper.Sqlite.Repository;
using DapperSqlServerRepo = Encina.Dapper.SqlServer.Repository;
using EfCoreRepo = Encina.EntityFrameworkCore.Repository;
using MongoDBRepo = Encina.MongoDB.Repository;

namespace Encina.ContractTests.Database.Repository;

/// <summary>
/// Contract tests verifying that UpdateImmutableAsync method is implemented consistently
/// across all repository implementations (FunctionalRepository and UnitOfWorkRepository).
/// </summary>
/// <remarks>
/// Note: UnitOfWorkRepository classes are internal in all providers, so their contracts
/// are enforced by implementing IFunctionalRepository&lt;TEntity, TId&gt; interface.
/// This test class only verifies public FunctionalRepository implementations.
/// </remarks>
[Trait("Category", "Contract")]
public sealed class UpdateImmutableAsyncContractTests
{
    #region IFunctionalRepository Interface Contract Tests

    [Fact]
    public void Contract_IFunctionalRepository_DefinesUpdateImmutableAsync()
    {
        // Contract: IFunctionalRepository interface must define UpdateImmutableAsync method
        var interfaceType = typeof(IFunctionalRepository<,>);
        var method = interfaceType.GetMethod("UpdateImmutableAsync");

        method.ShouldNotBeNull("IFunctionalRepository must define UpdateImmutableAsync method");

        // Verify return type is Task<Either<EncinaError, Unit>>
        var returnType = method.ReturnType;
        returnType.IsGenericType.ShouldBeTrue();
        returnType.GetGenericTypeDefinition().ShouldBe(typeof(Task<>));

        // Verify parameters
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].Name.ShouldBe("modified");
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region FunctionalRepository Method Signature Contract Tests

    [Fact]
    public void Contract_AllFunctionalRepositories_HaveUpdateImmutableAsync()
    {
        // Contract: All FunctionalRepository implementations must have UpdateImmutableAsync
        var allRepoTypes = GetAllFunctionalRepositoryTypes();

        foreach (var repoType in allRepoTypes)
        {
            var method = repoType.GetMethod("UpdateImmutableAsync");
            method.ShouldNotBeNull($"{repoType.Name} must have UpdateImmutableAsync method");

            // Verify return type is Task<Either<EncinaError, Unit>>
            var returnType = method.ReturnType;
            returnType.IsGenericType.ShouldBeTrue();
            returnType.GetGenericTypeDefinition().ShouldBe(typeof(Task<>),
                $"{repoType.Name}.UpdateImmutableAsync must return Task<Either<EncinaError, Unit>>");
        }
    }

    [Fact]
    public void Contract_UpdateImmutableAsync_HasCorrectParameterSignature()
    {
        // Contract: UpdateImmutableAsync(TEntity modified, CancellationToken) must have two parameters
        var allRepoTypes = GetAllFunctionalRepositoryTypes();

        foreach (var repoType in allRepoTypes)
        {
            var method = repoType.GetMethod("UpdateImmutableAsync")!;
            var parameters = method.GetParameters();

            parameters.Length.ShouldBe(2,
                $"{repoType.Name}.UpdateImmutableAsync must have exactly 2 parameters");

            parameters[0].Name.ShouldBe("modified",
                $"{repoType.Name}.UpdateImmutableAsync first parameter must be named 'modified'");

            parameters[1].ParameterType.ShouldBe(typeof(CancellationToken),
                $"{repoType.Name}.UpdateImmutableAsync second parameter must be CancellationToken");

            // CancellationToken should have a default value
            parameters[1].HasDefaultValue.ShouldBeTrue(
                $"{repoType.Name}.UpdateImmutableAsync CancellationToken must have default value");
        }
    }

    #endregion

    #region API Consistency Contract Tests

    [Fact]
    public void Contract_AllADOFunctionalRepositories_HaveIdenticalSignature()
    {
        // Contract: All ADO.NET FunctionalRepository implementations must have identical signatures
        var adoTypes = new[]
        {
            typeof(ADOSqliteRepo.FunctionalRepositoryADO<,>),
            typeof(ADOSqlServerRepo.FunctionalRepositoryADO<,>),
            typeof(ADOPostgreSQLRepo.FunctionalRepositoryADO<,>),
            typeof(ADOMySQLRepo.FunctionalRepositoryADO<,>)
        };

        VerifyMethodSignaturesMatch(adoTypes, "UpdateImmutableAsync");
    }

    [Fact]
    public void Contract_AllDapperFunctionalRepositories_HaveIdenticalSignature()
    {
        // Contract: All Dapper FunctionalRepository implementations must have identical signatures
        var dapperTypes = new[]
        {
            typeof(DapperSqliteRepo.FunctionalRepositoryDapper<,>),
            typeof(DapperSqlServerRepo.FunctionalRepositoryDapper<,>),
            typeof(DapperPostgreSQLRepo.FunctionalRepositoryDapper<,>),
            typeof(DapperMySQLRepo.FunctionalRepositoryDapper<,>)
        };

        VerifyMethodSignaturesMatch(dapperTypes, "UpdateImmutableAsync");
    }

    [Fact]
    public void Contract_AllProviderFamilies_HaveConsistentSignature()
    {
        // Contract: All provider families (ADO, Dapper, EF Core, MongoDB) must have consistent signatures
        var representativeTypes = new[]
        {
            typeof(ADOSqliteRepo.FunctionalRepositoryADO<,>),
            typeof(DapperSqliteRepo.FunctionalRepositoryDapper<,>),
            typeof(EfCoreRepo.FunctionalRepositoryEF<,>),
            typeof(MongoDBRepo.FunctionalRepositoryMongoDB<,>)
        };

        VerifyMethodSignaturesMatch(representativeTypes, "UpdateImmutableAsync");
    }

    #endregion

    #region Helper Methods

    private static Type[] GetAllFunctionalRepositoryTypes()
    {
        return
        [
            // ADO.NET providers (4)
            typeof(ADOSqliteRepo.FunctionalRepositoryADO<,>),
            typeof(ADOSqlServerRepo.FunctionalRepositoryADO<,>),
            typeof(ADOPostgreSQLRepo.FunctionalRepositoryADO<,>),
            typeof(ADOMySQLRepo.FunctionalRepositoryADO<,>),
            // Dapper providers (4)
            typeof(DapperSqliteRepo.FunctionalRepositoryDapper<,>),
            typeof(DapperSqlServerRepo.FunctionalRepositoryDapper<,>),
            typeof(DapperPostgreSQLRepo.FunctionalRepositoryDapper<,>),
            typeof(DapperMySQLRepo.FunctionalRepositoryDapper<,>),
            // EF Core (1)
            typeof(EfCoreRepo.FunctionalRepositoryEF<,>),
            // MongoDB (1)
            typeof(MongoDBRepo.FunctionalRepositoryMongoDB<,>)
        ];
    }

    private static void VerifyMethodSignaturesMatch(Type[] types, string methodName)
    {
        var referenceType = types[0];
        var referenceMethod = referenceType.GetMethod(methodName);
        referenceMethod.ShouldNotBeNull($"{referenceType.Name} must have {methodName} method");

        var referenceParams = referenceMethod.GetParameters();
        var referenceReturnType = referenceMethod.ReturnType;

        foreach (var type in types.Skip(1))
        {
            var method = type.GetMethod(methodName);
            method.ShouldNotBeNull($"{type.Name} must have {methodName} method");

            var parameters = method.GetParameters();
            parameters.Length.ShouldBe(referenceParams.Length,
                $"{type.Name}.{methodName} must have same parameter count as {referenceType.Name}");

            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i].Name.ShouldBe(referenceParams[i].Name,
                    $"{type.Name}.{methodName} parameter {i} must have same name as {referenceType.Name}");
            }

            // Return types must be equivalent (allow for generic type variation)
            method.ReturnType.GetGenericTypeDefinition()
                .ShouldBe(referenceReturnType.GetGenericTypeDefinition(),
                    $"{type.Name}.{methodName} must have same return type as {referenceType.Name}");
        }
    }

    #endregion
}
