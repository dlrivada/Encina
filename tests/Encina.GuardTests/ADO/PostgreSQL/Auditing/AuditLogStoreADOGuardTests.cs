using System.Data;
using Encina.ADO.PostgreSQL.Auditing;
using Encina.DomainModeling.Auditing;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.ADO.PostgreSQL.Auditing;

/// <summary>
/// Guard tests for <see cref="AuditLogStoreADO"/> to verify null parameter handling.
/// </summary>
public class AuditLogStoreADOGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new AuditLogStoreADO(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_ValidConnection_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.NotThrow(() => new AuditLogStoreADO(connection));
    }

    [Fact]
    public void Constructor_CustomTableName_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.NotThrow(() => new AuditLogStoreADO(connection, "CustomAuditLogs"));
    }

    [Fact]
    public async Task LogAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditLogStoreADO(connection);

        // Act & Assert
        var act = async () => await store.LogAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entry");
    }

    [Fact]
    public async Task GetHistoryAsync_NullEntityType_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditLogStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetHistoryAsync(null!, "entity-1");
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entityType");
    }

    [Fact]
    public async Task GetHistoryAsync_NullEntityId_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditLogStoreADO(connection);

        // Act & Assert
        var act = async () => await store.GetHistoryAsync("Order", null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entityId");
    }
}
