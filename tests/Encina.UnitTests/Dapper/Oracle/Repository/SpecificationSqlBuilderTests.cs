using System.Linq.Expressions;
using Encina.Dapper.Oracle.Repository;
using Encina.DomainModeling;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.Oracle.Repository;

/// <summary>
/// Unit tests for <see cref="SpecificationSqlBuilder{TEntity}"/> (Oracle implementation).
/// Tests Oracle-specific SQL syntax with double-quote identifiers and colon parameters.
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
    public void BuildWhereClause_EqualityComparison_GeneratesOracleSyntax()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var customerId = Guid.NewGuid();
        var spec = new CustomerOrdersOracleSpec(customerId);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - Oracle uses double quotes and colon parameters
        whereClause.ShouldBe("WHERE \"CustomerId\" = :p0");
        parameters["p0"].ShouldBe(customerId);
    }

    [Fact]
    public void BuildWhereClause_NotEqualComparison_GeneratesOracleSyntax()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new NotCustomerOracleSpec(Guid.NewGuid());

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - Oracle double-quoted identifiers
        whereClause.ShouldContain("\"CustomerId\" <>");
        parameters.Count.ShouldBe(1);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void BuildWhereClause_GreaterThan_GeneratesOracleSyntax()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new MinTotalOracleSpec(100m);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Total\" >=");
        whereClause.ShouldContain(":p0");
        parameters["p0"].ShouldBe(100m);
    }

    [Fact]
    public void BuildWhereClause_LessThan_GeneratesOracleSyntax()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new MaxTotalOracleSpec(1000m);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Total\" <=");
        whereClause.ShouldContain(":p0");
        parameters["p0"].ShouldBe(1000m);
    }

    #endregion

    #region Boolean Tests

    [Fact]
    public void BuildWhereClause_BooleanProperty_GeneratesOracleSyntax()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new ActiveOrdersOracleSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - Oracle uses NUMBER(1) for booleans, represented as 1
        whereClause.ShouldContain("\"IsActive\" = 1");
    }

    #endregion

    #region Null Check Tests

    [Fact]
    public void BuildWhereClause_NullCheck_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new NullDescriptionOracleSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Description\" IS NULL");
    }

    [Fact]
    public void BuildWhereClause_NotNullCheck_GeneratesIsNotNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new HasDescriptionOracleSpec();

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
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new ActiveOrdersOracleSpec().And(new MinTotalOracleSpec(100m));

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("AND");
    }

    [Fact]
    public void BuildWhereClause_OrCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new ActiveOrdersOracleSpec().Or(new MinTotalOracleSpec(1000m));

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("OR");
    }

    [Fact]
    public void BuildWhereClause_NotOperator_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new ActiveOrdersOracleSpec().Not();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("NOT");
    }

    #endregion

    #region String Method Tests

    [Fact]
    public void BuildWhereClause_StringContains_GeneratesLikeWithColonParam()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new DescriptionContainsOracleSpec("urgent");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - Oracle uses colon parameters
        whereClause.ShouldContain("\"Description\" LIKE :p0");
        parameters["p0"].ShouldBe("%urgent%");
    }

    [Fact]
    public void BuildWhereClause_StringStartsWith_GeneratesLikeWithColonParam()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new DescriptionStartsWithOracleSpec("Priority:");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Description\" LIKE :p0");
        parameters["p0"].ShouldBe("Priority:%");
    }

    [Fact]
    public void BuildWhereClause_StringEndsWith_GeneratesLikeWithColonParam()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new DescriptionEndsWithOracleSpec("completed");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Description\" LIKE :p0");
        parameters["p0"].ShouldBe("%completed");
    }

    #endregion

    #region BuildSelectStatement Tests

    [Fact]
    public void BuildSelectStatement_WithoutSpecification_ReturnsSelectAllWithQuotes()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("Orders");

        // Assert - Oracle uses double quotes for identifiers
        sql.ShouldStartWith("SELECT ");
        sql.ShouldContain("FROM \"Orders\"");
        sql.ShouldNotContain("WHERE");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    public void BuildSelectStatement_WithSpecification_ReturnsSelectWithWhereAndQuotes()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new ActiveOrdersOracleSpec();

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders", spec);

        // Assert
        sql.ShouldStartWith("SELECT ");
        sql.ShouldContain("FROM \"Orders\"");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("\"IsActive\"");
    }

    [Fact]
    public void BuildSelectStatement_ColumnsAreQuoted()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders");

        // Assert - All columns should be double-quoted for Oracle
        sql.ShouldContain("\"Id\"");
        sql.ShouldContain("\"CustomerId\"");
        sql.ShouldContain("\"Total\"");
    }

    #endregion

    #region SQL Injection Prevention Tests

    [Fact]
    public void BuildWhereClause_ParameterizedValues_PreventsSqlInjection()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var maliciousInput = "'; DROP TABLE Orders;--";
        var spec = new DescriptionContainsOracleSpec(maliciousInput);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - The malicious input should be parameterized, not embedded in SQL
        whereClause.ShouldNotContain("DROP TABLE");
        whereClause.ShouldContain(":p0"); // Oracle colon parameter
        var paramValue = parameters["p0"];
        paramValue.ShouldNotBeNull();
        paramValue.ToString()!.ShouldContain(maliciousInput);
    }

    #endregion

    #region Guard Clause Tests

    [Fact]
    public void Constructor_NullColumnMappings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SpecificationSqlBuilder<TestOrderOracle>(null!));
    }

    [Fact]
    public void BuildWhereClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause((Specification<TestOrderOracle>)null!));
    }

    #endregion

    #region Boolean Constants Tests

    [Fact]
    public void BuildWhereClause_BooleanConstantTrue_Generates1Equals1()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new TrueConstantOracleSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=1");
    }

    [Fact]
    public void BuildWhereClause_BooleanConstantFalse_Generates1Equals0()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new FalseConstantOracleSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=0");
    }

    #endregion

    #region Unsupported Expression Tests

    [Fact]
    public void BuildWhereClause_UnsupportedExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new UnsupportedOracleSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    #endregion

    #region String.Equals Method Tests

    [Fact]
    public void BuildWhereClause_StringEquals_GeneratesEqualityClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new DescriptionStringEqualsOracleSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Description\"");
        whereClause.ShouldContain("=");
        whereClause.ShouldContain(":p0");
    }

    [Fact]
    public void BuildWhereClause_StringEqualsNull_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new DescriptionStringEqualsNullOracleSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("IS NULL");
    }

    #endregion
}

