using System.Globalization;

using Encina.ADO.Sqlite.Pagination;
using Encina.DomainModeling.Pagination;
using Encina.Messaging;
using Encina.TestInfrastructure.Fixtures;

using Microsoft.Data.Sqlite;

using Shouldly;

namespace Encina.IntegrationTests.ADO.Sqlite.Pagination;

/// <summary>
/// Integration tests for <see cref="CursorPaginationHelper{TEntity}"/> using real SQLite.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("ADO-Sqlite")]
#pragma warning disable CA1001 // Type owns disposable field '_connection' but DisposeAsync handles cleanup via IAsyncLifetime
public class CursorPaginationHelperIntegrationTests : IAsyncLifetime
#pragma warning restore CA1001
{
    private readonly SqliteFixture _fixture;
    private SqliteConnection _connection = null!;
    private ICursorEncoder _encoder = null!;

    public CursorPaginationHelperIntegrationTests(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection(_fixture.ConnectionString);
        await _connection.OpenAsync();
        _encoder = new Base64JsonCursorEncoder();
        await CreateSchemaAsync();
        await SeedDataAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private async Task CreateSchemaAsync()
    {
        const string sql = """
            DROP TABLE IF EXISTS PaginationItems;
            CREATE TABLE PaginationItems (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Value INTEGER NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_PaginationItems_CreatedAtUtc ON PaginationItems(CreatedAtUtc);
            CREATE INDEX IF NOT EXISTS IX_PaginationItems_Value ON PaginationItems(Value);
            """;

        await using var command = new SqliteCommand(sql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task SeedDataAsync()
    {
        // Insert 50 items with sequential values and dates
        for (int i = 1; i <= 50; i++)
        {
            var id = Guid.NewGuid();
            var name = $"Item {i:D3}";
            var value = i;
            var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(i);

            const string sql = """
                INSERT INTO PaginationItems (Id, Name, Value, CreatedAtUtc)
                VALUES (@Id, @Name, @Value, @CreatedAtUtc)
                """;

            await using var command = new SqliteCommand(sql, _connection);
            command.Parameters.AddWithValue("@Id", id.ToString());
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Value", value);
            command.Parameters.AddWithValue("@CreatedAtUtc", createdAt.ToString("O"));
            await command.ExecuteNonQueryAsync();
        }
    }

    private CursorPaginationHelper<PaginationItem> CreateHelper()
    {
        return new CursorPaginationHelper<PaginationItem>(
            _connection,
            _encoder,
            reader => new PaginationItem
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Value = reader.GetInt32(reader.GetOrdinal("Value")),
                CreatedAtUtc = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAtUtc")), CultureInfo.InvariantCulture)
            });
    }

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_FirstPage_ReturnsCorrectItems()
    {
        // Arrange
        var helper = CreateHelper();

        // Act
        var result = await helper.ExecuteAsync<int>(
            tableName: "PaginationItems",
            keyColumn: "Value",
            cursor: null,
            pageSize: 10,
            isDescending: false);

        // Assert
        result.Items.Count.ShouldBe(10);
        result.HasNextPage.ShouldBeTrue();
        result.HasPreviousPage.ShouldBeFalse();
        result.NextCursor.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_SecondPage_ReturnsCorrectItems()
    {
        // Arrange
        var helper = CreateHelper();

        // First page
        var firstPage = await helper.ExecuteAsync<int>(
            tableName: "PaginationItems",
            keyColumn: "Value",
            cursor: null,
            pageSize: 10,
            isDescending: false);

        // Act - Second page
        var result = await helper.ExecuteAsync<int>(
            tableName: "PaginationItems",
            keyColumn: "Value",
            cursor: firstPage.NextCursor,
            pageSize: 10,
            isDescending: false);

        // Assert
        result.Items.Count.ShouldBe(10);
        result.HasNextPage.ShouldBeTrue();
        result.HasPreviousPage.ShouldBeTrue();
        result.Items[0].Value.ShouldBeGreaterThan(firstPage.Items[^1].Value);
    }

    [Fact]
    public async Task ExecuteAsync_LastPage_HasNoNextCursor()
    {
        // Arrange
        var helper = CreateHelper();

        // Navigate to last page
        string? cursor = null;
        CursorPaginatedResult<PaginationItem> result;
        do
        {
            result = await helper.ExecuteAsync<int>(
                tableName: "PaginationItems",
                keyColumn: "Value",
                cursor: cursor,
                pageSize: 10,
                isDescending: false);
            cursor = result.NextCursor;
        } while (result.HasNextPage);

        // Assert
        result.HasNextPage.ShouldBeFalse();
        result.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Descending_ReturnsItemsInReverseOrder()
    {
        // Arrange
        var helper = CreateHelper();

        // Act
        var result = await helper.ExecuteAsync<int>(
            tableName: "PaginationItems",
            keyColumn: "Value",
            cursor: null,
            pageSize: 10,
            isDescending: true);

        // Assert
        result.Items.Count.ShouldBe(10);
        result.Items[0].Value.ShouldBe(50); // Highest value first
        result.Items[^1].Value.ShouldBe(41);
    }

    [Fact]
    public async Task ExecuteAsync_WithWhereClause_FiltersCorrectly()
    {
        // Arrange
        var helper = CreateHelper();

        // Act - Only items where Value > 25
        var result = await helper.ExecuteAsync<int>(
            tableName: "PaginationItems",
            keyColumn: "Value",
            cursor: null,
            pageSize: 10,
            isDescending: false,
            whereClause: "\"Value\" > 25");

        // Assert
        result.Items.ShouldAllBe(item => item.Value > 25);
    }

    #endregion

    #region ExecuteCompositeAsync Tests

    [Fact]
    public async Task ExecuteCompositeAsync_FirstPage_ReturnsCorrectItems()
    {
        // Arrange
        var helper = CreateHelper();

        // Act
        var result = await helper.ExecuteCompositeAsync(
            tableName: "PaginationItems",
            keyColumns: ["Value", "Id"],
            cursor: null,
            pageSize: 10,
            keyDescending: [false, false]);

        // Assert
        result.Items.Count.ShouldBe(10);
        result.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteCompositeAsync_Navigation_WorksCorrectly()
    {
        // Arrange
        var helper = CreateHelper();

        // First page
        var firstPage = await helper.ExecuteCompositeAsync(
            tableName: "PaginationItems",
            keyColumns: ["Value", "Id"],
            cursor: null,
            pageSize: 10,
            keyDescending: [false, false]);

        // Act - Second page
        var secondPage = await helper.ExecuteCompositeAsync(
            tableName: "PaginationItems",
            keyColumns: ["Value", "Id"],
            cursor: firstPage.NextCursor,
            pageSize: 10,
            keyDescending: [false, false]);

        // Assert
        secondPage.Items.Count.ShouldBe(10);
        secondPage.Items[0].Value.ShouldBeGreaterThan(firstPage.Items[^1].Value);
    }

    #endregion

    #region Bidirectional Navigation Tests

    [Fact]
    public async Task ExecuteAsync_BackwardNavigation_ReturnsCorrectItems()
    {
        // Arrange
        var helper = CreateHelper();

        // Navigate forward to page 3
        var page1 = await helper.ExecuteAsync<int>(
            tableName: "PaginationItems",
            keyColumn: "Value",
            cursor: null,
            pageSize: 10,
            isDescending: false);

        var page2 = await helper.ExecuteAsync<int>(
            tableName: "PaginationItems",
            keyColumn: "Value",
            cursor: page1.NextCursor,
            pageSize: 10,
            isDescending: false);

        var page3 = await helper.ExecuteAsync<int>(
            tableName: "PaginationItems",
            keyColumn: "Value",
            cursor: page2.NextCursor,
            pageSize: 10,
            isDescending: false);

        // Act - Navigate backward from page 3
        var backToPage2 = await helper.ExecuteAsync<int>(
            tableName: "PaginationItems",
            keyColumn: "Value",
            cursor: page3.PreviousCursor,
            pageSize: 10,
            isDescending: false,
            direction: CursorDirection.Backward);

        // Assert - Items should match page 2
        backToPage2.Items[0].Value.ShouldBe(page2.Items[0].Value);
    }

    #endregion

    #region Test Entity

    private sealed record PaginationItem
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Value { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }

    #endregion
}
