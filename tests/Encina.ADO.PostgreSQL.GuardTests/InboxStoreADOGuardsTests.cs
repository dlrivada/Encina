using System.Data;
using Encina.ADO.PostgreSQL.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.ADO.PostgreSQL.GuardTests;

/// <summary>
/// Guard tests for <see cref="InboxStoreADO"/> to verify null parameter handling.
/// </summary>
public class InboxStoreADOGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        var tableName = "InboxMessages";

        // Act & Assert
        var act = () => new InboxStoreADO(connection, tableName);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    /// <summary>
    /// Verifies that GetMessageAsync throws ArgumentNullException when messageId is null.
    /// </summary>
    [Fact]
    public async Task GetMessageAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreADO(connection);
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
        var store = new InboxStoreADO(connection);
        var messageId = string.Empty;

        // Act & Assert
        var act = async () => await store.GetMessageAsync(messageId);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that AddAsync throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreADO(connection);
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
        var store = new InboxStoreADO(connection);
        string messageId = null!;

        // Act & Assert
        var act = async () => await store.MarkAsProcessedAsync(messageId, "response");
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
        var store = new InboxStoreADO(connection);
        var messageId = string.Empty;

        // Act & Assert
        var act = async () => await store.MarkAsProcessedAsync(messageId, "response");
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
        var store = new InboxStoreADO(connection);
        string messageId = null!;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, "error", null);
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
        var store = new InboxStoreADO(connection);
        var messageId = string.Empty;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, "error", null);
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
        var store = new InboxStoreADO(connection);
        var messageId = "msg-123";
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
        var store = new InboxStoreADO(connection);
        IEnumerable<string> messageIds = null!;

        // Act & Assert
        var act = async () => await store.RemoveExpiredMessagesAsync(messageIds);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("messageIds");
    }
}
