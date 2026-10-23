using System.Data;
using Encina.Compliance.GDPR;
using Encina.Security.Audit;
using NSubstitute;
using Shouldly;
using AdoMySqlAudit = Encina.ADO.MySQL.Auditing;
using AdoMySqlRegistry = Encina.ADO.MySQL.ProcessingActivity.ProcessingActivityRegistryADO;
using AdoPostgreSqlAudit = Encina.ADO.PostgreSQL.Auditing;
using AdoPostgreSqlRegistry = Encina.ADO.PostgreSQL.ProcessingActivity.ProcessingActivityRegistryADO;
using AdoSqlServerAudit = Encina.ADO.SqlServer.Auditing;
using AdoSqlServerRegistry = Encina.ADO.SqlServer.ProcessingActivity.ProcessingActivityRegistryADO;
using DapperMySqlAudit = Encina.Dapper.MySQL.Auditing;
using DapperMySqlRegistry = Encina.Dapper.MySQL.ProcessingActivity.ProcessingActivityRegistryDapper;
using DapperPostgreSqlAudit = Encina.Dapper.PostgreSQL.Auditing;
using DapperPostgreSqlRegistry = Encina.Dapper.PostgreSQL.ProcessingActivity.ProcessingActivityRegistryDapper;
using DapperSqlServerAudit = Encina.Dapper.SqlServer.Auditing;
using DapperSqlServerRegistry = Encina.Dapper.SqlServer.ProcessingActivity.ProcessingActivityRegistryDapper;

namespace Encina.UnitTests.Compliance.StoreFailures;

/// <summary>
/// Proves that the operation audit, read audit and processing activity stores of every relational provider
/// (ADO.NET and Dapper on SQL Server, PostgreSQL and MySQL) return an error that carries the exception and its type
/// name but never the exception's message text when the database cannot be reached (#1322, #2445).
/// </summary>
[Trait("Category", "Unit")]
public sealed class RelationalProviderStoreFailureTests
{
    private const string Sentinel = "SECRET-42";

