using System.Data;
using Encina.Dapper.MySQL.Auditing;
using Encina.Security.Audit;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.MySQL.Auditing;

/// <summary>
/// Guard tests for <see cref="AuditStoreDapper"/> to verify null and invalid parameter handling.
/// </summary>
public class AuditStoreDapperGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new AuditStoreDapper(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_ValidConnection_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.NotThrow(() => new AuditStoreDapper(connection));
    }

    [Fact]
    public void Constructor_CustomTableName_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.NotThrow(() => new AuditStoreDapper(connection, "CustomAuditEntries"));
    }

    [Fact]
    public async Task RecordAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.RecordAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entry");
    }

    [Fact]
    public async Task GetByEntityAsync_NullEntityType_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetByEntityAsync(null!, null);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task GetByEntityAsync_WhitespaceEntityType_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetByEntityAsync("  ", null);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task GetByUserAsync_NullUserId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetByUserAsync(null!, null, null);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task GetByUserAsync_WhitespaceUserId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetByUserAsync("  ", null, null);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_NullCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetByCorrelationIdAsync(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_WhitespaceCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetByCorrelationIdAsync("  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task QueryAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new AuditStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.QueryAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("query");
    }
}
