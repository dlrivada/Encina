using System.Reflection;
using ADOSqliteUoW = Encina.ADO.Sqlite.UnitOfWork;
using ADOSqlServerUoW = Encina.ADO.SqlServer.UnitOfWork;
using ADOPostgreSQLUoW = Encina.ADO.PostgreSQL.UnitOfWork;
using ADOMySQLUoW = Encina.ADO.MySQL.UnitOfWork;
using ADOOracleUoW = Encina.ADO.Oracle.UnitOfWork;
using DapperSqliteUoW = Encina.Dapper.Sqlite.UnitOfWork;
using DapperSqlServerUoW = Encina.Dapper.SqlServer.UnitOfWork;
using DapperPostgreSQLUoW = Encina.Dapper.PostgreSQL.UnitOfWork;
using DapperMySQLUoW = Encina.Dapper.MySQL.UnitOfWork;
using DapperOracleUoW = Encina.Dapper.Oracle.UnitOfWork;
using EfCoreUoW = Encina.EntityFrameworkCore.UnitOfWork;
using MongoDBUoW = Encina.MongoDB.UnitOfWork;
using Encina.DomainModeling;
using Shouldly;

namespace Encina.ContractTests.Database.UnitOfWork;

/// <summary>
/// Contract tests verifying that all Unit of Work implementations follow the same interface contracts.
/// These tests ensure behavioral and API consistency across all 12 database providers.
/// </summary>
[Trait("Category", "Contract")]
public sealed class UnitOfWorkContractTests
{
    #region IUnitOfWork Interface Implementation Contract Tests

    [Fact]
    public void Contract_AllADOProviders_ImplementIUnitOfWork()
    {
        // Contract: All ADO.NET UnitOfWork implementations must implement IUnitOfWork
        typeof(IUnitOfWork).IsAssignableFrom(typeof(ADOSqliteUoW.UnitOfWorkADO)).ShouldBeTrue(
            "ADO.Sqlite.UnitOfWorkADO must implement IUnitOfWork");
        typeof(IUnitOfWork).IsAssignableFrom(typeof(ADOSqlServerUoW.UnitOfWorkADO)).ShouldBeTrue(
            "ADO.SqlServer.UnitOfWorkADO must implement IUnitOfWork");
        typeof(IUnitOfWork).IsAssignableFrom(typeof(ADOPostgreSQLUoW.UnitOfWorkADO)).ShouldBeTrue(
            "ADO.PostgreSQL.UnitOfWorkADO must implement IUnitOfWork");
        typeof(IUnitOfWork).IsAssignableFrom(typeof(ADOMySQLUoW.UnitOfWorkADO)).ShouldBeTrue(
            "ADO.MySQL.UnitOfWorkADO must implement IUnitOfWork");
        typeof(IUnitOfWork).IsAssignableFrom(typeof(ADOOracleUoW.UnitOfWorkADO)).ShouldBeTrue(
            "ADO.Oracle.UnitOfWorkADO must implement IUnitOfWork");
    }

    [Fact]
    public void Contract_AllDapperProviders_ImplementIUnitOfWork()
    {
        // Contract: All Dapper UnitOfWork implementations must implement IUnitOfWork
        typeof(IUnitOfWork).IsAssignableFrom(typeof(DapperSqliteUoW.UnitOfWorkDapper)).ShouldBeTrue(
            "Dapper.Sqlite.UnitOfWorkDapper must implement IUnitOfWork");
        typeof(IUnitOfWork).IsAssignableFrom(typeof(DapperSqlServerUoW.UnitOfWorkDapper)).ShouldBeTrue(
            "Dapper.SqlServer.UnitOfWorkDapper must implement IUnitOfWork");
        typeof(IUnitOfWork).IsAssignableFrom(typeof(DapperPostgreSQLUoW.UnitOfWorkDapper)).ShouldBeTrue(
            "Dapper.PostgreSQL.UnitOfWorkDapper must implement IUnitOfWork");
        typeof(IUnitOfWork).IsAssignableFrom(typeof(DapperMySQLUoW.UnitOfWorkDapper)).ShouldBeTrue(
            "Dapper.MySQL.UnitOfWorkDapper must implement IUnitOfWork");
        typeof(IUnitOfWork).IsAssignableFrom(typeof(DapperOracleUoW.UnitOfWorkDapper)).ShouldBeTrue(
            "Dapper.Oracle.UnitOfWorkDapper must implement IUnitOfWork");
    }

