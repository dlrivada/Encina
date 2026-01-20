using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.MongoDB;
using Encina.MongoDB.Repository;
using Encina.TestInfrastructure.Fixtures;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Repository;

/// <summary>
/// Integration tests for <see cref="FunctionalRepositoryMongoDB{TEntity, TId}"/> using real MongoDB.
/// </summary>
[Collection(MongoDbTestCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public class FunctionalRepositoryMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private IMongoCollection<TestDocument>? _collection;
    private FunctionalRepositoryMongoDB<TestDocument, Guid>? _repository;

    public FunctionalRepositoryMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return Task.CompletedTask;
        }

        _collection = _fixture.Database!.GetCollection<TestDocument>("test_documents");
        _repository = new FunctionalRepositoryMongoDB<TestDocument, Guid>(
            _collection,
            d => d.Id);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_collection != null)
        {
            await _collection.DeleteManyAsync(Builders<TestDocument>.Filter.Empty);
        }
    }

    private async Task ClearDataAsync()
    {
        if (_collection != null)
        {
            await _collection.DeleteManyAsync(Builders<TestDocument>.Filter.Empty);
        }
    }

    private void SkipIfNotAvailable()
    {
        if (!_fixture.IsAvailable || _repository == null)
        {
            throw new SkipException("MongoDB container is not available");
        }
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsRight()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateTestDocument();
        await _repository!.AddAsync(entity);

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e =>
        {
            e.Id.ShouldBe(entity.Id);
            e.Name.ShouldBe(entity.Name);
        });
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsLeft()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository!.GetByIdAsync(nonExistingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_EmptyCollection_ReturnsEmptyList()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Act
        var result = await _repository!.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.ShouldBeEmpty());
    }

    [Fact]
    public async Task ListAsync_WithEntities_ReturnsAllEntities()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        await _repository!.AddRangeAsync(new[]
        {
            CreateTestDocument("Doc 1"),
            CreateTestDocument("Doc 2"),
            CreateTestDocument("Doc 3")
        });

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(3));
    }

    [Fact]
    public async Task ListAsync_WithSpecification_ReturnsFilteredEntities()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        await _repository!.AddRangeAsync(new[]
        {
            CreateTestDocument("Active", isActive: true),
            CreateTestDocument("Inactive", isActive: false)
        });

        var spec = new ActiveDocumentSpec();

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("Active");
        });
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidEntity_ReturnsRightAndPersists()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateTestDocument("New Document");

        // Act
        var result = await _repository!.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Name.ShouldBe("New Document"));

        // Verify persisted
        var stored = await _repository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_ReturnsRightAndUpdates()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateTestDocument("Original");
        await _repository!.AddAsync(entity);

        // Modify entity
        entity.Name = "Updated";

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _repository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e => e.Name.ShouldBe("Updated"));
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsLeft()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateTestDocument();

        // Act
        var result = await _repository!.UpdateAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_ReturnsRightAndRemoves()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateTestDocument();
        await _repository!.AddAsync(entity);

        // Act
        var result = await _repository.DeleteAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _repository.GetByIdAsync(entity.Id);
        stored.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsLeft()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository!.DeleteAsync(nonExistingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_WithEntities_ReturnsCorrectCount()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        await _repository!.AddRangeAsync(new[]
        {
            CreateTestDocument("Doc 1"),
            CreateTestDocument("Doc 2"),
            CreateTestDocument("Doc 3")
        });

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(3));
    }

    #endregion

    #region AnyAsync Tests

    [Fact]
    public async Task AnyAsync_EmptyCollection_ReturnsFalse()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Act
        var result = await _repository!.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    [Fact]
    public async Task AnyAsync_WithEntities_ReturnsTrue()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        await _repository!.AddAsync(CreateTestDocument());

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    #endregion

    #region Helper Methods

    private static TestDocument CreateTestDocument(
        string name = "Test Document",
        bool isActive = true,
        decimal amount = 100m)
    {
        return new TestDocument
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
/// Test document for MongoDB repository integration tests.
/// </summary>
public class TestDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Specification for active documents.
/// </summary>
public class ActiveDocumentSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.IsActive;
}

#endregion

/// <summary>
/// Exception to skip tests when infrastructure is not available.
/// </summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}
