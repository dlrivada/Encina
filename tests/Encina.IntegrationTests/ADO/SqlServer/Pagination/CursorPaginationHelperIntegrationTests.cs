using Encina.ADO.SqlServer.Pagination;
using Encina.DomainModeling.Pagination;
using Encina.Messaging;
using Encina.TestInfrastructure.Fixtures;

using Microsoft.Data.SqlClient;

using Shouldly;

namespace Encina.IntegrationTests.ADO.SqlServer.Pagination;

/// <summary>
/// Integration tests for <see cref="CursorPaginationHelper{TEntity}"/> using real SQL Server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
#pragma warning disable CA1001 // Type owns disposable field '_connection' but DisposeAsync handles cleanup via IAsyncLifetime
public class CursorPaginationHelperIntegrationTests : IAsyncLifetime
#pragma warning restore CA1001
{
    private readonly SqlServerFixture _fixture;
    private SqlConnection _connection = null!;
    private ICursorEncoder _encoder = null!;

    public CursorPaginationHelperIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _connection = new SqlConnection(_fixture.ConnectionString);
        await _connection.OpenAsync();
        _encoder = new Base64JsonCursorEncoder();
        await CreateSchemaAsync();
        await SeedDataAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await ClearDataAsync();
        await _connection.DisposeAsync();
    }

    private async Task CreateSchemaAsync()
    {
        const string sql = """
            IF OBJECT_ID('PaginationItems', 'U') IS NOT NULL
                DROP TABLE PaginationItems;

            CREATE TABLE PaginationItems (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Name NVARCHAR(100) NOT NULL,
                Value INT NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL
            );
            CREATE INDEX IX_PaginationItems_CreatedAtUtc ON PaginationItems(CreatedAtUtc);
            CREATE INDEX IX_PaginationItems_Value ON PaginationItems(Value);
            """;

        await using var command = new SqlCommand(sql, _connection);
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

            await using var command = new SqlCommand(sql, _connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Value", value);
            command.Parameters.AddWithValue("@CreatedAtUtc", createdAt);
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task ClearDataAsync()
    {
        const string sql = "DELETE FROM PaginationItems";
        await using var command = new SqlCommand(sql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    private CursorPaginationHelper<PaginationItem> CreateHelper()
    {
        return new CursorPaginationHelper<PaginationItem>(
            _connection,
            _encoder,
            reader => new PaginationItem
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Value = reader.GetInt32(reader.GetOrdinal("Value")),
                CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc"))
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
            whereClause: "[Value] > 25");

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
