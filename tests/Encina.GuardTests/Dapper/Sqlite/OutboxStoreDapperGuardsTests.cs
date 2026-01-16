using System.Data;
using Encina.Dapper.Sqlite;
using Encina.Dapper.Sqlite.Outbox;
using Encina.Messaging.Outbox;

namespace Encina.GuardTests.Dapper.Sqlite;

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
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
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
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("tableName");
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
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("tableName");
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
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("tableName");
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
        var act = () => store.AddAsync(message);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("message");
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
        var act = () => store.GetPendingMessagesAsync(batchSize, maxRetries);
        var ex = await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
        ex.ParamName.ShouldBe("batchSize");
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
        var act = () => store.GetPendingMessagesAsync(batchSize, maxRetries);
        var ex = await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
        ex.ParamName.ShouldBe("batchSize");
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
        var act = () => store.GetPendingMessagesAsync(batchSize, maxRetries);
        var ex = await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
        ex.ParamName.ShouldBe("maxRetries");
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
        var act = () => store.MarkAsProcessedAsync(messageId);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("messageId");
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
        var act = () => store.MarkAsFailedAsync(messageId, errorMessage, null);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("messageId");
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
        var act = () => store.MarkAsFailedAsync(messageId, errorMessage, null);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("errorMessage");
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
        var act = () => store.MarkAsFailedAsync(messageId, errorMessage, null);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("errorMessage");
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
        var act = () => store.MarkAsFailedAsync(messageId, errorMessage, null);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("errorMessage");
    }
}
