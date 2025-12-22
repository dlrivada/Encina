using System.Data;
using Encina.Dapper.Sqlite.Outbox;
using Encina.Messaging.Outbox;

namespace Encina.Dapper.Sqlite.GuardTests;

/// <summary>
/// Guard tests for <see cref="OutboxStoreDapper"/> to verify null parameter handling.
/// </summary>
public class OutboxStoreDapperGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;

        // Act & Assert
        var act = () => new OutboxStoreDapper(connection);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when tableName is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTableName_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        string tableName = null!;

        // Act & Assert
        var act = () => new OutboxStoreDapper(connection, tableName);
        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when tableName is empty.
    /// </summary>
    [Fact]
    public void Constructor_EmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var tableName = string.Empty;

        // Act & Assert
        var act = () => new OutboxStoreDapper(connection, tableName);
        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when tableName is whitespace.
    /// </summary>
    [Fact]
    public void Constructor_WhitespaceTableName_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var tableName = "   ";

        // Act & Assert
        var act = () => new OutboxStoreDapper(connection, tableName);
        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
    }

    /// <summary>
    /// Verifies that AddAsync throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        IOutboxMessage message = null!;

        // Act & Assert
        var act = async () => await store.AddAsync(message);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("message");
    }

    /// <summary>
    /// Verifies that GetPendingMessagesAsync throws ArgumentOutOfRangeException when batchSize is zero.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_ZeroBatchSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var batchSize = 0;
        var maxRetries = 3;

        // Act & Assert
        var act = async () => await store.GetPendingMessagesAsync(batchSize, maxRetries);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("batchSize");
    }

    /// <summary>
    /// Verifies that GetPendingMessagesAsync throws ArgumentOutOfRangeException when batchSize is negative.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_NegativeBatchSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var batchSize = -1;
        var maxRetries = 3;

        // Act & Assert
        var act = async () => await store.GetPendingMessagesAsync(batchSize, maxRetries);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("batchSize");
    }

    /// <summary>
    /// Verifies that GetPendingMessagesAsync throws ArgumentOutOfRangeException when maxRetries is negative.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_NegativeMaxRetries_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var batchSize = 10;
        var maxRetries = -1;

        // Act & Assert
        var act = async () => await store.GetPendingMessagesAsync(batchSize, maxRetries);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("maxRetries");
    }

    /// <summary>
    /// Verifies that MarkAsProcessedAsync throws ArgumentException when messageId is empty Guid.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_EmptyGuidMessageId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var messageId = Guid.Empty;

        // Act & Assert
        var act = async () => await store.MarkAsProcessedAsync(messageId);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentException when messageId is empty Guid.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_EmptyGuidMessageId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var messageId = Guid.Empty;
        var errorMessage = "test error";

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentException when errorMessage is null.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var messageId = Guid.NewGuid();
        string errorMessage = null!;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("errorMessage");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentException when errorMessage is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_EmptyErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var messageId = Guid.NewGuid();
        var errorMessage = string.Empty;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("errorMessage");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentException when errorMessage is whitespace.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_WhitespaceErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var messageId = Guid.NewGuid();
        var errorMessage = "   ";

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("errorMessage");
    }
}