    [Fact]
    public void Contract_EFCoreAndMongoDB_ImplementIUnitOfWork()
    {
        // Contract: EF Core and MongoDB UnitOfWork implementations must implement IUnitOfWork
        typeof(IUnitOfWork).IsAssignableFrom(typeof(EfCoreUoW.UnitOfWorkEF)).ShouldBeTrue(
            "EntityFrameworkCore.UnitOfWorkEF must implement IUnitOfWork");
        typeof(IUnitOfWork).IsAssignableFrom(typeof(MongoDBUoW.UnitOfWorkMongoDB)).ShouldBeTrue(
            "MongoDB.UnitOfWorkMongoDB must implement IUnitOfWork");
    }

    #endregion

    #region UnitOfWork API Consistency Contract Tests

    [Fact]
    public void Contract_AllADOProviders_HaveIdenticalPublicApi()
    {
        // Contract: All ADO.NET UnitOfWorkADO classes must have identical public APIs
        var adoSqliteType = typeof(ADOSqliteUoW.UnitOfWorkADO);
        var adoSqlServerType = typeof(ADOSqlServerUoW.UnitOfWorkADO);
        var adoPostgresType = typeof(ADOPostgreSQLUoW.UnitOfWorkADO);
        var adoMySQLType = typeof(ADOMySQLUoW.UnitOfWorkADO);
        var adoOracleType = typeof(ADOOracleUoW.UnitOfWorkADO);

        // Verify all ADO providers have the same public methods
        VerifyPublicMethodsMatch(adoSqliteType, adoSqlServerType, "ADO.SqlServer");
        VerifyPublicMethodsMatch(adoSqliteType, adoPostgresType, "ADO.PostgreSQL");
        VerifyPublicMethodsMatch(adoSqliteType, adoMySQLType, "ADO.MySQL");
        VerifyPublicMethodsMatch(adoSqliteType, adoOracleType, "ADO.Oracle");
    }

    [Fact]
    public void Contract_AllDapperProviders_HaveIdenticalPublicApi()
    {
        // Contract: All Dapper UnitOfWorkDapper classes must have identical public APIs
        var dapperSqliteType = typeof(DapperSqliteUoW.UnitOfWorkDapper);
        var dapperSqlServerType = typeof(DapperSqlServerUoW.UnitOfWorkDapper);
        var dapperPostgresType = typeof(DapperPostgreSQLUoW.UnitOfWorkDapper);
        var dapperMySQLType = typeof(DapperMySQLUoW.UnitOfWorkDapper);
        var dapperOracleType = typeof(DapperOracleUoW.UnitOfWorkDapper);

        // Verify all Dapper providers have the same public methods
        VerifyPublicMethodsMatch(dapperSqliteType, dapperSqlServerType, "Dapper.SqlServer");
        VerifyPublicMethodsMatch(dapperSqliteType, dapperPostgresType, "Dapper.PostgreSQL");
        VerifyPublicMethodsMatch(dapperSqliteType, dapperMySQLType, "Dapper.MySQL");
        VerifyPublicMethodsMatch(dapperSqliteType, dapperOracleType, "Dapper.Oracle");
    }

    [Fact]
    public void Contract_ADOAndDapper_HaveEquivalentPublicApi()
    {
        // Contract: ADO and Dapper UnitOfWork classes must have equivalent public APIs
        var adoType = typeof(ADOSqliteUoW.UnitOfWorkADO);
        var dapperType = typeof(DapperSqliteUoW.UnitOfWorkDapper);

        VerifyPublicMethodsMatch(adoType, dapperType, "Dapper.Sqlite vs ADO.Sqlite");
    }

    #endregion

    #region Constructor Signature Contract Tests

