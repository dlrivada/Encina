using System.Linq.Expressions;
using Encina.Dapper.SqlServer.Repository;
using Encina.DomainModeling;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.SqlServer.Repository;

/// <summary>
/// Unit tests for <see cref="SpecificationSqlBuilder{TEntity}"/>.
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
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var customerId = Guid.NewGuid();
        var spec = new CustomerOrdersSpec(customerId);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldBe("WHERE [CustomerId] = @p0");
        parameters["p0"].ShouldBe(customerId);
    }

    [Fact]
    public void BuildWhereClause_NotEqualComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new NotCustomerSpec(Guid.NewGuid());

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("[CustomerId] <>");
        parameters.Count.ShouldBe(1);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void BuildWhereClause_GreaterThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new MinTotalSpec(100m);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("[Total] >=");
        parameters["p0"].ShouldBe(100m);
    }

    [Fact]
    public void BuildWhereClause_LessThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new MaxTotalSpec(1000m);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("[Total] <=");
        parameters["p0"].ShouldBe(1000m);
    }

    #endregion

    #region Boolean Tests

    [Fact]
    public void BuildWhereClause_BooleanProperty_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new ActiveOrdersSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("[IsActive] = 1");
    }

    #endregion

    #region Null Check Tests

    [Fact]
    public void BuildWhereClause_NullCheck_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new NullDescriptionSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("[Description] IS NULL");
    }

    [Fact]
    public void BuildWhereClause_NotNullCheck_GeneratesIsNotNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new HasDescriptionSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("[Description] IS NOT NULL");
    }

    #endregion

    #region Logical Operator Tests

    [Fact]
    public void BuildWhereClause_AndCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new ActiveOrdersSpec().And(new MinTotalSpec(100m));

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("AND");
    }

    [Fact]
    public void BuildWhereClause_OrCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new ActiveOrdersSpec().Or(new MinTotalSpec(1000m));

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("OR");
    }

    [Fact]
    public void BuildWhereClause_NotOperator_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new ActiveOrdersSpec().Not();

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
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new DescriptionContainsSpec("urgent");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("[Description] LIKE @p0");
        parameters["p0"].ShouldBe("%urgent%");
    }

    [Fact]
    public void BuildWhereClause_StringStartsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new DescriptionStartsWithSpec("Priority:");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("[Description] LIKE @p0");
        parameters["p0"].ShouldBe("Priority:%");
    }

    [Fact]
    public void BuildWhereClause_StringEndsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new DescriptionEndsWithSpec("completed");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("[Description] LIKE @p0");
        parameters["p0"].ShouldBe("%completed");
    }

    #endregion

    #region BuildSelectStatement Tests

    [Fact]
    public void BuildSelectStatement_WithoutSpecification_ReturnsSelectAll()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("Orders");

        // Assert
        sql.ShouldStartWith("SELECT ");
        sql.ShouldContain("FROM Orders");
        sql.ShouldNotContain("WHERE");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    public void BuildSelectStatement_WithSpecification_ReturnsSelectWithWhere()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var spec = new ActiveOrdersSpec();

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders", spec);

        // Assert
        sql.ShouldStartWith("SELECT ");
        sql.ShouldContain("FROM Orders");
        sql.ShouldContain("WHERE");
    }

    #endregion

    #region SQL Injection Prevention Tests

    [Fact]
    public void BuildWhereClause_ParameterizedValues_PreventsSqlInjection()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrder>(_columnMappings);
        var maliciousInput = "'; DROP TABLE Orders;--";
        var spec = new DescriptionContainsSpec(maliciousInput);

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
}

#region Test Specifications

public class CustomerOrdersSpec : Specification<TestOrder>
{
    private readonly Guid _customerId;
    public CustomerOrdersSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestOrder, bool>> ToExpression()
        => o => o.CustomerId == _customerId;
}

public class NotCustomerSpec : Specification<TestOrder>
{
    private readonly Guid _customerId;
    public NotCustomerSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestOrder, bool>> ToExpression()
        => o => o.CustomerId != _customerId;
}

public class MinTotalSpec : Specification<TestOrder>
{
    private readonly decimal _minTotal;
    public MinTotalSpec(decimal minTotal) => _minTotal = minTotal;
    public override Expression<Func<TestOrder, bool>> ToExpression()
        => o => o.Total >= _minTotal;
}

public class MaxTotalSpec : Specification<TestOrder>
{
    private readonly decimal _maxTotal;
    public MaxTotalSpec(decimal maxTotal) => _maxTotal = maxTotal;
    public override Expression<Func<TestOrder, bool>> ToExpression()
        => o => o.Total <= _maxTotal;
}

public class ActiveOrdersSpec : Specification<TestOrder>
{
    public override Expression<Func<TestOrder, bool>> ToExpression()
        => o => o.IsActive;
}

public class NullDescriptionSpec : Specification<TestOrder>
{
    public override Expression<Func<TestOrder, bool>> ToExpression()
        => o => o.Description == null;
}

public class HasDescriptionSpec : Specification<TestOrder>
{
    public override Expression<Func<TestOrder, bool>> ToExpression()
        => o => o.Description != null;
}

public class DescriptionContainsSpec : Specification<TestOrder>
{
    private readonly string _searchTerm;
    public DescriptionContainsSpec(string searchTerm) => _searchTerm = searchTerm;
    public override Expression<Func<TestOrder, bool>> ToExpression()
        => o => o.Description != null && o.Description.Contains(_searchTerm);
}

public class DescriptionStartsWithSpec : Specification<TestOrder>
{
    private readonly string _prefix;
    public DescriptionStartsWithSpec(string prefix) => _prefix = prefix;
    public override Expression<Func<TestOrder, bool>> ToExpression()
        => o => o.Description != null && o.Description.StartsWith(_prefix);
}

public class DescriptionEndsWithSpec : Specification<TestOrder>
{
    private readonly string _suffix;
    public DescriptionEndsWithSpec(string suffix) => _suffix = suffix;
    public override Expression<Func<TestOrder, bool>> ToExpression()
        => o => o.Description != null && o.Description.EndsWith(_suffix);
}

#endregion
