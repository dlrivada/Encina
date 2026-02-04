using System.Collections;
using System.Data;
using System.Linq.Expressions;
using Encina.ADO.Sqlite.Repository;
using Encina.DomainModeling;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.Sqlite.Repository;

/// <summary>
/// Unit tests for <see cref="SpecificationSqlBuilder{TEntity}"/> in ADO.NET SQLite.
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
        var spec = new CustomerOrdersSqliteSpec(customerId);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - SQLite uses double-quotes for identifiers
        // SQLite doesn't have native GUID type, so GUIDs are stored as strings
        whereClause.ShouldBe("WHERE \"CustomerId\" = @p0");
        parameters["@p0"].ShouldBe(customerId.ToString());
    }

    [Fact]
    public void BuildWhereClause_NotEqualComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new NotCustomerSqliteSpec(Guid.NewGuid());

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - SQLite uses double-quotes
        whereClause.ShouldContain("\"CustomerId\" <>");
        parameters.Count.ShouldBe(1);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void BuildWhereClause_GreaterThanOrEqual_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new MinTotalSqliteSpec(100m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - SQLite uses double-quotes
        whereClause.ShouldContain("\"Total\" >=");
        parameters["@p0"].ShouldBe(100m);
    }

    [Fact]
    public void BuildWhereClause_LessThanOrEqual_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new MaxTotalSqliteSpec(1000m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - SQLite uses double-quotes
        whereClause.ShouldContain("\"Total\" <=");
        parameters["@p0"].ShouldBe(1000m);
    }

    [Fact]
    public void BuildWhereClause_GreaterThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new GreaterThanTotalSqliteSpec(500m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - SQLite uses double-quotes
        whereClause.ShouldContain("\"Total\" >");
        whereClause.ShouldNotContain(">=");
        parameters["@p0"].ShouldBe(500m);
    }

    [Fact]
    public void BuildWhereClause_LessThan_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new LessThanTotalSqliteSpec(500m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - SQLite uses double-quotes
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
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new ActiveOrdersSqliteSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - SQLite uses double-quotes and INTEGER (0/1) for booleans
        whereClause.ShouldContain("\"IsActive\" = 1");
    }

    #endregion

    #region Null Check Tests

    [Fact]
    public void BuildWhereClause_NullCheck_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new NullDescriptionSqliteSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - SQLite uses double-quotes
        whereClause.ShouldContain("\"Description\" IS NULL");
    }

    [Fact]
    public void BuildWhereClause_NotNullCheck_GeneratesIsNotNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new HasDescriptionSqliteSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - SQLite uses double-quotes
        whereClause.ShouldContain("\"Description\" IS NOT NULL");
    }

    #endregion

    #region Logical Operator Tests

    [Fact]
    public void BuildWhereClause_AndCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new ActiveOrdersSqliteSpec().And(new MinTotalSqliteSpec(100m));

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
        var spec = new ActiveOrdersSqliteSpec().Or(new MinTotalSqliteSpec(1000m));

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
        var spec = new ActiveOrdersSqliteSpec().Not();

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
        var spec = new DescriptionContainsSqliteSpec("urgent");

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - SQLite uses double-quotes
        whereClause.ShouldContain("\"Description\" LIKE @p");
        var likeParam = parameters.Values.FirstOrDefault(v => v?.ToString()?.Contains("urgent") == true);
        likeParam.ShouldBe("%urgent%");
    }

    [Fact]
    public void BuildWhereClause_StringStartsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new DescriptionStartsWithSqliteSpec("Priority:");

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - SQLite uses double-quotes
        whereClause.ShouldContain("\"Description\" LIKE @p");
        var likeParam = parameters.Values.FirstOrDefault(v => v?.ToString()?.Contains("Priority:") == true);
        likeParam.ShouldBe("Priority:%");
    }

    [Fact]
    public void BuildWhereClause_StringEndsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new DescriptionEndsWithSqliteSpec("completed");

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - SQLite uses double-quotes
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
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);

        // Act
        var (sql, addParameters) = builder.BuildSelectStatement("Orders");
        var parameters = CaptureParameters(addParameters);

        // Assert
        sql.ShouldStartWith("SELECT ");
        sql.ShouldContain("FROM \"Orders\"");
        sql.ShouldNotContain("WHERE");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    public void BuildSelectStatement_WithSpecification_ReturnsSelectWithWhere()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new ActiveOrdersSqliteSpec();

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
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders");

        // Assert - SQLite uses double-quotes
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
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var maliciousInput = "'; DROP TABLE Orders;--";
        var spec = new DescriptionContainsSqliteSpec(maliciousInput);

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
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var customerId = Guid.NewGuid();
        var spec = new CustomerOrdersSqliteSpec(customerId);
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
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new MinTotalSqliteSpec(100m).And(new MaxTotalSqliteSpec(1000m));
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
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.BuildWhereClause(null!));
    }

    [Fact]
    public void Constructor_NullColumnMappings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SpecificationSqlBuilder<TestOrderSqlite>(null!));
    }

    [Fact]
    public void BuildWhereClause_BooleanTrueConstant_Generates1Equals1()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new TrueConstantSqliteSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=1");
    }

    [Fact]
    public void BuildWhereClause_BooleanFalseConstant_Generates1Equals0()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new FalseConstantSqliteSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=0");
    }

    [Fact]
    public void BuildWhereClause_StringEquals_GeneratesEqualityClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new DescriptionEqualsSqliteSpec("Test");

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - SQLite uses double-quotes
        whereClause.ShouldContain("\"Description\" =");
        parameters.Values.ShouldContain("Test");
    }

    [Fact]
    public void BuildWhereClause_StringEqualsNull_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new DescriptionEqualsNullSqliteSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - SQLite uses double-quotes
        whereClause.ShouldContain("\"Description\" IS NULL");
    }

    [Fact]
    public void BuildWhereClause_UnsupportedStringMethod_Throws()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new TrimStringMethodSqliteSpec();

        // Act & Assert - Trim is not supported
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildWhereClause_UnsupportedBinaryOperator_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new UnsupportedBinaryOperatorSqliteSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildWhereClause_NegateUnaryOperator_HandledAsConvert()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new NegateOperatorSqliteSpec();

        // Act - Negate is handled via Convert path in the switch expression
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert - The SQL is generated (even if semantically unusual)
        whereClause.ShouldNotBeEmpty();
    }

    [Fact]
    public void BuildWhereClause_UnsupportedExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new UnsupportedExpressionSqliteSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildWhereClause_UnsupportedMethodCall_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new UnsupportedMethodCallSqliteSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildWhereClause_ConvertExpression_HandlesCorrectly()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(_columnMappings);
        var spec = new NullableTotalSqliteSpec(100m);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert - SQLite uses double-quotes
        whereClause.ShouldContain("\"Total\"");
        parameters.ShouldNotBeEmpty();
    }

    [Fact]
    public void BuildWhereClause_PropertyNotInMappings_UsesPropertyNameDirectly()
    {
        // Arrange
        var limitedMappings = new Dictionary<string, string> { ["Id"] = "Id" };
        var builder = new SpecificationSqlBuilder<TestOrderSqlite>(limitedMappings);
        var spec = new ActiveOrdersSqliteSpec();

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
        var parameters = new TestSqliteParameterCollection();

        command.Parameters.Returns(parameters);
        command.CreateParameter().Returns(_ => new TestSqliteParameter());

        return command;
    }

    #endregion
}

