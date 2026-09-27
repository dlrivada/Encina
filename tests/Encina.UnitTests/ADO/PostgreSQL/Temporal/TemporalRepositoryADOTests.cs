using System.Data;
using System.Linq.Expressions;
using Encina.ADO.PostgreSQL.Temporal;
using Encina.DomainModeling;
using Encina.Messaging.Temporal;
using Encina.Testing.Shouldly;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.PostgreSQL.Temporal;

/// <summary>
/// Unit tests for <see cref="TemporalRepositoryADO{TEntity, TId}"/>. Uses NSubstitute mocks of the
/// plain (non-<see cref="System.Data.Common.DbConnection"/>) ADO.NET interfaces, which drives the
/// repository through its synchronous-fallback code path — enough to exercise every branch of the
/// shared query helpers and the read-only repository methods (#1325 CRAP-gate follow-up).
/// </summary>
[Trait("Category", "Unit")]
public class TemporalRepositoryADOTests
{
    private readonly IDbConnection _mockConnection;
    private readonly IDbCommand _mockCommand;
    private readonly IDataReader _mockReader;
    private readonly IDataParameterCollection _mockParameters;
    private readonly ILogger<TemporalRepositoryADO<TestTemporalEntity, Guid>> _mockLogger;
    private readonly ITemporalEntityMapping<TestTemporalEntity, Guid> _mapping;

