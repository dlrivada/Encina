using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Stores;

/// <summary>
/// Guard clause tests for <see cref="OutboxStoreEF"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class OutboxStoreEFGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new OutboxStoreEF(dbContext));
        ex.ParamName.ShouldBe("dbContext");
    }

    [Fact]
    public void Constructor_NullTimeProvider_DoesNotThrow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestOutboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestOutboxDbContext(options);

        // Act & Assert - timeProvider is optional (defaults to TimeProvider.System)
        Should.NotThrow(() =>
            new OutboxStoreEF(dbContext, timeProvider: null));
    }

    #endregion

    #region AddAsync Guards

    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        IOutboxMessage message = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.AddAsync(message));
        ex.ParamName.ShouldBe("message");
    }

    #endregion

    #region MarkAsFailedAsync Guards

    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        string errorMessage = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.MarkAsFailedAsync(Guid.NewGuid(), errorMessage, null));
        ex.ParamName.ShouldBe("errorMessage");
    }

    #endregion

    #region Test Infrastructure

    private static OutboxStoreEF CreateStore()
    {
        var options = new DbContextOptionsBuilder<TestOutboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestOutboxDbContext(options);
        return new OutboxStoreEF(dbContext);
    }

    private sealed class TestOutboxDbContext : DbContext
    {
        public TestOutboxDbContext(DbContextOptions<TestOutboxDbContext> options) : base(options)
        {
        }

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }

    #endregion
}
