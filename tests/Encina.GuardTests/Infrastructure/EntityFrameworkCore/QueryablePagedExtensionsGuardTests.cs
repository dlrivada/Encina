using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Encina.GuardTests.Infrastructure.EntityFrameworkCore;

/// <summary>
/// Guard tests for <see cref="QueryablePagedExtensions"/>.
/// </summary>
public class QueryablePagedExtensionsGuardTests : IDisposable
{
    private readonly TestDbContext _dbContext;

    public QueryablePagedExtensionsGuardTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Test Infrastructure

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    }

    private sealed class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed record TestEntityDto(Guid Id, string Name);

    #endregion

    #region ToPagedResultAsync Guards

    [Fact]
    public async Task ToPagedResultAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        IQueryable<TestEntity>? query = null;
        var pagination = new PaginationOptions(1, 10);

        // Act
        var act = async () => await query!.ToPagedResultAsync(pagination);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("query");
    }

    [Fact]
    public async Task ToPagedResultAsync_NullPagination_ThrowsArgumentNullException()
    {
        // Arrange
        var query = _dbContext.TestEntities.AsQueryable();

        // Act
        var act = async () => await query.ToPagedResultAsync(null!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("pagination");
    }

    #endregion

    #region ToPagedResultAsync with Projection Guards

    [Fact]
    public async Task ToPagedResultAsync_WithProjection_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        IQueryable<TestEntity>? query = null;
        var pagination = new PaginationOptions(1, 10);

        // Act
        var act = async () => await query!.ToPagedResultAsync(
            e => new TestEntityDto(e.Id, e.Name),
            pagination);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("query");
    }

    [Fact]
    public async Task ToPagedResultAsync_WithProjection_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var query = _dbContext.TestEntities.AsQueryable();
        var pagination = new PaginationOptions(1, 10);

        // Act
        var act = async () => await query.ToPagedResultAsync(
            (System.Linq.Expressions.Expression<Func<TestEntity, TestEntityDto>>)null!,
            pagination);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("selector");
    }

    [Fact]
    public async Task ToPagedResultAsync_WithProjection_NullPagination_ThrowsArgumentNullException()
    {
        // Arrange
        var query = _dbContext.TestEntities.AsQueryable();

        // Act
        var act = async () => await query.ToPagedResultAsync(
            e => new TestEntityDto(e.Id, e.Name),
            null!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("pagination");
    }

    #endregion
}
