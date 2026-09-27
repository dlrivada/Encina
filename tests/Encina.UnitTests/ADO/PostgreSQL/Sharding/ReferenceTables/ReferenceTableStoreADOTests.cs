using System.ComponentModel.DataAnnotations;
using System.Data;
using Encina.ADO.PostgreSQL.Sharding.ReferenceTables;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.PostgreSQL.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReferenceTableStoreADO"/>. The store is otherwise only exercised
/// by integration tests against real Npgsql connections, where <c>IDbCommand</c> is always a
/// <c>DbCommand</c> — so the synchronous ADO.NET fallback branches (reached when the connection
/// is a plain <see cref="IDbConnection"/>, not a <c>DbConnection</c>) are never hit there. These
/// unit tests cover that fallback path and the exception paths (#1325 CRAP-gate follow-up).
/// </summary>
[Trait("Category", "Unit")]
public class ReferenceTableStoreADOTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new ReferenceTableStoreADO(null!));
    }

    [Fact]
    public async Task GetAllAsync_NonDbCommand_UsesSynchronousFallbackAndMapsEntities()
    {
        var id = Guid.NewGuid();
        var reader = Substitute.For<IDataReader>();
        reader.Read().Returns(true, false);
        reader.GetValue(0).Returns(id);
        reader.GetValue(1).Returns("Widget");

        var command = Substitute.For<IDbCommand>();
        command.ExecuteReader().Returns(reader);

        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(command);

        using var store = new ReferenceTableStoreADO(connection);

        var result = await store.GetAllAsync<TestReferenceEntity>();

        result.IsRight.ShouldBeTrue();
        result.IfRight(entities =>
        {
            entities.Count.ShouldBe(1);
            entities[0].Id.ShouldBe(id);
            entities[0].Name.ShouldBe("Widget");
        });
    }

    [Fact]
    public async Task GetAllAsync_NoRows_ReturnsEmptyList()
    {
        var reader = Substitute.For<IDataReader>();
        reader.Read().Returns(false);

        var command = Substitute.For<IDbCommand>();
        command.ExecuteReader().Returns(reader);

        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(command);

        using var store = new ReferenceTableStoreADO(connection);

        var result = await store.GetAllAsync<TestReferenceEntity>();

        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.ShouldBeEmpty());
    }

    [Fact]
    public async Task GetAllAsync_CommandThrows_ReturnsLeft()
    {
        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(_ => throw new InvalidOperationException("boom"));

        using var store = new ReferenceTableStoreADO(connection);

        var result = await store.GetAllAsync<TestReferenceEntity>();

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetHashAsync_DelegatesToGetAllAsync()
    {
        var reader = Substitute.For<IDataReader>();
        reader.Read().Returns(false);

        var command = Substitute.For<IDbCommand>();
        command.ExecuteReader().Returns(reader);

        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(command);

        using var store = new ReferenceTableStoreADO(connection);

        var result = await store.GetHashAsync<TestReferenceEntity>();

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task UpsertAsync_NullEntities_ThrowsArgumentNullException()
    {
        var connection = Substitute.For<IDbConnection>();
        using var store = new ReferenceTableStoreADO(connection);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.UpsertAsync<TestReferenceEntity>(null!));
    }

    [Fact]
    public async Task UpsertAsync_EmptyEntities_ReturnsZeroWithoutTouchingConnection()
    {
        var connection = Substitute.For<IDbConnection>();
        using var store = new ReferenceTableStoreADO(connection);

        var result = await store.UpsertAsync(Array.Empty<TestReferenceEntity>());

        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
        connection.DidNotReceive().CreateCommand();
    }

    [Fact]
    public async Task UpsertAsync_NonDbCommand_UsesSynchronousFallbackAndUpsertsBatch()
    {
        var parameters = Substitute.For<IDataParameterCollection>();
        var command = Substitute.For<IDbCommand>();
        command.Parameters.Returns(parameters);
        command.CreateParameter().Returns(_ => Substitute.For<IDbDataParameter>());
        command.ExecuteNonQuery().Returns(2);

        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(command);

        using var store = new ReferenceTableStoreADO(connection);
        var entities = new[]
        {
            new TestReferenceEntity { Id = Guid.NewGuid(), Name = "Widget" },
            new TestReferenceEntity { Id = Guid.NewGuid(), Name = "Gadget" }
        };

        var result = await store.UpsertAsync(entities);

        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));
    }

    [Fact]
    public async Task UpsertAsync_CommandThrows_ReturnsLeft()
    {
        var parameters = Substitute.For<IDataParameterCollection>();
        var command = Substitute.For<IDbCommand>();
        command.Parameters.Returns(parameters);
        command.CreateParameter().Returns(_ => Substitute.For<IDbDataParameter>());
        command.ExecuteNonQuery().Throws(new InvalidOperationException("boom"));

        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(command);

        using var store = new ReferenceTableStoreADO(connection);
        var entities = new[] { new TestReferenceEntity { Id = Guid.NewGuid(), Name = "Widget" } };

        var result = await store.UpsertAsync(entities);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Dispose_DisposesUnderlyingConnection()
    {
        var connection = Substitute.For<IDbConnection>();
        var store = new ReferenceTableStoreADO(connection);

        store.Dispose();

        connection.Received(1).Dispose();
    }
}

/// <summary>
/// Reference table test entity with a conventional "Id" primary key.
/// </summary>
public class TestReferenceEntity
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
