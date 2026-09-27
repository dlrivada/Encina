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
