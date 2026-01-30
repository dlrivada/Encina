using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Extensions;
using Encina.TestInfrastructure.Entities;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.ImmutableUpdates;

/// <summary>
/// MySQL-specific integration tests for <see cref="ImmutableUpdateExtensions"/>.
/// </summary>
/// <remarks>
/// MySQL/MariaDB support via Pomelo.EntityFrameworkCore.MySql is pending v10.0.0 release
/// which adds EF Core 10 compatibility. These tests will be skipped until then.
/// See: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class ImmutableUpdatesMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;
    private const string SkipReason = "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 (EF Core 10 compatible). See: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019";

    public ImmutableUpdatesMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task UpdateImmutable_ModifiedEntity_PersistsChangesToDatabase()
    {
        Skip.If(true, SkipReason);

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

    [SkippableFact]
    public async Task UpdateImmutable_PreservesDomainEvents()
    {
        Skip.If(true, SkipReason);

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

    [SkippableFact]
    public async Task UpdateImmutable_UntrackedEntity_ReturnsError()
    {
        Skip.If(true, SkipReason);

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

    [SkippableFact]
    public async Task UpdateImmutable_DetachesOriginalAndAttachesModified()
    {
        Skip.If(true, SkipReason);

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
