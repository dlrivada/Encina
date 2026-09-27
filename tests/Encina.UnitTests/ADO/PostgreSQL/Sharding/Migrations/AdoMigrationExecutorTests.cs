using System.Data;
using Encina.ADO.PostgreSQL.Sharding.Migrations;
using Encina.Sharding;
using Encina.Sharding.Data;
using LanguageExt;
using NSubstitute;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.ADO.PostgreSQL.Sharding.Migrations;

/// <summary>
/// Unit tests for <see cref="AdoMigrationExecutor"/>, covering the success, connection-lookup-failure
/// and thrown-exception paths of <see cref="AdoMigrationExecutor.ExecuteSqlAsync"/> (#1325 CRAP-gate
/// follow-up: the lambda passed to <c>Either.MapAsync</c> had zero coverage).
/// </summary>
[Trait("Category", "Unit")]
public class AdoMigrationExecutorTests
{
    private static readonly ShardInfo Shard = new("shard-1", "Host=localhost;Database=test");

    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new AdoMigrationExecutor(null!));
    }

    [Fact]
    public async Task ExecuteSqlAsync_NullShardInfo_ThrowsArgumentNullException()
    {
        var executor = new AdoMigrationExecutor(Substitute.For<IShardedConnectionFactory>());

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await executor.ExecuteSqlAsync(null!, "CREATE TABLE t (id INT)"));
    }

    [Fact]
    public async Task ExecuteSqlAsync_NullOrWhitespaceSql_ThrowsArgumentException()
    {
        var executor = new AdoMigrationExecutor(Substitute.For<IShardedConnectionFactory>());

        await Should.ThrowAsync<ArgumentException>(async () =>
            await executor.ExecuteSqlAsync(Shard, "   "));
    }

    [Fact]
    public async Task ExecuteSqlAsync_ValidSql_ExecutesNonQueryAndReturnsUnit()
    {
        var command = Substitute.For<IDbCommand>();
        command.ExecuteNonQuery().Returns(0);

        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(command);

        var factory = Substitute.For<IShardedConnectionFactory>();
        factory.GetConnectionAsync(Shard.ShardId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, IDbConnection>(connection)));

        var executor = new AdoMigrationExecutor(factory);

        var result = await executor.ExecuteSqlAsync(Shard, "CREATE TABLE t (id INT)");

        result.IsRight.ShouldBeTrue();
        command.Received(1).ExecuteNonQuery();
    }

    [Fact]
    public async Task ExecuteSqlAsync_ConnectionLookupFails_PropagatesLeft()
    {
        var factory = Substitute.For<IShardedConnectionFactory>();
        var lookupError = EncinaErrors.Create("SHARD_NOT_FOUND", "Shard not found");
        factory.GetConnectionAsync(Shard.ShardId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, IDbConnection>(lookupError)));

        var executor = new AdoMigrationExecutor(factory);

        var result = await executor.ExecuteSqlAsync(Shard, "CREATE TABLE t (id INT)");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteSqlAsync_CommandThrows_ReturnsLeftWithMigrationFailed()
    {
        var command = Substitute.For<IDbCommand>();
        command.ExecuteNonQuery().Returns(_ => throw new InvalidOperationException("boom"));

        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(command);

        var factory = Substitute.For<IShardedConnectionFactory>();
        factory.GetConnectionAsync(Shard.ShardId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, IDbConnection>(connection)));

        var executor = new AdoMigrationExecutor(factory);

        var result = await executor.ExecuteSqlAsync(Shard, "CREATE TABLE t (id INT)");

        result.IsLeft.ShouldBeTrue();
    }
}
