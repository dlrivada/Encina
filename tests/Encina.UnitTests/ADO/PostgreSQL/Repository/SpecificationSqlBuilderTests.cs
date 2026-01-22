using System.Collections;
using System.Data;
using System.Linq.Expressions;
using Encina.ADO.PostgreSQL.Repository;
using Encina.DomainModeling;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.PostgreSQL.Repository;

/// <summary>
/// Unit tests for <see cref="SpecificationSqlBuilder{TEntity}"/> in ADO.NET PostgreSQL.
/// PostgreSQL uses double-quote identifier quoting ("identifier").
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
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - PostgreSQL uses double-quotes for identifiers
        whereClause.ShouldBe("WHERE \"CustomerId\" = @p0");
        parameters["@p0"].ShouldBe(customerId);
    }

    [Fact]
    public void BuildWhereClause_NotEqualComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new NotCustomerPgSpec(Guid.NewGuid());

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - PostgreSQL uses double-quotes for identifiers
        whereClause.ShouldContain("\"CustomerId\" <>");
        parameters.Count.ShouldBe(1);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void BuildWhereClause_GreaterThanOrEqual_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new MinTotalPgSpec(100m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - PostgreSQL uses double-quotes for identifiers
        whereClause.ShouldContain("\"Total\" >=");
        parameters["@p0"].ShouldBe(100m);
    }

    [Fact]
    public void BuildWhereClause_LessThanOrEqual_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new MaxTotalPgSpec(1000m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - PostgreSQL uses double-quotes for identifiers
        whereClause.ShouldContain("\"Total\" <=");
        parameters["@p0"].ShouldBe(1000m);
    }

    [Fact]
    public void BuildWhereClause_GreaterThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new GreaterThanTotalPgSpec(500m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - PostgreSQL uses double-quotes for identifiers
        whereClause.ShouldContain("\"Total\" >");
        whereClause.ShouldNotContain(">=");
        parameters["@p0"].ShouldBe(500m);
    }

    [Fact]
    public void BuildWhereClause_LessThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new LessThanTotalPgSpec(500m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - PostgreSQL uses double-quotes for identifiers
        whereClause.ShouldContain("\"Total\" <");
        whereClause.ShouldNotContain("<=");
        parameters["@p0"].ShouldBe(500m);
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

        // Assert - PostgreSQL uses native TRUE/FALSE for booleans
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

        // Assert - PostgreSQL uses double-quotes for identifiers
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

        // Assert - PostgreSQL uses double-quotes for identifiers
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
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - PostgreSQL uses double-quotes for identifiers
        whereClause.ShouldContain("\"Description\" LIKE @p");
        var likeParam = parameters.Values.FirstOrDefault(v => v?.ToString()?.Contains("urgent") == true);
        likeParam.ShouldBe("%urgent%");
    }

    [Fact]
    public void BuildWhereClause_StringStartsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new DescriptionStartsWithPgSpec("Priority:");

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - PostgreSQL uses double-quotes for identifiers
        whereClause.ShouldContain("\"Description\" LIKE @p");
        var likeParam = parameters.Values.FirstOrDefault(v => v?.ToString()?.Contains("Priority:") == true);
        likeParam.ShouldBe("Priority:%");
    }

    [Fact]
    public void BuildWhereClause_StringEndsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new DescriptionEndsWithPgSpec("completed");

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - PostgreSQL uses double-quotes for identifiers
        whereClause.ShouldContain("\"Description\" LIKE @p");
        var likeParam = parameters.Values.FirstOrDefault(v => v?.ToString()?.Contains("completed") == true);
        likeParam.ShouldBe("%completed");
    }

    #endregion

    #region BuildSelectStatement Tests

    [Fact]
    public void BuildSelectStatement_WithoutSpecification_ReturnsSelectAll()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);

        // Act
        var (sql, addParameters) = builder.BuildSelectStatement("Orders");
        var parameters = CaptureParameters(addParameters);

        // Assert - PostgreSQL uses double-quotes for identifiers
        sql.ShouldStartWith("SELECT ");
        sql.ShouldContain("FROM \"Orders\"");
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
        var (sql, _) = builder.BuildSelectStatement("Orders", spec);

        // Assert - PostgreSQL uses double-quotes for identifiers
        sql.ShouldStartWith("SELECT ");
        sql.ShouldContain("FROM \"Orders\"");
        sql.ShouldContain("WHERE");
    }

    [Fact]
    public void BuildSelectStatement_IncludesAllMappedColumns()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders");

        // Assert - PostgreSQL uses double-quotes for column names
        sql.ShouldContain("\"Id\"");
        sql.ShouldContain("\"CustomerId\"");
        sql.ShouldContain("\"Total\"");
        sql.ShouldContain("\"IsActive\"");
        sql.ShouldContain("\"Description\"");
        sql.ShouldContain("\"CreatedAtUtc\"");
    }

    #endregion

    #region SQL Injection Prevention Tests

    [Fact]
    public void BuildWhereClause_ParameterizedValues_PreventsSqlInjection()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var maliciousInput = "'; DROP TABLE Orders;--";
        var spec = new DescriptionContainsPgSpec(maliciousInput);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        // The malicious input should be parameterized, not embedded in SQL
        whereClause.ShouldNotContain("DROP TABLE");
        whereClause.ShouldContain("@p");
        var containsMalicious = parameters.Values.Any(v => v != null && v.ToString()!.Contains(maliciousInput));
        containsMalicious.ShouldBeTrue();
    }

    #endregion

    #region Parameter Action Tests

    [Fact]
    public void BuildWhereClause_AddParametersAction_AddsParametersToCommand()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var customerId = Guid.NewGuid();
        var spec = new CustomerOrdersPgSpec(customerId);
        var command = CreateMockCommand();

        // Act
        var (_, addParameters) = builder.BuildWhereClause(spec);
        addParameters(command);

        // Assert
        command.Parameters.Count.ShouldBe(1);
    }

    [Fact]
    public void BuildWhereClause_MultipleParameters_AddsAllToCommand()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new MinTotalPgSpec(100m).And(new MaxTotalPgSpec(1000m));
        var command = CreateMockCommand();

        // Act
        var (_, addParameters) = builder.BuildWhereClause(spec);
        addParameters(command);

        // Assert
        command.Parameters.Count.ShouldBe(2);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void BuildWhereClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.BuildWhereClause(null!));
    }

    [Fact]
    public void Constructor_NullColumnMappings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SpecificationSqlBuilder<TestOrderPg>(null!));
    }

    [Fact]
    public void BuildWhereClause_BooleanTrueConstant_GeneratesTrueBoolean()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new TrueConstantPgSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - PostgreSQL uses native TRUE
        whereClause.ShouldContain("TRUE");
    }

    [Fact]
    public void BuildWhereClause_BooleanFalseConstant_GeneratesFalseBoolean()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new FalseConstantPgSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - PostgreSQL uses native FALSE
        whereClause.ShouldContain("FALSE");
    }

    [Fact]
    public void BuildWhereClause_StringEquals_GeneratesEqualityClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new DescriptionEqualsPgSpec("Test");

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - PostgreSQL uses double-quotes for identifiers
        whereClause.ShouldContain("\"Description\" =");
        parameters.Values.ShouldContain("Test");
    }

    [Fact]
    public void BuildWhereClause_StringEqualsNull_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new DescriptionEqualsNullPgSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - PostgreSQL uses double-quotes for identifiers
        whereClause.ShouldContain("\"Description\" IS NULL");
    }

    [Fact]
    public void BuildWhereClause_UnsupportedStringMethod_Throws()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new TrimStringMethodPgSpec();

        // Act & Assert - Trim is not supported
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildWhereClause_UnsupportedBinaryOperator_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new UnsupportedBinaryOperatorPgSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildWhereClause_NegateUnaryOperator_HandledAsConvert()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new NegateOperatorPgSpec();

        // Act - Negate is handled via Convert path in the switch expression
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - The SQL is generated (even if semantically unusual)
        whereClause.ShouldNotBeEmpty();
    }

    [Fact]
    public void BuildWhereClause_UnsupportedExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new UnsupportedExpressionPgSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildWhereClause_UnsupportedMethodCall_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new UnsupportedMethodCallPgSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildWhereClause_ConvertExpression_HandlesCorrectly()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var spec = new NullableTotalPgSpec(100m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - PostgreSQL uses double-quotes for identifiers
        whereClause.ShouldContain("\"Total\"");
        parameters.ShouldNotBeEmpty();
    }

    [Fact]
    public void BuildWhereClause_PropertyNotInMappings_UsesPropertyNameDirectly()
    {
        // Arrange
        var limitedMappings = new Dictionary<string, string> { ["Id"] = "Id" };
        var builder = new SpecificationSqlBuilder<TestOrderPg>(limitedMappings);
        var spec = new ActiveOrdersPgSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - Falls back to property name (IsActive) which should be validated
        whereClause.ShouldContain("IsActive");
    }

    #endregion

    #region PostgreSQL-Specific Tests

    [Fact]
    public void BuildSelectStatement_WithSnakeCaseColumns_UsesDoubleQuotes()
    {
        // Arrange - PostgreSQL typically uses snake_case
        var snakeCaseMappings = new Dictionary<string, string>
        {
            ["Id"] = "id",
            ["CustomerId"] = "customer_id",
            ["Total"] = "total_amount"
        };
        var builder = new SpecificationSqlBuilder<TestOrderPg>(snakeCaseMappings);

        // Act
        var (sql, _) = builder.BuildSelectStatement("orders");

        // Assert - PostgreSQL uses double-quotes for identifiers
        sql.ShouldContain("\"id\"");
        sql.ShouldContain("\"customer_id\"");
        sql.ShouldContain("\"total_amount\"");
        sql.ShouldContain("FROM \"orders\"");
    }

    [Fact]
    public void BuildWhereClause_GuidComparison_GeneratesCorrectSql()
    {
        // Arrange - PostgreSQL has native UUID support
        var builder = new SpecificationSqlBuilder<TestOrderPg>(_columnMappings);
        var customerId = Guid.NewGuid();
        var spec = new CustomerOrdersPgSpec(customerId);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - UUID is passed directly, no string conversion needed
        whereClause.ShouldBe("WHERE \"CustomerId\" = @p0");
        parameters["@p0"].ShouldBeOfType<Guid>();
        parameters["@p0"].ShouldBe(customerId);
    }

    #endregion

    #region Helper Methods

    private static Dictionary<string, object?> CaptureParameters(Action<IDbCommand> addParameters)
    {
        var command = CreateMockCommand();
        addParameters(command);

        var result = new Dictionary<string, object?>();
        foreach (IDbDataParameter param in command.Parameters)
        {
            result[param.ParameterName] = param.Value;
        }
        return result;
    }

    private static IDbCommand CreateMockCommand()
    {
        var command = Substitute.For<IDbCommand>();
        var parameters = new TestParameterCollectionPg();

        command.Parameters.Returns(parameters);
        command.CreateParameter().Returns(_ => new TestParameterPg());

        return command;
    }

    #endregion
}

