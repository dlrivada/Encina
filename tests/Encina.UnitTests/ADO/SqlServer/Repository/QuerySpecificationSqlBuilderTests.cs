using System.Collections;
using System.Data;
using System.Linq.Expressions;
using Encina.ADO.SqlServer.Repository;
using Encina.DomainModeling;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.SqlServer.Repository;

/// <summary>
/// Unit tests for <see cref="SpecificationSqlBuilder{TEntity}"/> QuerySpecification features in ADO.NET.
/// Tests ORDER BY, ThenBy, pagination, and keyset pagination.
/// </summary>
[Trait("Category", "Unit")]
public class QuerySpecificationSqlBuilderTests
{
    private readonly IReadOnlyDictionary<string, string> _columnMappings = new Dictionary<string, string>
    {
        ["Id"] = "Id",
        ["CustomerId"] = "CustomerId",
        ["Total"] = "Total",
        ["IsActive"] = "IsActive",
        ["Description"] = "Description",
        ["CreatedAtUtc"] = "CreatedAtUtc",
        ["Name"] = "Name"
    };

    #region BuildOrderByClause Tests

    [Fact]
    public void BuildOrderByClause_WithOrderBy_GeneratesAscendingOrder()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new OrderByNameQuerySpecADO();

        // Act
        var orderBy = builder.BuildOrderByClause(spec);

        // Assert
        orderBy.ShouldBe("ORDER BY [Name] ASC");
    }

    [Fact]
    public void BuildOrderByClause_WithOrderByDescending_GeneratesDescendingOrder()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new OrderByTotalDescQuerySpecADO();

        // Act
        var orderBy = builder.BuildOrderByClause(spec);

        // Assert
        orderBy.ShouldBe("ORDER BY [Total] DESC");
    }

    [Fact]
    public void BuildOrderByClause_WithThenBy_GeneratesMultiColumnOrder()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new OrderByNameThenByTotalQuerySpecADO();

        // Act
        var orderBy = builder.BuildOrderByClause(spec);

        // Assert
        orderBy.ShouldBe("ORDER BY [Name] ASC, [Total] ASC");
    }

    [Fact]
    public void BuildOrderByClause_WithThenByDescending_GeneratesMultiColumnOrder()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new OrderByNameThenByTotalDescQuerySpecADO();

        // Act
        var orderBy = builder.BuildOrderByClause(spec);

        // Assert
        orderBy.ShouldBe("ORDER BY [Name] ASC, [Total] DESC");
    }

    [Fact]
    public void BuildOrderByClause_WithMultipleThenBy_GeneratesComplexOrder()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new ComplexOrderQuerySpecADO();

        // Act
        var orderBy = builder.BuildOrderByClause(spec);

        // Assert
        orderBy.ShouldBe("ORDER BY [Name] DESC, [Total] ASC, [CreatedAtUtc] DESC");
    }

    [Fact]
    public void BuildOrderByClause_WithNoOrdering_ReturnsEmptyString()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new NoOrderingQuerySpecADO();

        // Act
        var orderBy = builder.BuildOrderByClause(spec);

        // Assert
        orderBy.ShouldBeEmpty();
    }

    [Fact]
    public void BuildOrderByClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildOrderByClause(null!));
    }

    #endregion

    #region BuildPaginationClause Tests

    [Fact]
    public void BuildPaginationClause_WithSkipAndTake_GeneratesOffsetFetch()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new PagedQuerySpecADO(skip: 10, take: 20);

        // Act
        var pagination = builder.BuildPaginationClause(spec);

        // Assert
        pagination.ShouldBe("OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY");
    }

    [Fact]
    public void BuildPaginationClause_WithSkipOnly_GeneratesOffsetFetchMaxValue()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new SkipOnlyQuerySpecADO(skip: 5);

        // Act
        var pagination = builder.BuildPaginationClause(spec);

        // Assert - API requires both skip and take, so we use MaxValue for take
        pagination.ShouldContain("OFFSET 5 ROWS");
        pagination.ShouldContain("FETCH NEXT");
    }

    [Fact]
    public void BuildPaginationClause_WithTakeOnly_GeneratesOffsetZeroFetch()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new TakeOnlyQuerySpecADO(take: 10);

        // Act
        var pagination = builder.BuildPaginationClause(spec);

        // Assert
        pagination.ShouldBe("OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY");
    }

    [Fact]
    public void BuildPaginationClause_WithNoPaging_ReturnsEmptyString()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new NoPagingQuerySpecADO();

        // Act
        var pagination = builder.BuildPaginationClause(spec);

        // Assert
        pagination.ShouldBeEmpty();
    }

    [Fact]
    public void BuildPaginationClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildPaginationClause(null!));
    }

    #endregion

    #region Keyset Pagination Tests

    [Fact]
    public void BuildWhereClause_WithKeysetPagination_GeneratesKeysetFilter()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var lastId = Guid.NewGuid();
        var spec = new KeysetPaginatedQuerySpecADO(lastId);

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        whereClause.ShouldContain("[IsActive] = 1");
        whereClause.ShouldContain("[Id] > @p");
        whereClause.ShouldContain("AND");
        parameters.Values.ShouldContain(lastId);
    }

    [Fact]
    public void BuildPaginationClause_WithKeysetPagination_GeneratesFetchOnly()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new KeysetPaginatedQuerySpecADO(Guid.NewGuid());

        // Act
        var pagination = builder.BuildPaginationClause(spec);

        // Assert
        pagination.ShouldBe("FETCH NEXT 10 ROWS ONLY");
        pagination.ShouldNotContain("OFFSET");
    }

    [Fact]
    public void BuildWhereClause_WithKeysetPaginationNoLastKey_DoesNotAddKeysetFilter()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new KeysetPaginatedNoLastKeyQuerySpecADO();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("[IsActive] = 1");
        whereClause.ShouldNotContain("[Id] >");
    }

    #endregion

    #region BuildSelectStatement with QuerySpecification Tests

    [Fact]
    public void BuildSelectStatement_WithQuerySpecification_IncludesAllClauses()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new FullFeaturedQuerySpecADO();

        // Act
        var (sql, addParameters) = builder.BuildSelectStatement("Orders", spec);
        var parameters = CaptureParameters(addParameters);

        // Assert
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM Orders");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("OFFSET");
        sql.ShouldContain("FETCH");
    }

    [Fact]
    public void BuildSelectStatement_WithKeysetPagination_IncludesOffsetZero()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new KeysetPaginatedQuerySpecADO(Guid.NewGuid());

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders", spec);

        // Assert
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("OFFSET 0 ROWS");
        sql.ShouldContain("FETCH NEXT 10 ROWS ONLY");
    }

    [Fact]
    public void BuildSelectStatement_WithQuerySpecification_NoOrdering_NoOrderByClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new NoOrderingQuerySpecADO();

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders", spec);

        // Assert
        sql.ShouldNotContain("ORDER BY");
    }

    [Fact]
    public void BuildSelectStatement_WithQuerySpecification_AddsParametersToCommand()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntityADO>(_columnMappings);
        var spec = new FullFeaturedQuerySpecADO();
        var command = CreateMockCommand();

        // Act
        var (_, addParameters) = builder.BuildSelectStatement("Orders", spec);
        addParameters(command);

        // Assert
        command.Parameters.Count.ShouldBeGreaterThan(0);
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
        var parameters = new TestParameterCollectionADO();

        command.Parameters.Returns(parameters);
        command.CreateParameter().Returns(_ => new TestParameterADO());

        return command;
    }

    #endregion
}

