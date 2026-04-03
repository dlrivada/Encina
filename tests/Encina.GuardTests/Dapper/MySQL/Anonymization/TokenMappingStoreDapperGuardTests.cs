using System.Data;
using Encina.Dapper.MySQL.Anonymization;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.MySQL.Anonymization;

/// <summary>
/// Guard tests for <see cref="TokenMappingStoreDapper"/> to verify null and invalid parameter handling.
/// </summary>
public class TokenMappingStoreDapperGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TokenMappingStoreDapper(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_ValidConnection_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.NotThrow(() => new TokenMappingStoreDapper(connection));
    }

    [Fact]
    public void Constructor_CustomTableName_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.NotThrow(() => new TokenMappingStoreDapper(connection, "CustomTokenMappings"));
    }

    [Fact]
    public async Task StoreAsync_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new TokenMappingStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.StoreAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("mapping");
    }

    [Fact]
    public async Task GetByTokenAsync_NullToken_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new TokenMappingStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetByTokenAsync(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task GetByTokenAsync_WhitespaceToken_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new TokenMappingStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetByTokenAsync("  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task GetByOriginalValueHashAsync_NullHash_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new TokenMappingStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetByOriginalValueHashAsync(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task GetByOriginalValueHashAsync_WhitespaceHash_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new TokenMappingStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetByOriginalValueHashAsync("  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task DeleteByKeyIdAsync_NullKeyId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new TokenMappingStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.DeleteByKeyIdAsync(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task DeleteByKeyIdAsync_WhitespaceKeyId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new TokenMappingStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.DeleteByKeyIdAsync("  ");
        Should.Throw<ArgumentException>(act);
    }
}