#region Test Helpers

/// <summary>
/// Simple test implementation of IDataParameterCollection for tests.
/// </summary>
file sealed class TestParameterCollectionPg : IDataParameterCollection
{
    private readonly List<IDbDataParameter> _parameters = [];

    public int Count => _parameters.Count;
    public bool IsSynchronized => false;
    public object SyncRoot { get; } = new();
    public bool IsFixedSize => false;
    public bool IsReadOnly => false;

    public object? this[int index]
    {
        get => _parameters[index];
        set => _parameters[index] = (IDbDataParameter)value!;
    }

    public object this[string parameterName]
    {
        get => _parameters.First(p => p.ParameterName == parameterName);
        set
        {
            var index = IndexOf(parameterName);
            if (index >= 0) _parameters[index] = (IDbDataParameter)value;
        }
    }

    public bool Contains(string parameterName) => _parameters.Any(p => p.ParameterName == parameterName);
    public int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
    public void RemoveAt(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index >= 0) _parameters.RemoveAt(index);
    }

    public int Add(object? value)
    {
        _parameters.Add((IDbDataParameter)value!);
        return _parameters.Count - 1;
    }

    public void Clear() => _parameters.Clear();
    public bool Contains(object? value) => _parameters.Contains((IDbDataParameter)value!);
    public int IndexOf(object? value) => _parameters.IndexOf((IDbDataParameter)value!);
    public void Insert(int index, object? value) => _parameters.Insert(index, (IDbDataParameter)value!);
    public void Remove(object? value) => _parameters.Remove((IDbDataParameter)value!);
    public void RemoveAt(int index) => _parameters.RemoveAt(index);
    public void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);
    public IEnumerator GetEnumerator() => _parameters.GetEnumerator();
}

