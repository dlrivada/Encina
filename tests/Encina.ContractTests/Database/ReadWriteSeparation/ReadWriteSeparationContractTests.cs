using System.Data;
using Shouldly;
using Xunit;

// Provider-specific aliases for factories
using ADOSqliteRW = Encina.ADO.Sqlite.ReadWriteSeparation;
using ADOSqlServerRW = Encina.ADO.SqlServer.ReadWriteSeparation;
using ADOPostgreSQLRW = Encina.ADO.PostgreSQL.ReadWriteSeparation;
using ADOMySQLRW = Encina.ADO.MySQL.ReadWriteSeparation;
using ADOOracleRW = Encina.ADO.Oracle.ReadWriteSeparation;
using DapperSqliteRW = Encina.Dapper.Sqlite.ReadWriteSeparation;
using DapperSqlServerRW = Encina.Dapper.SqlServer.ReadWriteSeparation;
using DapperPostgreSQLRW = Encina.Dapper.PostgreSQL.ReadWriteSeparation;
using DapperMySQLRW = Encina.Dapper.MySQL.ReadWriteSeparation;
using DapperOracleRW = Encina.Dapper.Oracle.ReadWriteSeparation;
using EntityFrameworkCoreRW = Encina.EntityFrameworkCore.ReadWriteSeparation;

namespace Encina.ContractTests.Database.ReadWriteSeparation;

/// <summary>
/// Contract tests verifying that all 12 providers implement the Read/Write Separation pattern
/// with consistent APIs.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>All providers expose the correct factory interface</description></item>
///   <item><description>All providers implement the same method signatures</description></item>
///   <item><description>All providers have consistent health check implementations</description></item>
///   <item><description>All providers have consistent pipeline behavior implementations</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Contract")]
[Trait("Feature", "ReadWriteSeparation")]
public sealed class ReadWriteSeparationContractTests
{
    #region Interface Implementation Tests - ADO Providers

    [Fact]
    public void ADOSqlite_ReadWriteConnectionFactory_ImplementsIReadWriteConnectionFactory()
    {
        // Assert
        typeof(ADOSqliteRW.IReadWriteConnectionFactory).IsAssignableFrom(
            typeof(ADOSqliteRW.ReadWriteConnectionFactory)).ShouldBeTrue(
            "ADO.Sqlite.ReadWriteConnectionFactory must implement IReadWriteConnectionFactory");
    }

    [Fact]
    public void ADOSqlServer_ReadWriteConnectionFactory_ImplementsIReadWriteConnectionFactory()
    {
        // Assert
        typeof(ADOSqlServerRW.IReadWriteConnectionFactory).IsAssignableFrom(
            typeof(ADOSqlServerRW.ReadWriteConnectionFactory)).ShouldBeTrue(
            "ADO.SqlServer.ReadWriteConnectionFactory must implement IReadWriteConnectionFactory");
    }

    [Fact]
    public void ADOPostgreSQL_ReadWriteConnectionFactory_ImplementsIReadWriteConnectionFactory()
    {
        // Assert
        typeof(ADOPostgreSQLRW.IReadWriteConnectionFactory).IsAssignableFrom(
            typeof(ADOPostgreSQLRW.ReadWriteConnectionFactory)).ShouldBeTrue(
            "ADO.PostgreSQL.ReadWriteConnectionFactory must implement IReadWriteConnectionFactory");
    }

    [Fact]
    public void ADOMySQL_ReadWriteConnectionFactory_ImplementsIReadWriteConnectionFactory()
    {
        // Assert
        typeof(ADOMySQLRW.IReadWriteConnectionFactory).IsAssignableFrom(
            typeof(ADOMySQLRW.ReadWriteConnectionFactory)).ShouldBeTrue(
            "ADO.MySQL.ReadWriteConnectionFactory must implement IReadWriteConnectionFactory");
    }

    [Fact]
    public void ADOOracle_ReadWriteConnectionFactory_ImplementsIReadWriteConnectionFactory()
    {
        // Assert
        typeof(ADOOracleRW.IReadWriteConnectionFactory).IsAssignableFrom(
            typeof(ADOOracleRW.ReadWriteConnectionFactory)).ShouldBeTrue(
            "ADO.Oracle.ReadWriteConnectionFactory must implement IReadWriteConnectionFactory");
    }

    #endregion

    #region Interface Implementation Tests - Dapper Providers

    [Fact]
    public void DapperSqlite_ReadWriteConnectionFactory_ImplementsIReadWriteConnectionFactory()
    {
        // Assert
        typeof(DapperSqliteRW.IReadWriteConnectionFactory).IsAssignableFrom(
            typeof(DapperSqliteRW.ReadWriteConnectionFactory)).ShouldBeTrue(
            "Dapper.Sqlite.ReadWriteConnectionFactory must implement IReadWriteConnectionFactory");
    }

    [Fact]
    public void DapperSqlServer_ReadWriteConnectionFactory_ImplementsIReadWriteConnectionFactory()
    {
        // Assert
        typeof(DapperSqlServerRW.IReadWriteConnectionFactory).IsAssignableFrom(
            typeof(DapperSqlServerRW.ReadWriteConnectionFactory)).ShouldBeTrue(
            "Dapper.SqlServer.ReadWriteConnectionFactory must implement IReadWriteConnectionFactory");
    }

