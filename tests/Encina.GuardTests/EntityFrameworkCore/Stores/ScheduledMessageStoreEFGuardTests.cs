using Encina.EntityFrameworkCore.Scheduling;
using Encina.Messaging.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace Encina.GuardTests.EntityFrameworkCore.Stores;

/// <summary>
/// Guard clause tests for <see cref="ScheduledMessageStoreEF"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class ScheduledMessageStoreEFGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ScheduledMessageStoreEF(dbContext));
        ex.ParamName.ShouldBe("dbContext");
    }

    #endregion

    #region AddAsync Guards

    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestScheduledDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestScheduledDbContext(options);
        var store = new ScheduledMessageStoreEF(dbContext);
        IScheduledMessage message = null!;

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
        var options = new DbContextOptionsBuilder<TestScheduledDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestScheduledDbContext(options);
        var store = new ScheduledMessageStoreEF(dbContext);
        string errorMessage = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.MarkAsFailedAsync(Guid.NewGuid(), errorMessage, null));
        ex.ParamName.ShouldBe("errorMessage");
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestScheduledDbContext : DbContext
    {
        public TestScheduledDbContext(DbContextOptions<TestScheduledDbContext> options) : base(options)
        {
        }
    }

    #endregion
}
