using Encina.DomainModeling;
using Encina.EntityFrameworkCore.SoftDelete;
using Microsoft.EntityFrameworkCore;

namespace Encina.GuardTests.Infrastructure.EntityFrameworkCore.SoftDelete;

/// <summary>
/// Guard tests for <see cref="SoftDeleteRepositoryEF{TEntity, TId}"/> to verify null parameter handling.
/// </summary>
public sealed class SoftDeleteRepositoryEFGuardTests
{
    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var act = () => new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("dbContext");
    }

    [Fact]
    public async Task ListWithDeletedAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        Specification<TestSoftDeletableEntity> specification = null!;

        // Act & Assert
        Func<Task> act = () => repository.ListWithDeletedAsync(specification);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task FindAsync_Specification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        Specification<TestSoftDeletableEntity> specification = null!;

        // Act & Assert
        Func<Task> act = () => repository.FindAsync(specification);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task FindAsync_Expression_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        System.Linq.Expressions.Expression<Func<TestSoftDeletableEntity, bool>> predicate = null!;

        // Act & Assert
        Func<Task> act = () => repository.FindAsync(predicate);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("predicate");
    }

    [Fact]
    public async Task FindOneAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        Specification<TestSoftDeletableEntity> specification = null!;

        // Act & Assert
        Func<Task> act = () => repository.FindOneAsync(specification);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task AnyAsync_Specification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        Specification<TestSoftDeletableEntity> specification = null!;

        // Act & Assert
        Func<Task> act = () => repository.AnyAsync(specification);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task AnyAsync_Expression_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        System.Linq.Expressions.Expression<Func<TestSoftDeletableEntity, bool>> predicate = null!;

        // Act & Assert
        Func<Task> act = () => repository.AnyAsync(predicate);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("predicate");
    }

    [Fact]
    public async Task CountAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        Specification<TestSoftDeletableEntity> specification = null!;

        // Act & Assert
        Func<Task> act = () => repository.CountAsync(specification);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task GetPagedAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        Specification<TestSoftDeletableEntity> specification = null!;

        // Act & Assert
        Func<Task> act = () => repository.GetPagedAsync(specification, 1, 10);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public async Task AddAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        TestSoftDeletableEntity entity = null!;

        // Act & Assert
        Func<Task> act = () => repository.AddAsync(entity);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public async Task AddRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        IEnumerable<TestSoftDeletableEntity> entities = null!;

        // Act & Assert
        Func<Task> act = () => repository.AddRangeAsync(entities);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entities");
    }

    [Fact]
    public void Update_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        TestSoftDeletableEntity entity = null!;

        // Act & Assert
        var act = () => repository.Update(entity);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public void UpdateRange_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        IEnumerable<TestSoftDeletableEntity> entities = null!;

        // Act & Assert
        var act = () => repository.UpdateRange(entities);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entities");
    }

    [Fact]
    public void Remove_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        TestSoftDeletableEntity entity = null!;

        // Act & Assert
        var act = () => repository.Remove(entity);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public void RemoveRange_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSoftDeleteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSoftDeleteDbContext(options);
        var repository = new SoftDeleteRepositoryEF<TestSoftDeletableEntity, Guid>(dbContext);
        IEnumerable<TestSoftDeletableEntity> entities = null!;

        // Act & Assert
        var act = () => repository.RemoveRange(entities);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entities");
    }

    /// <summary>
    /// Test entity for soft delete guard tests.
    /// </summary>
    private sealed class TestSoftDeletableEntity : IEntity<Guid>, ISoftDeletable, ISoftDeletableEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public string? ModifiedBy { get; set; }
    }

    /// <summary>
    /// Test DbContext for soft delete guard tests.
    /// </summary>
    private sealed class TestSoftDeleteDbContext : DbContext
    {
        public TestSoftDeleteDbContext(DbContextOptions<TestSoftDeleteDbContext> options) : base(options)
        {
        }

        public DbSet<TestSoftDeletableEntity> TestEntities => Set<TestSoftDeletableEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestSoftDeletableEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.HasQueryFilter(e => !e.IsDeleted);
            });
        }
    }
}