    public TemporalRepositoryADOTests()
    {
        _mockConnection = Substitute.For<IDbConnection>();
        _mockCommand = Substitute.For<IDbCommand>();
        _mockReader = Substitute.For<IDataReader>();
        _mockParameters = Substitute.For<IDataParameterCollection>();
        _mockLogger = Substitute.For<ILogger<TemporalRepositoryADO<TestTemporalEntity, Guid>>>();

        _mockCommand.Parameters.Returns(_mockParameters);
        _mockCommand.CreateParameter().Returns(_ => Substitute.For<IDbDataParameter>());
        _mockConnection.State.Returns(ConnectionState.Open);
        _mockConnection.CreateCommand().Returns(_mockCommand);

        _mapping = new TemporalEntityMappingBuilder<TestTemporalEntity, Guid>()
            .ToTable("orders")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "name")
            .WithPeriodColumns("sys_period_start", "sys_period_end")
            .WithHistoryTable("orders_history")
            .Build()
            .ShouldBeSuccess();
    }

    private TemporalRepositoryADO<TestTemporalEntity, Guid> CreateRepository(TemporalTableOptions? options = null) =>
        new(_mockConnection, _mapping, options ?? new TemporalTableOptions(), _mockLogger);

    private void SetupReaderWithRow(Guid id, string name)
    {
        _mockReader.Read().Returns(true, false);
        _mockReader.GetOrdinal("id").Returns(0);
        _mockReader.GetOrdinal("name").Returns(1);
        _mockReader.IsDBNull(0).Returns(false);
        _mockReader.IsDBNull(1).Returns(false);
        _mockReader.GetValue(0).Returns(id);
        _mockReader.GetValue(1).Returns(name);
        _mockCommand.ExecuteReader().Returns(_mockReader);
    }

    private void SetupReaderWithNoRows()
    {
        _mockReader.Read().Returns(false);
        _mockCommand.ExecuteReader().Returns(_mockReader);
    }

    #region Constructor

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TemporalRepositoryADO<TestTemporalEntity, Guid>(null!, _mapping, new TemporalTableOptions(), _mockLogger));
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TemporalRepositoryADO<TestTemporalEntity, Guid>(_mockConnection, null!, new TemporalTableOptions(), _mockLogger));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TemporalRepositoryADO<TestTemporalEntity, Guid>(_mockConnection, _mapping, null!, _mockLogger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TemporalRepositoryADO<TestTemporalEntity, Guid>(_mockConnection, _mapping, new TemporalTableOptions(), null!));
    }

    #endregion

    #region GetByIdAsync / GetAllAsync / GetPagedAsync

    [Fact]
    public async Task GetByIdAsync_EntityFound_ReturnsSome()
    {
        var repository = CreateRepository();
        var id = Guid.NewGuid();
        SetupReaderWithRow(id, "Widget");

        var result = await repository.GetByIdAsync(id);

        result.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_EntityNotFound_ReturnsNone()
    {
        var repository = CreateRepository();
        SetupReaderWithNoRows();

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        result.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllRows()
    {
        var repository = CreateRepository();
        SetupReaderWithRow(Guid.NewGuid(), "Widget");

        var result = await repository.GetAllAsync();

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetPagedAsync_ByPageNumber_ReturnsPagedResult()
    {
        var repository = CreateRepository();
        _mockCommand.ExecuteScalar().Returns(1);
        SetupReaderWithRow(Guid.NewGuid(), "Widget");

        var result = await repository.GetPagedAsync(1, 10);

        result.TotalCount.ShouldBe(1);
        result.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithSpecification_ReturnsPagedResult()
    {
        var repository = CreateRepository();
        _mockCommand.ExecuteScalar().Returns(1);
        SetupReaderWithRow(Guid.NewGuid(), "Widget");

        var result = await repository.GetPagedAsync(new ActiveTemporalEntitySpec(), 1, 10);

        result.TotalCount.ShouldBe(1);
        result.Items.Count.ShouldBe(1);
    }

    #endregion

    #region FindAsync / FindOneAsync

    [Fact]
    public async Task FindAsync_WithSpecification_ReturnsMatches()
    {
        var repository = CreateRepository();
        SetupReaderWithRow(Guid.NewGuid(), "Widget");

        var result = await repository.FindAsync(new ActiveTemporalEntitySpec());

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_WithPredicate_FiltersInMemory()
    {
        var repository = CreateRepository();
        SetupReaderWithRow(Guid.NewGuid(), "Widget");

        var result = await repository.FindAsync(e => e.Name == "Widget");

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task FindOneAsync_EntityFound_ReturnsSome()
    {
        var repository = CreateRepository();
        SetupReaderWithRow(Guid.NewGuid(), "Widget");

        var result = await repository.FindOneAsync(new ActiveTemporalEntitySpec());

        result.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task FindOneAsync_NoMatch_ReturnsNone()
    {
        var repository = CreateRepository();
        SetupReaderWithNoRows();

        var result = await repository.FindOneAsync(new ActiveTemporalEntitySpec());

        result.IsNone.ShouldBeTrue();
    }

    #endregion

    #region AnyAsync / CountAsync

    [Fact]
    public async Task AnyAsync_WithSpecification_ReturnsTrue()
    {
        var repository = CreateRepository();
        _mockCommand.ExecuteScalar().Returns(true);

        var result = await repository.AnyAsync(new ActiveTemporalEntitySpec());

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task AnyAsync_WithPredicate_FiltersInMemory()
    {
        var repository = CreateRepository();
        SetupReaderWithRow(Guid.NewGuid(), "Widget");

        var result = await repository.AnyAsync(e => e.Name == "Widget");

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CountAsync_WithSpecification_ReturnsCount()
    {
        var repository = CreateRepository();
        _mockCommand.ExecuteScalar().Returns(3);

        var result = await repository.CountAsync(new ActiveTemporalEntitySpec());

        result.ShouldBe(3);
    }

    [Fact]
    public async Task CountAsync_NoSpecification_ReturnsCount()
    {
        var repository = CreateRepository();
        _mockCommand.ExecuteScalar().Returns(2);

        var result = await repository.CountAsync();

        result.ShouldBe(2);
    }

    #endregion

    #region GetAsOfAsync

    [Fact]
    public async Task GetAsOfAsync_InvalidDateTimeKind_ReturnsLeft()
    {
        var repository = CreateRepository();

        var result = await repository.GetAsOfAsync(Guid.NewGuid(), DateTime.Now);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.ErrorCode.ShouldBe("REPOSITORY_INVALID_DATETIME_KIND"));
    }

    [Fact]
    public async Task GetAsOfAsync_EntityFound_ReturnsRight()
    {
        var repository = CreateRepository(new TemporalTableOptions { LogTemporalQueries = true });
        var id = Guid.NewGuid();
        SetupReaderWithRow(id, "Widget");

        var result = await repository.GetAsOfAsync(id, DateTime.UtcNow);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsOfAsync_EntityNotFound_ReturnsLeftWithNotFound()
    {
        var repository = CreateRepository();
        SetupReaderWithNoRows();

        var result = await repository.GetAsOfAsync(Guid.NewGuid(), DateTime.UtcNow);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsOfAsync_CommandThrows_ReturnsLeftWithOperationFailed()
    {
        var repository = CreateRepository();
        _mockCommand.ExecuteReader().Throws(new InvalidOperationException("boom"));

        var result = await repository.GetAsOfAsync(Guid.NewGuid(), DateTime.UtcNow);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetHistoryAsync

    [Fact]
    public async Task GetHistoryAsync_ReturnsAllVersions()
    {
        var repository = CreateRepository(new TemporalTableOptions { LogTemporalQueries = true });
        SetupReaderWithRow(Guid.NewGuid(), "Widget");

        var result = await repository.GetHistoryAsync(Guid.NewGuid());

        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(1));
    }

    [Fact]
    public async Task GetHistoryAsync_CommandThrows_ReturnsLeft()
    {
        var repository = CreateRepository();
        _mockCommand.ExecuteReader().Throws(new InvalidOperationException("boom"));

        var result = await repository.GetHistoryAsync(Guid.NewGuid());

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetChangedBetweenAsync

    [Fact]
    public async Task GetChangedBetweenAsync_FromAfterTo_ReturnsLeft()
    {
        var repository = CreateRepository();
        var to = DateTime.UtcNow;
        var from = to.AddDays(1);

        var result = await repository.GetChangedBetweenAsync(from, to);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.ErrorCode.ShouldBe("REPOSITORY_INVALID_TIME_RANGE"));
    }

    [Fact]
    public async Task GetChangedBetweenAsync_InvalidFromKind_ReturnsLeft()
    {
        var repository = CreateRepository();

        var result = await repository.GetChangedBetweenAsync(DateTime.Now, DateTime.UtcNow);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetChangedBetweenAsync_InvalidToKind_ReturnsLeft()
    {
        var repository = CreateRepository();

        var result = await repository.GetChangedBetweenAsync(DateTime.UtcNow.AddDays(-1), DateTime.Now);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetChangedBetweenAsync_ValidRange_ReturnsMatches()
    {
        var repository = CreateRepository(new TemporalTableOptions { LogTemporalQueries = true });
        SetupReaderWithRow(Guid.NewGuid(), "Widget");

        var result = await repository.GetChangedBetweenAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(1));
    }

    [Fact]
    public async Task GetChangedBetweenAsync_CommandThrows_ReturnsLeft()
    {
        var repository = CreateRepository();
        _mockCommand.ExecuteReader().Throws(new InvalidOperationException("boom"));

        var result = await repository.GetChangedBetweenAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region ListAsOfAsync

    [Fact]
    public void ListAsOfAsync_NullSpecification_ThrowsArgumentNullException()
    {
        var repository = CreateRepository();

        Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.ListAsOfAsync(null!, DateTime.UtcNow));
    }

    [Fact]
    public async Task ListAsOfAsync_InvalidDateTimeKind_ReturnsLeft()
    {
        var repository = CreateRepository();

        var result = await repository.ListAsOfAsync(new ActiveTemporalEntitySpec(), DateTime.Now);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ListAsOfAsync_ValidSpecification_ReturnsMatches()
    {
        var repository = CreateRepository(new TemporalTableOptions { LogTemporalQueries = true });
        SetupReaderWithRow(Guid.NewGuid(), "Widget");

        var result = await repository.ListAsOfAsync(new ActiveTemporalEntitySpec(), DateTime.UtcNow);

        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(1));
    }

    [Fact]
    public async Task ListAsOfAsync_CommandThrows_ReturnsLeft()
    {
        var repository = CreateRepository();
        _mockCommand.ExecuteReader().Throws(new InvalidOperationException("boom"));

        var result = await repository.ListAsOfAsync(new ActiveTemporalEntitySpec(), DateTime.UtcNow);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion
}

#region Test Entity and Specification

public class TestTemporalEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ActiveTemporalEntitySpec : Specification<TestTemporalEntity>
{
    public override Expression<Func<TestTemporalEntity, bool>> ToExpression() => e => e.Name != string.Empty;
}

#endregion
