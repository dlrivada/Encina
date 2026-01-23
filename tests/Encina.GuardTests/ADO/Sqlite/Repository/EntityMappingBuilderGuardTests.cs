using Encina.ADO.Sqlite.Repository;

namespace Encina.GuardTests.ADO.Sqlite.Repository;

/// <summary>
/// Guard tests for EntityMappingBuilder to verify null parameter handling.
/// </summary>
[Trait("Category", "Guard")]
public sealed class EntityMappingBuilderGuardTests
{
    [Fact]
    public void ToTable_NullTableName_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<RepositoryGuardTestEntity, Guid>();
        string tableName = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ToTable(tableName));
        ex.ParamName.ShouldBe("tableName");
    }

    [Fact]
    public void ToTable_EmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<RepositoryGuardTestEntity, Guid>();
        var tableName = string.Empty;

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.ToTable(tableName));
    }

    [Fact]
    public void HasId_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<RepositoryGuardTestEntity, Guid>();
        System.Linq.Expressions.Expression<Func<RepositoryGuardTestEntity, Guid>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.HasId(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void MapProperty_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<RepositoryGuardTestEntity, Guid>();
        System.Linq.Expressions.Expression<Func<RepositoryGuardTestEntity, object>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.MapProperty(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void ExcludeFromInsert_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<RepositoryGuardTestEntity, Guid>();
        System.Linq.Expressions.Expression<Func<RepositoryGuardTestEntity, object>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ExcludeFromInsert(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void ExcludeFromUpdate_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<RepositoryGuardTestEntity, Guid>();
        System.Linq.Expressions.Expression<Func<RepositoryGuardTestEntity, object>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ExcludeFromUpdate(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }
}

/// <summary>
/// Test entity for repository guard tests.
/// </summary>
public sealed class RepositoryGuardTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
