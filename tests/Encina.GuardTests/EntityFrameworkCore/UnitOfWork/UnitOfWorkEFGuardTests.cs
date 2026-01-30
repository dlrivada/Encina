using Encina.DomainModeling;
using Encina.EntityFrameworkCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.UnitOfWork;

/// <summary>
/// Guard clause tests for <see cref="UnitOfWorkEF"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class UnitOfWorkEFGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkEF(dbContext, serviceProvider));
        ex.ParamName.ShouldBe("dbContext");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestUoWDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestUoWDbContext(options);
        IServiceProvider serviceProvider = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkEF(dbContext, serviceProvider));
        ex.ParamName.ShouldBe("serviceProvider");
    }

    #endregion

    #region UpdateImmutable Guards

    [Fact]
    public void UpdateImmutable_NullModified_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestUoWDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestUoWDbContext(options);
        var serviceProvider = Substitute.For<IServiceProvider>();
        var unitOfWork = new UnitOfWorkEF(dbContext, serviceProvider);
        TestUoWAggregateRoot modified = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            unitOfWork.UpdateImmutable(modified));
        ex.ParamName.ShouldBe("modified");
    }

    #endregion

    #region UpdateImmutableAsync Guards

    [Fact]
    public async Task UpdateImmutableAsync_NullModified_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestUoWDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestUoWDbContext(options);
        var serviceProvider = Substitute.For<IServiceProvider>();
        var unitOfWork = new UnitOfWorkEF(dbContext, serviceProvider);
        TestUoWAggregateRoot modified = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await unitOfWork.UpdateImmutableAsync(modified));
        ex.ParamName.ShouldBe("modified");
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestUoWDbContext : DbContext
    {
        public TestUoWDbContext(DbContextOptions<TestUoWDbContext> options) : base(options)
        {
        }

        public DbSet<TestUoWAggregateRoot> Aggregates => Set<TestUoWAggregateRoot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestUoWAggregateRoot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Ignore(e => e.DomainEvents);
                entity.Ignore(e => e.RowVersion);
            });
        }
    }

    private sealed class TestUoWAggregateRoot : AggregateRoot<Guid>
    {
        public string Name { get; init; } = string.Empty;

        public TestUoWAggregateRoot(Guid id) : base(id) { }
    }

    #endregion
}