    public static TheoryData<string> Providers =>
    [
        "ado-sqlserver", "ado-postgresql", "ado-mysql",
        "dapper-sqlserver", "dapper-postgresql", "dapper-mysql"
    ];

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task OperationAuditStore_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage(string provider)
    {
        var result = await CreateOperationAuditStore(provider).RecordAsync(AuditEntry());

        AssertNoLeak(result, OperationAuditErrors.StoreErrorCode, sentinelInException: true);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task ReadAuditStore_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage(string provider)
    {
        var result = await CreateReadAuditStore(provider).LogReadAsync(ReadEntry());

        AssertNoLeak(result, ReadAuditErrors.StoreErrorCode, sentinelInException: true);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task ProcessingActivityRegistry_WhenTheDatabaseIsUnreachable_DoesNotLeakTheExceptionMessage(string provider)
    {
        var result = await CreateRegistry(provider).GetAllActivitiesAsync();

        AssertNoLeak(result, GDPRErrors.ProcessingActivityStoreErrorCode, sentinelInException: false);
    }

    private static IOperationAuditStore CreateOperationAuditStore(string provider)
    {
        var connection = FailingConnection();
        return provider switch
        {
            "ado-sqlserver" => new AdoSqlServerAudit.OperationAuditStoreADO(connection),
            "ado-postgresql" => new AdoPostgreSqlAudit.OperationAuditStoreADO(connection),
            "ado-mysql" => new AdoMySqlAudit.OperationAuditStoreADO(connection),
            "dapper-sqlserver" => new DapperSqlServerAudit.OperationAuditStoreDapper(connection),
            "dapper-postgresql" => new DapperPostgreSqlAudit.OperationAuditStoreDapper(connection),
            "dapper-mysql" => new DapperMySqlAudit.OperationAuditStoreDapper(connection),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
    }

    private static IReadAuditStore CreateReadAuditStore(string provider)
    {
        var connection = FailingConnection();
        return provider switch
        {
            "ado-sqlserver" => new AdoSqlServerAudit.ReadAuditStoreADO(connection),
            "ado-postgresql" => new AdoPostgreSqlAudit.ReadAuditStoreADO(connection),
            "ado-mysql" => new AdoMySqlAudit.ReadAuditStoreADO(connection),
            "dapper-sqlserver" => new DapperSqlServerAudit.ReadAuditStoreDapper(connection),
            "dapper-postgresql" => new DapperPostgreSqlAudit.ReadAuditStoreDapper(connection),
            "dapper-mysql" => new DapperMySqlAudit.ReadAuditStoreDapper(connection),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
    }

    private static IProcessingActivityRegistry CreateRegistry(string provider) => provider switch
    {
        "ado-sqlserver" => new AdoSqlServerRegistry("Server=127.0.0.1,1;Database=x;User Id=u;Password=p;Connect Timeout=1;Encrypt=False"),
        "ado-postgresql" => new AdoPostgreSqlRegistry(PostgreSqlUnreachable),
        "ado-mysql" => new AdoMySqlRegistry(MySqlUnreachable),
        "dapper-sqlserver" => new DapperSqlServerRegistry("Server=127.0.0.1,1;Database=x;User Id=u;Password=p;Connect Timeout=1;Encrypt=False"),
        "dapper-postgresql" => new DapperPostgreSqlRegistry(PostgreSqlUnreachable),
        "dapper-mysql" => new DapperMySqlRegistry(MySqlUnreachable),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };

    private const string PostgreSqlUnreachable = "Host=127.0.0.1;Port=1;Database=x;Username=u;Password=p;Timeout=1;Pooling=false";

    private const string MySqlUnreachable = "Server=127.0.0.1;Port=1;Database=x;User=u;Password=p;ConnectionTimeout=1;Pooling=false";

    private static IDbConnection FailingConnection()
    {
        var connection = Substitute.For<IDbConnection>();
        connection.State.Returns(ConnectionState.Closed);
        connection.CreateCommand().Returns(_ => throw new InvalidOperationException(Sentinel));
        connection.When(c => c.Open()).Do(_ => throw new InvalidOperationException(Sentinel));
        return connection;
    }

    private static void AssertNoLeak<T>(LanguageExt.Either<EncinaError, T> result, string expectedCode, bool sentinelInException)
    {
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Single();
        error.GetCode().IfNone(string.Empty).ShouldBe(expectedCode);
        error.Exception.IsSome.ShouldBeTrue();
        var exception = error.Exception.IfNone(() => throw new InvalidOperationException("missing exception"));
        exception.Message.ShouldNotBeNullOrWhiteSpace();
        if (sentinelInException)
        {
            exception.Message.ShouldContain(Sentinel);
        }

        error.Message.ShouldNotContain(Sentinel);
        error.Message.ShouldNotContain(exception.Message);
        error.Message.ShouldContain(exception.GetType().Name);
    }

    private static OperationAuditEntry AuditEntry()
    {
        var now = DateTimeOffset.UnixEpoch.AddYears(56);
        return new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr-1322",
            UserId = "user-1322",
            Action = "Create",
            EntityType = "Order",
            EntityId = "order-1",
            Outcome = AuditOutcome.Success,
            TimestampUtc = now.UtcDateTime,
            StartedAtUtc = now,
            CompletedAtUtc = now.AddMilliseconds(10),
            Metadata = new Dictionary<string, object?>()
        };
    }

    private static ReadAuditEntry ReadEntry() => new()
    {
        Id = Guid.NewGuid(),
        EntityType = "Patient",
        EntityId = "patient-1",
        UserId = "user-1322",
        AccessedAtUtc = DateTimeOffset.UnixEpoch.AddYears(56),
        AccessMethod = ReadAccessMethod.Repository,
        EntityCount = 1
    };
}
