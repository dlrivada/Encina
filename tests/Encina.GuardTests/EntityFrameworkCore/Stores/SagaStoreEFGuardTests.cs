using Encina.EntityFrameworkCore.Sagas;
using Encina.Messaging.Sagas;
using Microsoft.EntityFrameworkCore;

namespace Encina.GuardTests.EntityFrameworkCore.Stores;

/// <summary>
/// Guard clause tests for <see cref="SagaStoreEF"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class SagaStoreEFGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new SagaStoreEF(dbContext));
        ex.ParamName.ShouldBe("dbContext");
    }

    #endregion

    #region AddAsync Guards

    [Fact]
    public async Task AddAsync_NullSagaState_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSagaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSagaDbContext(options);
        var store = new SagaStoreEF(dbContext);
        ISagaState sagaState = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.AddAsync(sagaState));
        ex.ParamName.ShouldBe("sagaState");
    }

    #endregion

    #region UpdateAsync Guards

    [Fact]
    public async Task UpdateAsync_NullSagaState_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestSagaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestSagaDbContext(options);
        var store = new SagaStoreEF(dbContext);
        ISagaState sagaState = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.UpdateAsync(sagaState));
        ex.ParamName.ShouldBe("sagaState");
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestSagaDbContext : DbContext
    {
        public TestSagaDbContext(DbContextOptions<TestSagaDbContext> options) : base(options)
        {
        }
    }

    #endregion
}
