using System.Data;
using Encina.ADO.MySQL.Auditing;
using Encina.Security.Audit;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.ADO.MySQL.Auditing;

/// <summary>
/// Guard tests for <see cref="ReadAuditStoreADO"/> to verify null and invalid parameter handling.
/// </summary>
public class ReadAuditStoreADOGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ReadAuditStoreADO(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_ValidConnection_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.NotThrow(() => new ReadAuditStoreADO(connection));
    }

    [Fact]
    public void Constructor_CustomTableName_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.NotThrow(() => new ReadAuditStoreADO(connection, "CustomReadAuditEntries"));
    }

    [Fact]
    public async Task LogReadAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ReadAuditStoreADO(connection);

        // Act & Assert
        var act = async () => await store.LogReadAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entry");
    }

    [Fact]
    public async Task GetAccessHistoryAsync_NullEntityType_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ReadAuditStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetAccessHistoryAsync(null!, "entity-1");
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task GetAccessHistoryAsync_WhitespaceEntityType_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ReadAuditStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetAccessHistoryAsync("  ", "entity-1");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task GetAccessHistoryAsync_NullEntityId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ReadAuditStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetAccessHistoryAsync("Order", null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task GetAccessHistoryAsync_WhitespaceEntityId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ReadAuditStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetAccessHistoryAsync("Order", "  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_NullUserId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ReadAuditStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetUserAccessHistoryAsync(null!, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_WhitespaceUserId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ReadAuditStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetUserAccessHistoryAsync("  ", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task QueryAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ReadAuditStoreADO(connection);

        // Act & Assert
        var act = async () => await store.QueryAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("query");
    }
}
