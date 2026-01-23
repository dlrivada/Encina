using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.MongoDB.Repository;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.MongoDB.Repository;

/// <summary>
/// Guard clause tests for <see cref="FunctionalRepositoryMongoDB{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "MongoDB")]
public sealed class FunctionalRepositoryMongoDBGuardTests
{
    private static readonly Expression<Func<MongoRepositoryTestEntity, Guid>> IdSelector = e => e.Id;

    [Fact]
    public void Constructor_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IMongoCollection<MongoRepositoryTestEntity> collection = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryMongoDB<MongoRepositoryTestEntity, Guid>(collection, IdSelector));
        ex.ParamName.ShouldBe("collection");
    }

    [Fact]
    public void Constructor_NullIdSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<MongoRepositoryTestEntity>>();
        Expression<Func<MongoRepositoryTestEntity, Guid>> idSelector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryMongoDB<MongoRepositoryTestEntity, Guid>(collection, idSelector));
        ex.ParamName.ShouldBe("idSelector");
    }

    [Fact]
    public async Task ListAsync_WithSpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = CreateMockCollection();
        var repository = new FunctionalRepositoryMongoDB<MongoRepositoryTestEntity, Guid>(collection, IdSelector);
        Specification<MongoRepositoryTestEntity> specification = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.ListAsync(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = CreateMockCollection();
        var repository = new FunctionalRepositoryMongoDB<MongoRepositoryTestEntity, Guid>(collection, IdSelector);
        Specification<MongoRepositoryTestEntity> specification = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.FirstOrDefaultAsync(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task CountAsync_WithSpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = CreateMockCollection();
        var repository = new FunctionalRepositoryMongoDB<MongoRepositoryTestEntity, Guid>(collection, IdSelector);
        Specification<MongoRepositoryTestEntity> specification = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.CountAsync(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = CreateMockCollection();
        var repository = new FunctionalRepositoryMongoDB<MongoRepositoryTestEntity, Guid>(collection, IdSelector);
        Specification<MongoRepositoryTestEntity> specification = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.AnyAsync(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task AddAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = CreateMockCollection();
        var repository = new FunctionalRepositoryMongoDB<MongoRepositoryTestEntity, Guid>(collection, IdSelector);
        MongoRepositoryTestEntity entity = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.AddAsync(entity));
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = CreateMockCollection();
        var repository = new FunctionalRepositoryMongoDB<MongoRepositoryTestEntity, Guid>(collection, IdSelector);
        MongoRepositoryTestEntity entity = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.UpdateAsync(entity));
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public async Task DeleteAsync_WithEntity_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = CreateMockCollection();
        var repository = new FunctionalRepositoryMongoDB<MongoRepositoryTestEntity, Guid>(collection, IdSelector);
        MongoRepositoryTestEntity entity = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.DeleteAsync(entity));
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public async Task AddRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = CreateMockCollection();
        var repository = new FunctionalRepositoryMongoDB<MongoRepositoryTestEntity, Guid>(collection, IdSelector);
        IEnumerable<MongoRepositoryTestEntity> entities = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.AddRangeAsync(entities));
        ex.ParamName.ShouldBe("entities");
    }

    [Fact]
    public async Task UpdateRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = CreateMockCollection();
        var repository = new FunctionalRepositoryMongoDB<MongoRepositoryTestEntity, Guid>(collection, IdSelector);
        IEnumerable<MongoRepositoryTestEntity> entities = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.UpdateRangeAsync(entities));
        ex.ParamName.ShouldBe("entities");
    }

    [Fact]
    public async Task DeleteRangeAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = CreateMockCollection();
        var repository = new FunctionalRepositoryMongoDB<MongoRepositoryTestEntity, Guid>(collection, IdSelector);
        Specification<MongoRepositoryTestEntity> specification = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.DeleteRangeAsync(specification));
        ex.ParamName.ShouldBe("specification");
    }

    private static IMongoCollection<MongoRepositoryTestEntity> CreateMockCollection()
    {
        return Substitute.For<IMongoCollection<MongoRepositoryTestEntity>>();
    }
}

/// <summary>
/// Test entity for MongoDB repository guard tests.
/// </summary>
public sealed class MongoRepositoryTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
}
