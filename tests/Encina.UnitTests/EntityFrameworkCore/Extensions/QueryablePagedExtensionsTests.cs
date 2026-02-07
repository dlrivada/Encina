using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Extensions;

/// <summary>
/// Unit tests for <see cref="QueryablePagedExtensions"/>.
/// Uses EF Core InMemory database for isolation.
/// </summary>
[Trait("Category", "Unit")]
public class QueryablePagedExtensionsTests : IDisposable
{
    private readonly TestDbContext _dbContext;

    public QueryablePagedExtensionsTests()
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

    #region Test DbContext and Entity

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    }

    private sealed class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal Amount { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    private sealed record TestEntityDto(Guid Id, string Name);

    #endregion

    #region Test Data Helpers

    private static TestEntity CreateTestEntity(
        string name = "Test Entity",
        bool isActive = true,
        decimal amount = 100m,
        int sortOrder = 0)
    {
        return new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            Amount = amount,
            SortOrder = sortOrder,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-sortOrder)
        };
    }

    private async Task SeedTestData(int count)
    {
        var entities = Enumerable.Range(1, count)
            .Select(i => CreateTestEntity($"Entity-{i:D3}", isActive: i % 2 == 1, sortOrder: i))
            .ToList();

        _dbContext.TestEntities.AddRange(entities);
        await _dbContext.SaveChangesAsync();
    }

    #endregion

    #region ToPagedResultAsync Basic Tests

    [Fact]
    public async Task ToPagedResultAsync_FirstPage_ReturnsCorrectItems()
    {
        // Arrange
        await SeedTestData(25);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities
            .OrderBy(e => e.SortOrder)
            .ToPagedResultAsync(pagination);

        // Assert
        result.Items.Count.ShouldBe(10);
        result.PageNumber.ShouldBe(1);
        result.PageSize.ShouldBe(10);
        result.TotalCount.ShouldBe(25);
        result.TotalPages.ShouldBe(3);
        result.HasPreviousPage.ShouldBeFalse();
        result.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public async Task ToPagedResultAsync_MiddlePage_ReturnsCorrectItems()
    {
        // Arrange
        await SeedTestData(25);
        var pagination = new PaginationOptions(PageNumber: 2, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities
            .OrderBy(e => e.SortOrder)
            .ToPagedResultAsync(pagination);

        // Assert
        result.Items.Count.ShouldBe(10);
        result.PageNumber.ShouldBe(2);
        result.HasPreviousPage.ShouldBeTrue();
        result.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public async Task ToPagedResultAsync_LastPage_ReturnsPartialResults()
    {
        // Arrange
        await SeedTestData(25);
        var pagination = new PaginationOptions(PageNumber: 3, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities
            .OrderBy(e => e.SortOrder)
            .ToPagedResultAsync(pagination);

        // Assert
        result.Items.Count.ShouldBe(5); // 25 total, pages of 10: 10+10+5
        result.PageNumber.ShouldBe(3);
        result.HasPreviousPage.ShouldBeTrue();
        result.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public async Task ToPagedResultAsync_EmptyTable_ReturnsEmptyResult()
    {
        // Arrange - No data seeded
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities
            .ToPagedResultAsync(pagination);

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.TotalPages.ShouldBe(0);
        result.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task ToPagedResultAsync_PageBeyondTotal_ReturnsEmptyItems()
    {
        // Arrange
        await SeedTestData(10);
        var pagination = new PaginationOptions(PageNumber: 5, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities
            .ToPagedResultAsync(pagination);

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(10); // Total count is still accurate
    }

    [Fact]
    public async Task ToPagedResultAsync_SingleItemPerPage_WorksCorrectly()
    {
        // Arrange
        await SeedTestData(5);
        var pagination = new PaginationOptions(PageNumber: 3, PageSize: 1);

        // Act
        var result = await _dbContext.TestEntities
            .OrderBy(e => e.SortOrder)
            .ToPagedResultAsync(pagination);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.TotalPages.ShouldBe(5);
        result.HasPreviousPage.ShouldBeTrue();
        result.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public async Task ToPagedResultAsync_LargePageSize_ReturnsAllItems()
    {
        // Arrange
        await SeedTestData(5);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 100);

        // Act
        var result = await _dbContext.TestEntities
            .ToPagedResultAsync(pagination);

        // Assert
        result.Items.Count.ShouldBe(5);
        result.TotalPages.ShouldBe(1);
        result.HasNextPage.ShouldBeFalse();
    }

    #endregion

    #region ToPagedResultAsync With Filter Tests

    [Fact]
    public async Task ToPagedResultAsync_WithFilter_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestData(25); // 13 active (odd numbers), 12 inactive
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities
            .Where(e => e.IsActive)
            .OrderBy(e => e.SortOrder)
            .ToPagedResultAsync(pagination);

        // Assert
        result.Items.Count.ShouldBe(10);
        result.TotalCount.ShouldBe(13); // Only active entities counted
        result.TotalPages.ShouldBe(2); // 13 / 10 = 2 pages
        result.Items.ShouldAllBe(e => e.IsActive);
    }

    [Fact]
    public async Task ToPagedResultAsync_WithFilterNoMatches_ReturnsEmpty()
    {
        // Arrange
        await SeedTestData(10);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities
            .Where(e => e.Amount > 10000) // No entities have this amount
            .ToPagedResultAsync(pagination);

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.IsEmpty.ShouldBeTrue();
    }

    #endregion

    #region ToPagedResultAsync With Projection Tests

    [Fact]
    public async Task ToPagedResultAsync_WithProjection_ReturnsProjectedResults()
    {
        // Arrange
        await SeedTestData(25);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities
            .OrderBy(e => e.SortOrder)
            .ToPagedResultAsync(
                e => new TestEntityDto(e.Id, e.Name),
                pagination);

        // Assert
        result.Items.Count.ShouldBe(10);
        result.TotalCount.ShouldBe(25);
        result.Items.ShouldAllBe(dto => !string.IsNullOrEmpty(dto.Name));
    }

    [Fact]
    public async Task ToPagedResultAsync_WithProjection_EmptyResult_ReturnsEmptyProjected()
    {
        // Arrange - No data seeded
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities
            .ToPagedResultAsync(
                e => new TestEntityDto(e.Id, e.Name),
                pagination);

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task ToPagedResultAsync_WithProjectionAndFilter_WorksCorrectly()
    {
        // Arrange
        await SeedTestData(25);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 5);

        // Act
        var result = await _dbContext.TestEntities
            .Where(e => e.IsActive)
            .OrderBy(e => e.SortOrder)
            .ToPagedResultAsync(
                e => new TestEntityDto(e.Id, e.Name),
                pagination);

        // Assert
        result.Items.Count.ShouldBe(5);
        result.TotalCount.ShouldBe(13); // Only active entities
        result.TotalPages.ShouldBe(3); // 13 / 5 = 3 pages
    }

    #endregion

    #region Skip/Take Application Tests

    [Fact]
    public async Task ToPagedResultAsync_CorrectlyAppliesSkip()
    {
        // Arrange
        await SeedTestData(30);
        var pagination = new PaginationOptions(PageNumber: 3, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities
            .OrderBy(e => e.SortOrder)
            .ToPagedResultAsync(pagination);

        // Assert
        // Page 3 should skip 20 items (pages 1 and 2)
        result.Items[0].Name.ShouldBe("Entity-021");
        result.Items[^1].Name.ShouldBe("Entity-030");
    }

    [Fact]
    public async Task ToPagedResultAsync_CorrectlyLimitsTake()
    {
        // Arrange
        await SeedTestData(100);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 15);

        // Act
        var result = await _dbContext.TestEntities
            .OrderBy(e => e.SortOrder)
            .ToPagedResultAsync(pagination);

        // Assert
        result.Items.Count.ShouldBe(15);
        result.Items[0].Name.ShouldBe("Entity-001");
        result.Items[^1].Name.ShouldBe("Entity-015");
    }

    #endregion

    #region Count Accuracy Tests

    [Fact]
    public async Task ToPagedResultAsync_CountReflectsFilters()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("Active 1", isActive: true),
            CreateTestEntity("Active 2", isActive: true),
            CreateTestEntity("Active 3", isActive: true),
            CreateTestEntity("Inactive 1", isActive: false),
            CreateTestEntity("Inactive 2", isActive: false)
        );
        await _dbContext.SaveChangesAsync();

        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var activeResult = await _dbContext.TestEntities
            .Where(e => e.IsActive)
            .ToPagedResultAsync(pagination);

        var inactiveResult = await _dbContext.TestEntities
            .Where(e => !e.IsActive)
            .ToPagedResultAsync(pagination);

        // Assert
        activeResult.TotalCount.ShouldBe(3);
        inactiveResult.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task ToPagedResultAsync_CountIsIndependentOfPagination()
    {
        // Arrange
        await SeedTestData(50);

        var page1 = new PaginationOptions(PageNumber: 1, PageSize: 10);
        var page2 = new PaginationOptions(PageNumber: 2, PageSize: 20);
        var page3 = new PaginationOptions(PageNumber: 5, PageSize: 5);

        // Act
        var result1 = await _dbContext.TestEntities.ToPagedResultAsync(page1);
        var result2 = await _dbContext.TestEntities.ToPagedResultAsync(page2);
        var result3 = await _dbContext.TestEntities.ToPagedResultAsync(page3);

        // Assert - All should report the same total count
        result1.TotalCount.ShouldBe(50);
        result2.TotalCount.ShouldBe(50);
        result3.TotalCount.ShouldBe(50);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ToPagedResultAsync_ExactlyOnePageOfData_NoNextPage()
    {
        // Arrange
        await SeedTestData(10);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities.ToPagedResultAsync(pagination);

        // Assert
        result.Items.Count.ShouldBe(10);
        result.TotalPages.ShouldBe(1);
        result.HasNextPage.ShouldBeFalse();
        result.HasPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public async Task ToPagedResultAsync_DefaultPagination_WorksCorrectly()
    {
        // Arrange
        await SeedTestData(50);

        // Act
        var result = await _dbContext.TestEntities.ToPagedResultAsync(PaginationOptions.Default);

        // Assert
        result.Items.Count.ShouldBe(20); // Default page size
        result.PageNumber.ShouldBe(1);
        result.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task ToPagedResultAsync_LargeDataset_CorrectlyPaginates()
    {
        // Arrange
        await SeedTestData(1000);
        var pagination = new PaginationOptions(PageNumber: 50, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities
            .OrderBy(e => e.SortOrder)
            .ToPagedResultAsync(pagination);

        // Assert
        result.Items.Count.ShouldBe(10);
        result.TotalCount.ShouldBe(1000);
        result.TotalPages.ShouldBe(100);
        result.PageNumber.ShouldBe(50);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ToPagedResultAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        await SeedTestData(10);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await _dbContext.TestEntities.ToPagedResultAsync(pagination, cts.Token));
    }

    [Fact]
    public async Task ToPagedResultAsync_WithProjection_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        await SeedTestData(10);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await _dbContext.TestEntities.ToPagedResultAsync(
                e => new TestEntityDto(e.Id, e.Name),
                pagination,
                cts.Token));
    }

    #endregion

    #region Pagination Metadata Tests

    [Fact]
    public async Task ToPagedResultAsync_FirstItemIndex_CalculatesCorrectly()
    {
        // Arrange
        await SeedTestData(50);
        var pagination = new PaginationOptions(PageNumber: 3, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities.ToPagedResultAsync(pagination);

        // Assert
        result.FirstItemIndex.ShouldBe(21); // (3-1) * 10 + 1
    }

    [Fact]
    public async Task ToPagedResultAsync_LastItemIndex_CalculatesCorrectly()
    {
        // Arrange
        await SeedTestData(50);
        var pagination = new PaginationOptions(PageNumber: 3, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities.ToPagedResultAsync(pagination);

        // Assert
        result.LastItemIndex.ShouldBe(30); // Min(3*10, 50) = 30
    }

    [Fact]
    public async Task ToPagedResultAsync_LastPagePartial_LastItemIndex_CorrectlyLimited()
    {
        // Arrange
        await SeedTestData(45);
        var pagination = new PaginationOptions(PageNumber: 5, PageSize: 10);

        // Act
        var result = await _dbContext.TestEntities.ToPagedResultAsync(pagination);

        // Assert
        result.Items.Count.ShouldBe(5);
        result.LastItemIndex.ShouldBe(45); // Total count, not 50
    }

    #endregion

    #region Ordering Preservation Tests

    [Fact]
    public async Task ToPagedResultAsync_PreservesOrdering()
    {
        // Arrange
        await SeedTestData(30);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act - Order descending
        var result = await _dbContext.TestEntities
            .OrderByDescending(e => e.SortOrder)
            .ToPagedResultAsync(pagination);

        // Assert - Items should be in descending order
        result.Items[0].Name.ShouldBe("Entity-030");
        result.Items[^1].Name.ShouldBe("Entity-021");
    }

    [Fact]
    public async Task ToPagedResultAsync_WithProjection_PreservesOrdering()
    {
        // Arrange
        await SeedTestData(30);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act - Order descending
        var result = await _dbContext.TestEntities
            .OrderByDescending(e => e.SortOrder)
            .ToPagedResultAsync(
                e => new TestEntityDto(e.Id, e.Name),
                pagination);

        // Assert - Projected items should be in descending order
        result.Items[0].Name.ShouldBe("Entity-030");
        result.Items[^1].Name.ShouldBe("Entity-021");
    }

    #endregion
}
