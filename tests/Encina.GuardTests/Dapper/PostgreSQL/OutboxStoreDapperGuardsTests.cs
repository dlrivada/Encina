using System.Data;
using Encina.Dapper.PostgreSQL;
using Encina.Dapper.PostgreSQL.Outbox;
using Encina.Messaging.Outbox;

namespace Encina.GuardTests.Dapper.PostgreSQL;

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
        ex.ParamName.ShouldBe(nameof(connection));
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when tableName is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTableName_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        string tableName = null!;

        // Act & Assert
        var act = () => new OutboxStoreDapper(connection, tableName);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe(nameof(tableName));
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
        ex.ParamName.ShouldBe(nameof(tableName));
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
        ex.ParamName.ShouldBe(nameof(tableName));
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
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe(nameof(message));
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentNullException when errorMessage is null.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var messageId = Guid.NewGuid();
        string errorMessage = null!;

        // Act & Assert
        var act = () => store.MarkAsFailedAsync(messageId, errorMessage, null);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe(nameof(errorMessage));
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
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe(nameof(errorMessage));
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
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe(nameof(errorMessage));
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_EmptyMessageId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var messageId = Guid.Empty;
        var errorMessage = "Test error";

        // Act & Assert
        var act = () => store.MarkAsFailedAsync(messageId, errorMessage, null);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe(nameof(messageId));
    }

    /// <summary>
    /// Verifies that MarkAsProcessedAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_EmptyMessageId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var messageId = Guid.Empty;

        // Act & Assert
        var act = () => store.MarkAsProcessedAsync(messageId);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe(nameof(messageId));
    }

    /// <summary>
    /// Verifies that GetPendingMessagesAsync throws ArgumentException when batchSize is zero or negative.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetPendingMessagesAsync_InvalidBatchSize_ThrowsArgumentException(int batchSize)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act & Assert
        var act = () => store.GetPendingMessagesAsync(batchSize, 3);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe(nameof(batchSize));
    }

    /// <summary>
    /// Verifies that GetPendingMessagesAsync throws ArgumentException when maxRetries is negative.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetPendingMessagesAsync_NegativeMaxRetries_ThrowsArgumentException(int maxRetries)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act & Assert
        var act = () => store.GetPendingMessagesAsync(10, maxRetries);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe(nameof(maxRetries));
    }
}
