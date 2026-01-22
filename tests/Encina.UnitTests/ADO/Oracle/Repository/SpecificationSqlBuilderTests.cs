using System.Collections;
using System.Data;
using System.Linq.Expressions;
using Encina.ADO.Oracle.Repository;
using Encina.DomainModeling;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.Oracle.Repository;

/// <summary>
/// Unit tests for <see cref="SpecificationSqlBuilder{TEntity}"/> in ADO.NET Oracle.
/// Tests verify Oracle-specific SQL syntax (double-quote identifiers, colon parameters).
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
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var customerId = Guid.NewGuid();
        var spec = new CustomerOrdersOracleSpec(customerId);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - Oracle uses double-quotes and colon parameters
        whereClause.ShouldBe("WHERE \"CustomerId\" = :p0");
        parameters[":p0"].ShouldBe(customerId);
    }

    [Fact]
    public void BuildWhereClause_NotEqualComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new NotCustomerOracleSpec(Guid.NewGuid());

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        whereClause.ShouldContain("\"CustomerId\" <>");
        parameters.Count.ShouldBe(1);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void BuildWhereClause_GreaterThanOrEqual_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new MinTotalOracleSpec(100m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        whereClause.ShouldContain("\"Total\" >=");
        parameters[":p0"].ShouldBe(100m);
    }

    [Fact]
    public void BuildWhereClause_LessThanOrEqual_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new MaxTotalOracleSpec(1000m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        whereClause.ShouldContain("\"Total\" <=");
        parameters[":p0"].ShouldBe(1000m);
    }

    [Fact]
    public void BuildWhereClause_GreaterThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new GreaterThanTotalOracleSpec(500m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        whereClause.ShouldContain("\"Total\" >");
        whereClause.ShouldNotContain(">=");
        parameters[":p0"].ShouldBe(500m);
    }

    [Fact]
    public void BuildWhereClause_LessThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new LessThanTotalOracleSpec(500m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        whereClause.ShouldContain("\"Total\" <");
        whereClause.ShouldNotContain("<=");
        parameters[":p0"].ShouldBe(500m);
    }

    #endregion

    #region Boolean Tests

    [Fact]
    public void BuildWhereClause_BooleanProperty_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new ActiveOrdersOracleSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - Oracle uses NUMBER(1), represented as 1 for true
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
    public void BuildWhereClause_StringContains_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new DescriptionContainsOracleSpec("urgent");

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - Oracle uses double-quotes and colon parameters
        whereClause.ShouldContain("\"Description\" LIKE :p");
        var likeParam = parameters.Values.FirstOrDefault(v => v?.ToString()?.Contains("urgent") == true);
        likeParam.ShouldBe("%urgent%");
    }

    [Fact]
    public void BuildWhereClause_StringStartsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new DescriptionStartsWithOracleSpec("Priority:");

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        whereClause.ShouldContain("\"Description\" LIKE :p");
        var likeParam = parameters.Values.FirstOrDefault(v => v?.ToString()?.Contains("Priority:") == true);
        likeParam.ShouldBe("Priority:%");
    }

    [Fact]
    public void BuildWhereClause_StringEndsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new DescriptionEndsWithOracleSpec("completed");

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        whereClause.ShouldContain("\"Description\" LIKE :p");
        var likeParam = parameters.Values.FirstOrDefault(v => v?.ToString()?.Contains("completed") == true);
        likeParam.ShouldBe("%completed");
    }

    #endregion

    #region BuildSelectStatement Tests

    [Fact]
    public void BuildSelectStatement_WithoutSpecification_ReturnsSelectAll()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);

        // Act
        var (sql, addParameters) = builder.BuildSelectStatement("Orders");
        var parameters = CaptureParameters(addParameters);

        // Assert - Oracle uses double-quotes for table and column names
        sql.ShouldStartWith("SELECT ");
        sql.ShouldContain("FROM \"Orders\"");
        sql.ShouldNotContain("WHERE");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    public void BuildSelectStatement_WithSpecification_ReturnsSelectWithWhere()
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
    }

    [Fact]
    public void BuildSelectStatement_IncludesAllMappedColumns()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders");

        // Assert - Oracle uses double-quotes for identifiers
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
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var maliciousInput = "'; DROP TABLE Orders;--";
        var spec = new DescriptionContainsOracleSpec(maliciousInput);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        // The malicious input should be parameterized, not embedded in SQL
        whereClause.ShouldNotContain("DROP TABLE");
        whereClause.ShouldContain(":p");
        var containsMalicious = parameters.Values.Any(v => v != null && v.ToString()!.Contains(maliciousInput));
        containsMalicious.ShouldBeTrue();
    }

    #endregion

    #region Parameter Action Tests

    [Fact]
    public void BuildWhereClause_AddParametersAction_AddsParametersToCommand()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var customerId = Guid.NewGuid();
        var spec = new CustomerOrdersOracleSpec(customerId);
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
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new MinTotalOracleSpec(100m).And(new MaxTotalOracleSpec(1000m));
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
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.BuildWhereClause(null!));
    }

    [Fact]
    public void Constructor_NullColumnMappings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SpecificationSqlBuilder<TestOrderOracle>(null!));
    }

    [Fact]
    public void BuildWhereClause_BooleanTrueConstant_Generates1Equals1()
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
    public void BuildWhereClause_BooleanFalseConstant_Generates1Equals0()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new FalseConstantOracleSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=0");
    }

    [Fact]
    public void BuildWhereClause_StringEquals_GeneratesEqualityClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new DescriptionEqualsOracleSpec("Test");

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        whereClause.ShouldContain("\"Description\" =");
        parameters.Values.ShouldContain("Test");
    }

    [Fact]
    public void BuildWhereClause_StringEqualsNull_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new DescriptionEqualsNullOracleSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Description\" IS NULL");
    }

    [Fact]
    public void BuildWhereClause_UnsupportedStringMethod_Throws()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new TrimStringMethodOracleSpec();

        // Act & Assert - Trim is not supported
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildWhereClause_UnsupportedBinaryOperator_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new UnsupportedBinaryOperatorOracleSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildWhereClause_NegateUnaryOperator_HandledAsConvert()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new NegateOperatorOracleSpec();

        // Act - Negate is handled via Convert path in the switch expression
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - The SQL is generated (even if semantically unusual)
        whereClause.ShouldNotBeEmpty();
    }

    [Fact]
    public void BuildWhereClause_UnsupportedExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new UnsupportedExpressionOracleSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildWhereClause_UnsupportedMethodCall_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new UnsupportedMethodCallOracleSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildWhereClause_ConvertExpression_HandlesCorrectly()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(_columnMappings);
        var spec = new NullableTotalOracleSpec(100m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        whereClause.ShouldContain("\"Total\"");
        parameters.ShouldNotBeEmpty();
    }

    [Fact]
    public void BuildWhereClause_PropertyNotInMappings_UsesPropertyNameDirectly()
    {
        // Arrange
        var limitedMappings = new Dictionary<string, string> { ["Id"] = "Id" };
        var builder = new SpecificationSqlBuilder<TestOrderOracle>(limitedMappings);
        var spec = new ActiveOrdersOracleSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - Falls back to property name (IsActive) which should be validated
        whereClause.ShouldContain("IsActive");
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
        var parameters = new TestOracleParameterCollection();

        command.Parameters.Returns(parameters);
        command.CreateParameter().Returns(_ => new TestOracleParameter());

        return command;
    }

    #endregion
}

