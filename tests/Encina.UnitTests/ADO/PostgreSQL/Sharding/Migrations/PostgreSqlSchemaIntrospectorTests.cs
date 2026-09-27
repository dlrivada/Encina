using System.Data;
using Encina.ADO.PostgreSQL.Sharding.Migrations;
using Encina.Sharding;
using Encina.Sharding.Data;
using LanguageExt;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.ADO.PostgreSQL.Sharding.Migrations;

/// <summary>
/// Unit tests for <see cref="PostgreSqlSchemaIntrospector"/>, focused on proving that the
/// injected <see cref="TimeProvider"/> (rather than <see cref="DateTimeOffset.UtcNow"/>) supplies
/// <see cref="Encina.Sharding.Migrations.ShardSchema.IntrospectedAtUtc"/> (#1325).
/// </summary>
[Trait("Category", "Unit")]
public class PostgreSqlSchemaIntrospectorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new PostgreSqlSchemaIntrospector(null!));
    }

    [Fact]
    public async Task IntrospectAsync_UsesInjectedTimeProviderForIntrospectedAtUtc()
    {
        var timeProvider = new FakeTimeProvider(FixedNow);
        var factory = CreateFactory();
        var introspector = new PostgreSqlSchemaIntrospector(factory, timeProvider);
        var shard = new ShardInfo("shard-1", "Host=localhost;Database=test");

        var result = await introspector.IntrospectAsync(shard, includeColumns: false, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        result.IfRight(schema => schema.IntrospectedAtUtc.ShouldBe(FixedNow));
    }

    [Fact]
    public async Task IntrospectAsync_NoTimeProviderSupplied_DefaultsToSystemTime()
    {
        var factory = CreateFactory();
        var introspector = new PostgreSqlSchemaIntrospector(factory);
        var shard = new ShardInfo("shard-1", "Host=localhost;Database=test");
        var before = DateTimeOffset.UtcNow;

        var result = await introspector.IntrospectAsync(shard, includeColumns: false, CancellationToken.None);

        var after = DateTimeOffset.UtcNow;
        result.IsRight.ShouldBeTrue();
        result.IfRight(schema => schema.IntrospectedAtUtc.ShouldBeInRange(before.AddSeconds(-1), after.AddSeconds(1)));
    }

    [Fact]
    public async Task IntrospectAsync_TablesFoundWithoutColumns_ReturnsTablesWithNoColumns()
    {
        var tablesReader = Substitute.For<IDataReader>();
        tablesReader.Read().Returns(true, true, false);
        tablesReader.GetString(0).Returns("orders", "customers");

        var command = Substitute.For<IDbCommand>();
        command.ExecuteReader().Returns(tablesReader);

        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(command);

        var factory = Substitute.For<IShardedConnectionFactory>();
        factory.GetConnectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, IDbConnection>(connection)));

        var introspector = new PostgreSqlSchemaIntrospector(factory);
        var shard = new ShardInfo("shard-1", "Host=localhost;Database=test");

        var result = await introspector.IntrospectAsync(shard, includeColumns: false, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        result.IfRight(schema =>
        {
            schema.Tables.Count.ShouldBe(2);
            schema.Tables.ShouldAllBe(t => t.Columns.Count == 0);
        });
    }

    [Fact]
    public async Task IntrospectAsync_TablesFoundWithColumns_ReadsColumnsPerTable()
    {
        var tablesReader = Substitute.For<IDataReader>();
        tablesReader.Read().Returns(true, false);
        tablesReader.GetString(0).Returns("orders");

        var columnsReader = Substitute.For<IDataReader>();
        columnsReader.Read().Returns(true, false);
        columnsReader.GetString(0).Returns("id");
        columnsReader.GetString(1).Returns("uuid");
        columnsReader.GetString(2).Returns("NO");
        columnsReader.IsDBNull(3).Returns(true);

        var command = Substitute.For<IDbCommand>();
        command.ExecuteReader().Returns(tablesReader, columnsReader);
        command.Parameters.Returns(Substitute.For<IDataParameterCollection>());
        command.CreateParameter().Returns(_ => Substitute.For<IDbDataParameter>());

        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(command);

        var factory = Substitute.For<IShardedConnectionFactory>();
        factory.GetConnectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, IDbConnection>(connection)));

        var introspector = new PostgreSqlSchemaIntrospector(factory);
        var shard = new ShardInfo("shard-1", "Host=localhost;Database=test");

        var result = await introspector.IntrospectAsync(shard, includeColumns: true, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        result.IfRight(schema =>
        {
            schema.Tables.Count.ShouldBe(1);
            schema.Tables[0].Columns.Count.ShouldBe(1);
            schema.Tables[0].Columns[0].Name.ShouldBe("id");
            schema.Tables[0].Columns[0].IsNullable.ShouldBeFalse();
            schema.Tables[0].Columns[0].DefaultValue.ShouldBeNull();
        });
    }

    [Fact]
    public async Task CompareAsync_BothShardsIntrospected_ReturnsDiff()
    {
        var factory = CreateFactory();
        var introspector = new PostgreSqlSchemaIntrospector(factory);
        var shard = new ShardInfo("shard-1", "Host=localhost;Database=test");
        var baseline = new ShardInfo("shard-0", "Host=localhost;Database=baseline");

        var result = await introspector.CompareAsync(shard, baseline, includeColumnDiffs: false, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task CompareAsync_NullShard_ThrowsArgumentNullException()
    {
        var factory = CreateFactory();
        var introspector = new PostgreSqlSchemaIntrospector(factory);
        var baseline = new ShardInfo("shard-0", "Host=localhost;Database=baseline");

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await introspector.CompareAsync(null!, baseline, includeColumnDiffs: false, CancellationToken.None));
    }

    private static IShardedConnectionFactory CreateFactory()
    {
        var reader = Substitute.For<IDataReader>();
        reader.Read().Returns(false);

        var command = Substitute.For<IDbCommand>();
        command.ExecuteReader().Returns(reader);

        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(command);

        var factory = Substitute.For<IShardedConnectionFactory>();
        factory.GetConnectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, IDbConnection>(connection)));
        return factory;
    }
}
