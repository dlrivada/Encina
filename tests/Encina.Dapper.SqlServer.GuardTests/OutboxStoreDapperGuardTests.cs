using System.Data;
using Encina.Dapper.SqlServer.Outbox;

namespace Encina.Dapper.SqlServer.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="OutboxStoreDapper"/>.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class OutboxStoreDapperGuardTests
{
    /// <summary>
    /// Tests that constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        const string tableName = "OutboxMessages";

        // Act
        var act = () => new OutboxStoreDapper(connection, tableName);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connection");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentNullException when tableName is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTableName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        string tableName = null!;

        // Act
        var act = () => new OutboxStoreDapper(connection, tableName);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tableName");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentException when tableName is empty.
    /// </summary>
    [Fact]
    public void Constructor_EmptyTableName_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        const string tableName = "";

        // Act
        var act = () => new OutboxStoreDapper(connection, tableName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tableName");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentException when tableName is whitespace.
    /// </summary>
    [Fact]
    public void Constructor_WhitespaceTableName_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        const string tableName = "   ";

        // Act
        var act = () => new OutboxStoreDapper(connection, tableName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tableName");
    }

    /// <summary>
    /// Tests that AddAsync throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act
        var act = async () => await store.AddAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("message");
    }

    /// <summary>
    /// Tests that MarkAsFailedAsync throws ArgumentNullException when errorMessage is null.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var messageId = Guid.NewGuid();

        // Act
        var act = async () => await store.MarkAsFailedAsync(
            messageId,
            null!,
            null,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("errorMessage");
    }

    /// <summary>
    /// Tests that MarkAsFailedAsync throws ArgumentException when errorMessage is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_EmptyErrorMessage_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var messageId = Guid.NewGuid();

        // Act
        var act = async () => await store.MarkAsFailedAsync(
            messageId,
            "",
            null,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("errorMessage");
    }

    /// <summary>
    /// Tests that MarkAsFailedAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_EmptyMessageId_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act
        var act = async () => await store.MarkAsFailedAsync(
            Guid.Empty,
            "error",
            null,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    /// <summary>
    /// Tests that MarkAsProcessedAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_EmptyMessageId_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act
        var act = async () => await store.MarkAsProcessedAsync(Guid.Empty, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    /// <summary>
    /// Tests that GetPendingMessagesAsync throws ArgumentException when batchSize is zero or negative.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetPendingMessagesAsync_InvalidBatchSize_ShouldThrowArgumentException(int batchSize)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act
        var act = async () => await store.GetPendingMessagesAsync(
            batchSize,
            3,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(batchSize));
    }

    /// <summary>
    /// Tests that GetPendingMessagesAsync throws ArgumentException when maxRetries is negative.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetPendingMessagesAsync_NegativeMaxRetries_ShouldThrowArgumentException(int maxRetries)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act
        var act = async () => await store.GetPendingMessagesAsync(
            10,
            maxRetries,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(maxRetries));
    }
}
