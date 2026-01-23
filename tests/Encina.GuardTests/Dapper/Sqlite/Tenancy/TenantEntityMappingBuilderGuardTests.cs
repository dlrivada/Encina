using Encina.Dapper.Sqlite.Tenancy;
using Encina.Tenancy;

namespace Encina.GuardTests.Dapper.Sqlite.Tenancy;

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
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperSqlite, Guid>();
        string tableName = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ToTable(tableName));
        ex.ParamName.ShouldBe("tableName");
    }

    [Fact]
    public void ToTable_EmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperSqlite, Guid>();
        var tableName = string.Empty;

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.ToTable(tableName));
    }

    [Fact]
    public void HasId_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperSqlite, Guid>();
        System.Linq.Expressions.Expression<Func<TenantGuardTestOrderDapperSqlite, Guid>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.HasId(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void HasTenantId_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperSqlite, Guid>();
        System.Linq.Expressions.Expression<Func<TenantGuardTestOrderDapperSqlite, string>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.HasTenantId(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void MapProperty_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperSqlite, Guid>();
        System.Linq.Expressions.Expression<Func<TenantGuardTestOrderDapperSqlite, object>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.MapProperty(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void ExcludeFromInsert_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperSqlite, Guid>();
        System.Linq.Expressions.Expression<Func<TenantGuardTestOrderDapperSqlite, object>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ExcludeFromInsert(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void ExcludeFromUpdate_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperSqlite, Guid>();
        System.Linq.Expressions.Expression<Func<TenantGuardTestOrderDapperSqlite, object>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ExcludeFromUpdate(expression));
        ex.ParamName.ShouldBe("propertySelector");
    }
}

/// <summary>
/// Test entity for guard tests.
/// </summary>
public sealed class TenantGuardTestOrderDapperSqlite : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
