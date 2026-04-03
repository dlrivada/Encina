using System.Data;
using Encina.Dapper.MySQL.Pagination;
using Encina.DomainModeling.Pagination;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.MySQL.Pagination;

/// <summary>
/// Guard tests for <see cref="CursorPaginationHelper{TEntity}"/> to verify null and invalid parameter handling.
/// </summary>
public class CursorPaginationHelperGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        var encoder = Substitute.For<ICursorEncoder>();

        // Act & Assert
        var act = () => new CursorPaginationHelper<TestEntity>(null!, encoder);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullCursorEncoder_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        var act = () => new CursorPaginationHelper<TestEntity>(connection, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cursorEncoder");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var encoder = Substitute.For<ICursorEncoder>();

        // Act & Assert
        Should.NotThrow(() => new CursorPaginationHelper<TestEntity>(connection, encoder));
    }

    [Fact]
    public async Task ExecuteAsync_NullTableName_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.ExecuteAsync<Guid>(null!, "Id", null, 10, false);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_WhitespaceTableName_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.ExecuteAsync<Guid>("  ", "Id", null, 10, false);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_NullKeyColumn_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.ExecuteAsync<Guid>("Orders", null!, null, 10, false);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_WhitespaceKeyColumn_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.ExecuteAsync<Guid>("Orders", "  ", null, 10, false);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_PageSizeLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.ExecuteAsync<Guid>("Orders", "Id", null, 0, false);
        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task ExecuteCompositeAsync_NullKeyColumns_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.ExecuteCompositeAsync("Orders", null!, null, 10, [false]);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyColumns");
    }

    [Fact]
    public async Task ExecuteCompositeAsync_NullKeyDescending_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.ExecuteCompositeAsync("Orders", ["Id"], null, 10, null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyDescending");
    }

    [Fact]
    public async Task ExecuteCompositeAsync_EmptyKeyColumns_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.ExecuteCompositeAsync("Orders", [], null, 10, []);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("keyColumns");
    }

    private static CursorPaginationHelper<TestEntity> CreateSut()
    {
        var connection = Substitute.For<IDbConnection>();
        var encoder = Substitute.For<ICursorEncoder>();
        return new CursorPaginationHelper<TestEntity>(connection, encoder);
    }

    public class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
