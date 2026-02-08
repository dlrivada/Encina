using System.Data;
using System.Data.Common;

using Encina.ADO.MySQL.Pagination;
using Encina.DomainModeling.Pagination;
using Encina.Messaging;

using NSubstitute;

using Shouldly;

using Xunit;

namespace Encina.UnitTests.ADO.MySQL.Pagination;

/// <summary>
/// Unit tests for <see cref="CursorPaginationHelper{TEntity}"/> in ADO.NET MySQL.
/// </summary>
[Trait("Category", "Unit")]
public class CursorPaginationHelperTests
{
    private readonly ICursorEncoder _mockCursorEncoder;

    public CursorPaginationHelperTests()
    {
        _mockCursorEncoder = Substitute.For<ICursorEncoder>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            new CursorPaginationHelper<TestEntity>(
                null!,
                _mockCursorEncoder,
                r => new TestEntity()));

        exception.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullCursorEncoder_ThrowsArgumentNullException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            new CursorPaginationHelper<TestEntity>(
                mockConnection,
                null!,
                r => new TestEntity()));

        exception.ParamName.ShouldBe("cursorEncoder");
    }

    [Fact]
    public void Constructor_NullEntityMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            new CursorPaginationHelper<TestEntity>(
                mockConnection,
                _mockCursorEncoder,
                null!));

        exception.ParamName.ShouldBe("entityMapper");
    }

    #endregion

    #region ExecuteAsync Parameter Validation Tests

    [Fact]
    public async Task ExecuteAsync_NullTableName_ThrowsArgumentException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var helper = new CursorPaginationHelper<TestEntity>(
            mockConnection,
            _mockCursorEncoder,
            r => new TestEntity());

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await helper.ExecuteAsync<Guid>(
                tableName: null!,
                keyColumn: "Id",
                cursor: null,
                pageSize: 10,
                isDescending: false));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var helper = new CursorPaginationHelper<TestEntity>(
            mockConnection,
            _mockCursorEncoder,
            r => new TestEntity());

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await helper.ExecuteAsync<Guid>(
                tableName: "",
                keyColumn: "Id",
                cursor: null,
                pageSize: 10,
                isDescending: false));
    }

    [Fact]
    public async Task ExecuteAsync_NullKeyColumn_ThrowsArgumentException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var helper = new CursorPaginationHelper<TestEntity>(
            mockConnection,
            _mockCursorEncoder,
            r => new TestEntity());

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await helper.ExecuteAsync<Guid>(
                tableName: "TestTable",
                keyColumn: null!,
                cursor: null,
                pageSize: 10,
                isDescending: false));
    }

    [Fact]
    public async Task ExecuteAsync_ZeroPageSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var helper = new CursorPaginationHelper<TestEntity>(
            mockConnection,
            _mockCursorEncoder,
            r => new TestEntity());

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await helper.ExecuteAsync<Guid>(
                tableName: "TestTable",
                keyColumn: "Id",
                cursor: null,
                pageSize: 0,
                isDescending: false));
    }

    [Fact]
    public async Task ExecuteAsync_NegativePageSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var helper = new CursorPaginationHelper<TestEntity>(
            mockConnection,
            _mockCursorEncoder,
            r => new TestEntity());

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await helper.ExecuteAsync<Guid>(
                tableName: "TestTable",
                keyColumn: "Id",
                cursor: null,
                pageSize: -1,
                isDescending: false));
    }

    [Fact]
    public async Task ExecuteAsync_ExceedsMaxPageSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var helper = new CursorPaginationHelper<TestEntity>(
            mockConnection,
            _mockCursorEncoder,
            r => new TestEntity());

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await helper.ExecuteAsync<Guid>(
                tableName: "TestTable",
                keyColumn: "Id",
                cursor: null,
                pageSize: CursorPaginationOptions.MaxPageSize + 1,
                isDescending: false));
    }

    #endregion

    #region ExecuteCompositeAsync Parameter Validation Tests

    [Fact]
    public async Task ExecuteCompositeAsync_NullKeyColumns_ThrowsArgumentNullException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var helper = new CursorPaginationHelper<TestEntity>(
            mockConnection,
            _mockCursorEncoder,
            r => new TestEntity());

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await helper.ExecuteCompositeAsync(
                tableName: "TestTable",
                keyColumns: null!,
                cursor: null,
                pageSize: 10,
                keyDescending: [false]));
    }

    [Fact]
    public async Task ExecuteCompositeAsync_EmptyKeyColumns_ThrowsArgumentException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var helper = new CursorPaginationHelper<TestEntity>(
            mockConnection,
            _mockCursorEncoder,
            r => new TestEntity());

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await helper.ExecuteCompositeAsync(
                tableName: "TestTable",
                keyColumns: [],
                cursor: null,
                pageSize: 10,
                keyDescending: []));
    }

    [Fact]
    public async Task ExecuteCompositeAsync_MismatchedArrayLengths_ThrowsArgumentException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var helper = new CursorPaginationHelper<TestEntity>(
            mockConnection,
            _mockCursorEncoder,
            r => new TestEntity());

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(async () =>
            await helper.ExecuteCompositeAsync(
                tableName: "TestTable",
                keyColumns: ["Id", "CreatedAt"],
                cursor: null,
                pageSize: 10,
                keyDescending: [false]));

        exception.Message.ShouldContain("length");
    }

    [Fact]
    public async Task ExecuteCompositeAsync_ZeroPageSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var helper = new CursorPaginationHelper<TestEntity>(
            mockConnection,
            _mockCursorEncoder,
            r => new TestEntity());

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await helper.ExecuteCompositeAsync(
                tableName: "TestTable",
                keyColumns: ["Id"],
                cursor: null,
                pageSize: 0,
                keyDescending: [false]));
    }

    #endregion

    #region Helper Methods

    private static DbConnection CreateMockConnection()
    {
        var mockConnection = Substitute.For<DbConnection>();
        mockConnection.State.Returns(ConnectionState.Open);
        return mockConnection;
    }

    #endregion

    #region Test Entities

    private sealed record TestEntity
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }

    #endregion
}
