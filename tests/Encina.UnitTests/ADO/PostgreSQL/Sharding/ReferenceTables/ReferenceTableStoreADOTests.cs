using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
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
    public async Task UpsertAsync_DbCommand_UsesExecuteNonQueryAsync()
    {
        var command = new FakeDbCommand { NonQueryResult = 2 };
        var connection = new FakeDbConnection { CommandToReturn = command };

        using var store = new ReferenceTableStoreADO(connection);
        var entities = new[]
        {
            new TestReferenceEntity { Id = Guid.NewGuid(), Name = "Widget" },
            new TestReferenceEntity { Id = Guid.NewGuid(), Name = "Gadget" }
        };

        var result = await store.UpsertAsync(entities);

        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));
        command.ExecuteNonQueryAsyncCallCount.ShouldBe(1);
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

#region Fake ADO.NET Types (DbCommand path)

/// <summary>
/// Minimal <see cref="DbCommand"/> test double so <c>UpsertBatchAsync</c>'s
/// <c>ExecuteNonQueryAsync</c> branch (only reachable through a real <see cref="DbCommand"/>) is
/// exercised without a live connection.
/// </summary>
internal sealed class FakeDbCommand : DbCommand
{
    private readonly FakeDbParameterCollection _parameters = new();

    public int ExecuteNonQueryAsyncCallCount { get; private set; }
    public int NonQueryResult { get; set; } = 1;

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }
    public override bool DesignTimeVisible { get; set; }

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery() => NonQueryResult;

    public override object? ExecuteScalar() => null;

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter() => new FakeDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        throw new NotSupportedException("Not needed for UpsertBatchAsync coverage.");

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        ExecuteNonQueryAsyncCallCount++;
        return Task.FromResult(NonQueryResult);
    }
}

internal sealed class FakeDbParameter : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; }

    [AllowNull]
    public override string ParameterName { get; set; } = string.Empty;

    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;
    public override object? Value { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override int Size { get; set; }

    public override void ResetDbType()
    {
    }
}

internal sealed class FakeDbParameterCollection : DbParameterCollection
{
    private readonly List<object> _items = [];

    public override int Count => _items.Count;
    public override object SyncRoot { get; } = new();

    public override int Add(object value)
    {
        _items.Add(value);
        return _items.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var value in values)
        {
            _items.Add(value!);
        }
    }

    public override void Clear() => _items.Clear();

    public override bool Contains(object value) => _items.Contains(value);

    public override bool Contains(string value) =>
        _items.Any(p => string.Equals(((DbParameter)p).ParameterName, value, StringComparison.Ordinal));

    public override void CopyTo(Array array, int index) =>
        ((System.Collections.ICollection)_items).CopyTo(array, index);

    public override System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();

    protected override DbParameter GetParameter(int index) => (DbParameter)_items[index];

    protected override DbParameter GetParameter(string parameterName) =>
        (DbParameter)_items.First(p => string.Equals(((DbParameter)p).ParameterName, parameterName, StringComparison.Ordinal));

    public override int IndexOf(object value) => _items.IndexOf(value);

    public override int IndexOf(string parameterName) =>
        _items.FindIndex(p => string.Equals(((DbParameter)p).ParameterName, parameterName, StringComparison.Ordinal));

    public override void Insert(int index, object value) => _items.Insert(index, value);

    public override void Remove(object value) => _items.Remove(value);

    public override void RemoveAt(int index) => _items.RemoveAt(index);

    public override void RemoveAt(string parameterName) => _items.RemoveAt(IndexOf(parameterName));

    protected override void SetParameter(int index, DbParameter value) => _items[index] = value;

    protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
}

/// <summary>
/// Minimal <see cref="DbConnection"/> test double that returns a preconfigured <see cref="FakeDbCommand"/>.
/// </summary>
internal sealed class FakeDbConnection : DbConnection
{
    public FakeDbCommand? CommandToReturn { get; set; }

    [AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => "fake";
    public override string DataSource => "fake";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => ConnectionState.Open;

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Close()
    {
    }

    public override void Open()
    {
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        throw new NotSupportedException("Not needed for UpsertBatchAsync coverage.");

    protected override DbCommand CreateDbCommand() =>
        CommandToReturn ?? throw new InvalidOperationException("No command configured.");
}

#endregion
