using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Extensions;
using Encina.TestInfrastructure.Entities;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Sqlite.ImmutableUpdates;

/// <summary>
/// SQLite-specific integration tests for <see cref="ImmutableUpdateExtensions"/>.
/// Uses in-memory SQLite database via the fixture.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("EFCore-Sqlite")]
public sealed class ImmutableUpdatesSqliteTests : IAsyncLifetime
{
    private readonly EFCoreSqliteFixture _fixture;

    public ImmutableUpdatesSqliteTests(EFCoreSqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [Fact]
    public async Task UpdateImmutable_ModifiedEntity_PersistsChangesToDatabase()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();

        var id = Guid.NewGuid();
        var originalOrder = new TestImmutableOrder(id) { CustomerName = "John Doe", Status = "Pending" };
        context.Orders.Add(originalOrder);
        await context.SaveChangesAsync();

        // Create modified version (simulating immutable update via with-expression)
        var modifiedOrder = new TestImmutableOrder(id) { CustomerName = "John Doe", Status = "Shipped" };

        // Act
        var result = context.UpdateImmutable(modifiedOrder);

        // Assert
        result.IsRight.ShouldBeTrue();
        await context.SaveChangesAsync();

        // Verify changes persisted (use new context to avoid cache)
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var stored = await verifyContext.Orders.FindAsync(id);
        stored.ShouldNotBeNull();
        stored!.Status.ShouldBe("Shipped");
    }

    [Fact]
    public async Task UpdateImmutable_PreservesDomainEvents()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();

        var id = Guid.NewGuid();
        var originalOrder = new TestImmutableOrder(id) { CustomerName = "Jane Doe", Status = "Pending" };
        originalOrder.RaiseTestEvent(new TestOrderEvent(id, "Created"));
        originalOrder.RaiseTestEvent(new TestOrderEvent(id, "Updated"));

        context.Orders.Add(originalOrder);
        await context.SaveChangesAsync();

        // Create modified version (without events)
        var modifiedOrder = new TestImmutableOrder(id) { CustomerName = "Jane Doe", Status = "Processing" };

        // Act
        var result = context.UpdateImmutable(modifiedOrder);

        // Assert - Events should be copied from original
        result.IsRight.ShouldBeTrue();
        modifiedOrder.DomainEvents.Count.ShouldBe(2);
    }

    [Fact]
    public async Task UpdateImmutable_UntrackedEntity_ReturnsError()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();

        // Entity is not tracked
        var entity = new TestImmutableOrder(Guid.NewGuid()) { CustomerName = "Test", Status = "New" };

        // Act
        var result = context.UpdateImmutable(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(RepositoryErrors.EntityNotTrackedErrorCode));
        });
    }

    [Fact]
    public async Task UpdateImmutable_DetachesOriginalAndAttachesModified()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();

        var id = Guid.NewGuid();
        var originalOrder = new TestImmutableOrder(id) { CustomerName = "Original", Status = "Pending" };
        context.Orders.Add(originalOrder);
        await context.SaveChangesAsync();

        var modifiedOrder = new TestImmutableOrder(id) { CustomerName = "Original", Status = "Completed" };

        // Act
        var result = context.UpdateImmutable(modifiedOrder);

        // Assert
        result.IsRight.ShouldBeTrue();
        context.Entry(originalOrder).State.ShouldBe(EntityState.Detached);
        context.Entry(modifiedOrder).State.ShouldBe(EntityState.Modified);
    }

    [Fact]
    public async Task UpdateImmutable_MultipleUpdates_AllPersistCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var order1 = new TestImmutableOrder(id1) { CustomerName = "Customer 1", Status = "Pending" };
        var order2 = new TestImmutableOrder(id2) { CustomerName = "Customer 2", Status = "Pending" };

        context.Orders.AddRange(order1, order2);
        await context.SaveChangesAsync();

        // Act - Update both orders
        var modified1 = new TestImmutableOrder(id1) { CustomerName = "Customer 1", Status = "Shipped" };
        var modified2 = new TestImmutableOrder(id2) { CustomerName = "Customer 2", Status = "Delivered" };

        var result1 = context.UpdateImmutable(modified1);
        var result2 = context.UpdateImmutable(modified2);

        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();

        await context.SaveChangesAsync();

        // Assert - Verify both updates persisted
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var stored1 = await verifyContext.Orders.FindAsync(id1);
        var stored2 = await verifyContext.Orders.FindAsync(id2);

        stored1.ShouldNotBeNull();
        stored2.ShouldNotBeNull();
        stored1!.Status.ShouldBe("Shipped");
        stored2!.Status.ShouldBe("Delivered");
    }
}
