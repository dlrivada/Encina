using System.Linq.Expressions;
using Encina.Dapper.PostgreSQL.Repository;
using Encina.DomainModeling;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.PostgreSQL.Repository;

/// <summary>
/// Unit tests for <see cref="SpecificationSqlBuilder{TEntity}"/>.
/// Tests PostgreSQL-specific SQL generation with double-quote identifier quoting.
/// </summary>
[Trait("Category", "Unit")]
public class SpecificationSqlBuilderTests
{
    private readonly IReadOnlyDictionary<string, string> _columnMappings = new Dictionary<string, string>
    {
        ["Id"] = "Id",
        ["CustomerId"] = "CustomerId",
        ["Total"] = "Total",
        ["IsActive"] = "IsActive",
        ["Description"] = "Description",
        ["CreatedAtUtc"] = "CreatedAtUtc"
    };

    #region Equality Tests

    [Fact]
    public void BuildWhereClause_EqualityComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var customerId = Guid.NewGuid();
        var spec = new CustomerOrdersPgSpec(customerId);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldBe("WHERE \"CustomerId\" = @p0");
        parameters["p0"].ShouldBe(customerId);
    }

    [Fact]
    public void BuildWhereClause_NotEqualComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new NotCustomerPgSpec(Guid.NewGuid());

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"CustomerId\" <>");
        parameters.Count.ShouldBe(1);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void BuildWhereClause_GreaterThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new MinTotalPgSpec(100m);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Total\" >=");
        parameters["p0"].ShouldBe(100m);
    }

    [Fact]
    public void BuildWhereClause_LessThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new MaxTotalPgSpec(1000m);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Total\" <=");
        parameters["p0"].ShouldBe(1000m);
    }

    #endregion

    #region Boolean Tests

    [Fact]
    public void BuildWhereClause_BooleanProperty_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new ActiveOrdersPgSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        // PostgreSQL uses native boolean with TRUE literal
        whereClause.ShouldContain("\"IsActive\" = TRUE");
    }

    #endregion

    #region Null Check Tests

    [Fact]
    public void BuildWhereClause_NullCheck_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new NullDescriptionPgSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Description\" IS NULL");
    }

    [Fact]
    public void BuildWhereClause_NotNullCheck_GeneratesIsNotNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new HasDescriptionPgSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Description\" IS NOT NULL");
    }

    #endregion

    #region Logical Operator Tests

    [Fact]
    public void BuildWhereClause_AndCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new ActiveOrdersPgSpec().And(new MinTotalPgSpec(100m));

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("AND");
    }

    [Fact]
    public void BuildWhereClause_OrCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new ActiveOrdersPgSpec().Or(new MinTotalPgSpec(1000m));

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("OR");
    }

    [Fact]
    public void BuildWhereClause_NotOperator_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new ActiveOrdersPgSpec().Not();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("NOT");
    }

    #endregion

    #region String Method Tests

    [Fact]
    public void BuildWhereClause_StringContains_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new DescriptionContainsPgSpec("urgent");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Description\" LIKE @p0");
        parameters["p0"].ShouldBe("%urgent%");
    }

    [Fact]
    public void BuildWhereClause_StringStartsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new DescriptionStartsWithPgSpec("Priority:");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Description\" LIKE @p0");
        parameters["p0"].ShouldBe("Priority:%");
    }

    [Fact]
    public void BuildWhereClause_StringEndsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new DescriptionEndsWithPgSpec("completed");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Description\" LIKE @p0");
        parameters["p0"].ShouldBe("%completed");
    }

    #endregion

    #region BuildSelectStatement Tests

    [Fact]
    public void BuildSelectStatement_WithoutSpecification_ReturnsSelectAll()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("orders");

        // Assert
        sql.ShouldStartWith("SELECT ");
        sql.ShouldContain("FROM \"orders\"");
        sql.ShouldNotContain("WHERE");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    public void BuildSelectStatement_WithSpecification_ReturnsSelectWithWhere()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new ActiveOrdersPgSpec();

        // Act
        var (sql, _) = builder.BuildSelectStatement("orders", spec);

        // Assert
        sql.ShouldStartWith("SELECT ");
        sql.ShouldContain("FROM \"orders\"");
        sql.ShouldContain("WHERE");
    }

    [Fact]
    public void BuildSelectStatement_UsesDoubleQuoteIdentifiers()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);

        // Act
        var (sql, _) = builder.BuildSelectStatement("orders");

        // Assert
        // PostgreSQL uses double-quotes for identifiers
        sql.ShouldContain("\"Id\"");
        sql.ShouldContain("\"CustomerId\"");
        sql.ShouldContain("\"orders\"");
    }

    #endregion

    #region SQL Injection Prevention Tests

    [Fact]
    public void BuildWhereClause_ParameterizedValues_PreventsSqlInjection()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var maliciousInput = "'; DROP TABLE orders;--";
        var spec = new DescriptionContainsPgSpec(maliciousInput);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        // The malicious input should be parameterized, not embedded in SQL
        whereClause.ShouldNotContain("DROP TABLE");
        whereClause.ShouldContain("@p0");
        var paramValue = parameters["p0"];
        paramValue.ShouldNotBeNull();
        paramValue.ToString()!.ShouldContain(maliciousInput);
    }

    #endregion

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_NullColumnMappings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SpecificationSqlBuilder<TestOrderPg>(null!));
    }

    [Fact]
    public void BuildWhereClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause((Specification<TestOrderPg>)null!));
    }

    #endregion

    #region Boolean Constant Tests

    [Fact]
    public void BuildWhereClause_BooleanConstantTrue_GeneratesTrue()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new TrueConstantPgSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        // PostgreSQL uses native boolean TRUE literal
        whereClause.ShouldContain("TRUE");
    }

    [Fact]
    public void BuildWhereClause_BooleanConstantFalse_GeneratesFalse()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new FalseConstantPgSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        // PostgreSQL uses native boolean FALSE literal
        whereClause.ShouldContain("FALSE");
    }

    #endregion
}