    [Fact]
    public void DapperPostgreSQL_ReadWriteConnectionFactory_ImplementsIReadWriteConnectionFactory()
    {
        // Assert
        typeof(DapperPostgreSQLRW.IReadWriteConnectionFactory).IsAssignableFrom(
            typeof(DapperPostgreSQLRW.ReadWriteConnectionFactory)).ShouldBeTrue(
            "Dapper.PostgreSQL.ReadWriteConnectionFactory must implement IReadWriteConnectionFactory");
    }

    [Fact]
    public void DapperMySQL_ReadWriteConnectionFactory_ImplementsIReadWriteConnectionFactory()
    {
        // Assert
        typeof(DapperMySQLRW.IReadWriteConnectionFactory).IsAssignableFrom(
            typeof(DapperMySQLRW.ReadWriteConnectionFactory)).ShouldBeTrue(
            "Dapper.MySQL.ReadWriteConnectionFactory must implement IReadWriteConnectionFactory");
    }

    [Fact]
    public void DapperOracle_ReadWriteConnectionFactory_ImplementsIReadWriteConnectionFactory()
    {
        // Assert
        typeof(DapperOracleRW.IReadWriteConnectionFactory).IsAssignableFrom(
            typeof(DapperOracleRW.ReadWriteConnectionFactory)).ShouldBeTrue(
            "Dapper.Oracle.ReadWriteConnectionFactory must implement IReadWriteConnectionFactory");
    }

    #endregion

    #region Interface Implementation Tests - EF Core

    [Fact]
    public void EntityFrameworkCore_ReadWriteDbContextFactory_ImplementsIReadWriteDbContextFactory()
    {
        // Assert
        typeof(EntityFrameworkCoreRW.IReadWriteDbContextFactory<>).IsGenericType.ShouldBeTrue(
            "EF Core IReadWriteDbContextFactory should be a generic interface");

        typeof(EntityFrameworkCoreRW.ReadWriteDbContextFactory<>).IsGenericType.ShouldBeTrue(
            "EF Core ReadWriteDbContextFactory should be a generic class");
    }

    #endregion

    #region Consistent Method Signatures - IReadWriteConnectionFactory

    [Theory]
    [InlineData(typeof(ADOSqliteRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOOracleRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqliteRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperOracleRW.IReadWriteConnectionFactory))]
    public void AllProviders_IReadWriteConnectionFactory_HasCreateWriteConnectionMethod(Type factoryType)
    {
        // Act
        var method = factoryType.GetMethod("CreateWriteConnection");

        // Assert
        method.ShouldNotBeNull($"{factoryType.Name} should have CreateWriteConnection method");
        method.ReturnType.ShouldBe(typeof(IDbConnection));
        method.GetParameters().Length.ShouldBe(0);
    }

    [Theory]
    [InlineData(typeof(ADOSqliteRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOOracleRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqliteRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperOracleRW.IReadWriteConnectionFactory))]
    public void AllProviders_IReadWriteConnectionFactory_HasCreateReadConnectionMethod(Type factoryType)
    {
        // Act
        var method = factoryType.GetMethod("CreateReadConnection");

        // Assert
        method.ShouldNotBeNull($"{factoryType.Name} should have CreateReadConnection method");
        method.ReturnType.ShouldBe(typeof(IDbConnection));
        method.GetParameters().Length.ShouldBe(0);
    }

    [Theory]
    [InlineData(typeof(ADOSqliteRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOOracleRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqliteRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperOracleRW.IReadWriteConnectionFactory))]
    public void AllProviders_IReadWriteConnectionFactory_HasCreateConnectionMethod(Type factoryType)
    {
        // Act
        var method = factoryType.GetMethod("CreateConnection");

        // Assert
        method.ShouldNotBeNull($"{factoryType.Name} should have CreateConnection method");
        method.ReturnType.ShouldBe(typeof(IDbConnection));
        method.GetParameters().Length.ShouldBe(0);
    }

    [Theory]
    [InlineData(typeof(ADOSqliteRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOOracleRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqliteRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperOracleRW.IReadWriteConnectionFactory))]
    public void AllProviders_IReadWriteConnectionFactory_HasGetWriteConnectionStringMethod(Type factoryType)
    {
        // Act
        var method = factoryType.GetMethod("GetWriteConnectionString");

        // Assert
        method.ShouldNotBeNull($"{factoryType.Name} should have GetWriteConnectionString method");
        method.ReturnType.ShouldBe(typeof(string));
        method.GetParameters().Length.ShouldBe(0);
    }

    [Theory]
    [InlineData(typeof(ADOSqliteRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOOracleRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqliteRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperOracleRW.IReadWriteConnectionFactory))]
    public void AllProviders_IReadWriteConnectionFactory_HasGetReadConnectionStringMethod(Type factoryType)
    {
        // Act
        var method = factoryType.GetMethod("GetReadConnectionString");

        // Assert
        method.ShouldNotBeNull($"{factoryType.Name} should have GetReadConnectionString method");
        method.ReturnType.ShouldBe(typeof(string));
        method.GetParameters().Length.ShouldBe(0);
    }