#region Test Helpers

/// <summary>
/// Simple test implementation of IDataParameterCollection for SQLite tests.
/// </summary>
file sealed class TestSqliteParameterCollection : IDataParameterCollection
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
/// Simple test implementation of IDbDataParameter for SQLite tests.
/// </summary>
file sealed class TestSqliteParameter : IDbDataParameter
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
/// Test entity for ADO.NET SQLite repository tests.
/// </summary>
public class TestOrderSqlite
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

public class CustomerOrdersSqliteSpec : Specification<TestOrderSqlite>
{
    private readonly Guid _customerId;
    public CustomerOrdersSqliteSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.CustomerId == _customerId;
}

public class NotCustomerSqliteSpec : Specification<TestOrderSqlite>
{
    private readonly Guid _customerId;
    public NotCustomerSqliteSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.CustomerId != _customerId;
}

public class MinTotalSqliteSpec : Specification<TestOrderSqlite>
{
    private readonly decimal _minTotal;
    public MinTotalSqliteSpec(decimal minTotal) => _minTotal = minTotal;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Total >= _minTotal;
}

public class MaxTotalSqliteSpec : Specification<TestOrderSqlite>
{
    private readonly decimal _maxTotal;
    public MaxTotalSqliteSpec(decimal maxTotal) => _maxTotal = maxTotal;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Total <= _maxTotal;
}

