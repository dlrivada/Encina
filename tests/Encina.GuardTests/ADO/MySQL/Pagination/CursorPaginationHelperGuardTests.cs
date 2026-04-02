using System.Data;
using System.Data.Common;

using Encina.ADO.MySQL.Pagination;
using Encina.DomainModeling.Pagination;

namespace Encina.GuardTests.ADO.MySQL.Pagination;

/// <summary>
/// Guard tests for <see cref="CursorPaginationHelper{TEntity}"/> to verify null parameter handling.
/// </summary>
public class CursorPaginationHelperGuardTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        DbConnection connection = null!;
        var cursorEncoder = Substitute.For<ICursorEncoder>();

        // Act & Assert
        var act = () => new CursorPaginationHelper<TestEntity>(
            connection,
            cursorEncoder,
            r => new TestEntity());
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when cursorEncoder is null.
    /// </summary>
    [Fact]
    public void Constructor_NullCursorEncoder_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        ICursorEncoder cursorEncoder = null!;

        // Act & Assert
        var act = () => new CursorPaginationHelper<TestEntity>(
            connection,
            cursorEncoder,
            r => new TestEntity());
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cursorEncoder");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when entityMapper is null.
    /// </summary>
    [Fact]
    public void Constructor_NullEntityMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var cursorEncoder = Substitute.For<ICursorEncoder>();
        Func<IDataReader, TestEntity> entityMapper = null!;

        // Act & Assert
        var act = () => new CursorPaginationHelper<TestEntity>(
            connection,
            cursorEncoder,
            entityMapper);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entityMapper");
    }

    /// <summary>
    /// Verifies that ExecuteAsync throws ArgumentException when tableName is null.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NullTableName_ThrowsArgumentException()
    {
        // Arrange
        var helper = CreateHelper();
        string tableName = null!;

        // Act & Assert
        var act = () => helper.ExecuteAsync<Guid>(
            tableName: tableName,
            keyColumn: "Id",
            cursor: null,
            pageSize: 10,
            isDescending: false);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("tableName");
    }

    /// <summary>
    /// Verifies that ExecuteAsync throws ArgumentException when keyColumn is null.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NullKeyColumn_ThrowsArgumentException()
    {
        // Arrange
        var helper = CreateHelper();
        string keyColumn = null!;

        // Act & Assert
        var act = () => helper.ExecuteAsync<Guid>(
            tableName: "TestTable",
            keyColumn: keyColumn,
            cursor: null,
            pageSize: 10,
            isDescending: false);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("keyColumn");
    }

    /// <summary>
    /// Verifies that ExecuteCompositeAsync throws ArgumentNullException when keyColumns is null.
    /// </summary>
    [Fact]
    public async Task ExecuteCompositeAsync_NullKeyColumns_ThrowsArgumentNullException()
    {
        // Arrange
        var helper = CreateHelper();
        string[] keyColumns = null!;

        // Act & Assert
        var act = () => helper.ExecuteCompositeAsync(
            tableName: "TestTable",
            keyColumns: keyColumns,
            cursor: null,
            pageSize: 10,
            keyDescending: [false]);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyColumns");
    }

    /// <summary>
    /// Verifies that ExecuteCompositeAsync throws ArgumentNullException when keyDescending is null.
    /// </summary>
    [Fact]
    public async Task ExecuteCompositeAsync_NullKeyDescending_ThrowsArgumentNullException()
    {
        // Arrange
        var helper = CreateHelper();
        bool[] keyDescending = null!;

        // Act & Assert
        var act = () => helper.ExecuteCompositeAsync(
            tableName: "TestTable",
            keyColumns: ["Id"],
            cursor: null,
            pageSize: 10,
            keyDescending: keyDescending);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyDescending");
    }

    /// <summary>
    /// Verifies that ExecuteAsync throws ArgumentException when tableName is empty.
    /// </summary>
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

    /// <summary>
    /// Verifies that ExecuteAsync throws ArgumentException when keyColumn is empty.
    /// </summary>
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

    /// <summary>
    /// Verifies that ExecuteAsync throws ArgumentOutOfRangeException when pageSize is zero.
    /// </summary>
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

    /// <summary>
    /// Verifies that ExecuteAsync throws ArgumentOutOfRangeException when pageSize is negative.
    /// </summary>
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

    /// <summary>
    /// Verifies that ExecuteAsync throws ArgumentOutOfRangeException when pageSize exceeds MaxPageSize.
    /// </summary>
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

    /// <summary>
    /// Verifies that ExecuteCompositeAsync throws ArgumentException when tableName is null.
    /// </summary>
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

    /// <summary>
    /// Verifies that ExecuteCompositeAsync throws ArgumentException when keyColumns is empty.
    /// </summary>
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

    /// <summary>
    /// Verifies that ExecuteCompositeAsync throws ArgumentException when keyColumns and keyDescending lengths mismatch.
    /// </summary>
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

    /// <summary>
    /// Verifies that ExecuteCompositeAsync throws ArgumentOutOfRangeException when pageSize is zero.
    /// </summary>
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

    /// <summary>
    /// Verifies that ExecuteCompositeAsync throws ArgumentOutOfRangeException when pageSize exceeds MaxPageSize.
    /// </summary>
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

    private static DbConnection CreateMockConnection()
    {
        var connection = Substitute.For<DbConnection>();
        connection.State.Returns(ConnectionState.Open);
        return connection;
    }

    private static CursorPaginationHelper<TestEntity> CreateHelper()
    {
        var connection = CreateMockConnection();
        var cursorEncoder = Substitute.For<ICursorEncoder>();
        return new CursorPaginationHelper<TestEntity>(
            connection,
            cursorEncoder,
            r => new TestEntity());
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
