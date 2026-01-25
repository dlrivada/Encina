using Encina.EntityFrameworkCore.UnitOfWork;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.UnitOfWork;

/// <summary>
/// SQL Server-specific integration tests for <see cref="UnitOfWorkEF"/>.
/// Uses real SQL Server database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class UnitOfWorkEFSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public UnitOfWorkEFSqlServerTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [Fact]
    public async Task Transaction_CommitMultipleEntities_AllPersisted()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        await using var unitOfWork = new UnitOfWorkEF(context, serviceProvider);

        var entity1 = CreateTestEntity("Entity 1");
        var entity2 = CreateTestEntity("Entity 2");

        // Act
        var beginResult = await unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        context.Set<TestRepositoryEntity>().Add(entity1);
        context.Set<TestRepositoryEntity>().Add(entity2);

        var saveResult = await unitOfWork.SaveChangesAsync();
        saveResult.IsRight.ShouldBeTrue();

        var commitResult = await unitOfWork.CommitAsync();
        commitResult.IsRight.ShouldBeTrue();

        // Assert - Verify with fresh context
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var count = verifyContext.Set<TestRepositoryEntity>().Count();
        count.ShouldBe(2);
    }

    [Fact]
    public async Task Transaction_Rollback_NoChangesPersisted()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        await using var unitOfWork = new UnitOfWorkEF(context, serviceProvider);

        var entity = CreateTestEntity("Should Not Persist");

        // Act
        var beginResult = await unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        context.Set<TestRepositoryEntity>().Add(entity);
        var saveResult = await unitOfWork.SaveChangesAsync();
        saveResult.IsRight.ShouldBeTrue();

        await unitOfWork.RollbackAsync();

        // Assert - Verify with fresh context
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var count = verifyContext.Set<TestRepositoryEntity>().Count();
        count.ShouldBe(0);
    }

    [Fact]
    public async Task SaveChangesAsync_ReturnsAffectedRowCount()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        await using var unitOfWork = new UnitOfWorkEF(context, serviceProvider);

        context.Set<TestRepositoryEntity>().Add(CreateTestEntity("Entity 1"));
        context.Set<TestRepositoryEntity>().Add(CreateTestEntity("Entity 2"));

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));
    }

    [Fact]
    public async Task BeginTransaction_SetsHasActiveTransactionTrue()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        await using var unitOfWork = new UnitOfWorkEF(context, serviceProvider);

        unitOfWork.HasActiveTransaction.ShouldBeFalse();

        // Act
        var result = await unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        unitOfWork.HasActiveTransaction.ShouldBeTrue();
    }

    [Fact]
    public async Task Commit_ClearsHasActiveTransaction()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        await using var unitOfWork = new UnitOfWorkEF(context, serviceProvider);

        await unitOfWork.BeginTransactionAsync();
        unitOfWork.HasActiveTransaction.ShouldBeTrue();

        // Act
        await unitOfWork.CommitAsync();

        // Assert
        unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    private static TestRepositoryEntity CreateTestEntity(string name = "Test Entity")
    {
        return new TestRepositoryEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Amount = 100m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
