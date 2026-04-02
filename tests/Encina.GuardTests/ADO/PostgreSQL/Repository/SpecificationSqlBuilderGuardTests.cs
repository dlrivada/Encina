using System.Linq.Expressions;
using Encina.ADO.PostgreSQL.Repository;
using Encina.DomainModeling;
using Shouldly;

namespace Encina.GuardTests.ADO.PostgreSQL.Repository;

/// <summary>
/// Guard clause tests for <see cref="SpecificationSqlBuilder{TEntity}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "ADO.PostgreSQL")]
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
    // ----- GetColumnNameFromSelector guards -----

    /// <summary>
    /// Verifies that GetColumnNameFromSelector throws ArgumentNullException when selector is null.
    /// </summary>
    [Fact]
    public void GetColumnNameFromSelector_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);
        Expression<Func<SpecSqlBuilderTestEntity, string>> selector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.GetColumnNameFromSelector(selector));
        ex.ParamName.ShouldBe("selector");
    }

    /// <summary>
    /// Verifies that GetColumnNameFromSelector returns the mapped column name for a known property.
    /// </summary>
    [Fact]
    public void GetColumnNameFromSelector_KnownProperty_ReturnsMappedColumnName()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);

        // Act
        var result = builder.GetColumnNameFromSelector<string>(e => e.Name);

        // Assert
        result.ShouldBe("Name");
    }

    /// <summary>
    /// Verifies that GetColumnNameFromSelector handles value type boxing (Convert expression).
    /// </summary>
    [Fact]
    public void GetColumnNameFromSelector_ValueTypeProperty_ReturnsMappedColumnName()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);

        // Act
        var result = builder.GetColumnNameFromSelector<decimal>(e => e.Amount);

        // Assert
        result.ShouldBe("Amount");
    }

    // ----- BuildSelectStatement (no specification) guards -----

    /// <summary>
    /// Verifies that BuildSelectStatement without specification generates valid SQL with all columns.
    /// </summary>
    [Fact]
    public void BuildSelectStatement_NoSpecification_GeneratesSqlWithAllColumns()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);

        // Act
        var (sql, _) = builder.BuildSelectStatement("TestTable");

        // Assert
        sql.ShouldContain("SELECT");
        sql.ShouldContain("\"Id\"");
        sql.ShouldContain("\"Name\"");
        sql.ShouldContain("\"Amount\"");
        sql.ShouldContain("\"IsActive\"");
        sql.ShouldContain("TestTable");
    }

    // ----- BuildAggregationSql guards -----

    /// <summary>
    /// Verifies that BuildAggregationSql generates correct SQL without predicate.
    /// </summary>
    [Fact]
    public void BuildAggregationSql_NoPredicate_GeneratesSelectWithAggregate()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);

        // Act
        var (sql, _) = builder.BuildAggregationSql("TestTable", "COUNT(*)", null);

        // Assert
        sql.ShouldBe("SELECT COUNT(*) FROM TestTable");
    }

    /// <summary>
    /// Verifies that BuildAggregationSql generates correct SQL with a predicate.
    /// </summary>
    [Fact]
    public void BuildAggregationSql_WithPredicate_GeneratesWhereClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);
        Expression<Func<SpecSqlBuilderTestEntity, bool>> predicate = e => e.IsActive;

        // Act
        var (sql, _) = builder.BuildAggregationSql("TestTable", "COUNT(*)", predicate);

        // Assert
        sql.ShouldContain("WHERE");
        sql.ShouldContain("\"IsActive\"");
    }

    // ----- BuildWhereClause translation error guards -----

    /// <summary>
    /// Verifies that BuildWhereClause with a valid specification generates parameterized SQL.
    /// </summary>
    [Fact]
    public void BuildWhereClause_ValidSpecification_GeneratesParameterizedSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);
        var spec = new ActiveEntitiesSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("WHERE");
        whereClause.ShouldContain("\"IsActive\"");
    }

    /// <summary>
    /// Verifies that BuildSelectStatement with a Specification generates correct SQL.
    /// </summary>
    [Fact]
    public void BuildSelectStatement_WithSpecification_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<SpecSqlBuilderTestEntity>(ValidColumnMappings);
        var spec = new ActiveEntitiesSpec();

        // Act
        var (sql, _) = builder.BuildSelectStatement("TestTable", spec);

        // Assert
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM \"TestTable\"");
        sql.ShouldContain("WHERE");
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
