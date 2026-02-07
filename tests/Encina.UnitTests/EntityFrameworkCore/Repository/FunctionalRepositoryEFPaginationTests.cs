using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Repository;

/// <summary>
/// Unit tests for pagination functionality in <see cref="FunctionalRepositoryEF{TEntity, TId}"/>.
/// Uses EF Core InMemory database for isolation.
/// </summary>
[Trait("Category", "Unit")]
public class FunctionalRepositoryEFPaginationTests : IDisposable
{
    private readonly RepositoryTestDbContext _dbContext;
    private readonly FunctionalRepositoryEF<TestEntity, Guid> _repository;

    public FunctionalRepositoryEFPaginationTests()
    {
        var options = new DbContextOptionsBuilder<RepositoryTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RepositoryTestDbContext(options);
        _repository = new FunctionalRepositoryEF<TestEntity, Guid>(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

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

    #region GetPagedAsync(PaginationOptions) Tests

    [Fact]
    public async Task GetPagedAsync_WithPaginationOptions_ReturnsCorrectPage()
    {
        // Arrange
        await SeedTestData(25);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _repository.GetPagedAsync(pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(10);
            paged.PageNumber.ShouldBe(1);
            paged.PageSize.ShouldBe(10);
            paged.TotalCount.ShouldBe(25);
            paged.TotalPages.ShouldBe(3);
        });
    }

    [Fact]
    public async Task GetPagedAsync_Page2_ReturnsCorrectItems()
    {
        // Arrange
        await SeedTestData(25);
        var pagination = new PaginationOptions(PageNumber: 2, PageSize: 10);

        // Act
        var result = await _repository.GetPagedAsync(pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(10);
            paged.PageNumber.ShouldBe(2);
            paged.HasPreviousPage.ShouldBeTrue();
            paged.HasNextPage.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task GetPagedAsync_LastPage_ReturnsPartialResults()
    {
        // Arrange
        await SeedTestData(25);
        var pagination = new PaginationOptions(PageNumber: 3, PageSize: 10);

        // Act
        var result = await _repository.GetPagedAsync(pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(5); // 25 total, page 3 has 5 remaining
            paged.PageNumber.ShouldBe(3);
            paged.HasPreviousPage.ShouldBeTrue();
            paged.HasNextPage.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetPagedAsync_EmptyTable_ReturnsEmptyResult()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _repository.GetPagedAsync(pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.ShouldBeEmpty();
            paged.TotalCount.ShouldBe(0);
            paged.TotalPages.ShouldBe(0);
            paged.IsEmpty.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task GetPagedAsync_PageBeyondTotal_ReturnsEmptyItems()
    {
        // Arrange
        await SeedTestData(10);
        var pagination = new PaginationOptions(PageNumber: 5, PageSize: 10);

        // Act
        var result = await _repository.GetPagedAsync(pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.ShouldBeEmpty();
            paged.TotalCount.ShouldBe(10); // Total count is still accurate
        });
    }

    [Fact]
    public async Task GetPagedAsync_SingleItemPerPage_WorksCorrectly()
    {
        // Arrange
        await SeedTestData(5);
        var pagination = new PaginationOptions(PageNumber: 3, PageSize: 1);

        // Act
        var result = await _repository.GetPagedAsync(pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(1);
            paged.TotalPages.ShouldBe(5);
            paged.HasPreviousPage.ShouldBeTrue();
            paged.HasNextPage.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task GetPagedAsync_LargePageSize_ReturnsAllItems()
    {
        // Arrange
        await SeedTestData(5);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 100);

        // Act
        var result = await _repository.GetPagedAsync(pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(5);
            paged.TotalPages.ShouldBe(1);
            paged.HasNextPage.ShouldBeFalse();
        });
    }

    #endregion

    #region GetPagedAsync(Specification, PaginationOptions) Tests

    [Fact]
    public async Task GetPagedAsync_WithSpecification_FiltersAndPaginates()
    {
        // Arrange
        await SeedTestData(25); // 13 active (odd numbers), 12 inactive
        var spec = new ActiveEntitySpec();
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 5);

        // Act
        var result = await _repository.GetPagedAsync(spec, pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(5);
            paged.TotalCount.ShouldBe(13); // Only active entities
            paged.TotalPages.ShouldBe(3); // 13 / 5 = 3 pages
            paged.Items.ShouldAllBe(e => e.IsActive);
        });
    }

    [Fact]
    public async Task GetPagedAsync_WithSpecification_Page2_FiltersCorrectly()
    {
        // Arrange
        await SeedTestData(25);
        var spec = new ActiveEntitySpec();
        var pagination = new PaginationOptions(PageNumber: 2, PageSize: 5);

        // Act
        var result = await _repository.GetPagedAsync(spec, pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(5);
            paged.Items.ShouldAllBe(e => e.IsActive);
            paged.HasPreviousPage.ShouldBeTrue();
            paged.HasNextPage.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task GetPagedAsync_WithSpecificationNoMatches_ReturnsEmpty()
    {
        // Arrange
        await SeedTestData(10);
        // MinAmountSpec(10000) should match nothing
        var spec = new MinAmountSpec(10000);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _repository.GetPagedAsync(spec, pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.ShouldBeEmpty();
            paged.TotalCount.ShouldBe(0);
            paged.IsEmpty.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task GetPagedAsync_WithCombinedSpecification_WorksCorrectly()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("High Active", isActive: true, amount: 200),
            CreateTestEntity("Low Active", isActive: true, amount: 50),
            CreateTestEntity("High Inactive", isActive: false, amount: 300),
            CreateTestEntity("Medium Active", isActive: true, amount: 150)
        );
        await _dbContext.SaveChangesAsync();

        var spec = new ActiveEntitySpec().And(new MinAmountSpec(100));
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _repository.GetPagedAsync(spec, pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(2); // High Active (200) and Medium Active (150)
            paged.TotalCount.ShouldBe(2);
            paged.Items.ShouldAllBe(e => e.IsActive && e.Amount >= 100);
        });
    }

    #endregion

    #region GetPagedAsync(IPagedSpecification) Tests

    [Fact]
    public async Task GetPagedAsync_WithPagedSpecification_WorksCorrectly()
    {
        // Arrange
        await SeedTestData(25);
        var pagination = new PaginationOptions(PageNumber: 2, PageSize: 5);
        var spec = new ActivePagedSpec(pagination);

        // Act
        var result = await _repository.GetPagedAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(5);
            paged.PageNumber.ShouldBe(2);
            paged.PageSize.ShouldBe(5);
            paged.TotalCount.ShouldBe(13); // Active entities only
            paged.Items.ShouldAllBe(e => e.IsActive);
        });
    }

    [Fact]
    public async Task GetPagedAsync_WithPagedSpecification_LastPage_WorksCorrectly()
    {
        // Arrange
        await SeedTestData(25); // 13 active
        var pagination = new PaginationOptions(PageNumber: 3, PageSize: 5);
        var spec = new ActivePagedSpec(pagination);

        // Act
        var result = await _repository.GetPagedAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(3); // 13 total, pages of 5: 5+5+3
            paged.HasPreviousPage.ShouldBeTrue();
            paged.HasNextPage.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetPagedAsync_WithPagedSpecification_NullPagination_ThrowsException()
    {
        // This test verifies the PagedQuerySpecification constructor throws for null pagination
        // The actual GetPagedAsync won't be called because specification construction fails

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ActivePagedSpec(null!));
    }

    #endregion

    #region Pagination Metadata Tests

    [Fact]
    public async Task GetPagedAsync_FirstItemIndex_CalculatesCorrectly()
    {
        // Arrange
        await SeedTestData(50);
        var pagination = new PaginationOptions(PageNumber: 3, PageSize: 10);

        // Act
        var result = await _repository.GetPagedAsync(pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.FirstItemIndex.ShouldBe(21); // (3-1) * 10 + 1
        });
    }

    [Fact]
    public async Task GetPagedAsync_LastItemIndex_CalculatesCorrectly()
    {
        // Arrange
        await SeedTestData(50);
        var pagination = new PaginationOptions(PageNumber: 3, PageSize: 10);

        // Act
        var result = await _repository.GetPagedAsync(pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.LastItemIndex.ShouldBe(30); // Min(3*10, 50) = 30
        });
    }

