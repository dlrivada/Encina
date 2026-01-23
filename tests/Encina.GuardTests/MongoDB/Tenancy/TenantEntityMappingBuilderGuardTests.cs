using Encina.MongoDB.Tenancy;
using Encina.Tenancy;
using Shouldly;

namespace Encina.GuardTests.MongoDB.Tenancy;

/// <summary>
/// Guard clause tests for <see cref="TenantEntityMappingBuilder{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "MongoDB")]
public sealed class TenantEntityMappingBuilderGuardTests
{
    [Fact]
    public void ToCollection_NullCollectionName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrder, Guid>();
        string collectionName = null!;

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.ToCollection(collectionName));
    }

    [Fact]
    public void ToCollection_EmptyCollectionName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrder, Guid>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.ToCollection(string.Empty));
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
    public void MapField_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<TenantGuardTestOrder, Guid>();
        System.Linq.Expressions.Expression<Func<TenantGuardTestOrder, object>> expression = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.MapField(expression));
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
