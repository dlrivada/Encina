using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Extensions;
using Encina.TestInfrastructure.Entities;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.ImmutableUpdates;

/// <summary>
/// SQL Server-specific integration tests for <see cref="ImmutableUpdateExtensions"/>.
/// Requires running SQL Server instance via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class ImmutableUpdatesSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public ImmutableUpdatesSqlServerTests(EFCoreSqlServerFixture fixture)
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

        // Create modified version
        var modifiedOrder = new TestImmutableOrder(id) { CustomerName = "John Doe", Status = "Shipped" };

        // Act
        var result = context.UpdateImmutable(modifiedOrder);

        // Assert
        result.IsRight.ShouldBeTrue();
        await context.SaveChangesAsync();

        // Verify changes persisted
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

        // Assert
        result.IsRight.ShouldBeTrue();
        modifiedOrder.DomainEvents.Count.ShouldBe(2);
    }

    [Fact]
    public async Task UpdateImmutable_UntrackedEntity_ReturnsError()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();

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
}