    [Fact]
    public async Task GetPagedAsync_LastPagePartial_LastItemIndex_CorrectlyLimited()
    {
        // Arrange
        await SeedTestData(45);
        var pagination = new PaginationOptions(PageNumber: 5, PageSize: 10);

        // Act
        var result = await _repository.GetPagedAsync(pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(5);
            paged.LastItemIndex.ShouldBe(45); // Total count, not 50
        });
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetPagedAsync_DefaultPagination_WorksCorrectly()
    {
        // Arrange
        await SeedTestData(50);

        // Act
        var result = await _repository.GetPagedAsync(PaginationOptions.Default);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(20); // Default page size
            paged.PageNumber.ShouldBe(1);
            paged.PageSize.ShouldBe(20);
        });
    }

    [Fact]
    public async Task GetPagedAsync_ExactlyOnePageOfData_NoNextPage()
    {
        // Arrange
        await SeedTestData(10);
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _repository.GetPagedAsync(pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(10);
            paged.TotalPages.ShouldBe(1);
            paged.HasNextPage.ShouldBeFalse();
            paged.HasPreviousPage.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetPagedAsync_LargeDataset_WorksCorrectly()
    {
        // Arrange
        await SeedTestData(1000);
        var pagination = new PaginationOptions(PageNumber: 50, PageSize: 10);

        // Act
        var result = await _repository.GetPagedAsync(pagination);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(10);
            paged.TotalCount.ShouldBe(1000);
            paged.TotalPages.ShouldBe(100);
            paged.PageNumber.ShouldBe(50);
        });
    }

    #endregion

    #region Test Specifications

    private sealed class ActiveEntitySpec : QuerySpecification<TestEntity>
    {
        public ActiveEntitySpec()
        {
            AddCriteria(e => e.IsActive);
        }
    }

    private sealed class MinAmountSpec : QuerySpecification<TestEntity>
    {
        public MinAmountSpec(decimal minAmount)
        {
            AddCriteria(e => e.Amount >= minAmount);
        }
    }

    private sealed class ActivePagedSpec : PagedQuerySpecification<TestEntity>
    {
        public ActivePagedSpec(PaginationOptions pagination) : base(pagination)
        {
            AddCriteria(e => e.IsActive);
        }
    }

    #endregion
}
