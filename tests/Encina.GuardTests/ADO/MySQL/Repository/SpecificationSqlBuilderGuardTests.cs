using System.Linq.Expressions;
using Encina.ADO.MySQL.Repository;
using Encina.DomainModeling;
using Shouldly;

namespace Encina.GuardTests.ADO.MySQL.Repository;

/// <summary>
/// Guard clause tests for <see cref="SpecificationSqlBuilder{TEntity}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "ADO.MySQL")]
public sealed class SpecificationSqlBuilderGuardTests
{
    private static readonly IReadOnlyDictionary<string, string> ValidColumnMappings =
        new Dictionary<string, string>
        {
            ["Id"] = "Id",
            ["Name"] = "Name",
            ["Amount"] = "Amount",
            ["IsActive"] = "IsActive"
        };

    [Fact]
    public void Constructor_NullColumnMappings_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyDictionary<string, string> columnMappings = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(columnMappings));
        ex.ParamName.ShouldBe("columnMappings");
    }

    [Fact]
    public void BuildWhereClause_Specification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);
        Specification<SpecSqlBuilderTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public void BuildWhereClause_QuerySpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);
        QuerySpecification<SpecSqlBuilderTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public void BuildOrderByClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);
        IQuerySpecification<SpecSqlBuilderTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildOrderByClause(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public void BuildPaginationClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);
        IQuerySpecification<SpecSqlBuilderTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildPaginationClause(specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public void BuildSelectStatement_WithSpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);
        Specification<SpecSqlBuilderTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildSelectStatement("TestTable", specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public void BuildSelectStatement_WithQuerySpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);
        QuerySpecification<SpecSqlBuilderTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildSelectStatement("TestTable", specification));
        ex.ParamName.ShouldBe("specification");
    }
}

/// <summary>
/// Test entity for specification SQL builder guard tests.
/// </summary>
public sealed class SpecSqlBuilderTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Simple specification for testing guard clauses.
/// </summary>
public sealed class ActiveEntitiesSpec : Specification<SpecSqlBuilderTestEntity>
{
    public override Expression<Func<SpecSqlBuilderTestEntity, bool>> ToExpression()
        => entity => entity.IsActive;
}