#region Test Helpers

/// <summary>
/// Simple test implementation of IDataParameterCollection for tests.
/// </summary>
file sealed class TestParameterCollectionADO : IDataParameterCollection
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
file sealed class TestParameterADO : IDbDataParameter
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
/// Test entity for QuerySpecification tests.
/// </summary>
public class TestOrderEntityADO
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

#endregion

#region Test QuerySpecifications

/// <summary>
/// QuerySpecification with OrderBy ascending.
/// </summary>
public class OrderByNameQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public OrderByNameQuerySpecADO()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
    }
}

/// <summary>
/// QuerySpecification with OrderByDescending.
/// </summary>
public class OrderByTotalDescQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public OrderByTotalDescQuerySpecADO()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderByDescending(e => e.Total);
    }
}

/// <summary>
/// QuerySpecification with OrderBy and ThenBy.
/// </summary>
public class OrderByNameThenByTotalQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public OrderByNameThenByTotalQuerySpecADO()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
        ApplyThenBy(e => e.Total);
    }
}

/// <summary>
/// QuerySpecification with OrderBy and ThenByDescending.
/// </summary>
public class OrderByNameThenByTotalDescQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public OrderByNameThenByTotalDescQuerySpecADO()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
        ApplyThenByDescending(e => e.Total);
    }
}

/// <summary>
/// QuerySpecification with complex multi-column ordering.
/// </summary>
public class ComplexOrderQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public ComplexOrderQuerySpecADO()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderByDescending(e => e.Name);
        ApplyThenBy(e => e.Total);
        ApplyThenByDescending(e => e.CreatedAtUtc);
    }
}

/// <summary>
/// QuerySpecification without any ordering.
/// </summary>
public class NoOrderingQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public NoOrderingQuerySpecADO()
    {
        AddCriteria(e => e.IsActive);
    }
}

/// <summary>
/// QuerySpecification with paging (skip and take).
/// </summary>
public class PagedQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public PagedQuerySpecADO(int skip, int take)
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
        ApplyPaging(skip, take);
    }
}

/// <summary>
/// QuerySpecification with skip only (simulated via large take).
/// Note: The API requires both skip and take via ApplyPaging.
/// </summary>
public class SkipOnlyQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public SkipOnlyQuerySpecADO(int skip)
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
        // Use very large take to simulate "skip only"
        ApplyPaging(skip, int.MaxValue);
    }
}

/// <summary>
/// QuerySpecification with take only (zero skip).
/// </summary>
public class TakeOnlyQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public TakeOnlyQuerySpecADO(int take)
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
        ApplyPaging(0, take);
    }
}

/// <summary>
/// QuerySpecification without paging.
/// </summary>
public class NoPagingQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public NoPagingQuerySpecADO()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
    }
}

/// <summary>
/// QuerySpecification with keyset pagination.
/// </summary>
public class KeysetPaginatedQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public KeysetPaginatedQuerySpecADO(Guid lastId)
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Id);
        ApplyKeysetPagination(e => e.Id, lastId, 10);
    }
}

/// <summary>
/// QuerySpecification with keyset pagination but no last key value.
/// </summary>
public class KeysetPaginatedNoLastKeyQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public KeysetPaginatedNoLastKeyQuerySpecADO()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Id);
        ApplyKeysetPagination(e => e.Id, null, 10);
    }
}

/// <summary>
/// QuerySpecification with all features enabled.
/// </summary>
public class FullFeaturedQuerySpecADO : QuerySpecification<TestOrderEntityADO>
{
    public FullFeaturedQuerySpecADO()
    {
        AddCriteria(e => e.IsActive);
        AddCriteria(e => e.Total > 100);
        ApplyOrderBy(e => e.Name);
        ApplyThenByDescending(e => e.CreatedAtUtc);
        ApplyPaging(5, 10);
    }
}

#endregion
