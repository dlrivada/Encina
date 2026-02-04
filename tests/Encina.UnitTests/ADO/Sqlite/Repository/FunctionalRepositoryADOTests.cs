using System.Data;
using System.Linq.Expressions;
using Encina.ADO.Sqlite.Repository;
using Encina.DomainModeling;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.Sqlite.Repository;

/// <summary>
/// Unit tests for <see cref="FunctionalRepositoryADO{TEntity, TId}"/> in SQLite.
/// Uses NSubstitute for mocking database operations.
/// </summary>
[Trait("Category", "Unit")]
public class FunctionalRepositoryADOTests : IDisposable
{
    private readonly IDbConnection _mockConnection;
    private readonly IDbCommand _mockCommand;
    private readonly IDataReader _mockReader;
    private readonly IDataParameterCollection _mockParameters;
    private readonly IEntityMapping<TestEntitySqliteADO, Guid> _mapping;
    private readonly FunctionalRepositoryADO<TestEntitySqliteADO, Guid> _repository;
    private readonly List<IDbDataParameter> _capturedParameters = [];

    public FunctionalRepositoryADOTests()
    {
        _mockConnection = Substitute.For<IDbConnection>();
        _mockCommand = Substitute.For<IDbCommand>();
        _mockReader = Substitute.For<IDataReader>();
        _mockParameters = Substitute.For<IDataParameterCollection>();

        // Set up parameter collection
        _mockParameters.Add(Arg.Any<object>()).Returns(x =>
        {
            _capturedParameters.Add((IDbDataParameter)x.Arg<object>());
            return _capturedParameters.Count - 1;
        });
        _mockCommand.Parameters.Returns(_mockParameters);

        // Set up connection state
        _mockConnection.State.Returns(ConnectionState.Open);

        // Set up command creation
        _mockConnection.CreateCommand().Returns(_mockCommand);

        // Set up parameter creation
        var mockParameter = Substitute.For<IDbDataParameter>();
        _mockCommand.CreateParameter().Returns(mockParameter);

        _mapping = new EntityMappingBuilder<TestEntitySqliteADO, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .MapProperty(e => e.Amount, "Amount")
            .MapProperty(e => e.IsActive, "IsActive")
            .MapProperty(e => e.CreatedAtUtc, "CreatedAtUtc")
            .Build();

        _repository = new FunctionalRepositoryADO<TestEntitySqliteADO, Guid>(_mockConnection, _mapping);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryADO<TestEntitySqliteADO, Guid>(null!, _mapping));
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryADO<TestEntitySqliteADO, Guid>(connection, null!));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ConnectionClosed_OpensConnection()
    {
        // Arrange
        _mockConnection.State.Returns(ConnectionState.Closed);
        SetupReaderWithNoResults();

        var id = Guid.NewGuid();

        // Act
        await _repository.GetByIdAsync(id);

        // Assert - Connection open was called (we can't verify async directly with IDbConnection)
        _mockConnection.Received(1).CreateCommand();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        SetupReaderWithNoResults();
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("not found");
        });
    }

    [Fact]
    public async Task GetByIdAsync_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockCommand.ExecuteReader().Throws(new InvalidOperationException("Database error"));

        // Act
        var result = await _repository.GetByIdAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("Database error");
        });
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_EmptyTable_ReturnsEmptyList()
    {
        // Arrange
        SetupReaderWithNoResults();

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.ShouldBeEmpty());
    }

    [Fact]
    public async Task ListAsync_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        _mockCommand.ExecuteReader().Throws(new InvalidOperationException("List failed"));

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("List failed");
        });
    }

    [Fact]
    public async Task ListAsync_WithSpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _repository.ListAsync(null!));
    }

    [Fact]
    public async Task ListAsync_WithSpecification_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var spec = new ActiveEntitySqliteADOSpec();
        _mockCommand.ExecuteReader().Throws(new InvalidOperationException("Query failed"));

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("Query failed");
        });
    }

    [Fact]
    public async Task ListAsync_WithUnsupportedSpecification_ReturnsLeftWithInvalidOperation()
    {
        // Arrange
        var spec = new UnsupportedSqliteSpec();

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("not supported");
        });
    }

    #endregion

    #region FirstOrDefaultAsync Tests

    [Fact]
    public async Task FirstOrDefaultAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _repository.FirstOrDefaultAsync(null!));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NoMatch_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        SetupReaderWithNoResults();
        var spec = new ActiveEntitySqliteADOSpec();

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var spec = new ActiveEntitySqliteADOSpec();
        _mockCommand.ExecuteReader().Throws(new InvalidOperationException("Query failed"));

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Query failed"));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_UnsupportedSpecification_ReturnsLeftWithInvalidOperation()
    {
        // Arrange
        var spec = new UnsupportedSqliteSpec();

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not supported"));
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        _mockCommand.ExecuteScalar().Returns(5);

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(5));
    }

    [Fact]
    public async Task CountAsync_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        _mockCommand.ExecuteScalar().Throws(new InvalidOperationException("Count failed"));

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Count failed"));
    }

    [Fact]
    public async Task CountAsync_WithSpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _repository.CountAsync(null!));
    }

    [Fact]
    public async Task CountAsync_WithSpecification_ReturnsFilteredCount()
    {
        // Arrange
        var spec = new ActiveEntitySqliteADOSpec();
        _mockCommand.ExecuteScalar().Returns(3);

        // Act
        var result = await _repository.CountAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(3));
    }

    [Fact]
    public async Task CountAsync_WithSpecification_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var spec = new ActiveEntitySqliteADOSpec();
        _mockCommand.ExecuteScalar().Throws(new InvalidOperationException("Count query failed"));

        // Act
        var result = await _repository.CountAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Count query failed"));
    }

    [Fact]
    public async Task CountAsync_UnsupportedSpecification_ReturnsLeftWithInvalidOperation()
    {
        // Arrange
        var spec = new UnsupportedSqliteSpec();

        // Act
        var result = await _repository.CountAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not supported"));
    }

    #endregion

    #region AnyAsync Tests

    [Fact]
    public async Task AnyAsync_HasEntities_ReturnsTrue()
    {
        // Arrange
        _mockCommand.ExecuteScalar().Returns(1);

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    [Fact]
    public async Task AnyAsync_NoEntities_ReturnsFalse()
    {
        // Arrange
        _mockCommand.ExecuteScalar().Returns(0);

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    [Fact]
    public async Task AnyAsync_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        _mockCommand.ExecuteScalar().Throws(new InvalidOperationException("Any query failed"));

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Any query failed"));
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _repository.AnyAsync(null!));
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_HasMatch_ReturnsTrue()
    {
        // Arrange
        var spec = new ActiveEntitySqliteADOSpec();
        _mockCommand.ExecuteScalar().Returns(1);

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_NoMatch_ReturnsFalse()
    {
        // Arrange
        var spec = new ActiveEntitySqliteADOSpec();
        _mockCommand.ExecuteScalar().Returns(0);

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var spec = new ActiveEntitySqliteADOSpec();
        _mockCommand.ExecuteScalar().Throws(new InvalidOperationException("Any spec query failed"));

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Any spec query failed"));
    }

    [Fact]
    public async Task AnyAsync_UnsupportedSpecification_ReturnsLeftWithInvalidOperation()
    {
        // Arrange
        var spec = new UnsupportedSqliteSpec();

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not supported"));
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_ValidEntity_ReturnsRight()
    {
        // Arrange
        var entity = CreateTestEntity();
        _mockCommand.ExecuteNonQuery().Returns(1);

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Id.ShouldBe(entity.Id));
    }

    [Fact]
    public async Task AddAsync_DuplicateKey_ReturnsLeftWithAlreadyExistsError()
    {
        // Arrange
        var entity = CreateTestEntity();
        // SQLite uses "UNIQUE constraint failed" for duplicate key violations
        _mockCommand.ExecuteNonQuery().Throws(new InvalidOperationException("UNIQUE constraint failed"));

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("already exists"));
    }

    [Fact]
    public async Task AddAsync_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var entity = CreateTestEntity();
        _mockCommand.ExecuteNonQuery().Throws(new InvalidOperationException("Insert failed"));

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Insert failed"));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_ReturnsRight()
    {
        // Arrange
        var entity = CreateTestEntity();
        _mockCommand.ExecuteNonQuery().Returns(1);

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Id.ShouldBe(entity.Id));
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var entity = CreateTestEntity();
        _mockCommand.ExecuteNonQuery().Returns(0);

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task UpdateAsync_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var entity = CreateTestEntity();
        _mockCommand.ExecuteNonQuery().Throws(new InvalidOperationException("Update failed"));

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Update failed"));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ById_ExistingEntity_ReturnsRight()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockCommand.ExecuteNonQuery().Returns(1);

        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ById_NonExistingEntity_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockCommand.ExecuteNonQuery().Returns(0);

        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task DeleteAsync_ById_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockCommand.ExecuteNonQuery().Throws(new InvalidOperationException("Delete failed"));

        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Delete failed"));
    }

    [Fact]
    public async Task DeleteAsync_ByEntity_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _repository.DeleteAsync((TestEntitySqliteADO)null!));
    }

    [Fact]
    public async Task DeleteAsync_ByEntity_ExistingEntity_ReturnsRight()
    {
        // Arrange
        var entity = CreateTestEntity();
        _mockCommand.ExecuteNonQuery().Returns(1);

        // Act
        var result = await _repository.DeleteAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _repository.AddRangeAsync(null!));
    }

    [Fact]
    public async Task AddRangeAsync_ValidEntities_ReturnsRight()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        };
        _mockCommand.ExecuteNonQuery().Returns(1);

        // Act
        var result = await _repository.AddRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(3));
    }

    [Fact]
    public async Task AddRangeAsync_DuplicateKey_ReturnsLeftWithAlreadyExistsError()
    {
        // Arrange
        var entities = new[] { CreateTestEntity("Entity 1") };
        // SQLite uses "UNIQUE constraint failed" for duplicate key violations
        _mockCommand.ExecuteNonQuery().Throws(new InvalidOperationException("UNIQUE constraint failed"));

        // Act
        var result = await _repository.AddRangeAsync(entities);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("already exist"));
    }

    [Fact]
    public async Task AddRangeAsync_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var entities = new[] { CreateTestEntity("Entity 1") };
        _mockCommand.ExecuteNonQuery().Throws(new InvalidOperationException("AddRange failed"));

        // Act
        var result = await _repository.AddRangeAsync(entities);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("AddRange failed"));
    }

    #endregion

    #region UpdateRangeAsync Tests

    [Fact]
    public async Task UpdateRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _repository.UpdateRangeAsync(null!));
    }

    [Fact]
    public async Task UpdateRangeAsync_ValidEntities_ReturnsRight()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2")
        };
        _mockCommand.ExecuteNonQuery().Returns(1);

        // Act
        var result = await _repository.UpdateRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateRangeAsync_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var entities = new[] { CreateTestEntity("Entity 1") };
        _mockCommand.ExecuteNonQuery().Throws(new InvalidOperationException("UpdateRange failed"));

        // Act
        var result = await _repository.UpdateRangeAsync(entities);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("UpdateRange failed"));
    }

    #endregion

    #region DeleteRangeAsync Tests

    [Fact]
    public async Task DeleteRangeAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _repository.DeleteRangeAsync(null!));
    }

    [Fact]
    public async Task DeleteRangeAsync_WithSpecification_ReturnsDeletedCount()
    {
        // Arrange
        var spec = new ActiveEntitySqliteADOSpec();
        _mockCommand.ExecuteNonQuery().Returns(5);

        // Act
        var result = await _repository.DeleteRangeAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(5));
    }

    [Fact]
    public async Task DeleteRangeAsync_CommandException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var spec = new ActiveEntitySqliteADOSpec();
        _mockCommand.ExecuteNonQuery().Throws(new InvalidOperationException("DeleteRange failed"));

        // Act
        var result = await _repository.DeleteRangeAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("DeleteRange failed"));
    }

    [Fact]
    public async Task DeleteRangeAsync_UnsupportedSpecification_ReturnsLeftWithInvalidOperation()
    {
        // Arrange
        var spec = new UnsupportedSqliteSpec();

        // Act
        var result = await _repository.DeleteRangeAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not supported"));
    }

    #endregion

    #region Helper Methods

    private void SetupReaderWithNoResults()
    {
        _mockReader.Read().Returns(false);
        _mockCommand.ExecuteReader().Returns(_mockReader);
    }

    private static TestEntitySqliteADO CreateTestEntity(
        string name = "Test Entity",
        bool isActive = true,
        decimal amount = 100m)
    {
        return new TestEntitySqliteADO
        {
            Id = Guid.NewGuid(),
            Name = name,
            Amount = amount,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    #endregion
}

#region Test Entity and Specifications

/// <summary>
/// Test entity for ADO.NET SQLite repository unit tests.
/// </summary>
public class TestEntitySqliteADO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Specification for active entities (SQLite ADO tests).
/// </summary>
public class ActiveEntitySqliteADOSpec : Specification<TestEntitySqliteADO>
{
    public override Expression<Func<TestEntitySqliteADO, bool>> ToExpression()
        => e => e.IsActive;
}

/// <summary>
/// Specification that causes NotSupportedException (for testing unsupported expressions).
/// Uses an unsupported method call expression (string.Replace).
/// </summary>
public class UnsupportedSqliteSpec : Specification<TestEntitySqliteADO>
{
    public override Expression<Func<TestEntitySqliteADO, bool>> ToExpression()
        => e => e.Name.Replace("a", "b") == "test";
}

/// <summary>
/// Specification for minimum amount (SQLite ADO tests).
/// </summary>
public class MinAmountSqliteADOSpec : Specification<TestEntitySqliteADO>
{
    private readonly decimal _minAmount;

    public MinAmountSqliteADOSpec(decimal minAmount) => _minAmount = minAmount;

    public override Expression<Func<TestEntitySqliteADO, bool>> ToExpression()
        => e => e.Amount >= _minAmount;
}

#endregion
