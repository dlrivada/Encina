using System.Linq.Expressions;
using Encina.Dapper.Sqlite.Repository;
using Encina.DomainModeling;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.Sqlite.Repository;

/// <summary>
/// Unit tests for <see cref="SpecificationSqlBuilder{TEntity}"/> for SQLite.
/// SQLite uses double-quote identifier quoting ("identifier") instead of SQL Server's square brackets.
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
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var customerId = Guid.NewGuid();
        var spec = new SqliteCustomerOrdersSpec(customerId);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        // SQLite uses double-quote identifiers
        whereClause.ShouldBe("WHERE \"CustomerId\" = @p0");
        parameters["p0"].ShouldBe(customerId);
    }

    [Fact]
    public void BuildWhereClause_NotEqualComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteNotCustomerSpec(Guid.NewGuid());

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        // SQLite uses double-quote identifiers
        whereClause.ShouldContain("\"CustomerId\" <>");
        parameters.Count.ShouldBe(1);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void BuildWhereClause_GreaterThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteMinTotalSpec(100m);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        // SQLite uses double-quote identifiers
        whereClause.ShouldContain("\"Total\" >=");
        parameters["p0"].ShouldBe(100m);
    }

    [Fact]
    public void BuildWhereClause_LessThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteMaxTotalSpec(1000m);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        // SQLite uses double-quote identifiers
        whereClause.ShouldContain("\"Total\" <=");
        parameters["p0"].ShouldBe(1000m);
    }

    #endregion

    #region Boolean Tests

    [Fact]
    public void BuildWhereClause_BooleanProperty_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteActiveOrdersSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        // SQLite stores booleans as INTEGER (0/1), uses double-quote identifiers
        whereClause.ShouldContain("\"IsActive\" = 1");
    }

    #endregion

    #region Null Check Tests

    [Fact]
    public void BuildWhereClause_NullCheck_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteNullDescriptionSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        // SQLite uses double-quote identifiers
        whereClause.ShouldContain("\"Description\" IS NULL");
    }

    [Fact]
    public void BuildWhereClause_NotNullCheck_GeneratesIsNotNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteHasDescriptionSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        // SQLite uses double-quote identifiers
        whereClause.ShouldContain("\"Description\" IS NOT NULL");
    }

    #endregion

    #region Logical Operator Tests

    [Fact]
    public void BuildWhereClause_AndCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteActiveOrdersSpec().And(new SqliteMinTotalSpec(100m));

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("AND");
    }

    [Fact]
    public void BuildWhereClause_OrCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteActiveOrdersSpec().Or(new SqliteMinTotalSpec(1000m));

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("OR");
    }

    [Fact]
    public void BuildWhereClause_NotOperator_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteActiveOrdersSpec().Not();

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
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteDescriptionContainsSpec("urgent");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        // SQLite uses double-quote identifiers
        whereClause.ShouldContain("\"Description\" LIKE @p0");
        parameters["p0"].ShouldBe("%urgent%");
    }

    [Fact]
    public void BuildWhereClause_StringStartsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteDescriptionStartsWithSpec("Priority:");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        // SQLite uses double-quote identifiers
        whereClause.ShouldContain("\"Description\" LIKE @p0");
        parameters["p0"].ShouldBe("Priority:%");
    }

    [Fact]
    public void BuildWhereClause_StringEndsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteDescriptionEndsWithSpec("completed");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        // SQLite uses double-quote identifiers
        whereClause.ShouldContain("\"Description\" LIKE @p0");
        parameters["p0"].ShouldBe("%completed");
    }

    #endregion

    #region BuildSelectStatement Tests

    [Fact]
    public void BuildSelectStatement_WithoutSpecification_ReturnsSelectAll()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("Orders");

        // Assert
        sql.ShouldStartWith("SELECT ");
        // SQLite uses double-quote identifiers for table names
        sql.ShouldContain("FROM \"Orders\"");
        sql.ShouldNotContain("WHERE");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    public void BuildSelectStatement_WithSpecification_ReturnsSelectWithWhere()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteActiveOrdersSpec();

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders", spec);

        // Assert
        sql.ShouldStartWith("SELECT ");
        // SQLite uses double-quote identifiers for table names
        sql.ShouldContain("FROM \"Orders\"");
        sql.ShouldContain("WHERE");
    }

    #endregion

    #region SQL Injection Prevention Tests

    [Fact]
    public void BuildWhereClause_ParameterizedValues_PreventsSqlInjection()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var maliciousInput = "'; DROP TABLE Orders;--";
        var spec = new SqliteDescriptionContainsSpec(maliciousInput);

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
            new SpecificationSqlBuilder<TestOrderSqlite>(null!));
    }

    [Fact]
    public void BuildWhereClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause((Specification<TestOrderSqlite>)null!));
    }

    #endregion

    #region Boolean Constant Tests

    [Fact]
    public void BuildWhereClause_BooleanConstantTrue_Generates1Equals1()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteTrueConstantSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=1");
    }

    [Fact]
    public void BuildWhereClause_BooleanConstantFalse_Generates1Equals0()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteFalseConstantSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=0");
    }

    #endregion

    #region String Equals Tests

    [Fact]
    public void BuildWhereClause_StringEquals_GeneratesEqualityClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteDescriptionStringEqualsSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        // SQLite uses double-quote identifiers
        whereClause.ShouldContain("\"Description\"");
        whereClause.ShouldContain("=");
    }

    [Fact]
    public void BuildWhereClause_StringEqualsNull_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteDescriptionStringEqualsNullSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("IS NULL");
    }

    #endregion

    #region Unsupported Expression Tests

    [Fact]
    public void BuildWhereClause_UnsupportedExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new SqliteUnsupportedSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    #endregion
}

