using System.Data;
using Encina.Dapper.MySQL.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.Dapper.MySQL.GuardTests;

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
    /// Verifies that MarkAsProcessedAsync throws ArgumentNullException when messageId is null.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);
        string messageId = null!;
        var response = "test response";

        // Act & Assert
        var act = async () => await store.MarkAsProcessedAsync(messageId, response);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("messageId");
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
        var errorMessage = "test error";

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("messageId");
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
