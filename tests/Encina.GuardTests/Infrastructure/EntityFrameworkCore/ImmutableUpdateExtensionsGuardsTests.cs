using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Encina.GuardTests.Infrastructure.EntityFrameworkCore;

/// <summary>
/// Guard tests for <see cref="ImmutableUpdateExtensions"/> to verify null parameter handling.
/// </summary>
public class ImmutableUpdateExtensionsGuardsTests
{
    #region Test Types

    private sealed class TestAggregateRoot : AggregateRoot<Guid>
    {
        public string Name { get; init; } = string.Empty;
        public TestAggregateRoot() : base(Guid.NewGuid()) { }
        public TestAggregateRoot(Guid id) : base(id) { }
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestAggregateRoot> Entities => Set<TestAggregateRoot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregateRoot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Ignore(e => e.DomainEvents);
                entity.Ignore(e => e.RowVersion);
            });
        }
    }

    #endregion

    #region UpdateImmutable Guards

    /// <summary>
    /// Verifies that UpdateImmutable throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public void UpdateImmutable_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext context = null!;
        var entity = new TestAggregateRoot(Guid.NewGuid());

        // Act & Assert
        // Use Action wrapper since UpdateImmutable returns Either<EncinaError, Unit>
        Action act = () => _ = context.UpdateImmutable(entity);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that UpdateImmutable throws ArgumentNullException when modified entity is null.
    /// </summary>
    [Fact]
    public void UpdateImmutable_NullModified_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new TestDbContext(options);
        TestAggregateRoot modified = null!;

        // Act & Assert
        // Use Action wrapper since UpdateImmutable returns Either<EncinaError, Unit>
        Action act = () => _ = context.UpdateImmutable(modified);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("modified");
    }

    #endregion

    #region UpdateImmutableAsync Guards

    /// <summary>
    /// Verifies that UpdateImmutableAsync throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task UpdateImmutableAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext context = null!;
        var entity = new TestAggregateRoot(Guid.NewGuid());

        // Act & Assert
        // Wrap in async lambda that returns Task (not Task<T>) for Should.ThrowAsync
        Func<Task> act = async () => _ = await context.UpdateImmutableAsync(entity);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that UpdateImmutableAsync throws ArgumentNullException when modified entity is null.
    /// </summary>
    [Fact]
    public async Task UpdateImmutableAsync_NullModified_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new TestDbContext(options);
        TestAggregateRoot modified = null!;

        // Act & Assert
        // Wrap in async lambda that returns Task (not Task<T>) for Should.ThrowAsync
        Func<Task> act = async () => _ = await context.UpdateImmutableAsync(modified);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("modified");
    }

    #endregion

    #region WithPreservedEvents Guards

    /// <summary>
    /// Verifies that WithPreservedEvents throws ArgumentNullException when newInstance is null.
    /// </summary>
    [Fact]
    public void WithPreservedEvents_NullNewInstance_ThrowsArgumentNullException()
    {
        // Arrange
        TestAggregateRoot newInstance = null!;
        var originalInstance = new TestAggregateRoot(Guid.NewGuid());

        // Act & Assert
        var act = () => newInstance.WithPreservedEvents(originalInstance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("newInstance");
    }

    /// <summary>
    /// Verifies that WithPreservedEvents throws ArgumentNullException when originalInstance is null.
    /// </summary>
    [Fact]
    public void WithPreservedEvents_NullOriginalInstance_ThrowsArgumentNullException()
    {
        // Arrange
        var newInstance = new TestAggregateRoot(Guid.NewGuid());
        TestAggregateRoot originalInstance = null!;

        // Act & Assert
        var act = () => newInstance.WithPreservedEvents(originalInstance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("originalInstance");
    }

    #endregion
}