#region Test Specifications for Oracle

public class CustomerOrdersOracleSpec : Specification<TestOrderOracle>
{
    private readonly Guid _customerId;
    public CustomerOrdersOracleSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.CustomerId == _customerId;
}

public class NotCustomerOracleSpec : Specification<TestOrderOracle>
{
    private readonly Guid _customerId;
    public NotCustomerOracleSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.CustomerId != _customerId;
}

public class MinTotalOracleSpec : Specification<TestOrderOracle>
{
    private readonly decimal _minTotal;
    public MinTotalOracleSpec(decimal minTotal) => _minTotal = minTotal;
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Total >= _minTotal;
}

public class MaxTotalOracleSpec : Specification<TestOrderOracle>
{
    private readonly decimal _maxTotal;
    public MaxTotalOracleSpec(decimal maxTotal) => _maxTotal = maxTotal;
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Total <= _maxTotal;
}

public class ActiveOrdersOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.IsActive;
}

public class NullDescriptionOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Description == null;
}

public class HasDescriptionOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Description != null;
}

public class DescriptionContainsOracleSpec : Specification<TestOrderOracle>
{
    private readonly string _searchTerm;
    public DescriptionContainsOracleSpec(string searchTerm) => _searchTerm = searchTerm;
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Description != null && o.Description.Contains(_searchTerm);
}

public class DescriptionStartsWithOracleSpec : Specification<TestOrderOracle>
{
    private readonly string _prefix;
    public DescriptionStartsWithOracleSpec(string prefix) => _prefix = prefix;
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Description != null && o.Description.StartsWith(_prefix);
}

public class DescriptionEndsWithOracleSpec : Specification<TestOrderOracle>
{
    private readonly string _suffix;
    public DescriptionEndsWithOracleSpec(string suffix) => _suffix = suffix;
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Description != null && o.Description.EndsWith(_suffix);
}

public class TrueConstantOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => e => true;
}

public class FalseConstantOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => e => false;
}

/// <summary>
/// Specification that causes NotSupportedException (for testing unsupported expressions).
/// Uses an unsupported method call expression (List.Contains).
/// </summary>
public class UnsupportedOracleSpec : Specification<TestOrderOracle>
{
    private readonly List<Guid> _validIds = [Guid.NewGuid()];

    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => e => _validIds.Contains(e.Id);
}

public class DescriptionStringEqualsOracleSpec : Specification<TestOrderOracle>
{
    private readonly string _value;
    public DescriptionStringEqualsOracleSpec(string value) => _value = value;
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => e => e.Description != null && e.Description.Equals(_value, StringComparison.Ordinal);
}

public class DescriptionStringEqualsNullOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => e => e.Description != null && e.Description.Equals(null, StringComparison.Ordinal);
}

#endregion
