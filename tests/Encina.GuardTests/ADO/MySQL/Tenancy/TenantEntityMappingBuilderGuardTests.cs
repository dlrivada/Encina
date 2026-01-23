using System.Linq.Expressions;
using Encina.ADO.MySQL.Tenancy;

namespace Encina.GuardTests.ADO.MySQL.Tenancy;

/// <summary>
/// Guard tests for <see cref="TenantEntityMappingBuilder{TEntity, TId}"/> to verify null parameter handling.
/// </summary>
[Trait("Category", "Guard")]
public class TenantEntityMappingBuilderGuardTests
{
    /// <summary>
    /// Verifies that the ToTable method throws ArgumentNullException when tableName is null.
    /// </summary>
    [Fact]
    public void ToTable_NullTableName_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderMySQL, Guid>();
        string tableName = null!;

        // Act & Assert
        var act = () => builder.ToTable(tableName);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("tableName");
    }

    /// <summary>
    /// Verifies that the ToTable method throws ArgumentException when tableName is empty.
    /// </summary>
    [Fact]
    public void ToTable_EmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderMySQL, Guid>();
        string tableName = string.Empty;

        // Act & Assert
        var act = () => builder.ToTable(tableName);
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("tableName");
    }

    /// <summary>
    /// Verifies that the HasId method throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void HasId_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderMySQL, Guid>();
        Expression<Func<TenantGuardTestOrderMySQL, Guid>> propertySelector = null!;

        // Act & Assert
        var act = () => builder.HasId(propertySelector);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("propertySelector");
    }

    /// <summary>
    /// Verifies that the HasTenantId method throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void HasTenantId_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderMySQL, Guid>();
        Expression<Func<TenantGuardTestOrderMySQL, string>> propertySelector = null!;

        // Act & Assert
        var act = () => builder.HasTenantId(propertySelector);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("propertySelector");
    }

    /// <summary>
    /// Verifies that the MapProperty method throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void MapProperty_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderMySQL, Guid>();
        Expression<Func<TenantGuardTestOrderMySQL, decimal>> propertySelector = null!;

        // Act & Assert
        var act = () => builder.MapProperty(propertySelector);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("propertySelector");
    }

    /// <summary>
    /// Verifies that the ExcludeFromInsert method throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void ExcludeFromInsert_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderMySQL, Guid>();
        Expression<Func<TenantGuardTestOrderMySQL, decimal>> propertySelector = null!;

        // Act & Assert
        var act = () => builder.ExcludeFromInsert(propertySelector);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("propertySelector");
    }

    /// <summary>
    /// Verifies that the ExcludeFromUpdate method throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void ExcludeFromUpdate_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderMySQL, Guid>();
        Expression<Func<TenantGuardTestOrderMySQL, decimal>> propertySelector = null!;

        // Act & Assert
        var act = () => builder.ExcludeFromUpdate(propertySelector);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("propertySelector");
    }
}

/// <summary>
/// Test entity for TenantEntityMappingBuilder guard tests.
/// </summary>
public class TenantGuardTestOrderMySQL
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = null!;
    public decimal Amount { get; set; }
}