#region Test Specifications

public class CustomerOrdersPgSpec : Specification<TestOrderPg>
{
    private readonly Guid _customerId;
    public CustomerOrdersPgSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.CustomerId == _customerId;
}

public class NotCustomerPgSpec : Specification<TestOrderPg>
{
    private readonly Guid _customerId;
    public NotCustomerPgSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.CustomerId != _customerId;
}

public class MinTotalPgSpec : Specification<TestOrderPg>
{
    private readonly decimal _minTotal;
    public MinTotalPgSpec(decimal minTotal) => _minTotal = minTotal;
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Total >= _minTotal;
}

public class MaxTotalPgSpec : Specification<TestOrderPg>
{
    private readonly decimal _maxTotal;
    public MaxTotalPgSpec(decimal maxTotal) => _maxTotal = maxTotal;
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Total <= _maxTotal;
}

public class ActiveOrdersPgSpec : Specification<TestOrderPg>
{
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.IsActive;
}

public class NullDescriptionPgSpec : Specification<TestOrderPg>
{
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Description == null;
}

public class HasDescriptionPgSpec : Specification<TestOrderPg>
{
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Description != null;
}

public class DescriptionContainsPgSpec : Specification<TestOrderPg>
{
    private readonly string _searchTerm;
    public DescriptionContainsPgSpec(string searchTerm) => _searchTerm = searchTerm;
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Description != null && o.Description.Contains(_searchTerm);
}

public class DescriptionStartsWithPgSpec : Specification<TestOrderPg>
{
    private readonly string _prefix;
    public DescriptionStartsWithPgSpec(string prefix) => _prefix = prefix;
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Description != null && o.Description.StartsWith(_prefix);
}

public class DescriptionEndsWithPgSpec : Specification<TestOrderPg>
{
    private readonly string _suffix;
    public DescriptionEndsWithPgSpec(string suffix) => _suffix = suffix;
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Description != null && o.Description.EndsWith(_suffix);
}

public class TrueConstantPgSpec : Specification<TestOrderPg>
{
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => _ => true;
}

public class FalseConstantPgSpec : Specification<TestOrderPg>
{
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => _ => false;
}

#endregion
