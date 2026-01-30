using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Repository;

/// <summary>
/// Guard clause tests for <see cref="FunctionalRepositoryEF{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class FunctionalRepositoryEFGuardTests
{
    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryEF<RepositoryTestEntity, Guid>(dbContext));
        ex.ParamName.ShouldBe("dbContext");
    }

    [Fact]
    public async Task ListAsync_WithSpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var repository = new FunctionalRepositoryEF<RepositoryTestEntity, Guid>(dbContext);
        Specification<RepositoryTestEntity> specification = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.ListAsync(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var repository = new FunctionalRepositoryEF<RepositoryTestEntity, Guid>(dbContext);
        Specification<RepositoryTestEntity> specification = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.FirstOrDefaultAsync(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task CountAsync_WithSpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var repository = new FunctionalRepositoryEF<RepositoryTestEntity, Guid>(dbContext);
        Specification<RepositoryTestEntity> specification = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.CountAsync(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var repository = new FunctionalRepositoryEF<RepositoryTestEntity, Guid>(dbContext);
        Specification<RepositoryTestEntity> specification = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.AnyAsync(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task AddAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var repository = new FunctionalRepositoryEF<RepositoryTestEntity, Guid>(dbContext);
        RepositoryTestEntity entity = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.AddAsync(entity));
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var repository = new FunctionalRepositoryEF<RepositoryTestEntity, Guid>(dbContext);
        RepositoryTestEntity entity = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.UpdateAsync(entity));
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public async Task DeleteAsync_WithEntity_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var repository = new FunctionalRepositoryEF<RepositoryTestEntity, Guid>(dbContext);
        RepositoryTestEntity entity = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.DeleteAsync(entity));
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public async Task AddRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var repository = new FunctionalRepositoryEF<RepositoryTestEntity, Guid>(dbContext);
        IEnumerable<RepositoryTestEntity> entities = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.AddRangeAsync(entities));
        ex.ParamName.ShouldBe("entities");
    }

    [Fact]
    public async Task UpdateRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var repository = new FunctionalRepositoryEF<RepositoryTestEntity, Guid>(dbContext);
        IEnumerable<RepositoryTestEntity> entities = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.UpdateRangeAsync(entities));
        ex.ParamName.ShouldBe("entities");
    }

    [Fact]
    public async Task DeleteRangeAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var repository = new FunctionalRepositoryEF<RepositoryTestEntity, Guid>(dbContext);
        Specification<RepositoryTestEntity> specification = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.DeleteRangeAsync(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task UpdateImmutableAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var repository = new FunctionalRepositoryEF<RepositoryTestEntity, Guid>(dbContext);
        RepositoryTestEntity entity = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            repository.UpdateImmutableAsync(entity));
        ex.ParamName.ShouldBe("modified");
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<RepositoryTestEntity> TestEntities => Set<RepositoryTestEntity>();
    }
}

/// <summary>
/// Test entity for EF Core repository guard tests.
/// </summary>
public sealed class RepositoryTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
}
