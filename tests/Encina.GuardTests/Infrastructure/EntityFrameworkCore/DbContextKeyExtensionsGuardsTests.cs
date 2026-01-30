using Encina.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Encina.GuardTests.Infrastructure.EntityFrameworkCore;

/// <summary>
/// Guard tests for <see cref="DbContextKeyExtensions"/> to verify null parameter handling.
/// </summary>
public class DbContextKeyExtensionsGuardsTests
{
    #region Test Types

    private sealed class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestEntity> Entities => Set<TestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
            });
        }
    }

    #endregion

    #region GetPrimaryKeyValue Guards

    /// <summary>
    /// Verifies that GetPrimaryKeyValue throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public void GetPrimaryKeyValue_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext context = null!;
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act & Assert
        var act = () => context.GetPrimaryKeyValue(entity);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that GetPrimaryKeyValue throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void GetPrimaryKeyValue_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new TestDbContext(options);
        TestEntity entity = null!;

        // Act & Assert
        var act = () => context.GetPrimaryKeyValue(entity);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    #endregion

    #region GetPrimaryKeyValues Guards

    /// <summary>
    /// Verifies that GetPrimaryKeyValues throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public void GetPrimaryKeyValues_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext context = null!;
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act & Assert
        var act = () => context.GetPrimaryKeyValues(entity);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that GetPrimaryKeyValues throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void GetPrimaryKeyValues_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new TestDbContext(options);
        TestEntity entity = null!;

        // Act & Assert
        var act = () => context.GetPrimaryKeyValues(entity);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    #endregion

    #region GetPrimaryKeyPropertyName Guards

    /// <summary>
    /// Verifies that GetPrimaryKeyPropertyName throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public void GetPrimaryKeyPropertyName_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext context = null!;

        // Act & Assert
        var act = () => context.GetPrimaryKeyPropertyName<TestEntity>();
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    #endregion
}
