using System.Data;
using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using NSubstitute;
using Shouldly;
using AdoMySql = Encina.ADO.MySQL.Anonymization.TokenMappingStoreADO;
using AdoPostgreSql = Encina.ADO.PostgreSQL.Anonymization.TokenMappingStoreADO;
using AdoSqlServer = Encina.ADO.SqlServer.Anonymization.TokenMappingStoreADO;
using DapperMySql = Encina.Dapper.MySQL.Anonymization.TokenMappingStoreDapper;
using DapperPostgreSql = Encina.Dapper.PostgreSQL.Anonymization.TokenMappingStoreDapper;
using DapperSqlServer = Encina.Dapper.SqlServer.Anonymization.TokenMappingStoreDapper;

namespace Encina.UnitTests.Compliance.StoreFailures;

/// <summary>
/// Proves that every read and delete operation of the ADO.NET and Dapper token mapping stores of the three relational
/// providers returns <see cref="AnonymizationErrors.StoreErrorCode"/> carrying the exception and its type name, never
/// the exception's message text, when the connection fails (#1322).
/// </summary>
[Trait("Category", "Unit")]
public sealed class TokenMappingStoreFailureTests
{
    private const string Sentinel = "SECRET-42";

    public static TheoryData<string> Providers =>
    [
        "ado-sqlserver", "ado-postgresql", "ado-mysql",
        "dapper-sqlserver", "dapper-postgresql", "dapper-mysql"
    ];

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task GetByTokenAsync_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage(string provider)
    {
        var result = await CreateStore(provider).GetByTokenAsync("token");

        AssertNoLeak(result);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task GetByOriginalValueHashAsync_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage(string provider)
    {
        var result = await CreateStore(provider).GetByOriginalValueHashAsync("hash");

        AssertNoLeak(result);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task GetAllAsync_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage(string provider)
    {
        var result = await CreateStore(provider).GetAllAsync();

        AssertNoLeak(result);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task DeleteByKeyIdAsync_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage(string provider)
    {
        var result = await CreateStore(provider).DeleteByKeyIdAsync("key-1");

        AssertNoLeak(result);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task StoreAsync_WhenTheConnectionFails_DoesNotLeakTheExceptionMessage(string provider)
    {
        var mapping = new TokenMapping
        {
            Id = Guid.NewGuid().ToString(),
            Token = "token",
            OriginalValueHash = "hash",
            EncryptedOriginalValue = [1, 2, 3],
            KeyId = "key-1",
            CreatedAtUtc = DateTimeOffset.UnixEpoch.AddYears(56)
        };

        var result = await CreateStore(provider).StoreAsync(mapping);

        AssertNoLeak(result);
    }

    private static ITokenMappingStore CreateStore(string provider)
    {
        var connection = FailingConnection();
        return provider switch
        {
            "ado-sqlserver" => new AdoSqlServer(connection),
            "ado-postgresql" => new AdoPostgreSql(connection),
            "ado-mysql" => new AdoMySql(connection),
            "dapper-sqlserver" => new DapperSqlServer(connection),
            "dapper-postgresql" => new DapperPostgreSql(connection),
            "dapper-mysql" => new DapperMySql(connection),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
    }

    private static IDbConnection FailingConnection()
    {
        var connection = Substitute.For<IDbConnection>();
        connection.State.Returns(ConnectionState.Closed);
        connection.CreateCommand().Returns(_ => throw new InvalidOperationException(Sentinel));
        connection.When(c => c.Open()).Do(_ => throw new InvalidOperationException(Sentinel));
        return connection;
    }

    private static void AssertNoLeak<T>(LanguageExt.Either<EncinaError, T> result)
    {
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Single();
        error.GetCode().IfNone(string.Empty).ShouldBe(AnonymizationErrors.StoreErrorCode);
        error.Exception.IsSome.ShouldBeTrue();
        error.Message.ShouldNotContain(Sentinel);
        error.Message.ShouldContain(nameof(InvalidOperationException));
    }
}
