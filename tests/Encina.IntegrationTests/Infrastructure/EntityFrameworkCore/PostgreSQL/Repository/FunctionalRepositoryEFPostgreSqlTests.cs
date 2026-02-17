using Encina.EntityFrameworkCore.Repository;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.Repository;

/// <summary>
/// PostgreSQL-specific integration tests for <see cref="FunctionalRepositoryEF{TEntity, TId}"/>.
/// Uses real PostgreSQL database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class FunctionalRepositoryEFPostgreSqlTests : IAsyncLifetime
{
    private readonly EFCorePostgreSqlFixture _fixture;

    public FunctionalRepositoryEFPostgreSqlTests(EFCorePostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [Fact]
    public async Task AddAsync_ValidEntity_PersistsToDatabase()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestPostgreSqlDbContext>();
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
        await using var verifyContext = _fixture.CreateDbContext<TestPostgreSqlDbContext>();
        var stored = await verifyContext.Set<TestRepositoryEntity>().FindAsync(entity.Id);
        stored.ShouldNotBeNull();
        stored!.Name.ShouldBe("Test Entity");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestPostgreSqlDbContext>();

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

    [Fact]
    public async Task ListAsync_WithEntities_ReturnsAll()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestPostgreSqlDbContext>();

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

    [Fact]
    public async Task DeleteAsync_ExistingEntity_RemovesFromDatabase()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestPostgreSqlDbContext>();

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
        await using var verifyContext = _fixture.CreateDbContext<TestPostgreSqlDbContext>();
        var stored = await verifyContext.Set<TestRepositoryEntity>().FindAsync(entity.Id);
        stored.ShouldBeNull();
    }
}
