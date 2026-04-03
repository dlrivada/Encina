using System.Data;
using Encina.Dapper.PostgreSQL.Pagination;
using Encina.DomainModeling.Pagination;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.PostgreSQL.Pagination;

/// <summary>
/// Guard tests for <see cref="CursorPaginationHelper{TEntity}"/> to verify null parameter handling.
/// </summary>
public class CursorPaginationHelperGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        var cursorEncoder = Substitute.For<ICursorEncoder>();

        // Act & Assert
        var act = () => new CursorPaginationHelper<TestEntity>(connection, cursorEncoder);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullCursorEncoder_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        ICursorEncoder cursorEncoder = null!;

        // Act & Assert
        var act = () => new CursorPaginationHelper<TestEntity>(connection, cursorEncoder);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cursorEncoder");
    }

    [Fact]
    public async Task ExecuteAsync_NullTableName_ThrowsArgumentException()
    {
        // Arrange
        var helper = CreateHelper();

        // Act & Assert
        var act = () => helper.ExecuteAsync<Guid>(
            tableName: null!,
            keyColumn: "Id",
            cursor: null,
            pageSize: 10,
            isDescending: false);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("tableName");
    }

    [Fact]
    public async Task ExecuteAsync_NullKeyColumn_ThrowsArgumentException()
    {
        // Arrange
        var helper = CreateHelper();

        // Act & Assert
        var act = () => helper.ExecuteAsync<Guid>(
            tableName: "TestTable",
            keyColumn: null!,
            cursor: null,
            pageSize: 10,
            isDescending: false);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("keyColumn");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var helper = CreateHelper();

        // Act & Assert
        var act = () => helper.ExecuteAsync<Guid>(
            tableName: "",
            keyColumn: "Id",
            cursor: null,
            pageSize: 10,
            isDescending: false);
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyKeyColumn_ThrowsArgumentException()
    {
        // Arrange
        var helper = CreateHelper();

        // Act & Assert
        var act = () => helper.ExecuteAsync<Guid>(
            tableName: "TestTable",
            keyColumn: "",
            cursor: null,
            pageSize: 10,
            isDescending: false);
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroPageSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var helper = CreateHelper();

        // Act & Assert
        var act = () => helper.ExecuteAsync<Guid>(
            tableName: "TestTable",
            keyColumn: "Id",
            cursor: null,
            pageSize: 0,
            isDescending: false);
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_NegativePageSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var helper = CreateHelper();

        // Act & Assert
        var act = () => helper.ExecuteAsync<Guid>(
            tableName: "TestTable",
            keyColumn: "Id",
            cursor: null,
            pageSize: -1,
            isDescending: false);
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_ExceedsMaxPageSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var helper = CreateHelper();
        var maxPageSize = CursorPaginationOptions.MaxPageSize;

        // Act & Assert
        var act = () => helper.ExecuteAsync<Guid>(
            tableName: "TestTable",
            keyColumn: "Id",
            cursor: null,
            pageSize: maxPageSize + 1,
            isDescending: false);
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task ExecuteCompositeAsync_NullKeyColumns_ThrowsArgumentNullException()
    {
        // Arrange
        var helper = CreateHelper();

        // Act & Assert
        var act = () => helper.ExecuteCompositeAsync(
            tableName: "TestTable",
            keyColumns: null!,
            cursor: null,
            pageSize: 10,
            keyDescending: [false]);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyColumns");
    }

    [Fact]
    public async Task ExecuteCompositeAsync_NullKeyDescending_ThrowsArgumentNullException()
    {
        // Arrange
        var helper = CreateHelper();

        // Act & Assert
        var act = () => helper.ExecuteCompositeAsync(
            tableName: "TestTable",
            keyColumns: ["Id"],
            cursor: null,
            pageSize: 10,
            keyDescending: null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyDescending");
    }

    [Fact]
    public async Task ExecuteCompositeAsync_NullTableName_ThrowsArgumentException()
    {
        // Arrange
        var helper = CreateHelper();

        // Act & Assert
        var act = () => helper.ExecuteCompositeAsync(
            tableName: null!,
            keyColumns: ["Id"],
            cursor: null,
            pageSize: 10,
            keyDescending: [false]);
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task ExecuteCompositeAsync_EmptyKeyColumns_ThrowsArgumentException()
    {
        // Arrange
        var helper = CreateHelper();

        // Act & Assert
        var act = () => helper.ExecuteCompositeAsync(
            tableName: "TestTable",
            keyColumns: [],
            cursor: null,
            pageSize: 10,
            keyDescending: []);
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task ExecuteCompositeAsync_MismatchedLengths_ThrowsArgumentException()
    {
        // Arrange
        var helper = CreateHelper();

        // Act & Assert
        var act = () => helper.ExecuteCompositeAsync(
            tableName: "TestTable",
            keyColumns: ["Id", "Name"],
            cursor: null,
            pageSize: 10,
            keyDescending: [false]); // Only 1 element, should match 2
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task ExecuteCompositeAsync_ZeroPageSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var helper = CreateHelper();

        // Act & Assert
        var act = () => helper.ExecuteCompositeAsync(
            tableName: "TestTable",
            keyColumns: ["Id"],
            cursor: null,
            pageSize: 0,
            keyDescending: [false]);
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task ExecuteCompositeAsync_ExceedsMaxPageSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var helper = CreateHelper();
        var maxPageSize = CursorPaginationOptions.MaxPageSize;

        // Act & Assert
        var act = () => helper.ExecuteCompositeAsync(
            tableName: "TestTable",
            keyColumns: ["Id"],
            cursor: null,
            pageSize: maxPageSize + 1,
            keyDescending: [false]);
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    #region Helper Methods

    private static CursorPaginationHelper<TestEntity> CreateHelper()
    {
        var connection = Substitute.For<IDbConnection>();
        var cursorEncoder = Substitute.For<ICursorEncoder>();
        return new CursorPaginationHelper<TestEntity>(connection, cursorEncoder);
    }

    #endregion

    #region Test Entities

    private sealed record TestEntity
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; init; } = string.Empty;
    }

    #endregion
}