public class GreaterThanTotalSqliteSpec : Specification<TestOrderSqlite>
{
    private readonly decimal _total;
    public GreaterThanTotalSqliteSpec(decimal total) => _total = total;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Total > _total;
}

public class LessThanTotalSqliteSpec : Specification<TestOrderSqlite>
{
    private readonly decimal _total;
    public LessThanTotalSqliteSpec(decimal total) => _total = total;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Total < _total;
}

public class ActiveOrdersSqliteSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.IsActive;
}

public class NullDescriptionSqliteSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description == null;
}

public class HasDescriptionSqliteSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null;
}

public class DescriptionContainsSqliteSpec : Specification<TestOrderSqlite>
{
    private readonly string _searchTerm;
    public DescriptionContainsSqliteSpec(string searchTerm) => _searchTerm = searchTerm;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null && o.Description.Contains(_searchTerm);
}

public class DescriptionStartsWithSqliteSpec : Specification<TestOrderSqlite>
{
    private readonly string _prefix;
    public DescriptionStartsWithSqliteSpec(string prefix) => _prefix = prefix;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null && o.Description.StartsWith(_prefix);
}

public class DescriptionEndsWithSqliteSpec : Specification<TestOrderSqlite>
{
    private readonly string _suffix;
    public DescriptionEndsWithSqliteSpec(string suffix) => _suffix = suffix;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null && o.Description.EndsWith(_suffix);
}

public class TrueConstantSqliteSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => true;
}

public class FalseConstantSqliteSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => false;
}

public class DescriptionEqualsSqliteSpec : Specification<TestOrderSqlite>
{
    private readonly string _value;
    public DescriptionEqualsSqliteSpec(string value) => _value = value;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null && o.Description.Equals(_value, StringComparison.Ordinal);
}

public class DescriptionEqualsNullSqliteSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null && o.Description.Equals(null, StringComparison.Ordinal);
}

/// <summary>
/// Specification using Trim which is not a supported string method.
/// </summary>
public class TrimStringMethodSqliteSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Description != null && o.Description.Trim() == "TEST";
}

public class UnsupportedBinaryOperatorSqliteSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
    {
        // XOR is not supported
        var param = Expression.Parameter(typeof(TestOrderSqlite), "o");
        var left = Expression.Property(param, nameof(TestOrderSqlite.IsActive));
        var right = Expression.Constant(true);
        var xor = Expression.ExclusiveOr(left, right);
        return Expression.Lambda<Func<TestOrderSqlite, bool>>(xor, param);
    }
}

/// <summary>
/// Specification using Negate which is handled via Convert path.
/// </summary>
public class NegateOperatorSqliteSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
    {
        // Negate is handled via the Convert path in the switch expression
        var param = Expression.Parameter(typeof(TestOrderSqlite), "o");
        var total = Expression.Property(param, nameof(TestOrderSqlite.Total));
        var negated = Expression.Negate(total);
        var comparison = Expression.GreaterThan(negated, Expression.Constant(0m));
        return Expression.Lambda<Func<TestOrderSqlite, bool>>(comparison, param);
    }
}

public class UnsupportedExpressionSqliteSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
    {
        // NewArrayExpression is not supported at root level
        var param = Expression.Parameter(typeof(TestOrderSqlite), "o");
        var newArray = Expression.NewArrayInit(typeof(int), Expression.Constant(1));
        var lengthProp = Expression.ArrayLength(newArray);
        var comparison = Expression.Equal(lengthProp, Expression.Constant(1));
        return Expression.Lambda<Func<TestOrderSqlite, bool>>(comparison, param);
    }
}

public class UnsupportedMethodCallSqliteSpec : Specification<TestOrderSqlite>
{
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
    {
        // Math.Abs is not a string method and not supported
        var param = Expression.Parameter(typeof(TestOrderSqlite), "o");
        var total = Expression.Property(param, nameof(TestOrderSqlite.Total));
        var absMethod = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(decimal)])!;
        var absCall = Expression.Call(absMethod, total);
        var comparison = Expression.GreaterThan(absCall, Expression.Constant(0m));
        return Expression.Lambda<Func<TestOrderSqlite, bool>>(comparison, param);
    }
}

public class NullableTotalSqliteSpec : Specification<TestOrderSqlite>
{
    private readonly decimal? _total;
    public NullableTotalSqliteSpec(decimal? total) => _total = total;
    public override Expression<Func<TestOrderSqlite, bool>> ToExpression()
        => o => o.Total >= _total!.Value;
}

#endregion