    [Fact]
    public void Contract_AllADOProviders_HaveConsistentConstructorSignatures()
    {
        // Contract: All ADO.NET UnitOfWorkADO classes must have the same constructor signature
        var providers = new[]
        {
            typeof(ADOSqliteUoW.UnitOfWorkADO),
            typeof(ADOSqlServerUoW.UnitOfWorkADO),
            typeof(ADOPostgreSQLUoW.UnitOfWorkADO),
            typeof(ADOMySQLUoW.UnitOfWorkADO),
            typeof(ADOOracleUoW.UnitOfWorkADO)
        };

        foreach (var provider in providers)
        {
            var constructors = provider.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            constructors.Length.ShouldBe(1,
                $"{provider.FullName} should have exactly one public constructor");

            var ctor = constructors[0];
            var parameters = ctor.GetParameters();

            parameters.Length.ShouldBe(2,
                $"{provider.FullName} constructor should have 2 parameters");

            // First parameter should be IDbConnection
            parameters[0].ParameterType.FullName.ShouldBe("System.Data.IDbConnection",
                $"{provider.FullName} first parameter should be IDbConnection");

            // Second parameter should be IServiceProvider
            parameters[1].ParameterType.ShouldBe(typeof(IServiceProvider),
                $"{provider.FullName} second parameter should be IServiceProvider");
        }
    }

    [Fact]
    public void Contract_AllDapperProviders_HaveConsistentConstructorSignatures()
    {
        // Contract: All Dapper UnitOfWorkDapper classes must have the same constructor signature
        var providers = new[]
        {
            typeof(DapperSqliteUoW.UnitOfWorkDapper),
            typeof(DapperSqlServerUoW.UnitOfWorkDapper),
            typeof(DapperPostgreSQLUoW.UnitOfWorkDapper),
            typeof(DapperMySQLUoW.UnitOfWorkDapper),
            typeof(DapperOracleUoW.UnitOfWorkDapper)
        };

        foreach (var provider in providers)
        {
            var constructors = provider.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            constructors.Length.ShouldBe(1,
                $"{provider.FullName} should have exactly one public constructor");

            var ctor = constructors[0];
            var parameters = ctor.GetParameters();

            parameters.Length.ShouldBe(2,
                $"{provider.FullName} constructor should have 2 parameters");

            // First parameter should be IDbConnection
            parameters[0].ParameterType.FullName.ShouldBe("System.Data.IDbConnection",
                $"{provider.FullName} first parameter should be IDbConnection");

            // Second parameter should be IServiceProvider
            parameters[1].ParameterType.ShouldBe(typeof(IServiceProvider),
                $"{provider.FullName} second parameter should be IServiceProvider");
        }
    }

    #endregion

    #region IAsyncDisposable Contract Tests

    [Fact]
    public void Contract_AllProviders_ImplementIAsyncDisposable()
    {
        // Contract: All UnitOfWork implementations must implement IAsyncDisposable
        var allProviders = new[]
        {
            typeof(ADOSqliteUoW.UnitOfWorkADO),
            typeof(ADOSqlServerUoW.UnitOfWorkADO),
            typeof(ADOPostgreSQLUoW.UnitOfWorkADO),
            typeof(ADOMySQLUoW.UnitOfWorkADO),
            typeof(ADOOracleUoW.UnitOfWorkADO),
            typeof(DapperSqliteUoW.UnitOfWorkDapper),
            typeof(DapperSqlServerUoW.UnitOfWorkDapper),
            typeof(DapperPostgreSQLUoW.UnitOfWorkDapper),
            typeof(DapperMySQLUoW.UnitOfWorkDapper),
            typeof(DapperOracleUoW.UnitOfWorkDapper),
            typeof(EfCoreUoW.UnitOfWorkEF),
            typeof(MongoDBUoW.UnitOfWorkMongoDB)
        };

        foreach (var provider in allProviders)
        {
            typeof(IAsyncDisposable).IsAssignableFrom(provider).ShouldBeTrue(
                $"{provider.FullName} must implement IAsyncDisposable");
        }
    }

    #endregion

    #region Sealed Class Contract Tests

    [Fact]
    public void Contract_AllUnitOfWorkImplementations_AreSealed()
    {
        // Contract: All UnitOfWork implementations should be sealed for performance and safety
        var allProviders = new[]
        {
            typeof(ADOSqliteUoW.UnitOfWorkADO),
            typeof(ADOSqlServerUoW.UnitOfWorkADO),
            typeof(ADOPostgreSQLUoW.UnitOfWorkADO),
            typeof(ADOMySQLUoW.UnitOfWorkADO),
            typeof(ADOOracleUoW.UnitOfWorkADO),
            typeof(DapperSqliteUoW.UnitOfWorkDapper),
            typeof(DapperSqlServerUoW.UnitOfWorkDapper),
            typeof(DapperPostgreSQLUoW.UnitOfWorkDapper),
            typeof(DapperMySQLUoW.UnitOfWorkDapper),
            typeof(DapperOracleUoW.UnitOfWorkDapper),
            typeof(EfCoreUoW.UnitOfWorkEF),
            typeof(MongoDBUoW.UnitOfWorkMongoDB)
        };

        foreach (var provider in allProviders)
        {
            provider.IsSealed.ShouldBeTrue(
                $"{provider.FullName} should be sealed");
        }
    }

