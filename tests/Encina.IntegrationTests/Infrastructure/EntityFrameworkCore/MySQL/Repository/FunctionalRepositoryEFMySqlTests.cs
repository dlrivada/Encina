using Encina.DomainModeling.Pagination;
using Encina.EntityFrameworkCore.Extensions;
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

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [Fact]
    public async Task AddAsync_ValidEntity_PersistsToDatabase()
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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

    [Fact]
    public async Task ListAsync_WithEntities_ReturnsAll()
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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

    [Fact]
    public async Task DeleteAsync_ExistingEntity_RemovesFromDatabase()
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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

    #region String and Guid keysets (#3201)

    private static readonly string[] KeysetNames = ["a", "b", "c", "d"];

    [Theory]
    [InlineData(false, "b", new[] { "c", "d" })]
    [InlineData(true, "c", new[] { "b", "a" })]
    public async Task ListAsync_StringKeyset_ReturnsTheNamesAfterTheLastOne(bool descending, string lastName, string[] expected)
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange - string has no comparison operator, so the keyset filter goes through string.Compare
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        await SeedKeysetEntitiesAsync(context);
        var repository = new FunctionalRepositoryEF<TestRepositoryEntity, Guid>(context);

        // Act
        var result = await repository.ListAsync(RepositoryEntityKeysetSpec.ByName(descending, lastName));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Select(e => e.Name).ShouldBe(expected));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ListAsync_GuidKeyset_ReturnsTheIdsAfterTheLastOneInTheDatabaseOrder(bool descending)
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange - Guid has no comparison operator, so the keyset filter goes through Guid.CompareTo. Each database
        // orders Guids its own way, so the expected page is read from the database's own ORDER BY.
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        await SeedKeysetEntitiesAsync(context);
        var entities = context.Set<TestRepositoryEntity>().AsNoTracking();
        var databaseOrder = descending
            ? await entities.OrderByDescending(e => e.Id).Select(e => e.Id).ToListAsync()
            : await entities.OrderBy(e => e.Id).Select(e => e.Id).ToListAsync();
        var repository = new FunctionalRepositoryEF<TestRepositoryEntity, Guid>(context);

        // Act
        var result = await repository.ListAsync(RepositoryEntityKeysetSpec.ById(descending, databaseOrder[1]));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Select(e => e.Id).ShouldBe(databaseOrder.Skip(2)));
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_GuidKey_SecondPageFollowsTheDatabaseOrder()
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange - the cursor pagination keyset filter has the same Guid comparison
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        await SeedKeysetEntitiesAsync(context);
        var query = context.Set<TestRepositoryEntity>().AsNoTracking().OrderBy(e => e.Id);
        var databaseOrder = await query.Select(e => e.Id).ToListAsync();
        var encoder = new Base64JsonCursorEncoder();
        var firstPage = await query.ToCursorPaginatedAsync(cursor: null, pageSize: 2, keySelector: e => e.Id, cursorEncoder: encoder);

        // Act
        var secondPage = await query.ToCursorPaginatedAsync(cursor: firstPage.NextCursor, pageSize: 2, keySelector: e => e.Id, cursorEncoder: encoder);

        // Assert
        secondPage.Items.Select(e => e.Id).ShouldBe(databaseOrder.Skip(2));
    }

    private static async Task SeedKeysetEntitiesAsync(DbContext context)
    {
        // Only these rows: the expected pages assume no rows left by other tests in the shared database
        await context.Set<TestRepositoryEntity>().ExecuteDeleteAsync();
        context.Set<TestRepositoryEntity>().AddRange(KeysetNames.Select(name => new TestRepositoryEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Amount = 10m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        }));
        await context.SaveChangesAsync();
    }

    #endregion
}