#region Test Helpers

/// <summary>
/// Simple test implementation of IDataParameterCollection for Oracle tests.
/// </summary>
file sealed class TestOracleParameterCollection : IDataParameterCollection
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
/// Simple test implementation of IDbDataParameter for Oracle tests.
/// </summary>
file sealed class TestOracleParameter : IDbDataParameter
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
/// Test entity for ADO.NET Oracle repository tests.
/// </summary>
public class TestOrderOracle
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

public class GreaterThanTotalOracleSpec : Specification<TestOrderOracle>
{
    private readonly decimal _total;
    public GreaterThanTotalOracleSpec(decimal total) => _total = total;
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Total > _total;
}

public class LessThanTotalOracleSpec : Specification<TestOrderOracle>
{
    private readonly decimal _total;
    public LessThanTotalOracleSpec(decimal total) => _total = total;
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Total < _total;
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
        => o => true;
}

public class FalseConstantOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => false;
}

public class DescriptionEqualsOracleSpec : Specification<TestOrderOracle>
{
    private readonly string _value;
    public DescriptionEqualsOracleSpec(string value) => _value = value;
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Description != null && o.Description.Equals(_value, StringComparison.Ordinal);
}

public class DescriptionEqualsNullOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Description != null && o.Description.Equals(null, StringComparison.Ordinal);
}

/// <summary>
/// Specification using Trim which is not a supported string method.
/// </summary>
public class TrimStringMethodOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Description != null && o.Description.Trim() == "TEST";
}

public class UnsupportedBinaryOperatorOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
    {
        // XOR is not supported
        var param = Expression.Parameter(typeof(TestOrderOracle), "o");
        var left = Expression.Property(param, nameof(TestOrderOracle.IsActive));
        var right = Expression.Constant(true);
        var xor = Expression.ExclusiveOr(left, right);
        return Expression.Lambda<Func<TestOrderOracle, bool>>(xor, param);
    }
}

/// <summary>
/// Specification using Negate which is handled via Convert path.
/// </summary>
public class NegateOperatorOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
    {
        // Negate is handled via the Convert path in the switch expression
        var param = Expression.Parameter(typeof(TestOrderOracle), "o");
        var total = Expression.Property(param, nameof(TestOrderOracle.Total));
        var negated = Expression.Negate(total);
        var comparison = Expression.GreaterThan(negated, Expression.Constant(0m));
        return Expression.Lambda<Func<TestOrderOracle, bool>>(comparison, param);
    }
}

public class UnsupportedExpressionOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
    {
        // NewArrayExpression is not supported at root level
        var param = Expression.Parameter(typeof(TestOrderOracle), "o");
        var newArray = Expression.NewArrayInit(typeof(int), Expression.Constant(1));
        var lengthProp = Expression.ArrayLength(newArray);
        var comparison = Expression.Equal(lengthProp, Expression.Constant(1));
        return Expression.Lambda<Func<TestOrderOracle, bool>>(comparison, param);
    }
}

public class UnsupportedMethodCallOracleSpec : Specification<TestOrderOracle>
{
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
    {
        // Math.Abs is not a string method and not supported
        var param = Expression.Parameter(typeof(TestOrderOracle), "o");
        var total = Expression.Property(param, nameof(TestOrderOracle.Total));
        var absMethod = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(decimal)])!;
        var absCall = Expression.Call(absMethod, total);
        var comparison = Expression.GreaterThan(absCall, Expression.Constant(0m));
        return Expression.Lambda<Func<TestOrderOracle, bool>>(comparison, param);
    }
}

public class NullableTotalOracleSpec : Specification<TestOrderOracle>
{
    private readonly decimal? _total;
    public NullableTotalOracleSpec(decimal? total) => _total = total;
    public override Expression<Func<TestOrderOracle, bool>> ToExpression()
        => o => o.Total >= _total!.Value;
}

#endregion
