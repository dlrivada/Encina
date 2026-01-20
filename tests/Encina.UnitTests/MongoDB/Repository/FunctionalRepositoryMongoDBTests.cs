using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.MongoDB.Repository;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.MongoDB.Repository;

/// <summary>
/// Unit tests for <see cref="FunctionalRepositoryMongoDB{TEntity, TId}"/>.
/// Uses NSubstitute to mock MongoDB collection operations.
/// </summary>
[Trait("Category", "Unit")]
public class FunctionalRepositoryMongoDBTests
{
    private readonly IMongoCollection<TestDocument> _mockCollection;
    private readonly FunctionalRepositoryMongoDB<TestDocument, Guid> _repository;

    public FunctionalRepositoryMongoDBTests()
    {
        _mockCollection = Substitute.For<IMongoCollection<TestDocument>>();
        _repository = new FunctionalRepositoryMongoDB<TestDocument, Guid>(
            _mockCollection,
            d => d.Id);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryMongoDB<TestDocument, Guid>(null!, d => d.Id));
    }

    [Fact]
    public void Constructor_NullIdSelector_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryMongoDB<TestDocument, Guid>(
                _mockCollection,
                null!));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsRight()
    {
        // Arrange
        var entity = CreateTestDocument();
        var mockCursor = CreateMockCursor(entity);

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(mockCursor);

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e =>
        {
            e.Id.ShouldBe(entity.Id);
            e.Status.ShouldBe(entity.Status);
        });
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var mockCursor = CreateMockCursor();

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(mockCursor);

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("not found");
            error.Message.ShouldContain(nonExistingId.ToString());
        });
    }

    [Fact]
    public async Task GetByIdAsync_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Connection failed"));

        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("failed"));
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_WithEntities_ReturnsAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestDocument("Doc 1"),
            CreateTestDocument("Doc 2"),
            CreateTestDocument("Doc 3")
        };
        var mockCursor = CreateMockCursor(entities);

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(mockCursor);

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(3));
    }

    [Fact]
    public async Task ListAsync_EmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var mockCursor = CreateMockCursor();

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(mockCursor);

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.ShouldBeEmpty());
    }

    [Fact]
    public async Task ListAsync_WithSpecification_ReturnsFilteredEntities()
    {
        // Arrange
        var activeEntity = CreateTestDocument("Active", isActive: true);
        var mockCursor = CreateMockCursor(activeEntity);

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(mockCursor);

        var spec = new ActiveDocumentsSpec();

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Status.ShouldBe("Active");
        });
    }

    [Fact]
    public async Task ListAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _repository.ListAsync((Specification<TestDocument>)null!));
    }

    [Fact]
    public async Task ListAsync_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Connection failed"));

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("failed"));
    }

    #endregion

    #region FirstOrDefaultAsync Tests

    [Fact]
    public async Task FirstOrDefaultAsync_MatchingEntity_ReturnsRight()
    {
        // Arrange
        var entity = CreateTestDocument("Test", isActive: true);
        var mockCursor = CreateMockCursor(entity);

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(mockCursor);

        var spec = new ActiveDocumentsSpec();

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Status.ShouldBe("Test"));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NoMatch_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var mockCursor = CreateMockCursor();

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(mockCursor);

        var spec = new ActiveDocumentsSpec();

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _repository.FirstOrDefaultAsync(null!));
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_WithEntities_ReturnsCorrectCount()
    {
        // Arrange
        _mockCollection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(5L);

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(5));
    }

    [Fact]
    public async Task CountAsync_EmptyCollection_ReturnsZero()
    {
        // Arrange
        _mockCollection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(0L);

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task CountAsync_WithSpecification_ReturnsFilteredCount()
    {
        // Arrange
        _mockCollection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(2L);

        var spec = new ActiveDocumentsSpec();

        // Act
        var result = await _repository.CountAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));
    }

    [Fact]
    public async Task CountAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _repository.CountAsync(null!));
    }

    #endregion

    #region AnyAsync Tests

    [Fact]
    public async Task AnyAsync_WithEntities_ReturnsTrue()
    {
        // Arrange
        _mockCollection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(1L);

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    [Fact]
    public async Task AnyAsync_EmptyCollection_ReturnsFalse()
    {
        // Arrange
        _mockCollection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(0L);

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_NoMatch_ReturnsFalse()
    {
        // Arrange
        _mockCollection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(0L);

        var spec = new ActiveDocumentsSpec();

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_HasMatch_ReturnsTrue()
    {
        // Arrange
        _mockCollection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(1L);

        var spec = new ActiveDocumentsSpec();

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    [Fact]
    public async Task AnyAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _repository.AnyAsync(null!));
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidEntity_ReturnsRight()
    {
        // Arrange
        var entity = CreateTestDocument("New Entity");

        _mockCollection.InsertOneAsync(
            Arg.Any<TestDocument>(),
            Arg.Any<InsertOneOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Status.ShouldBe("New Entity"));

        await _mockCollection.Received(1).InsertOneAsync(
            entity,
            Arg.Any<InsertOneOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var entity = CreateTestDocument();

        _mockCollection.InsertOneAsync(
            Arg.Any<TestDocument>(),
            Arg.Any<InsertOneOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Insert failed"));

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("failed"));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_ReturnsRight()
    {
        // Arrange
        var entity = CreateTestDocument("Updated");
        var replaceResult = Substitute.For<ReplaceOneResult>();
        replaceResult.MatchedCount.Returns(1L);

        _mockCollection.ReplaceOneAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<TestDocument>(),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(replaceResult);

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Status.ShouldBe("Updated"));
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var entity = CreateTestDocument();
        var replaceResult = Substitute.For<ReplaceOneResult>();
        replaceResult.MatchedCount.Returns(0L);

        _mockCollection.ReplaceOneAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<TestDocument>(),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(replaceResult);

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _repository.UpdateAsync(null!));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ById_ExistingEntity_ReturnsRight()
    {
        // Arrange
        var id = Guid.NewGuid();
        var deleteResult = Substitute.For<DeleteResult>();
        deleteResult.DeletedCount.Returns(1L);

        _mockCollection.DeleteOneAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(deleteResult);

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
        var deleteResult = Substitute.For<DeleteResult>();
        deleteResult.DeletedCount.Returns(0L);

        _mockCollection.DeleteOneAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(deleteResult);

        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task DeleteAsync_ByEntity_ExistingEntity_ReturnsRight()
    {
        // Arrange
        var entity = CreateTestDocument();
        var deleteResult = Substitute.For<DeleteResult>();
        deleteResult.DeletedCount.Returns(1L);

        _mockCollection.DeleteOneAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(deleteResult);

        // Act
        var result = await _repository.DeleteAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _repository.DeleteAsync((TestDocument)null!));
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_ValidEntities_ReturnsRight()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestDocument("Entity 1"),
            CreateTestDocument("Entity 2"),
            CreateTestDocument("Entity 3")
        };

        _mockCollection.InsertManyAsync(
            Arg.Any<IEnumerable<TestDocument>>(),
            Arg.Any<InsertManyOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _repository.AddRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(3));
    }

    [Fact]
    public async Task AddRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _repository.AddRangeAsync(null!));
    }

    #endregion

    #region UpdateRangeAsync Tests

    [Fact]
    public async Task UpdateRangeAsync_ExistingEntities_ReturnsRight()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestDocument("Entity 1"),
            CreateTestDocument("Entity 2")
        };

        // Create an acknowledged bulk write result using the concrete nested class
        var bulkWriteResult = new BulkWriteResult<TestDocument>.Acknowledged(
            requestCount: 2,
            matchedCount: 2,
            deletedCount: 0,
            insertedCount: 0,
            modifiedCount: 2,
            processedRequests: [],
            upserts: []);

        _mockCollection.BulkWriteAsync(
            Arg.Any<IEnumerable<WriteModel<TestDocument>>>(),
            Arg.Any<BulkWriteOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(bulkWriteResult);

        // Act
        var result = await _repository.UpdateRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _repository.UpdateRangeAsync(null!));
    }

    [Fact]
    public async Task UpdateRangeAsync_EmptyEntities_ReturnsRightWithoutCallingBulkWrite()
    {
        // Arrange
        var entities = Array.Empty<TestDocument>();

        // Act
        var result = await _repository.UpdateRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();

        await _mockCollection.DidNotReceive().BulkWriteAsync(
            Arg.Any<IEnumerable<WriteModel<TestDocument>>>(),
            Arg.Any<BulkWriteOptions>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteRangeAsync Tests

    [Fact]
    public async Task DeleteRangeAsync_WithSpecification_DeletesMatchingEntities()
    {
        // Arrange
        var deleteResult = Substitute.For<DeleteResult>();
        deleteResult.DeletedCount.Returns(2L);

        _mockCollection.DeleteManyAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(deleteResult);

        var spec = new ActiveDocumentsSpec();

        // Act
        var result = await _repository.DeleteRangeAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));
    }

    [Fact]
    public async Task DeleteRangeAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _repository.DeleteRangeAsync(null!));
    }

    [Fact]
    public async Task DeleteRangeAsync_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var spec = new ActiveDocumentsSpec();
        _mockCollection.DeleteManyAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Delete failed"));

        // Act
        var result = await _repository.DeleteRangeAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Delete failed"));
    }

    [Fact]
    public async Task DeleteRangeAsync_UnsupportedSpecification_ReturnsLeftWithInvalidOperation()
    {
        // Arrange - Using combined specification which throws NotSupportedException
        var spec = new UnsupportedMongoSpec();

        // Act
        var result = await _repository.DeleteRangeAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not supported"));
    }

    #endregion

    #region Additional Exception Tests

    [Fact]
    public async Task ListAsync_WithSpecification_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var spec = new ActiveDocumentsSpec();
        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Query failed"));

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Query failed"));
    }

    [Fact]
    public async Task ListAsync_WithSpecification_UnsupportedSpec_ReturnsLeftWithInvalidOperation()
    {
        // Arrange
        var spec = new UnsupportedMongoSpec();

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not supported"));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var spec = new ActiveDocumentsSpec();
        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Query failed"));

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
        var spec = new UnsupportedMongoSpec();

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not supported"));
    }

    [Fact]
    public async Task CountAsync_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        _mockCollection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Count failed"));

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Count failed"));
    }

    [Fact]
    public async Task CountAsync_WithSpecification_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var spec = new ActiveDocumentsSpec();
        _mockCollection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Count failed"));

        // Act
        var result = await _repository.CountAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Count failed"));
    }

    [Fact]
    public async Task CountAsync_WithSpecification_UnsupportedSpec_ReturnsLeftWithInvalidOperation()
    {
        // Arrange
        var spec = new UnsupportedMongoSpec();

        // Act
        var result = await _repository.CountAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not supported"));
    }

    [Fact]
    public async Task AnyAsync_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        _mockCollection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Any failed"));

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Any failed"));
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var spec = new ActiveDocumentsSpec();
        _mockCollection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Any failed"));

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Any failed"));
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_UnsupportedSpec_ReturnsLeftWithInvalidOperation()
    {
        // Arrange
        var spec = new UnsupportedMongoSpec();

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not supported"));
    }

    [Fact]
    public async Task UpdateAsync_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var entity = CreateTestDocument();
        _mockCollection.ReplaceOneAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<TestDocument>(),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Update failed"));

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Update failed"));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var entity = CreateTestDocument();
        var replaceResult = Substitute.For<ReplaceOneResult>();
        replaceResult.MatchedCount.Returns(0L);

        _mockCollection.ReplaceOneAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<TestDocument>(),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(replaceResult);

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task DeleteAsync_ById_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockCollection.DeleteOneAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Delete failed"));

        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("Delete failed"));
    }

    [Fact]
    public async Task DeleteAsync_ById_NotFound_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var deleteResult = Substitute.For<DeleteResult>();
        deleteResult.DeletedCount.Returns(0L);

        _mockCollection.DeleteOneAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(deleteResult);

        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task AddRangeAsync_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var entities = new[] { CreateTestDocument("Doc1"), CreateTestDocument("Doc2") };
        _mockCollection.InsertManyAsync(
            Arg.Any<IEnumerable<TestDocument>>(),
            Arg.Any<InsertManyOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("InsertMany failed"));

        // Act
        var result = await _repository.AddRangeAsync(entities);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("InsertMany failed"));
    }

    [Fact]
    public async Task UpdateRangeAsync_MongoException_ReturnsLeftWithPersistenceError()
    {
        // Arrange
        var entities = new[] { CreateTestDocument("Doc1"), CreateTestDocument("Doc2") };
        _mockCollection.BulkWriteAsync(
            Arg.Any<IEnumerable<WriteModel<TestDocument>>>(),
            Arg.Any<BulkWriteOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("BulkWrite failed"));

        // Act
        var result = await _repository.UpdateRangeAsync(entities);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("BulkWrite failed"));
    }

    #endregion

    #region Helper Methods

    private static TestDocument CreateTestDocument(
        string status = "Test",
        bool isActive = true,
        decimal amount = 100m)
    {
        return new TestDocument
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Status = status,
            Amount = amount,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static IAsyncCursor<TestDocument> CreateMockCursor(params TestDocument[] documents)
    {
        var cursor = Substitute.For<IAsyncCursor<TestDocument>>();
        var enumerator = documents.GetEnumerator();
        var moveNextCalled = false;

        cursor.Current.Returns(_ => documents);
        cursor.MoveNext(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (!moveNextCalled)
            {
                moveNextCalled = true;
                return documents.Length > 0;
            }
            return false;
        });
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (!moveNextCalled)
            {
                moveNextCalled = true;
                return documents.Length > 0;
            }
            return false;
        });

        return cursor;
    }

    #endregion
}