/// <summary>
/// Simple test implementation of IDbDataParameter for tests.
/// </summary>
file sealed class TestParameterPg : IDbDataParameter
{
    public DbType DbType { get; set; }
    public ParameterDirection Direction { get; set; }
    public bool IsNullable => true;
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member
    public string ParameterName { get; set; } = string.Empty;
    public string SourceColumn { get; set; } = string.Empty;
#pragma warning restore CS8767
    public DataRowVersion SourceVersion { get; set; }
    public object? Value { get; set; }
    public byte Precision { get; set; }
    public byte Scale { get; set; }
    public int Size { get; set; }
}

#endregion

#region Test Entity

/// <summary>
/// Test entity for ADO.NET PostgreSQL repository tests.
/// </summary>
public class TestOrderPg
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

#endregion

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

public class GreaterThanTotalPgSpec : Specification<TestOrderPg>
{
    private readonly decimal _total;
    public GreaterThanTotalPgSpec(decimal total) => _total = total;
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Total > _total;
}

public class LessThanTotalPgSpec : Specification<TestOrderPg>
{
    private readonly decimal _total;
    public LessThanTotalPgSpec(decimal total) => _total = total;
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Total < _total;
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
        => o => true;
}

public class FalseConstantPgSpec : Specification<TestOrderPg>
{
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => false;
}