    #endregion

    #region HasActiveTransaction Property Contract Tests

    [Fact]
    public void Contract_AllProviders_HaveHasActiveTransactionProperty()
    {
        // Contract: All UnitOfWork implementations must have HasActiveTransaction property
        var allProviders = new[]
        {
            typeof(ADOSqliteUoW.UnitOfWorkADO),
            typeof(ADOSqlServerUoW.UnitOfWorkADO),
            typeof(ADOPostgreSQLUoW.UnitOfWorkADO),
            typeof(ADOMySQLUoW.UnitOfWorkADO),
            typeof(ADOOracleUoW.UnitOfWorkADO),
            typeof(DapperSqliteUoW.UnitOfWorkDapper),
            typeof(DapperSqlServerUoW.UnitOfWorkDapper),
            typeof(DapperPostgreSQLUoW.UnitOfWorkDapper),
            typeof(DapperMySQLUoW.UnitOfWorkDapper),
            typeof(DapperOracleUoW.UnitOfWorkDapper),
            typeof(EfCoreUoW.UnitOfWorkEF),
            typeof(MongoDBUoW.UnitOfWorkMongoDB)
        };

        foreach (var provider in allProviders)
        {
            var property = provider.GetProperty("HasActiveTransaction", BindingFlags.Public | BindingFlags.Instance);

            property.ShouldNotBeNull(
                $"{provider.FullName} must have HasActiveTransaction property");
            property!.PropertyType.ShouldBe(typeof(bool),
                $"{provider.FullName}.HasActiveTransaction must be of type bool");
            property.GetMethod.ShouldNotBeNull(
                $"{provider.FullName}.HasActiveTransaction must have a getter");
        }
    }

    #endregion

    #region UnitOfWorkRepository Contract Tests

    [Fact]
    public void Contract_AllADOProviders_HaveUnitOfWorkRepositoryADO()
    {
        // Contract: All ADO.NET providers must have a UnitOfWorkRepositoryADO class
        var adoSqliteType = typeof(ADOSqliteUoW.UnitOfWorkADO).Assembly.GetType("Encina.ADO.Sqlite.UnitOfWork.UnitOfWorkRepositoryADO`2");
        var adoSqlServerType = typeof(ADOSqlServerUoW.UnitOfWorkADO).Assembly.GetType("Encina.ADO.SqlServer.UnitOfWork.UnitOfWorkRepositoryADO`2");
        var adoPostgresType = typeof(ADOPostgreSQLUoW.UnitOfWorkADO).Assembly.GetType("Encina.ADO.PostgreSQL.UnitOfWork.UnitOfWorkRepositoryADO`2");
        var adoMySQLType = typeof(ADOMySQLUoW.UnitOfWorkADO).Assembly.GetType("Encina.ADO.MySQL.UnitOfWork.UnitOfWorkRepositoryADO`2");
        var adoOracleType = typeof(ADOOracleUoW.UnitOfWorkADO).Assembly.GetType("Encina.ADO.Oracle.UnitOfWork.UnitOfWorkRepositoryADO`2");

        adoSqliteType.ShouldNotBeNull("ADO.Sqlite must have UnitOfWorkRepositoryADO");
        adoSqlServerType.ShouldNotBeNull("ADO.SqlServer must have UnitOfWorkRepositoryADO");
        adoPostgresType.ShouldNotBeNull("ADO.PostgreSQL must have UnitOfWorkRepositoryADO");
        adoMySQLType.ShouldNotBeNull("ADO.MySQL must have UnitOfWorkRepositoryADO");
        adoOracleType.ShouldNotBeNull("ADO.Oracle must have UnitOfWorkRepositoryADO");
    }