    #endregion

    #region Health Check Implementations

    [Fact]
    public void ADOSqlite_HasHealthCheckWithCorrectName()
    {
        // Assert
        ADOSqliteRW.ReadWriteSeparationHealthCheck.DefaultName.ShouldBe(
            "encina-read-write-separation-ado-sqlite");
    }

    [Fact]
    public void ADOSqlServer_HasHealthCheckWithCorrectName()
    {
        // Assert
        ADOSqlServerRW.ReadWriteSeparationHealthCheck.DefaultName.ShouldBe(
            "encina-read-write-separation-ado");
    }

    [Fact]
    public void ADOPostgreSQL_HasHealthCheckWithCorrectName()
    {
        // Assert
        ADOPostgreSQLRW.ReadWriteSeparationHealthCheck.DefaultName.ShouldBe(
            "encina-read-write-separation-ado-postgresql");
    }

    [Fact]
    public void ADOMySQL_HasHealthCheckWithCorrectName()
    {
        // Assert
        ADOMySQLRW.ReadWriteSeparationHealthCheck.DefaultName.ShouldBe(
            "encina-read-write-separation-ado-mysql");
    }

    [Fact]
    public void ADOOracle_HasHealthCheckWithCorrectName()
    {
        // Assert
        ADOOracleRW.ReadWriteSeparationHealthCheck.DefaultName.ShouldBe(
            "encina-read-write-separation-ado-oracle");
    }

    [Fact]
    public void DapperSqlite_HasHealthCheckWithCorrectName()
    {
        // Assert
        DapperSqliteRW.ReadWriteSeparationHealthCheck.DefaultName.ShouldBe(
            "encina-read-write-separation-dapper-sqlite");
    }

    [Fact]
    public void DapperSqlServer_HasHealthCheckWithCorrectName()
    {
        // Assert
        DapperSqlServerRW.ReadWriteSeparationHealthCheck.DefaultName.ShouldBe(
            "encina-read-write-separation-dapper");
    }

    [Fact]
    public void DapperPostgreSQL_HasHealthCheckWithCorrectName()
    {
        // Assert
        DapperPostgreSQLRW.ReadWriteSeparationHealthCheck.DefaultName.ShouldBe(
            "encina-read-write-separation-dapper-postgresql");
    }

    [Fact]
    public void DapperMySQL_HasHealthCheckWithCorrectName()
    {
        // Assert
        DapperMySQLRW.ReadWriteSeparationHealthCheck.DefaultName.ShouldBe(
            "encina-read-write-separation-dapper-mysql");
    }

    [Fact]
    public void DapperOracle_HasHealthCheckWithCorrectName()
    {
        // Assert
        DapperOracleRW.ReadWriteSeparationHealthCheck.DefaultName.ShouldBe(
            "encina-read-write-separation-dapper-oracle");
    }

    #endregion

    #region Pipeline Behavior Implementations

    [Theory]
    [InlineData(typeof(ADOSqliteRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(ADOSqlServerRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(ADOPostgreSQLRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(ADOMySQLRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(ADOOracleRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(DapperSqliteRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(DapperSqlServerRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(DapperPostgreSQLRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(DapperMySQLRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(DapperOracleRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(EntityFrameworkCoreRW.ReadWriteRoutingPipelineBehavior<,>))]
    public void AllProviders_PipelineBehavior_IsGenericAndSealed(Type behaviorType)
    {
        // Assert
        behaviorType.IsGenericType.ShouldBeTrue($"{behaviorType.Name} should be generic");
        behaviorType.IsSealed.ShouldBeTrue($"{behaviorType.Name} should be sealed");
        behaviorType.GetGenericArguments().Length.ShouldBe(2,
            $"{behaviorType.Name} should have 2 generic type parameters (TRequest, TResponse)");
    }

    #endregion

    #region Factory Implementation Consistency

    [Theory]
    [InlineData(typeof(ADOSqliteRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOOracleRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqliteRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperOracleRW.ReadWriteConnectionFactory))]
    public void AllProviders_ReadWriteConnectionFactory_IsSealed(Type factoryType)
    {
        // Assert
        factoryType.IsSealed.ShouldBeTrue($"{factoryType.FullName} should be sealed");
    }

    [Theory]
    [InlineData(typeof(ADOSqliteRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOOracleRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqliteRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperOracleRW.ReadWriteConnectionFactory))]
    public void AllProviders_ReadWriteConnectionFactory_HasSingleConstructorWithSelector(Type factoryType)
    {
        // Act
        var constructors = factoryType.GetConstructors();

        // Assert
        constructors.Length.ShouldBe(1, $"{factoryType.FullName} should have exactly one public constructor");

        var parameters = constructors[0].GetParameters();
        parameters.Length.ShouldBe(1, "Constructor should have exactly one parameter");
        parameters[0].ParameterType.Name.ShouldBe("IReadWriteConnectionSelector",
            "Constructor parameter should be IReadWriteConnectionSelector");
    }

    #endregion
}