public class DescriptionEqualsPgSpec : Specification<TestOrderPg>
{
    private readonly string _value;
    public DescriptionEqualsPgSpec(string value) => _value = value;
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Description != null && o.Description.Equals(_value, StringComparison.Ordinal);
}

public class DescriptionEqualsNullPgSpec : Specification<TestOrderPg>
{
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Description != null && o.Description.Equals(null, StringComparison.Ordinal);
}

/// <summary>
/// Specification using Trim which is not a supported string method.
/// </summary>
public class TrimStringMethodPgSpec : Specification<TestOrderPg>
{
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Description != null && o.Description.Trim() == "TEST";
}

public class UnsupportedBinaryOperatorPgSpec : Specification<TestOrderPg>
{
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
    {
        // XOR is not supported
        var param = Expression.Parameter(typeof(TestOrderPg), "o");
        var left = Expression.Property(param, nameof(TestOrderPg.IsActive));
        var right = Expression.Constant(true);
        var xor = Expression.ExclusiveOr(left, right);
        return Expression.Lambda<Func<TestOrderPg, bool>>(xor, param);
    }
}

/// <summary>
/// Specification using Negate which is handled via Convert path.
/// </summary>
public class NegateOperatorPgSpec : Specification<TestOrderPg>
{
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
    {
        // Negate is handled via the Convert path in the switch expression
        var param = Expression.Parameter(typeof(TestOrderPg), "o");
        var total = Expression.Property(param, nameof(TestOrderPg.Total));
        var negated = Expression.Negate(total);
        var comparison = Expression.GreaterThan(negated, Expression.Constant(0m));
        return Expression.Lambda<Func<TestOrderPg, bool>>(comparison, param);
    }
}

public class UnsupportedExpressionPgSpec : Specification<TestOrderPg>
{
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
    {
        // NewArrayExpression is not supported at root level
        var param = Expression.Parameter(typeof(TestOrderPg), "o");
        var newArray = Expression.NewArrayInit(typeof(int), Expression.Constant(1));
        var lengthProp = Expression.ArrayLength(newArray);
        var comparison = Expression.Equal(lengthProp, Expression.Constant(1));
        return Expression.Lambda<Func<TestOrderPg, bool>>(comparison, param);
    }
}

public class UnsupportedMethodCallPgSpec : Specification<TestOrderPg>
{
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
    {
        // Math.Abs is not a string method and not supported
        var param = Expression.Parameter(typeof(TestOrderPg), "o");
        var total = Expression.Property(param, nameof(TestOrderPg.Total));
        var absMethod = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(decimal)])!;
        var absCall = Expression.Call(absMethod, total);
        var comparison = Expression.GreaterThan(absCall, Expression.Constant(0m));
        return Expression.Lambda<Func<TestOrderPg, bool>>(comparison, param);
    }
}

public class NullableTotalPgSpec : Specification<TestOrderPg>
{
    private readonly decimal? _total;
    public NullableTotalPgSpec(decimal? total) => _total = total;
    public override Expression<Func<TestOrderPg, bool>> ToExpression()
        => o => o.Total >= _total!.Value;
}

#endregion
