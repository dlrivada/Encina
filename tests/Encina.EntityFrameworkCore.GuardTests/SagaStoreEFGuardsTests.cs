using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Sagas;
using Encina.Messaging.Sagas;

namespace Encina.EntityFrameworkCore.GuardTests;

/// <summary>
/// Guard tests for <see cref="SagaStoreEF"/> to verify null parameter handling.
/// </summary>
public class SagaStoreEFGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when dbContext is null.
    /// </summary>
    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var act = () => new SagaStoreEF(dbContext);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("dbContext");
    }

    /// <summary>
    /// Verifies that AddAsync throws ArgumentNullException when saga is null.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullSaga_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var store = new SagaStoreEF(dbContext);
        ISagaState saga = null!;

        // Act & Assert
        Func<Task> act = () => store.AddAsync(saga);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("saga");
    }

    /// <summary>
    /// Verifies that UpdateAsync throws ArgumentNullException when saga is null.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NullSaga_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var store = new SagaStoreEF(dbContext);
        ISagaState saga = null!;

        // Act & Assert
        Func<Task> act = () => store.UpdateAsync(saga);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("saga");
    }

    /// <summary>
    /// Test DbContext for in-memory database testing.
    /// </summary>
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<SagaState> SagaStates => Set<SagaState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SagaState>(entity =>
            {
                entity.HasKey(e => e.SagaId);
                entity.Property(e => e.SagaType).IsRequired();
                entity.Property(e => e.Status).IsRequired();
            });
        }
    }
}
