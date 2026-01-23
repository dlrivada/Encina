using System.Linq.Expressions;
using Encina.Dapper.Oracle.Tenancy;
using Encina.Tenancy;

namespace Encina.GuardTests.Dapper.Oracle.Tenancy;

/// <summary>
/// Guard tests for <see cref="TenantEntityMappingBuilder{TEntity, TId}"/> to verify null parameter handling.
/// </summary>
[Trait("Category", "Guard")]
public sealed class TenantEntityMappingBuilderGuardTests
{
    /// <summary>
    /// Verifies that the ToTable method throws ArgumentNullException when tableName is null.
    /// </summary>
    [Fact]
    public void ToTable_NullTableName_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperOracle, Guid>();
        string tableName = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ToTable(tableName));
        ex.ParamName.ShouldBe("tableName");
    }

    /// <summary>
    /// Verifies that the ToTable method throws ArgumentException when tableName is empty.
    /// </summary>
    [Fact]
    public void ToTable_EmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperOracle, Guid>();
        var tableName = string.Empty;

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.ToTable(tableName));
    }

    /// <summary>
    /// Verifies that the HasId method throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void HasId_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperOracle, Guid>();
        Expression<Func<TenantGuardTestOrderDapperOracle, Guid>> propertySelector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.HasId(propertySelector));
        ex.ParamName.ShouldBe("propertySelector");
    }

    /// <summary>
    /// Verifies that the HasTenantId method throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void HasTenantId_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperOracle, Guid>();
        Expression<Func<TenantGuardTestOrderDapperOracle, string>> propertySelector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.HasTenantId(propertySelector));
        ex.ParamName.ShouldBe("propertySelector");
    }

    /// <summary>
    /// Verifies that the MapProperty method throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void MapProperty_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperOracle, Guid>();
        Expression<Func<TenantGuardTestOrderDapperOracle, object>> propertySelector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.MapProperty(propertySelector));
        ex.ParamName.ShouldBe("propertySelector");
    }

    /// <summary>
    /// Verifies that the ExcludeFromInsert method throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void ExcludeFromInsert_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperOracle, Guid>();
        Expression<Func<TenantGuardTestOrderDapperOracle, object>> propertySelector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ExcludeFromInsert(propertySelector));
        ex.ParamName.ShouldBe("propertySelector");
    }

    /// <summary>
    /// Verifies that the ExcludeFromUpdate method throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void ExcludeFromUpdate_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrderDapperOracle, Guid>();
        Expression<Func<TenantGuardTestOrderDapperOracle, object>> propertySelector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ExcludeFromUpdate(propertySelector));
        ex.ParamName.ShouldBe("propertySelector");
    }
}

/// <summary>
/// Test entity for guard tests.
/// </summary>
public sealed class TenantGuardTestOrderDapperOracle : ITenantEntity
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
}
