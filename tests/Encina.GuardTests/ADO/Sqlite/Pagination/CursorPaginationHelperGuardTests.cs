using System.Data;
using System.Data.Common;

using Encina.ADO.Sqlite.Pagination;
using Encina.DomainModeling.Pagination;

namespace Encina.GuardTests.ADO.Sqlite.Pagination;

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