    [Fact]
    public void Contract_AllDapperProviders_HaveUnitOfWorkRepositoryDapper()
    {
        // Contract: All Dapper providers must have a UnitOfWorkRepositoryDapper class
        var dapperSqliteType = typeof(DapperSqliteUoW.UnitOfWorkDapper).Assembly.GetType("Encina.Dapper.Sqlite.UnitOfWork.UnitOfWorkRepositoryDapper`2");
        var dapperSqlServerType = typeof(DapperSqlServerUoW.UnitOfWorkDapper).Assembly.GetType("Encina.Dapper.SqlServer.UnitOfWork.UnitOfWorkRepositoryDapper`2");
        var dapperPostgresType = typeof(DapperPostgreSQLUoW.UnitOfWorkDapper).Assembly.GetType("Encina.Dapper.PostgreSQL.UnitOfWork.UnitOfWorkRepositoryDapper`2");
        var dapperMySQLType = typeof(DapperMySQLUoW.UnitOfWorkDapper).Assembly.GetType("Encina.Dapper.MySQL.UnitOfWork.UnitOfWorkRepositoryDapper`2");
        var dapperOracleType = typeof(DapperOracleUoW.UnitOfWorkDapper).Assembly.GetType("Encina.Dapper.Oracle.UnitOfWork.UnitOfWorkRepositoryDapper`2");

        dapperSqliteType.ShouldNotBeNull("Dapper.Sqlite must have UnitOfWorkRepositoryDapper");
        dapperSqlServerType.ShouldNotBeNull("Dapper.SqlServer must have UnitOfWorkRepositoryDapper");
        dapperPostgresType.ShouldNotBeNull("Dapper.PostgreSQL must have UnitOfWorkRepositoryDapper");
        dapperMySQLType.ShouldNotBeNull("Dapper.MySQL must have UnitOfWorkRepositoryDapper");
        dapperOracleType.ShouldNotBeNull("Dapper.Oracle must have UnitOfWorkRepositoryDapper");
    }

    #endregion

    #region IUnitOfWork Interface Members Contract

    [Fact]
    public void Contract_IUnitOfWork_HasRequiredMembers()
    {
        // Contract: IUnitOfWork interface must have all required members
        var iuowType = typeof(IUnitOfWork);

        // Check methods
        var methods = iuowType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Select(m => m.Name).ShouldContain("Repository",
            "IUnitOfWork must have Repository method");
        methods.Select(m => m.Name).ShouldContain("SaveChangesAsync",
            "IUnitOfWork must have SaveChangesAsync method");
        methods.Select(m => m.Name).ShouldContain("BeginTransactionAsync",
            "IUnitOfWork must have BeginTransactionAsync method");
        methods.Select(m => m.Name).ShouldContain("CommitAsync",
            "IUnitOfWork must have CommitAsync method");
        methods.Select(m => m.Name).ShouldContain("RollbackAsync",
            "IUnitOfWork must have RollbackAsync method");

        // Check property
        var hasActiveTransactionProp = iuowType.GetProperty("HasActiveTransaction");
        hasActiveTransactionProp.ShouldNotBeNull(
            "IUnitOfWork must have HasActiveTransaction property");
        hasActiveTransactionProp!.PropertyType.ShouldBe(typeof(bool));
    }

    [Fact]
    public void Contract_IUnitOfWork_InheritsFromIAsyncDisposable()
    {
        // Contract: IUnitOfWork must inherit from IAsyncDisposable
        typeof(IAsyncDisposable).IsAssignableFrom(typeof(IUnitOfWork)).ShouldBeTrue(
            "IUnitOfWork must inherit from IAsyncDisposable");
    }

    #endregion

    #region Helper Methods

    private static void VerifyPublicMethodsMatch(Type referenceType, Type compareType, string providerName)
    {
        var referenceMethods = GetPublicMethods(referenceType);
        var compareMethods = GetPublicMethods(compareType);

        // All reference methods should exist in compare type
        foreach (var method in referenceMethods)
        {
            compareMethods.ShouldContain(method,
                $"{providerName} is missing public method: {method}");
        }

        // No extra methods in compare type
        foreach (var method in compareMethods)
        {
            referenceMethods.ShouldContain(method,
                $"{providerName} has extra public method: {method}");
        }
    }

    private static HashSet<string> GetPublicMethods(Type type)
    {
        return type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName) // Exclude property getters/setters
            .Select(m => m.Name)
            .ToHashSet();
    }

    #endregion
}
