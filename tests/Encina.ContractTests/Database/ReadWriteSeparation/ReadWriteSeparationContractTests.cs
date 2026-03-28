using System.Data;
using Encina;
using LanguageExt;
using Shouldly;
using Xunit;
using ADOMySQLRW = Encina.ADO.MySQL.ReadWriteSeparation;
using ADOPostgreSQLRW = Encina.ADO.PostgreSQL.ReadWriteSeparation;
// Provider-specific aliases for factories
using ADOSqlServerRW = Encina.ADO.SqlServer.ReadWriteSeparation;
using DapperMySQLRW = Encina.Dapper.MySQL.ReadWriteSeparation;
using DapperPostgreSQLRW = Encina.Dapper.PostgreSQL.ReadWriteSeparation;
using DapperSqlServerRW = Encina.Dapper.SqlServer.ReadWriteSeparation;
using EntityFrameworkCoreRW = Encina.EntityFrameworkCore.ReadWriteSeparation;

namespace Encina.ContractTests.Database.ReadWriteSeparation;

/// <summary>
/// Contract tests verifying that all 10 providers implement the Read/Write Separation pattern
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

    #endregion

    #region Interface Implementation Tests - Dapper Providers

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
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.IReadWriteConnectionFactory))]
    public void AllProviders_IReadWriteConnectionFactory_HasCreateWriteConnectionMethod(Type factoryType)
    {
        // Act
        var method = factoryType.GetMethod("CreateWriteConnection");

        // Assert
        method.ShouldNotBeNull($"{factoryType.Name} should have CreateWriteConnection method");
        method.ReturnType.ShouldBe(typeof(Either<EncinaError, IDbConnection>));
        method.GetParameters().Length.ShouldBe(0);
    }

    [Theory]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.IReadWriteConnectionFactory))]
    public void AllProviders_IReadWriteConnectionFactory_HasCreateReadConnectionMethod(Type factoryType)
    {
        // Act
        var method = factoryType.GetMethod("CreateReadConnection");

        // Assert
        method.ShouldNotBeNull($"{factoryType.Name} should have CreateReadConnection method");
        method.ReturnType.ShouldBe(typeof(Either<EncinaError, IDbConnection>));
        method.GetParameters().Length.ShouldBe(0);
    }

    [Theory]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.IReadWriteConnectionFactory))]
    public void AllProviders_IReadWriteConnectionFactory_HasCreateConnectionMethod(Type factoryType)
    {
        // Act
        var method = factoryType.GetMethod("CreateConnection");

        // Assert
        method.ShouldNotBeNull($"{factoryType.Name} should have CreateConnection method");
        method.ReturnType.ShouldBe(typeof(Either<EncinaError, IDbConnection>));
        method.GetParameters().Length.ShouldBe(0);
    }

    [Theory]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.IReadWriteConnectionFactory))]
    public void AllProviders_IReadWriteConnectionFactory_HasGetWriteConnectionStringMethod(Type factoryType)
    {
        // Act
        var method = factoryType.GetMethod("GetWriteConnectionString");

        // Assert
        method.ShouldNotBeNull($"{factoryType.Name} should have GetWriteConnectionString method");
        method.ReturnType.ShouldBe(typeof(Either<EncinaError, string>));
        method.GetParameters().Length.ShouldBe(0);
    }

    [Theory]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.IReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.IReadWriteConnectionFactory))]
    public void AllProviders_IReadWriteConnectionFactory_HasGetReadConnectionStringMethod(Type factoryType)
    {
        // Act
        var method = factoryType.GetMethod("GetReadConnectionString");

        // Assert
        method.ShouldNotBeNull($"{factoryType.Name} should have GetReadConnectionString method");
        method.ReturnType.ShouldBe(typeof(Either<EncinaError, string>));
        method.GetParameters().Length.ShouldBe(0);
    }

    #endregion

    #region Health Check Implementations

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

    #endregion

    #region Pipeline Behavior Implementations

    [Theory]
    [InlineData(typeof(ADOSqlServerRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(ADOSqlServerRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(ADOPostgreSQLRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(ADOMySQLRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(DapperSqlServerRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(DapperSqlServerRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(DapperPostgreSQLRW.ReadWriteRoutingPipelineBehavior<,>))]
    [InlineData(typeof(DapperMySQLRW.ReadWriteRoutingPipelineBehavior<,>))]
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
    [InlineData(typeof(ADOSqlServerRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.ReadWriteConnectionFactory))]
    public void AllProviders_ReadWriteConnectionFactory_IsSealed(Type factoryType)
    {
        // Assert
        factoryType.IsSealed.ShouldBeTrue($"{factoryType.FullName} should be sealed");
    }

    [Theory]
    [InlineData(typeof(ADOSqlServerRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOSqlServerRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOPostgreSQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(ADOMySQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperSqlServerRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperPostgreSQLRW.ReadWriteConnectionFactory))]
    [InlineData(typeof(DapperMySQLRW.ReadWriteConnectionFactory))]
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
