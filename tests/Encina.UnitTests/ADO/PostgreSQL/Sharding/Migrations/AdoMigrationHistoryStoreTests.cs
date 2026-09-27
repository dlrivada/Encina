using System.Data;
using Encina.ADO.PostgreSQL.Sharding.Migrations;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using LanguageExt;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.ADO.PostgreSQL.Sharding.Migrations;

/// <summary>
/// Unit tests for <see cref="AdoMigrationHistoryStore"/>, focused on proving that the
/// injected <see cref="TimeProvider"/> (rather than <see cref="DateTime.UtcNow"/>) supplies
/// the applied/rolled-back timestamps (#1325).
/// </summary>
[Trait("Category", "Unit")]
public class AdoMigrationHistoryStoreTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new AdoMigrationHistoryStore(null!));
    }

    [Fact]
    public async Task RecordAppliedAsync_UsesInjectedTimeProviderForAppliedAtUtc()
    {
        var timeProvider = new FakeTimeProvider(FixedNow);
        var capturedParameters = new List<IDbDataParameter>();
        var connection = CreateFakeConnection(capturedParameters);
        var factory = CreateFactory(connection);
        var store = new AdoMigrationHistoryStore(factory, timeProvider);
        var script = new MigrationScript("m1", "CREATE TABLE t (id INT)", "DROP TABLE t", "desc", "checksum");

        var result = await store.RecordAppliedAsync("shard-1", script, TimeSpan.FromSeconds(1));

        result.IsRight.ShouldBeTrue();
        var appliedAtParam = capturedParameters.Single(p => p.ParameterName == "@AppliedAtUtc");
        appliedAtParam.Value.ShouldBe(FixedNow.UtcDateTime);
    }

    [Fact]
    public async Task RecordRolledBackAsync_UsesInjectedTimeProviderForRolledBackAtUtc()
    {
        var timeProvider = new FakeTimeProvider(FixedNow);
        var capturedParameters = new List<IDbDataParameter>();
        var connection = CreateFakeConnection(capturedParameters);
        var factory = CreateFactory(connection);
        var store = new AdoMigrationHistoryStore(factory, timeProvider);

        var result = await store.RecordRolledBackAsync("shard-1", "m1");

        result.IsRight.ShouldBeTrue();
        var rolledBackAtParam = capturedParameters.Single(p => p.ParameterName == "@RolledBackAtUtc");
        rolledBackAtParam.Value.ShouldBe(FixedNow.UtcDateTime);
    }

    [Fact]
    public async Task ApplyHistoricalMigrationsAsync_UsesInjectedTimeProviderForAppliedAtUtc()
    {
        var timeProvider = new FakeTimeProvider(FixedNow);
        var capturedParameters = new List<IDbDataParameter>();
        var connection = CreateFakeConnection(capturedParameters);
        var factory = CreateFactory(connection);
        var store = new AdoMigrationHistoryStore(factory, timeProvider);
        var script = new MigrationScript("m1", "CREATE TABLE t (id INT)", "DROP TABLE t", "desc", "checksum");

        var result = await store.ApplyHistoricalMigrationsAsync("shard-1", [script]);

        result.IsRight.ShouldBeTrue();
        var appliedAtParam = capturedParameters.Single(p => p.ParameterName == "@AppliedAtUtc");
        appliedAtParam.Value.ShouldBe(FixedNow.UtcDateTime);
    }

    [Fact]
    public async Task RecordAppliedAsync_NoTimeProviderSupplied_DefaultsToSystemTime()
    {
        var capturedParameters = new List<IDbDataParameter>();
        var connection = CreateFakeConnection(capturedParameters);
        var factory = CreateFactory(connection);
        var store = new AdoMigrationHistoryStore(factory);
        var script = new MigrationScript("m1", "CREATE TABLE t (id INT)", "DROP TABLE t", "desc", "checksum");
        var before = DateTime.UtcNow;

        var result = await store.RecordAppliedAsync("shard-1", script, TimeSpan.FromSeconds(1));

        var after = DateTime.UtcNow;
        result.IsRight.ShouldBeTrue();
        var appliedAtParam = capturedParameters.Single(p => p.ParameterName == "@AppliedAtUtc");
        var capturedValue = (DateTime)appliedAtParam.Value!;
        capturedValue.ShouldBeInRange(before.AddSeconds(-1), after.AddSeconds(1));
    }

    private static IShardedConnectionFactory CreateFactory(IDbConnection connection)
    {
        var factory = Substitute.For<IShardedConnectionFactory>();
        factory.GetConnectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, IDbConnection>(connection)));
        return factory;
    }

    private static IDbConnection CreateFakeConnection(List<IDbDataParameter> capturedParameters)
    {
        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(_ => CreateFakeCommand(capturedParameters));
        return connection;
    }

    private static IDbCommand CreateFakeCommand(List<IDbDataParameter> capturedParameters)
    {
        var command = Substitute.For<IDbCommand>();
        var reader = Substitute.For<IDataReader>();
        reader.Read().Returns(false);
        command.ExecuteReader().Returns(reader);
        command.CreateParameter().Returns(_ =>
        {
            var parameter = Substitute.For<IDbDataParameter>();
            capturedParameters.Add(parameter);
            return parameter;
        });
        return command;
    }
}
