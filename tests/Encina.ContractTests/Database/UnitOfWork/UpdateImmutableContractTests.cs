using System.Reflection;
using Encina.DomainModeling;
using LanguageExt;
using Shouldly;
using ADOMySQLUoW = Encina.ADO.MySQL.UnitOfWork;
using ADOPostgreSQLUoW = Encina.ADO.PostgreSQL.UnitOfWork;
using ADOSqliteUoW = Encina.ADO.Sqlite.UnitOfWork;
using ADOSqlServerUoW = Encina.ADO.SqlServer.UnitOfWork;
using DapperMySQLUoW = Encina.Dapper.MySQL.UnitOfWork;
using DapperPostgreSQLUoW = Encina.Dapper.PostgreSQL.UnitOfWork;
using DapperSqliteUoW = Encina.Dapper.Sqlite.UnitOfWork;
using DapperSqlServerUoW = Encina.Dapper.SqlServer.UnitOfWork;
using EfCoreUoW = Encina.EntityFrameworkCore.UnitOfWork;
using MongoDBUoW = Encina.MongoDB.UnitOfWork;

namespace Encina.ContractTests.Database.UnitOfWork;

/// <summary>
/// Contract tests verifying that UpdateImmutable methods are implemented consistently
/// across all 13 Unit of Work providers.
/// </summary>
[Trait("Category", "Contract")]
public sealed class UpdateImmutableContractTests
{
    #region UpdateImmutable Method Signature Contract Tests

    [Fact]
    public void Contract_AllProviders_HaveUpdateImmutableMethod()
    {
        // Contract: All UnitOfWork implementations must have UpdateImmutable<TEntity> method
        var allProviderTypes = GetAllUnitOfWorkTypes();

        foreach (var providerType in allProviderTypes)
        {
            var method = providerType.GetMethod("UpdateImmutable");
            method.ShouldNotBeNull($"{providerType.Name} must have UpdateImmutable method");

            // Verify it's a generic method
            method.IsGenericMethod.ShouldBeTrue(
                $"{providerType.Name}.UpdateImmutable must be a generic method");

            // Verify return type is Either<EncinaError, Unit>
            var returnType = method.ReturnType;
            returnType.IsGenericType.ShouldBeTrue();
            returnType.GetGenericTypeDefinition().ShouldBe(typeof(Either<,>),
                $"{providerType.Name}.UpdateImmutable must return Either<EncinaError, Unit>");
        }
    }

    [Fact]
    public void Contract_AllProviders_HaveUpdateImmutableAsyncMethod()
    {
        // Contract: All UnitOfWork implementations must have UpdateImmutableAsync<TEntity> method
        var allProviderTypes = GetAllUnitOfWorkTypes();

        foreach (var providerType in allProviderTypes)
        {
            var method = providerType.GetMethod("UpdateImmutableAsync");
            method.ShouldNotBeNull($"{providerType.Name} must have UpdateImmutableAsync method");

            // Verify it's a generic method
            method.IsGenericMethod.ShouldBeTrue(
                $"{providerType.Name}.UpdateImmutableAsync must be a generic method");

            // Verify return type is Task<Either<EncinaError, Unit>>
            var returnType = method.ReturnType;
            returnType.IsGenericType.ShouldBeTrue();
            returnType.GetGenericTypeDefinition().ShouldBe(typeof(Task<>),
                $"{providerType.Name}.UpdateImmutableAsync must return Task<Either<EncinaError, Unit>>");
        }
    }

    [Fact]
    public void Contract_UpdateImmutable_HasCorrectParameterSignature()
    {
        // Contract: UpdateImmutable<TEntity>(TEntity modified) must have exactly one parameter
        var allProviderTypes = GetAllUnitOfWorkTypes();

        foreach (var providerType in allProviderTypes)
        {
            var method = providerType.GetMethod("UpdateImmutable")!;
            var parameters = method.GetParameters();

            parameters.Length.ShouldBe(1,
                $"{providerType.Name}.UpdateImmutable must have exactly 1 parameter");

            // Parameter should be the generic type TEntity
            parameters[0].Name.ShouldBe("modified",
                $"{providerType.Name}.UpdateImmutable parameter must be named 'modified'");
        }
    }

    [Fact]
    public void Contract_UpdateImmutableAsync_HasCorrectParameterSignature()
    {
        // Contract: UpdateImmutableAsync<TEntity>(TEntity modified, CancellationToken) must have two parameters
        var allProviderTypes = GetAllUnitOfWorkTypes();

        foreach (var providerType in allProviderTypes)
        {
            var method = providerType.GetMethod("UpdateImmutableAsync")!;
            var parameters = method.GetParameters();

            parameters.Length.ShouldBe(2,
                $"{providerType.Name}.UpdateImmutableAsync must have exactly 2 parameters");

            parameters[0].Name.ShouldBe("modified",
                $"{providerType.Name}.UpdateImmutableAsync first parameter must be named 'modified'");

            parameters[1].ParameterType.ShouldBe(typeof(CancellationToken),
                $"{providerType.Name}.UpdateImmutableAsync second parameter must be CancellationToken");

            // CancellationToken should have a default value
            parameters[1].HasDefaultValue.ShouldBeTrue(
                $"{providerType.Name}.UpdateImmutableAsync CancellationToken must have default value");
        }
    }

