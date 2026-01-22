using System.Linq.Expressions;
using Encina.Dapper.MySQL.Repository;
using Encina.DomainModeling;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.MySQL.Repository;

/// <summary>
/// Unit tests for <see cref="SpecificationSqlBuilder{TEntity}"/> (MySQL implementation).
/// Verifies MySQL-specific SQL syntax with backtick identifiers.
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
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var customerId = Guid.NewGuid();
        var spec = new CustomerOrdersMySQLSpec(customerId);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks for identifiers
        whereClause.ShouldBe("WHERE `CustomerId` = @p0");
        parameters["p0"].ShouldBe(customerId);
    }

    [Fact]
    public void BuildWhereClause_NotEqualComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new NotCustomerMySQLSpec(Guid.NewGuid());

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("`CustomerId` <>");
        parameters.Count.ShouldBe(1);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void BuildWhereClause_GreaterThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new MinTotalMySQLSpec(100m);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("`Total` >=");
        parameters["p0"].ShouldBe(100m);
    }

    [Fact]
    public void BuildWhereClause_LessThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new MaxTotalMySQLSpec(1000m);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("`Total` <=");
        parameters["p0"].ShouldBe(1000m);
    }

    #endregion

    #region Boolean Tests

    [Fact]
    public void BuildWhereClause_BooleanProperty_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new ActiveOrdersMySQLSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses TINYINT(1) for booleans (0/1)
        whereClause.ShouldContain("`IsActive` = 1");
    }

    #endregion

    #region Null Check Tests

    [Fact]
    public void BuildWhereClause_NullCheck_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new NullDescriptionMySQLSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("`Description` IS NULL");
    }

    [Fact]
    public void BuildWhereClause_NotNullCheck_GeneratesIsNotNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new HasDescriptionMySQLSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("`Description` IS NOT NULL");
    }

    #endregion

    #region Logical Operator Tests

    [Fact]
    public void BuildWhereClause_AndCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new ActiveOrdersMySQLSpec().And(new MinTotalMySQLSpec(100m));

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("AND");
    }

    [Fact]
    public void BuildWhereClause_OrCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new ActiveOrdersMySQLSpec().Or(new MinTotalMySQLSpec(1000m));

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("OR");
    }

    [Fact]
    public void BuildWhereClause_NotOperator_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new ActiveOrdersMySQLSpec().Not();

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
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new DescriptionContainsMySQLSpec("urgent");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("`Description` LIKE @p0");
        parameters["p0"].ShouldBe("%urgent%");
    }

    [Fact]
    public void BuildWhereClause_StringStartsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new DescriptionStartsWithMySQLSpec("Priority:");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("`Description` LIKE @p0");
        parameters["p0"].ShouldBe("Priority:%");
    }

    [Fact]
    public void BuildWhereClause_StringEndsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new DescriptionEndsWithMySQLSpec("completed");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("`Description` LIKE @p0");
        parameters["p0"].ShouldBe("%completed");
    }

    #endregion

    #region BuildSelectStatement Tests

    [Fact]
    public void BuildSelectStatement_WithoutSpecification_ReturnsSelectAll()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("Orders");

        // Assert - MySQL uses backticks
        sql.ShouldStartWith("SELECT ");
        sql.ShouldContain("FROM `Orders`");
        sql.ShouldNotContain("WHERE");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    public void BuildSelectStatement_WithSpecification_ReturnsSelectWithWhere()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var spec = new ActiveOrdersMySQLSpec();

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders", spec);

        // Assert - MySQL uses backticks
        sql.ShouldStartWith("SELECT ");
        sql.ShouldContain("FROM `Orders`");
        sql.ShouldContain("WHERE");
    }

    [Fact]
    public void BuildSelectStatement_ColumnsUseBackticks()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders");

        // Assert - MySQL column identifiers should use backticks
        sql.ShouldContain("`Id`");
        sql.ShouldContain("`CustomerId`");
        sql.ShouldContain("`Total`");
    }

    #endregion

    #region SQL Injection Prevention Tests

    [Fact]
    public void BuildWhereClause_ParameterizedValues_PreventsSqlInjection()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);
        var maliciousInput = "'; DROP TABLE Orders;--";
        var spec = new DescriptionContainsMySQLSpec(maliciousInput);

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
            new SpecificationSqlBuilder<TestOrderMySQL>(null!));
    }

    [Fact]
    public void BuildWhereClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderMySQL>(_columnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause((Specification<TestOrderMySQL>)null!));
    }

    #endregion
}

#region Test Specifications

public class CustomerOrdersMySQLSpec : Specification<TestOrderMySQL>
{
    private readonly Guid _customerId;
    public CustomerOrdersMySQLSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestOrderMySQL, bool>> ToExpression()
        => o => o.CustomerId == _customerId;
}

public class NotCustomerMySQLSpec : Specification<TestOrderMySQL>
{
    private readonly Guid _customerId;
    public NotCustomerMySQLSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestOrderMySQL, bool>> ToExpression()
        => o => o.CustomerId != _customerId;
}

public class MinTotalMySQLSpec : Specification<TestOrderMySQL>
{
    private readonly decimal _minTotal;
    public MinTotalMySQLSpec(decimal minTotal) => _minTotal = minTotal;
    public override Expression<Func<TestOrderMySQL, bool>> ToExpression()
        => o => o.Total >= _minTotal;
}

public class MaxTotalMySQLSpec : Specification<TestOrderMySQL>
{
    private readonly decimal _maxTotal;
    public MaxTotalMySQLSpec(decimal maxTotal) => _maxTotal = maxTotal;
    public override Expression<Func<TestOrderMySQL, bool>> ToExpression()
        => o => o.Total <= _maxTotal;
}

public class ActiveOrdersMySQLSpec : Specification<TestOrderMySQL>
{
    public override Expression<Func<TestOrderMySQL, bool>> ToExpression()
        => o => o.IsActive;
}

public class NullDescriptionMySQLSpec : Specification<TestOrderMySQL>
{
    public override Expression<Func<TestOrderMySQL, bool>> ToExpression()
        => o => o.Description == null;
}

public class HasDescriptionMySQLSpec : Specification<TestOrderMySQL>
{
    public override Expression<Func<TestOrderMySQL, bool>> ToExpression()
        => o => o.Description != null;
}

public class DescriptionContainsMySQLSpec : Specification<TestOrderMySQL>
{
    private readonly string _searchTerm;
    public DescriptionContainsMySQLSpec(string searchTerm) => _searchTerm = searchTerm;
    public override Expression<Func<TestOrderMySQL, bool>> ToExpression()
        => o => o.Description != null && o.Description.Contains(_searchTerm);
}

public class DescriptionStartsWithMySQLSpec : Specification<TestOrderMySQL>
{
    private readonly string _prefix;
    public DescriptionStartsWithMySQLSpec(string prefix) => _prefix = prefix;
    public override Expression<Func<TestOrderMySQL, bool>> ToExpression()
        => o => o.Description != null && o.Description.StartsWith(_prefix);
}

public class DescriptionEndsWithMySQLSpec : Specification<TestOrderMySQL>
{
    private readonly string _suffix;
    public DescriptionEndsWithMySQLSpec(string suffix) => _suffix = suffix;
    public override Expression<Func<TestOrderMySQL, bool>> ToExpression()
        => o => o.Description != null && o.Description.EndsWith(_suffix);
}

#endregion
