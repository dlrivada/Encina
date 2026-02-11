using Encina.EntityFrameworkCore.UnitOfWork;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.UnitOfWork;

/// <summary>
/// MySQL-specific integration tests for <see cref="UnitOfWorkEF"/>.
/// Uses real MySQL database via Testcontainers.
/// Tests are skipped until Pomelo.EntityFrameworkCore.MySql v10 is released.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class UnitOfWorkEFMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;

    public UnitOfWorkEFMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [Fact]
    public async Task Transaction_CommitMultipleEntities_AllPersisted()
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
