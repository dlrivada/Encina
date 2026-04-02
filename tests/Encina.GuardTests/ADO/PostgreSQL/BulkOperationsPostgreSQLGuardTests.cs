using System.Data;
using Encina.ADO.PostgreSQL.BulkOperations;
using Encina.ADO.PostgreSQL.Repository;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.ADO.PostgreSQL;

/// <summary>
/// Guard tests for <see cref="BulkOperationsPostgreSQL{TEntity, TId}"/> to verify null parameter handling.
/// </summary>
public class BulkOperationsPostgreSQLGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = Substitute.For<IEntityMapping<TestEntity, Guid>>();
        mapping.ColumnMappings.Returns(new Dictionary<string, string>());

        // Act & Assert
        var act = () => new BulkOperationsPostgreSQL<TestEntity, Guid>(null!, mapping);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        var act = () => new BulkOperationsPostgreSQL<TestEntity, Guid>(connection, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("mapping");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var mapping = Substitute.For<IEntityMapping<TestEntity, Guid>>();
        mapping.ColumnMappings.Returns(new Dictionary<string, string>());

        // Act & Assert
        Should.NotThrow(() => new BulkOperationsPostgreSQL<TestEntity, Guid>(connection, mapping));
    }

    [Fact]
    public async Task BulkInsertAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.BulkInsertAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entities");
    }

    [Fact]
    public async Task BulkInsertAsync_EmptyEntities_ReturnsZero()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.BulkInsertAsync(Array.Empty<TestEntity>());

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: count => count.ShouldBe(0), Left: _ => { });
    }

    [Fact]
    public async Task BulkUpdateAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.BulkUpdateAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entities");
    }

    [Fact]
    public async Task BulkUpdateAsync_EmptyEntities_ReturnsZero()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.BulkUpdateAsync(Array.Empty<TestEntity>());

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: count => count.ShouldBe(0), Left: _ => { });
    }

    [Fact]
    public async Task BulkDeleteAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.BulkDeleteAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entities");
    }

    [Fact]
    public async Task BulkDeleteAsync_EmptyEntities_ReturnsZero()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.BulkDeleteAsync(Array.Empty<TestEntity>());

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: count => count.ShouldBe(0), Left: _ => { });
    }

    [Fact]
    public async Task BulkMergeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.BulkMergeAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entities");
    }

    [Fact]
    public async Task BulkMergeAsync_EmptyEntities_ReturnsZero()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.BulkMergeAsync(Array.Empty<TestEntity>());

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: count => count.ShouldBe(0), Left: _ => { });
    }

    [Fact]
    public async Task BulkReadAsync_NullIds_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.BulkReadAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("ids");
    }

    [Fact]
    public async Task BulkReadAsync_EmptyIds_ReturnsEmptyList()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.BulkReadAsync(Array.Empty<object>());

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: list => list.Count.ShouldBe(0), Left: _ => { });
    }

    private static BulkOperationsPostgreSQL<TestEntity, Guid> CreateSut()
    {
        var connection = Substitute.For<IDbConnection>();
        var mapping = Substitute.For<IEntityMapping<TestEntity, Guid>>();
        mapping.ColumnMappings.Returns(new Dictionary<string, string>());
        return new BulkOperationsPostgreSQL<TestEntity, Guid>(connection, mapping);
    }

    public class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
