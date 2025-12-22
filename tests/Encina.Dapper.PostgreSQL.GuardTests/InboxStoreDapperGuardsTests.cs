using System.Data;
using Encina.Dapper.PostgreSQL.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.Dapper.PostgreSQL.GuardTests;

/// <summary>
/// Guard tests for <see cref="InboxStoreDapper"/> to verify null parameter handling.
/// </summary>
public class InboxStoreDapperGuardsTests
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
        var act = () => new InboxStoreDapper(connection);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
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
        var act = () => new InboxStoreDapper(connection, tableName);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tableName");
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
        var act = () => new InboxStoreDapper(connection, tableName);
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
        var act = () => new InboxStoreDapper(connection, tableName);
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
        var store = new InboxStoreDapper(connection);
        IInboxMessage message = null!;

        // Act & Assert
        var act = async () => await store.AddAsync(message);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("message");
    }

    /// <summary>
    /// Verifies that GetMessageAsync throws ArgumentNullException when messageId is null.
    /// </summary>
    [Fact]
    public async Task GetMessageAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);
        string messageId = null!;

        // Act & Assert
        var act = async () => await store.GetMessageAsync(messageId);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that GetMessageAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task GetMessageAsync_EmptyMessageId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);
        var messageId = string.Empty;

        // Act & Assert
        var act = async () => await store.GetMessageAsync(messageId);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that GetMessageAsync throws ArgumentException when messageId is whitespace.
    /// </summary>
    [Fact]
    public async Task GetMessageAsync_WhitespaceMessageId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);
        var messageId = "   ";

        // Act & Assert
        var act = async () => await store.GetMessageAsync(messageId);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that MarkAsProcessedAsync throws ArgumentNullException when messageId is null.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);
        string messageId = null!;

        // Act & Assert
        var act = async () => await store.MarkAsProcessedAsync(messageId, null);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that MarkAsProcessedAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_EmptyMessageId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);
        var messageId = string.Empty;

        // Act & Assert
        var act = async () => await store.MarkAsProcessedAsync(messageId, null);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentNullException when messageId is null.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);
        string messageId = null!;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, "Error", null);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_EmptyMessageId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);
        var messageId = string.Empty;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, "Error", null);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentNullException when errorMessage is null.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);
        var messageId = "test-message-id";
        string errorMessage = null!;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("errorMessage");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentException when errorMessage is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_EmptyErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);
        var messageId = "test-message-id";
        var errorMessage = string.Empty;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("errorMessage");
    }

    /// <summary>
    /// Verifies that GetExpiredMessagesAsync throws ArgumentException when batchSize is zero or negative.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetExpiredMessagesAsync_InvalidBatchSize_ThrowsArgumentException(int batchSize)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetExpiredMessagesAsync(batchSize);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName(nameof(batchSize));
    }

    /// <summary>
    /// Verifies that RemoveExpiredMessagesAsync throws ArgumentNullException when messageIds is null.
    /// </summary>
    [Fact]
    public async Task RemoveExpiredMessagesAsync_NullMessageIds_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);
        IEnumerable<string> messageIds = null!;

        // Act & Assert
        var act = async () => await store.RemoveExpiredMessagesAsync(messageIds);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("messageIds");
    }
}
