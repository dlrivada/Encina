using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.MongoDB.Repository;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.MongoDB.Repository;

/// <summary>
/// Guard clause tests for <see cref="SpecificationFilterBuilder{TEntity}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "MongoDB")]
public sealed class SpecificationFilterBuilderGuardTests
{
    [Fact]
    public void BuildFilter_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationFilterBuilder<MongoFilterBuilderTestEntity>();
        Specification<MongoFilterBuilderTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildFilter(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public void BuildFilter_WithQuerySpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationFilterBuilder<MongoFilterBuilderTestEntity>();
        QuerySpecification<MongoFilterBuilderTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildFilter(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public void BuildFilterOrEmpty_NullSpecification_ReturnsEmptyFilter()
    {
        // Arrange
        var builder = new SpecificationFilterBuilder<MongoFilterBuilderTestEntity>();
        Specification<MongoFilterBuilderTestEntity>? specification = null;

        // Act
        var result = builder.BuildFilterOrEmpty(specification);

        // Assert - Empty filter should be returned, not throw
        result.ShouldNotBeNull();
    }

    [Fact]
    public void BuildSortDefinition_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationFilterBuilder<MongoFilterBuilderTestEntity>();
        IQuerySpecification<MongoFilterBuilderTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildSortDefinition(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public void BuildKeysetFilter_NullKeysetProperty_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationFilterBuilder<MongoFilterBuilderTestEntity>();
        Expression<Func<MongoFilterBuilderTestEntity, object>> keysetProperty = null!;
        var lastKeyValue = Guid.NewGuid();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildKeysetFilter(keysetProperty, lastKeyValue));
        ex.ParamName.ShouldBe("keysetProperty");
    }

    [Fact]
    public void BuildKeysetFilter_NullLastKeyValue_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationFilterBuilder<MongoFilterBuilderTestEntity>();
        Expression<Func<MongoFilterBuilderTestEntity, object>> keysetProperty = e => e.Id;
        object lastKeyValue = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildKeysetFilter(keysetProperty, lastKeyValue));
        ex.ParamName.ShouldBe("lastKeyValue");
    }

    [Fact]
    public void BuildFilter_ValidSpecification_DoesNotThrow()
    {
        // Arrange
        var builder = new SpecificationFilterBuilder<MongoFilterBuilderTestEntity>();
        var specification = new MongoFilterBuilderActiveEntitiesSpec();

        // Act & Assert - Should not throw
        Should.NotThrow(() => builder.BuildFilter(specification));
    }

    [Fact]
    public void BuildKeysetFilter_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var builder = new SpecificationFilterBuilder<MongoFilterBuilderTestEntity>();
        Expression<Func<MongoFilterBuilderTestEntity, object>> keysetProperty = e => e.Id;
        var lastKeyValue = Guid.NewGuid();

        // Act & Assert - Should not throw
        var result = Should.NotThrow(() => builder.BuildKeysetFilter(keysetProperty, lastKeyValue));
        result.ShouldNotBeNull();
    }
}

/// <summary>
/// Test entity for MongoDB filter builder guard tests.
/// </summary>
public sealed class MongoFilterBuilderTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Simple specification for testing filter builder guard clauses.
/// </summary>
public sealed class MongoFilterBuilderActiveEntitiesSpec : Specification<MongoFilterBuilderTestEntity>
{
    public override Expression<Func<MongoFilterBuilderTestEntity, bool>> ToExpression()
        => entity => entity.IsActive;
}
