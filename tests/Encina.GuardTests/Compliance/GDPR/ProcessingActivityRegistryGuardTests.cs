using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ADOMySQLPA = Encina.ADO.MySQL.ProcessingActivity;
using ADOPostgreSQLPA = Encina.ADO.PostgreSQL.ProcessingActivity;
using ADOSqlitePA = Encina.ADO.Sqlite.ProcessingActivity;
using ADOSqlServerPA = Encina.ADO.SqlServer.ProcessingActivity;
using DapperMySQLPA = Encina.Dapper.MySQL.ProcessingActivity;
using DapperPostgreSQLPA = Encina.Dapper.PostgreSQL.ProcessingActivity;
using DapperSqlitePA = Encina.Dapper.Sqlite.ProcessingActivity;
using DapperSqlServerPA = Encina.Dapper.SqlServer.ProcessingActivity;
using EFCorePA = Encina.EntityFrameworkCore.ProcessingActivity;

namespace Encina.GuardTests.Compliance.GDPR;

/// <summary>
/// Guard clause tests for ProcessingActivityRegistry provider implementations
/// and the <see cref="ProcessingActivityHealthCheck"/>.
/// Verifies that invalid constructor arguments are properly rejected across all 12
/// database providers (ADO.NET, Dapper, EF Core) and the health check.
/// </summary>
/// <remarks>
/// MongoDB is excluded because its constructor creates a <c>MongoClient</c> eagerly,
/// making it impractical to test guard clauses without a running server.
/// The <see cref="InMemoryProcessingActivityRegistry"/> guard tests live in
/// <see cref="GDPRGuardTests"/>.
/// </remarks>
public class ProcessingActivityRegistryGuardTests
{
    // ──────────────────────────────────────────────
    //  ADO.NET Providers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that the ADO.NET SQLite ProcessingActivityRegistryADO constructor
    /// throws <see cref="ArgumentException"/> when given a null, empty, or whitespace connection string.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ADOSqlite_Constructor_InvalidConnectionString_ThrowsArgumentException(string? connectionString)
    {
        var act = () => new ADOSqlitePA.ProcessingActivityRegistryADO(connectionString!);
        Should.Throw<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that the ADO.NET SQL Server ProcessingActivityRegistryADO constructor
    /// throws <see cref="ArgumentException"/> when given a null, empty, or whitespace connection string.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ADOSqlServer_Constructor_InvalidConnectionString_ThrowsArgumentException(string? connectionString)
    {
        var act = () => new ADOSqlServerPA.ProcessingActivityRegistryADO(connectionString!);
        Should.Throw<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that the ADO.NET PostgreSQL ProcessingActivityRegistryADO constructor
    /// throws <see cref="ArgumentException"/> when given a null, empty, or whitespace connection string.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ADOPostgreSQL_Constructor_InvalidConnectionString_ThrowsArgumentException(string? connectionString)
    {
        var act = () => new ADOPostgreSQLPA.ProcessingActivityRegistryADO(connectionString!);
        Should.Throw<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that the ADO.NET MySQL ProcessingActivityRegistryADO constructor
    /// throws <see cref="ArgumentException"/> when given a null, empty, or whitespace connection string.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ADOMySQL_Constructor_InvalidConnectionString_ThrowsArgumentException(string? connectionString)
    {
        var act = () => new ADOMySQLPA.ProcessingActivityRegistryADO(connectionString!);
        Should.Throw<ArgumentException>(act);
    }

    // ──────────────────────────────────────────────
    //  Dapper Providers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that the Dapper SQLite ProcessingActivityRegistryDapper constructor
    /// throws <see cref="ArgumentException"/> when given a null, empty, or whitespace connection string.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DapperSqlite_Constructor_InvalidConnectionString_ThrowsArgumentException(string? connectionString)
    {
        var act = () => new DapperSqlitePA.ProcessingActivityRegistryDapper(connectionString!);
        Should.Throw<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that the Dapper SQL Server ProcessingActivityRegistryDapper constructor
    /// throws <see cref="ArgumentException"/> when given a null, empty, or whitespace connection string.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DapperSqlServer_Constructor_InvalidConnectionString_ThrowsArgumentException(string? connectionString)
    {
        var act = () => new DapperSqlServerPA.ProcessingActivityRegistryDapper(connectionString!);
        Should.Throw<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that the Dapper PostgreSQL ProcessingActivityRegistryDapper constructor
    /// throws <see cref="ArgumentException"/> when given a null, empty, or whitespace connection string.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DapperPostgreSQL_Constructor_InvalidConnectionString_ThrowsArgumentException(string? connectionString)
    {
        var act = () => new DapperPostgreSQLPA.ProcessingActivityRegistryDapper(connectionString!);
        Should.Throw<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that the Dapper MySQL ProcessingActivityRegistryDapper constructor
    /// throws <see cref="ArgumentException"/> when given a null, empty, or whitespace connection string.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DapperMySQL_Constructor_InvalidConnectionString_ThrowsArgumentException(string? connectionString)
    {
        var act = () => new DapperMySQLPA.ProcessingActivityRegistryDapper(connectionString!);
        Should.Throw<ArgumentException>(act);
    }

    // ──────────────────────────────────────────────
    //  EF Core Provider
    // ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that the EF Core ProcessingActivityRegistryEF constructor
    /// throws <see cref="ArgumentNullException"/> when given a null <see cref="DbContext"/>.
    /// </summary>
    [Fact]
    public void EFCore_Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        var act = () => new EFCorePA.ProcessingActivityRegistryEF(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("dbContext");
    }

    // ──────────────────────────────────────────────
    //  ProcessingActivityHealthCheck
    // ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="ProcessingActivityHealthCheck"/> throws
    /// <see cref="ArgumentNullException"/> when the <paramref name="serviceProvider"/> is null.
    /// </summary>
    [Fact]
    public void HealthCheck_Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var logger = Substitute.For<ILogger<ProcessingActivityHealthCheck>>();
        var act = () => new ProcessingActivityHealthCheck(null!, logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    /// <summary>
    /// Verifies that <see cref="ProcessingActivityHealthCheck"/> throws
    /// <see cref="ArgumentNullException"/> when the <paramref name="logger"/> is null.
    /// </summary>
    [Fact]
    public void HealthCheck_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var act = () => new ProcessingActivityHealthCheck(serviceProvider, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
