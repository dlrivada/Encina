using System.Data;
using Encina.ADO.PostgreSQL.Outbox;
using Encina.Messaging.Outbox;

namespace Encina.ADO.PostgreSQL.GuardTests;

/// <summary>
/// Guard tests for <see cref="OutboxStoreADO"/> to verify null parameter handling.
/// </summary>
public class OutboxStoreADOGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        var tableName = "OutboxMessages";

        // Act & Assert
        var act = () => new OutboxStoreADO(connection, tableName);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    /// <summary>
    /// Verifies that AddAsync throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreADO(connection);
        IOutboxMessage message = null!;

        // Act & Assert
        var act = async () => await store.AddAsync(message);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("message");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentNullException when errorMessage is null.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreADO(connection);
        var messageId = Guid.NewGuid();
        string errorMessage = null!;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("errorMessage");
    }

    /// <summary>
    /// Verifies that GetPendingMessagesAsync throws ArgumentException when batchSize is negative.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_NegativeBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreADO(connection);
        var batchSize = -1;
        var maxRetries = 3;

        // Act & Assert
        var act = async () => await store.GetPendingMessagesAsync(batchSize, maxRetries);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("batchSize");
    }

    /// <summary>
    /// Verifies that GetPendingMessagesAsync throws ArgumentException when batchSize is zero.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_ZeroBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreADO(connection);
        var batchSize = 0;
        var maxRetries = 3;

        // Act & Assert
        var act = async () => await store.GetPendingMessagesAsync(batchSize, maxRetries);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("batchSize");
    }

    /// <summary>
    /// Verifies that GetPendingMessagesAsync throws ArgumentException when maxRetries is negative.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_NegativeMaxRetries_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreADO(connection);
        var batchSize = 10;
        var maxRetries = -1;

        // Act & Assert
        var act = async () => await store.GetPendingMessagesAsync(batchSize, maxRetries);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("maxRetries");
    }
}
