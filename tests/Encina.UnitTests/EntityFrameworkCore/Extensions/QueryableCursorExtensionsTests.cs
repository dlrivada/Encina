using Encina.DomainModeling.Pagination;
using Encina.EntityFrameworkCore.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Encina.UnitTests.EntityFrameworkCore.Extensions;

/// <summary>
/// Unit tests for <see cref="QueryableCursorExtensions"/>.
/// These tests verify cursor pagination behavior using an in-memory database.
/// </summary>
public sealed class QueryableCursorExtensionsTests : IDisposable
{
    private readonly CursorTestDbContext _context;
    private readonly Base64JsonCursorEncoder _encoder = new();

    public QueryableCursorExtensionsTests()
    {
        var options = new DbContextOptionsBuilder<CursorTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CursorTestDbContext(options);
        SeedData();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void SeedData()
    {
        // Seed 10 items with predictable data
        for (var i = 1; i <= 10; i++)
        {
            _context.Items.Add(new CursorTestEntity
            {
                Id = i,
                Name = $"Item {i:D2}",
                CreatedAtUtc = new DateTime(2025, 1, i, 12, 0, 0, DateTimeKind.Utc),
                SortOrder = i
            });
        }

        _context.SaveChanges();
    }

    #region ToCursorPaginatedAsync - Simple Key Tests

    [Fact]
    public async Task ToCursorPaginatedAsync_FirstPage_ReturnsCorrectItems()
    {
        // Arrange
        var query = _context.Items.OrderBy(x => x.SortOrder);

        // Act
        var result = await query.ToCursorPaginatedAsync(
            cursor: null,
            pageSize: 3,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].SortOrder.Should().Be(1);
        result.Items[1].SortOrder.Should().Be(2);
        result.Items[2].SortOrder.Should().Be(3);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
        result.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_SecondPage_UsesNextCursor()
    {
        // Arrange
        var query = _context.Items.OrderBy(x => x.SortOrder);
        var firstPage = await query.ToCursorPaginatedAsync(
            cursor: null,
            pageSize: 3,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Act
        var secondPage = await query.ToCursorPaginatedAsync(
            cursor: firstPage.NextCursor,
            pageSize: 3,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Assert
        secondPage.Items.Should().HaveCount(3);
        secondPage.Items[0].SortOrder.Should().Be(4);
        secondPage.Items[1].SortOrder.Should().Be(5);
        secondPage.Items[2].SortOrder.Should().Be(6);
        secondPage.HasPreviousPage.Should().BeTrue();
        secondPage.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_LastPage_HasNoNextPage()
    {
        // Arrange
        var query = _context.Items.OrderBy(x => x.SortOrder);

        // Navigate to last page
        var cursor = _encoder.Encode(8); // After item 8, we get items 9 and 10

        // Act
        var result = await query.ToCursorPaginatedAsync(
            cursor: cursor,
            pageSize: 5,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Assert
        result.Items.Should().HaveCount(2); // Only items 9 and 10
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_EmptyResult_ReturnsEmptyWithCorrectFlags()
    {
        // Arrange
        var query = _context.Items.Where(x => x.Id > 1000).OrderBy(x => x.SortOrder);

        // Act
        var result = await query.ToCursorPaginatedAsync(
            cursor: null,
            pageSize: 5,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Assert
        result.Items.Should().BeEmpty();
        result.IsEmpty.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
        result.NextCursor.Should().BeNull();
        result.PreviousCursor.Should().BeNull();
    }

    #endregion

    #region ToCursorPaginatedDescendingAsync Tests

    [Fact]
    public async Task ToCursorPaginatedDescendingAsync_FirstPage_ReturnsItemsInDescendingOrder()
    {
        // Arrange
        var query = _context.Items.OrderByDescending(x => x.SortOrder);

        // Act
        var result = await query.ToCursorPaginatedDescendingAsync(
            cursor: null,
            pageSize: 3,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].SortOrder.Should().Be(10);
        result.Items[1].SortOrder.Should().Be(9);
        result.Items[2].SortOrder.Should().Be(8);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToCursorPaginatedDescendingAsync_SecondPage_ContinuesCorrectly()
    {
        // Arrange
        var query = _context.Items.OrderByDescending(x => x.SortOrder);
        var firstPage = await query.ToCursorPaginatedDescendingAsync(
            cursor: null,
            pageSize: 3,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Act
        var secondPage = await query.ToCursorPaginatedDescendingAsync(
            cursor: firstPage.NextCursor,
            pageSize: 3,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Assert
        secondPage.Items.Should().HaveCount(3);
        secondPage.Items[0].SortOrder.Should().Be(7);
        secondPage.Items[1].SortOrder.Should().Be(6);
        secondPage.Items[2].SortOrder.Should().Be(5);
    }

    #endregion

    #region ToCursorPaginatedAsync with Options Tests

    [Fact]
    public async Task ToCursorPaginatedAsync_WithOptions_UsesOptionsCorrectly()
    {
        // Arrange
        var query = _context.Items.OrderBy(x => x.SortOrder);
        var options = new CursorPaginationOptions(Cursor: null, PageSize: 4);

        // Act
        var result = await query.ToCursorPaginatedAsync(
            options: options,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Assert
        result.Items.Should().HaveCount(4);
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_BackwardDirection_ReturnsItemsInReverse()
    {
        // Arrange
        var query = _context.Items.OrderBy(x => x.SortOrder);

        // Get first page and its end cursor
        var firstPage = await query.ToCursorPaginatedAsync(
            cursor: null,
            pageSize: 3,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Get next page
        var secondPage = await query.ToCursorPaginatedAsync(
            cursor: firstPage.NextCursor,
            pageSize: 3,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Use backward direction from second page's previous cursor
        var backwardOptions = new CursorPaginationOptions(
            Cursor: secondPage.PreviousCursor,
            PageSize: 3,
            Direction: CursorDirection.Backward);

        // Act - Go backward from second page
        var previousPage = await query.ToCursorPaginatedAsync(
            options: backwardOptions,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Assert - Should get items before the second page
        previousPage.Items.Should().HaveCount(3);
        previousPage.Items[0].SortOrder.Should().Be(1);
        previousPage.Items[1].SortOrder.Should().Be(2);
        previousPage.Items[2].SortOrder.Should().Be(3);
    }

    #endregion

    #region DateTime Key Tests

    [Fact]
    public async Task ToCursorPaginatedAsync_DateTimeKey_PaginatesCorrectly()
    {
        // Arrange
        var query = _context.Items.OrderByDescending(x => x.CreatedAtUtc);

        // Act
        var result = await query.ToCursorPaginatedDescendingAsync(
            cursor: null,
            pageSize: 3,
            keySelector: x => x.CreatedAtUtc,
            cursorEncoder: _encoder);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].CreatedAtUtc.Day.Should().Be(10);
        result.Items[1].CreatedAtUtc.Day.Should().Be(9);
        result.Items[2].CreatedAtUtc.Day.Should().Be(8);
    }

    #endregion

    #region Projection Tests

    [Fact]
    public async Task ToCursorPaginatedAsync_WithProjection_ReturnsProjectedItems()
    {
        // Arrange
        var query = _context.Items.OrderBy(x => x.SortOrder);

        // Act
        var result = await query.ToCursorPaginatedAsync(
            selector: x => new CursorTestDto(x.Id, x.Name),
            cursor: null,
            pageSize: 3,
            keySelector: x => x.SortOrder,
            cursorEncoder: _encoder);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Should().BeOfType<CursorTestDto>();
        result.Items[0].Id.Should().Be(1);
        result.Items[0].Name.Should().Be("Item 01");
    }

    #endregion

    #region Composite Key Tests

    [Fact]
    public async Task ToCursorPaginatedCompositeAsync_FirstPage_ReturnsCorrectItems()
    {
        // Arrange - Order by CreatedAt DESC, then Id ASC
        var query = _context.Items
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id);

        // Act
        var result = await query.ToCursorPaginatedCompositeAsync(
            cursor: null,
            pageSize: 3,
            keySelector: x => new { x.CreatedAtUtc, x.Id },
            cursorEncoder: _encoder,
            keyDescending: [true, false]);

        // Assert
        result.Items.Should().HaveCount(3);
        result.HasNextPage.Should().BeTrue();
    }

    #endregion

    #region Guard Clause Tests

    [Fact]
    public async Task ToCursorPaginatedAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        IQueryable<CursorTestEntity>? query = null;

        // Act
        var act = async () => await query!.ToCursorPaginatedAsync(
            cursor: null,
            pageSize: 5,
            keySelector: x => x.Id,
            cursorEncoder: _encoder);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("query");
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_NullKeySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var query = _context.Items.OrderBy(x => x.Id);

        // Act
        var act = async () => await query.ToCursorPaginatedAsync(
            cursor: null,
            pageSize: 5,
            keySelector: (System.Linq.Expressions.Expression<Func<CursorTestEntity, int>>)null!,
            cursorEncoder: _encoder);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("keySelector");
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_NullEncoder_ThrowsArgumentNullException()
    {
        // Arrange
        var query = _context.Items.OrderBy(x => x.Id);

        // Act
        var act = async () => await query.ToCursorPaginatedAsync(
            cursor: null,
            pageSize: 5,
            keySelector: x => x.Id,
            cursorEncoder: null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("cursorEncoder");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ToCursorPaginatedAsync_InvalidPageSize_ThrowsArgumentOutOfRangeException(int pageSize)
    {
        // Arrange
        var query = _context.Items.OrderBy(x => x.Id);

        // Act
        var act = async () => await query.ToCursorPaginatedAsync(
            cursor: null,
            pageSize: pageSize,
            keySelector: x => x.Id,
            cursorEncoder: _encoder);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(pageSize));
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_PageSizeExceedsMax_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var query = _context.Items.OrderBy(x => x.Id);

        // Act
        var act = async () => await query.ToCursorPaginatedAsync(
            cursor: null,
            pageSize: CursorPaginationOptions.MaxPageSize + 1,
            keySelector: x => x.Id,
            cursorEncoder: _encoder);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("pageSize");
    }

    #endregion

    #region Test Infrastructure

    private sealed class CursorTestDbContext : DbContext
    {
        public CursorTestDbContext(DbContextOptions<CursorTestDbContext> options)
            : base(options)
        {
        }

        public DbSet<CursorTestEntity> Items => Set<CursorTestEntity>();
    }

    private sealed class CursorTestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public int SortOrder { get; set; }
    }

    private sealed record CursorTestDto(int Id, string Name);

    #endregion
}
