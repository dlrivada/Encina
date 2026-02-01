using System.Data;
using Encina.Dapper.Sqlite.Auditing;
using Encina.DomainModeling.Auditing;
using NSubstitute;

namespace Encina.GuardTests.Dapper.Sqlite;

/// <summary>
/// Guard tests for <see cref="AuditLogStoreDapper"/> to verify null parameter handling.
/// </summary>
public class AuditLogStoreDapperGuardsTests
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
        var act = () => new AuditLogStoreDapper(connection);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    /// <summary>
    /// Verifies that LogAsync throws ArgumentNullException when entry is null.
    /// </summary>
    [Fact]
    public async Task LogAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditLogStoreDapper(connection);
        AuditLogEntry entry = null!;

        // Act & Assert
        Func<Task> act = () => store.LogAsync(entry);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entry");
    }

    /// <summary>
    /// Verifies that GetHistoryAsync throws ArgumentNullException when entityType is null.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_NullEntityType_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditLogStoreDapper(connection);
        string entityType = null!;
        const string entityId = "123";

        // Act & Assert
        Func<Task> act = () => store.GetHistoryAsync(entityType, entityId);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entityType");
    }

    /// <summary>
    /// Verifies that GetHistoryAsync throws ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_NullEntityId_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditLogStoreDapper(connection);
        const string entityType = "Order";
        string entityId = null!;

        // Act & Assert
        Func<Task> act = () => store.GetHistoryAsync(entityType, entityId);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when tableName is null or empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidTableName_ThrowsArgumentException(string? tableName)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        var act = () => new AuditLogStoreDapper(connection, tableName!);
        Should.Throw<ArgumentException>(act);
    }
}
