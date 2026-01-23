using Encina.ADO.Sqlite.Tenancy;
using Encina.Tenancy;

namespace Encina.GuardTests.ADO.Sqlite.Tenancy;

/// <summary>
/// Guard tests for TenantEntityMappingBuilder to verify null parameter handling.
/// </summary>
[Trait("Category", "Guard")]
public sealed class TenantEntityMappingBuilderGuardTests
{
    [Fact]
    public void ToTable_NullTableName_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrder, Guid>();
        string tableName = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ToTable(tableName));
        ex.ParamName.ShouldBe("tableName");
    }

    [Fact]
    public void ToTable_EmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrder, Guid>();
        var tableName = string.Empty;

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.ToTable(tableName));
    }

    [Fact]
    public void HasId_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrder, Guid>();
        System.Linq.Expressions.Expression<Func<TenantGuardTestOrder, Guid>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.HasId(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void HasTenantId_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrder, Guid>();
        System.Linq.Expressions.Expression<Func<TenantGuardTestOrder, string>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.HasTenantId(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void MapProperty_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrder, Guid>();
        System.Linq.Expressions.Expression<Func<TenantGuardTestOrder, object>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.MapProperty(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void ExcludeFromInsert_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrder, Guid>();
        System.Linq.Expressions.Expression<Func<TenantGuardTestOrder, object>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ExcludeFromInsert(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void ExcludeFromUpdate_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrder, Guid>();
        System.Linq.Expressions.Expression<Func<TenantGuardTestOrder, object>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ExcludeFromUpdate(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }
}

/// <summary>
/// Test entity for guard tests.
/// </summary>
public sealed class TenantGuardTestOrder : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
