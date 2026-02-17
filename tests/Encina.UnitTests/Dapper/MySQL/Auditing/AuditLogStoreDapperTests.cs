using System.Data;
using Encina.Dapper.MySQL.Auditing;
using Encina.DomainModeling.Auditing;
using NSubstitute;

namespace Encina.UnitTests.Dapper.MySQL.Auditing;

/// <summary>
/// Unit tests for <see cref="AuditLogStoreDapper"/>.
/// </summary>
public sealed class AuditLogStoreDapperTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AuditLogStoreDapper(null!));
    }

    [Fact]
    public void Constructor_WithValidConnection_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var store = new AuditLogStoreDapper(connection);

        // Assert
        store.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomTableName_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var store = new AuditLogStoreDapper(connection, "CustomAuditLogs");

        // Assert
        store.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidTableName_ThrowsArgumentException(string? tableName)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new AuditLogStoreDapper(connection, tableName!));
    }

    [Theory]
    [InlineData("DROP TABLE;")]
    [InlineData("audit--logs")]
    [InlineData("audit;logs")]
    public void Constructor_MaliciousTableName_ThrowsArgumentException(string tableName)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new AuditLogStoreDapper(connection, tableName));
    }

    #endregion

    #region LogAsync Validation Tests

    [Fact]
    public async Task LogAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditLogStoreDapper(connection);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentNullException>(
            async () => await store.LogAsync(null!));

        exception.ParamName.ShouldBe("entry");
    }

    #endregion

    #region GetHistoryAsync Validation Tests

    [Fact]
    public async Task GetHistoryAsync_NullEntityType_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditLogStoreDapper(connection);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentNullException>(
            async () => await store.GetHistoryAsync(null!, "123"));

        exception.ParamName.ShouldBe("entityType");
    }

    [Fact]
    public async Task GetHistoryAsync_NullEntityId_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditLogStoreDapper(connection);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentNullException>(
            async () => await store.GetHistoryAsync("Order", null!));

        exception.ParamName.ShouldBe("entityId");
    }

    #endregion

    #region Helper for Entry Creation

    private static AuditLogEntry CreateTestEntry(
        string entityType = "Order",
        string entityId = "order-123") =>
        new(
            Id: Guid.NewGuid().ToString(),
            EntityType: entityType,
            EntityId: entityId,
            Action: AuditAction.Created,
            UserId: "test-user",
            TimestampUtc: DateTime.UtcNow,
            OldValues: null,
            NewValues: "{\"Name\":\"Test\"}",
            CorrelationId: Guid.NewGuid().ToString());

    #endregion
}