    #endregion

    #region Type Constraint Contract Tests

    [Fact]
    public void Contract_UpdateImmutable_HasClassConstraint()
    {
        // Contract: UpdateImmutable<TEntity> must have 'where TEntity : class' constraint
        var allProviderTypes = GetAllUnitOfWorkTypes();

        foreach (var providerType in allProviderTypes)
        {
            var method = providerType.GetMethod("UpdateImmutable")!;
            var genericArguments = method.GetGenericArguments();

            genericArguments.Length.ShouldBe(1,
                $"{providerType.Name}.UpdateImmutable must have exactly 1 generic type parameter");

            var constraints = genericArguments[0].GetGenericParameterConstraints();
            // 'class' constraint is represented by ReferenceTypeConstraint attribute
            (genericArguments[0].GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint)
                .ShouldBe(GenericParameterAttributes.ReferenceTypeConstraint,
                    $"{providerType.Name}.UpdateImmutable TEntity must have 'class' constraint");
        }
    }

    [Fact]
    public void Contract_UpdateImmutableAsync_HasClassConstraint()
    {
        // Contract: UpdateImmutableAsync<TEntity> must have 'where TEntity : class' constraint
        var allProviderTypes = GetAllUnitOfWorkTypes();

        foreach (var providerType in allProviderTypes)
        {
            var method = providerType.GetMethod("UpdateImmutableAsync")!;
            var genericArguments = method.GetGenericArguments();

            genericArguments.Length.ShouldBe(1,
                $"{providerType.Name}.UpdateImmutableAsync must have exactly 1 generic type parameter");

            (genericArguments[0].GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint)
                .ShouldBe(GenericParameterAttributes.ReferenceTypeConstraint,
                    $"{providerType.Name}.UpdateImmutableAsync TEntity must have 'class' constraint");
        }
    }

    #endregion

    #region API Consistency Contract Tests

    [Fact]
    public void Contract_AllADOProviders_HaveIdenticalUpdateImmutableSignature()
    {
        // Contract: All ADO.NET providers must have identical UpdateImmutable signatures
        var adoTypes = new[]
        {
            typeof(ADOSqliteUoW.UnitOfWorkADO),
            typeof(ADOSqlServerUoW.UnitOfWorkADO),
            typeof(ADOPostgreSQLUoW.UnitOfWorkADO),
            typeof(ADOMySQLUoW.UnitOfWorkADO)
        };

        VerifyMethodSignaturesMatch(adoTypes, "UpdateImmutable");
        VerifyMethodSignaturesMatch(adoTypes, "UpdateImmutableAsync");
    }

    [Fact]
    public void Contract_AllDapperProviders_HaveIdenticalUpdateImmutableSignature()
    {
        // Contract: All Dapper providers must have identical UpdateImmutable signatures
        var dapperTypes = new[]
        {
            typeof(DapperSqliteUoW.UnitOfWorkDapper),
            typeof(DapperSqlServerUoW.UnitOfWorkDapper),
            typeof(DapperPostgreSQLUoW.UnitOfWorkDapper),
            typeof(DapperMySQLUoW.UnitOfWorkDapper)
        };

        VerifyMethodSignaturesMatch(dapperTypes, "UpdateImmutable");
        VerifyMethodSignaturesMatch(dapperTypes, "UpdateImmutableAsync");
    }

    #endregion

    #region Helper Methods

    private static Type[] GetAllUnitOfWorkTypes()
    {
        return
        [
            // ADO.NET providers (4)
            typeof(ADOSqliteUoW.UnitOfWorkADO),
            typeof(ADOSqlServerUoW.UnitOfWorkADO),
            typeof(ADOPostgreSQLUoW.UnitOfWorkADO),
            typeof(ADOMySQLUoW.UnitOfWorkADO),
            // Dapper providers (4)
            typeof(DapperSqliteUoW.UnitOfWorkDapper),
            typeof(DapperSqlServerUoW.UnitOfWorkDapper),
            typeof(DapperPostgreSQLUoW.UnitOfWorkDapper),
            typeof(DapperMySQLUoW.UnitOfWorkDapper),
            // EF Core (1)
            typeof(EfCoreUoW.UnitOfWorkEF),
            // MongoDB (1)
            typeof(MongoDBUoW.UnitOfWorkMongoDB)
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
