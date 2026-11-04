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

    public ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return ValueTask.CompletedTask;
        }

        _collection = _fixture.Database!.GetCollection<TestDocument>("test_documents");
        _repository = new FunctionalRepositoryMongoDB<TestDocument, Guid>(
            _collection,
            d => d.Id);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
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

    #region String Method Wildcard Tests

    // Each value would also match other names if its LIKE wildcards ('%', '_', SQL Server's '[')
    // or the '!' escape character were not matched literally (#3156).
    private static readonly string[] Names = ["500", "50% off", "100%", "a_b", "axb", "[a]", "hey!"];

    [Theory]
    [InlineData(nameof(string.Contains), "50%", "50% off")]
    [InlineData(nameof(string.StartsWith), "a_", "a_b")]
    [InlineData(nameof(string.EndsWith), "0%", "100%")]
    [InlineData(nameof(string.Contains), "[a]", "[a]")]
    [InlineData(nameof(string.Contains), "!", "hey!")]
    public async Task ListAsync_StringMethodSpecification_MatchesLikeWildcardsLiterally(string method, string value, string expectedName)
    {
        // Arrange
        SkipIfNotAvailable();
        await ClearDataAsync();
        await _repository!.AddRangeAsync(Names.Select(name => CreateTestDocument(name)).ToArray());
        var spec = new DocumentNameSpec(method, value);

        // Act
        var result = await _repository!.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Select(e => e.Name).ToList().ShouldBe([expectedName]));
    }

    #endregion

    #region Keyset Direction Tests

    private static readonly decimal[] KeysetValues = [100m, 200m, 300m, 400m];

    [Fact]
    public async Task QuerySpecificationFilter_KeysetSortedDescending_ReturnsTheKeysBelowTheLastOne()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - the next page of a descending keyset holds the smaller keys (#3154). ListAsync does not
        // apply a QuerySpecification's keyset or sort yet (#3151), so this runs the builder's filter and sort.
        await _repository!.AddRangeAsync(KeysetValues.Select(a => CreateTestDocument($"Document {a}", amount: a)).ToArray());
        var builder = new SpecificationFilterBuilder<TestDocument>();
        var spec = new DescendingAmountKeysetDocumentSpec(lastAmount: 300m);

        // Act
        var documents = await _collection!
            .Find(builder.BuildFilter(spec))
            .Sort(builder.BuildSortDefinition(spec)!)
            .ToListAsync();

        // Assert
        documents.Select(d => d.Amount).ShouldBe([200m, 100m]);
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
    [global::MongoDB.Bson.Serialization.Attributes.BsonGuidRepresentation(global::MongoDB.Bson.GuidRepresentation.Standard)]
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

/// <summary>
/// Specification matching a TestDocument whose name contains, starts with or ends with a value.
/// </summary>
public class DocumentNameSpec(string method, string value) : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression() => method switch
    {
        nameof(string.Contains) => e => e.Name.Contains(value),
        nameof(string.StartsWith) => e => e.Name.StartsWith(value),
        nameof(string.EndsWith) => e => e.Name.EndsWith(value),
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Expected Contains, StartsWith or EndsWith."),
    };
}

/// <summary>
/// Next page of documents by amount, sorted in descending order, after <c>lastAmount</c>.
/// </summary>
public class DescendingAmountKeysetDocumentSpec : QuerySpecification<TestDocument>
{
    public DescendingAmountKeysetDocumentSpec(decimal lastAmount)
    {
        ApplyOrderByDescending(d => d.Amount);
        ApplyKeysetPagination(d => d.Amount, lastAmount, 10);
    }
}

#endregion

/// <summary>
/// Exception to skip tests when infrastructure is not available.
/// </summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}
