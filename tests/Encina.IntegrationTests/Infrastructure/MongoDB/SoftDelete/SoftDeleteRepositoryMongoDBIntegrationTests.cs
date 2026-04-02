using System.Linq.Expressions;
using Encina.IntegrationTests.Infrastructure.MongoDB;
using Encina.Messaging.SoftDelete;
using Encina.MongoDB.SoftDelete;
using Encina.TestInfrastructure.Fixtures;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.SoftDelete;

/// <summary>
/// Integration tests for <see cref="SoftDeletableFunctionalRepositoryMongoDB{TEntity, TId}"/>
/// using real MongoDB via Testcontainers.
/// </summary>
[Collection(MongoDbTestCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class SoftDeleteRepositoryMongoDBIntegrationTests : IAsyncLifetime
{
    private const string CollectionName = "soft_delete_test_docs";

    private readonly MongoDbFixture _fixture;
    private IMongoCollection<SoftDeletableDocument>? _collection;
    private SoftDeletableFunctionalRepositoryMongoDB<SoftDeletableDocument, Guid>? _repository;

    public SoftDeleteRepositoryMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return ValueTask.CompletedTask;
        }

        _collection = _fixture.Database!.GetCollection<SoftDeletableDocument>(CollectionName);

        var mappingResult = new SoftDeleteEntityMappingBuilder<SoftDeletableDocument, Guid>()
            .HasId<Guid>(d => d.Id)
            .HasSoftDelete(d => d.IsDeleted, nameof(SoftDeletableDocument.IsDeleted))
            .HasDeletedAt(d => d.DeletedAtUtc, nameof(SoftDeletableDocument.DeletedAtUtc))
            .HasDeletedBy(d => d.DeletedBy, nameof(SoftDeletableDocument.DeletedBy))
            .Build();

        var mapping = mappingResult.Match(
            right => right,
            left => throw new InvalidOperationException($"Failed to build mapping: {left}"));

        var options = new SoftDeleteOptions
        {
            AutoFilterSoftDeletedQueries = true,
            TrackDeletedAt = true,
            TrackDeletedBy = true
        };

        _repository = new SoftDeletableFunctionalRepositoryMongoDB<SoftDeletableDocument, Guid>(
            _collection,
            mapping,
            options);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_collection is not null)
        {
            await _collection.DeleteManyAsync(Builders<SoftDeletableDocument>.Filter.Empty);
        }
    }

    private async Task ClearDataAsync()
    {
        if (_collection is not null)
        {
            await _collection.DeleteManyAsync(Builders<SoftDeletableDocument>.Filter.Empty);
        }
    }

    private void SkipIfNotAvailable()
    {
        if (!_fixture.IsAvailable || _repository is null)
        {
            Assert.Skip("MongoDB container is not available");
        }
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_NewEntity_SetsIsDeletedToFalse()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateDocument();

        // Act
        var result = await _repository!.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e =>
        {
            e.IsDeleted.ShouldBeFalse();
            e.Id.ShouldBe(entity.Id);
        });

        var stored = await _collection!.Find(d => d.Id == entity.Id).FirstOrDefaultAsync();
        stored.ShouldNotBeNull();
        stored.IsDeleted.ShouldBeFalse();
    }

    #endregion

    #region Soft Delete Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_SetsIsDeletedToTrue()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateDocument();
        await _repository!.AddAsync(entity);

        // Act
        var result = await _repository.DeleteAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _collection!.Find(d => d.Id == entity.Id).FirstOrDefaultAsync();
        stored.ShouldNotBeNull();
        stored.IsDeleted.ShouldBeTrue();
        stored.DeletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeleted_ReturnsError()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateDocument();
        await _repository!.AddAsync(entity);
        await _repository.DeleteAsync(entity.Id);

        // Act
        var result = await _repository.DeleteAsync(entity.Id);

        // Assert - soft-deleted entities are filtered from queries, so delete should fail
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Filtered Query Tests

    [Fact]
    public async Task GetByIdAsync_SoftDeletedEntity_ReturnsNotFound()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateDocument();
        await _repository!.AddAsync(entity);
        await _repository.DeleteAsync(entity.Id);

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ListAsync_ExcludesSoftDeletedEntities()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var active1 = CreateDocument("Active1");
        var active2 = CreateDocument("Active2");
        var toDelete = CreateDocument("ToDelete");

        await _repository!.AddAsync(active1);
        await _repository.AddAsync(active2);
        await _repository.AddAsync(toDelete);
        await _repository.DeleteAsync(toDelete.Id);

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities =>
        {
            entities.Count.ShouldBe(2);
            entities.ShouldContain(e => e.Id == active1.Id);
            entities.ShouldContain(e => e.Id == active2.Id);
            entities.ShouldNotContain(e => e.Id == toDelete.Id);
        });
    }

    [Fact]
    public async Task CountAsync_ExcludesSoftDeletedEntities()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var active = CreateDocument("Active");
        var toDelete = CreateDocument("ToDelete");

        await _repository!.AddAsync(active);
        await _repository.AddAsync(toDelete);
        await _repository.DeleteAsync(toDelete.Id);

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(1));
    }

    [Fact]
    public async Task AnyAsync_NoActiveEntities_ReturnsFalse()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateDocument();
        await _repository!.AddAsync(entity);
        await _repository.DeleteAsync(entity.Id);

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    #endregion

    #region Restore Tests

    [Fact]
    public async Task RestoreAsync_SoftDeletedEntity_MakesItVisibleAgain()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateDocument("Restorable");
        await _repository!.AddAsync(entity);
        await _repository.DeleteAsync(entity.Id);

        // Verify it is filtered out
        var beforeRestore = await _repository.GetByIdAsync(entity.Id);
        beforeRestore.IsLeft.ShouldBeTrue();

        // Act
        var restoreResult = await _repository.RestoreAsync(entity.Id);

        // Assert
        restoreResult.IsRight.ShouldBeTrue();
        restoreResult.IfRight(restored =>
        {
            restored.IsDeleted.ShouldBeFalse();
            restored.DeletedAtUtc.ShouldBeNull();
        });

        // Verify it is visible again
        var afterRestore = await _repository.GetByIdAsync(entity.Id);
        afterRestore.IsRight.ShouldBeTrue();
        afterRestore.IfRight(e => e.Name.ShouldBe("Restorable"));
    }

    [Fact]
    public async Task RestoreAsync_NonDeletedEntity_ReturnsError()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateDocument();
        await _repository!.AddAsync(entity);

        // Act
        var result = await _repository.RestoreAsync(entity.Id);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetByIdWithDeletedAsync Tests

    [Fact]
    public async Task GetByIdWithDeletedAsync_SoftDeletedEntity_ReturnsEntity()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateDocument("WithDeleted");
        await _repository!.AddAsync(entity);
        await _repository.DeleteAsync(entity.Id);

        // Act
        var result = await _repository.GetByIdWithDeletedAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e =>
        {
            e.Name.ShouldBe("WithDeleted");
            e.IsDeleted.ShouldBeTrue();
        });
    }

    #endregion

    #region HardDeleteAsync Tests

    [Fact]
    public async Task HardDeleteAsync_RemovesDocumentPermanently()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateDocument();
        await _repository!.AddAsync(entity);

        // Act
        var result = await _repository.HardDeleteAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();

        // Verify completely gone from MongoDB
        var stored = await _collection!.Find(d => d.Id == entity.Id).FirstOrDefaultAsync();
        stored.ShouldBeNull();
    }

    [Fact]
    public async Task HardDeleteAsync_NonExistingEntity_ReturnsError()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Act
        var result = await _repository!.HardDeleteAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ActiveEntity_Succeeds()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = CreateDocument("Original");
        await _repository!.AddAsync(entity);

        entity.Name = "Updated";

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _collection!.Find(d => d.Id == entity.Id).FirstOrDefaultAsync();
        stored.ShouldNotBeNull();
        stored.Name.ShouldBe("Updated");
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_MultipleEntities_AllHaveIsDeletedFalse()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entities = new[]
        {
            CreateDocument("Range1"),
            CreateDocument("Range2"),
            CreateDocument("Range3")
        };

        // Act
        var result = await _repository!.AddRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(added =>
        {
            added.Count.ShouldBe(3);
            added.ShouldAllBe(e => !e.IsDeleted);
        });
    }

    #endregion

    #region Test Helpers

    private static SoftDeletableDocument CreateDocument(string name = "TestDoc")
    {
        return new SoftDeletableDocument
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedBy = null
        };
    }

    #endregion
}

#region Test Entity

/// <summary>
/// Test entity with soft delete properties for MongoDB integration tests.
/// </summary>
public class SoftDeletableDocument
{
    [global::MongoDB.Bson.Serialization.Attributes.BsonGuidRepresentation(global::MongoDB.Bson.GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public string? DeletedBy { get; set; }
}

#endregion
