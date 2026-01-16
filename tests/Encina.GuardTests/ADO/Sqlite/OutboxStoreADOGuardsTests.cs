using System.Data;
using Encina.ADO.Sqlite;
using Encina.ADO.Sqlite.Outbox;
using Encina.Messaging.Outbox;
using Microsoft.Data.Sqlite;

namespace Encina.GuardTests.ADO.Sqlite;

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

        // Act & Assert
        var act = () => new OutboxStoreADO(connection);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when tableName is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTableName_ThrowsArgumentNullException()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        string tableName = null!;

        // Act & Assert
        var act = () => new OutboxStoreADO(connection, tableName);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("tableName");
    }

    /// <summary>
    /// Verifies that AddAsync throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var store = new OutboxStoreADO(connection);
        IOutboxMessage message = null!;

        // Act & Assert
        var act = () => store.AddAsync(message);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("message");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentNullException when errorMessage is null.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var store = new OutboxStoreADO(connection);
        var messageId = Guid.NewGuid();
        string errorMessage = null!;

        // Act & Assert
        var act = () => store.MarkAsFailedAsync(messageId, errorMessage, null);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("errorMessage");
    }
}
