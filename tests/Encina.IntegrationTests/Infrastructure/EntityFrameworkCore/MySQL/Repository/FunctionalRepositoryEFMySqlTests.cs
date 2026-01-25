using Encina.EntityFrameworkCore.Repository;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.Repository;

/// <summary>
/// MySQL-specific integration tests for <see cref="FunctionalRepositoryEF{TEntity, TId}"/>.
/// Uses real MySQL database via Testcontainers.
/// Tests are skipped until Pomelo.EntityFrameworkCore.MySql v10 is released.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class FunctionalRepositoryEFMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;

    public FunctionalRepositoryEFMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [SkippableFact]
    public async Task AddAsync_ValidEntity_PersistsToDatabase()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var repository = new FunctionalRepositoryEF<TestRepositoryEntity, Guid>(context);

        var entity = new TestRepositoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Entity",
            Amount = 100m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        var result = await repository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var stored = await verifyContext.Set<TestRepositoryEntity>().FindAsync(entity.Id);
        stored.ShouldNotBeNull();
        stored!.Name.ShouldBe("Test Entity");
    }

    [SkippableFact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();

        var entity = new TestRepositoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Existing Entity",
            Amount = 50m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Set<TestRepositoryEntity>().Add(entity);
        await context.SaveChangesAsync();

        var repository = new FunctionalRepositoryEF<TestRepositoryEntity, Guid>(context);

        // Act
        var result = await repository.GetByIdAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Name.ShouldBe("Existing Entity"));
    }

    [SkippableFact]
    public async Task ListAsync_WithEntities_ReturnsAll()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();

        context.Set<TestRepositoryEntity>().AddRange(
            new TestRepositoryEntity { Id = Guid.NewGuid(), Name = "Entity 1", Amount = 10m, IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new TestRepositoryEntity { Id = Guid.NewGuid(), Name = "Entity 2", Amount = 20m, IsActive = false, CreatedAtUtc = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new FunctionalRepositoryEF<TestRepositoryEntity, Guid>(context);

        // Act
        var result = await repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(2));
    }

    [SkippableFact]
    public async Task DeleteAsync_ExistingEntity_RemovesFromDatabase()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();

        var entity = new TestRepositoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            Amount = 30m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Set<TestRepositoryEntity>().Add(entity);
        await context.SaveChangesAsync();

        var repository = new FunctionalRepositoryEF<TestRepositoryEntity, Guid>(context);

        // Act
        var result = await repository.DeleteAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var stored = await verifyContext.Set<TestRepositoryEntity>().FindAsync(entity.Id);
        stored.ShouldBeNull();
    }
}