#region Test Specifications for SQLite

public class SqliteCustomerOrdersSpec : Specification<TestOrderSqlite>
{
    private readonly Guid _customerId;
    public SqliteCustomerOrdersSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.CustomerId == _customerId;
}

public class SqliteNotCustomerSpec : Specification<TestOrderSqlite>
{
    private readonly Guid _customerId;
    public SqliteNotCustomerSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.CustomerId != _customerId;
}

public class SqliteMinTotalSpec : Specification<TestOrderSqlite>
{
    private readonly decimal _minTotal;
    public SqliteMinTotalSpec(decimal minTotal) => _minTotal = minTotal;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Total >= _minTotal;
}

public class SqliteMaxTotalSpec : Specification<TestOrderSqlite>
{
    private readonly decimal _maxTotal;
    public SqliteMaxTotalSpec(decimal maxTotal) => _maxTotal = maxTotal;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Total <= _maxTotal;
}

public class SqliteActiveOrdersSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.IsActive;
}

public class SqliteNullDescriptionSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description == null;
}

public class SqliteHasDescriptionSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null;
}

public class SqliteDescriptionContainsSpec : Specification<TestOrderSqlite>
{
    private readonly string _searchTerm;
    public SqliteDescriptionContainsSpec(string searchTerm) => _searchTerm = searchTerm;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null && o.Description.Contains(_searchTerm);
}

public class SqliteDescriptionStartsWithSpec : Specification<TestOrderSqlite>
{
    private readonly string _prefix;
    public SqliteDescriptionStartsWithSpec(string prefix) => _prefix = prefix;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null && o.Description.StartsWith(_prefix);
}

public class SqliteDescriptionEndsWithSpec : Specification<TestOrderSqlite>
{
    private readonly string _suffix;
    public SqliteDescriptionEndsWithSpec(string suffix) => _suffix = suffix;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null && o.Description.EndsWith(_suffix);
}

public class SqliteTrueConstantSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => true;
}

public class SqliteFalseConstantSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => false;
}

public class SqliteDescriptionStringEqualsSpec : Specification<TestOrderSqlite>
{
    private readonly string _value;
    public SqliteDescriptionStringEqualsSpec(string value) => _value = value;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null && o.Description.Equals(_value, StringComparison.Ordinal);
}

public class SqliteDescriptionStringEqualsNullSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null && o.Description.Equals(null, StringComparison.Ordinal);
}

/// <summary>
/// Specification that causes NotSupportedException (for testing unsupported expressions).
/// Uses an unsupported method call expression (string.Replace).
/// </summary>
public class SqliteUnsupportedSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null && o.Description.Replace("a", "b") == "test";
}

#endregion
