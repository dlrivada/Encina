using Encina.DomainModeling.Pagination;
using Encina.EntityFrameworkCore.Extensions;
using Encina.EntityFrameworkCore.Repository;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.Repository;

/// <summary>
/// SQL Server-specific integration tests for <see cref="FunctionalRepositoryEF{TEntity, TId}"/>.
/// Uses real SQL Server database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class FunctionalRepositoryEFSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public FunctionalRepositoryEFSqlServerTests(EFCoreSqlServerFixture fixture)
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

    private static readonly string[] TiedNames = ["a", "b", "b", "b", "c"];

    [Theory]
    [InlineData(false, "b", new[] { "c", "d" })]
    [InlineData(true, "c", new[] { "b", "a" })]
    public async Task ListAsync_StringKeyset_ReturnsTheNamesAfterTheLastOne(bool descending, string lastName, string[] expected)
    {
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

    [Fact]
    public async Task ToCursorPaginatedCompositeAsync_TiedStringKey_SecondPageFollowsTheDatabaseOrder()
    {
        // Arrange - page 1 ends inside a run of equal names, so page 2 continues through the id; the name is
        // matched with == OR string.Compare == 0, and both must translate to the column's collation
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        await context.Set<TestRepositoryEntity>().ExecuteDeleteAsync();
        context.Set<TestRepositoryEntity>().AddRange(TiedNames.Select(name => new TestRepositoryEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Amount = 10m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        }));
        await context.SaveChangesAsync();
        var query = context.Set<TestRepositoryEntity>().AsNoTracking().OrderBy(e => e.Name).ThenBy(e => e.Id);
        var databaseOrder = await query.Select(e => e.Id).ToListAsync();
        var encoder = new Base64JsonCursorEncoder();
        var firstPage = await query.ToCursorPaginatedCompositeAsync(null, 2, e => new { e.Name, e.Id }, encoder, [false, false]);

        // Act
        var secondPage = await query.ToCursorPaginatedCompositeAsync(firstPage.NextCursor, 2, e => new { e.Name, e.Id }, encoder, [false, false]);

        // Assert
        secondPage.Items.Select(e => e.Id).ShouldBe(databaseOrder.Skip(2).Take(2));
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
