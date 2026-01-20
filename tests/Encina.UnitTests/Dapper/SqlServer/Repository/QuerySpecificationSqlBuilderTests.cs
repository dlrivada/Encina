using System.Linq.Expressions;
using Encina.Dapper.SqlServer.Repository;
using Encina.DomainModeling;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.SqlServer.Repository;

/// <summary>
/// Unit tests for <see cref="SpecificationSqlBuilder{TEntity}"/> QuerySpecification features.
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
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new OrderByNameQuerySpec();

        // Act
        var orderBy = builder.BuildOrderByClause(spec);

        // Assert
        orderBy.ShouldBe("ORDER BY [Name] ASC");
    }

    [Fact]
    public void BuildOrderByClause_WithOrderByDescending_GeneratesDescendingOrder()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new OrderByTotalDescQuerySpec();

        // Act
        var orderBy = builder.BuildOrderByClause(spec);

        // Assert
        orderBy.ShouldBe("ORDER BY [Total] DESC");
    }

    [Fact]
    public void BuildOrderByClause_WithThenBy_GeneratesMultiColumnOrder()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new OrderByNameThenByTotalQuerySpec();

        // Act
        var orderBy = builder.BuildOrderByClause(spec);

        // Assert
        orderBy.ShouldBe("ORDER BY [Name] ASC, [Total] ASC");
    }

    [Fact]
    public void BuildOrderByClause_WithThenByDescending_GeneratesMultiColumnOrder()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new OrderByNameThenByTotalDescQuerySpec();

        // Act
        var orderBy = builder.BuildOrderByClause(spec);

        // Assert
        orderBy.ShouldBe("ORDER BY [Name] ASC, [Total] DESC");
    }

    [Fact]
    public void BuildOrderByClause_WithMultipleThenBy_GeneratesComplexOrder()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new ComplexOrderQuerySpec();

        // Act
        var orderBy = builder.BuildOrderByClause(spec);

        // Assert
        orderBy.ShouldBe("ORDER BY [Name] DESC, [Total] ASC, [CreatedAtUtc] DESC");
    }

    [Fact]
    public void BuildOrderByClause_WithNoOrdering_ReturnsEmptyString()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new NoOrderingQuerySpec();

        // Act
        var orderBy = builder.BuildOrderByClause(spec);

        // Assert
        orderBy.ShouldBeEmpty();
    }

    [Fact]
    public void BuildOrderByClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);

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
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new PagedQuerySpec(skip: 10, take: 20);

        // Act
        var pagination = builder.BuildPaginationClause(spec);

        // Assert
        pagination.ShouldBe("OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY");
    }

    [Fact]
    public void BuildPaginationClause_WithSkipOnly_GeneratesOffsetFetchMaxValue()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new SkipOnlyQuerySpec(skip: 5);

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
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new TakeOnlyQuerySpec(take: 10);

        // Act
        var pagination = builder.BuildPaginationClause(spec);

        // Assert
        pagination.ShouldBe("OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY");
    }

    [Fact]
    public void BuildPaginationClause_WithNoPaging_ReturnsEmptyString()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new NoPagingQuerySpec();

        // Act
        var pagination = builder.BuildPaginationClause(spec);

        // Assert
        pagination.ShouldBeEmpty();
    }

    [Fact]
    public void BuildPaginationClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);

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
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var lastId = Guid.NewGuid();
        var spec = new KeysetPaginatedQuerySpec(lastId);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

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
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new KeysetPaginatedQuerySpec(Guid.NewGuid());

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
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new KeysetPaginatedNoLastKeyQuerySpec();

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
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new FullFeaturedQuerySpec();

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("Orders", spec);

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
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new KeysetPaginatedQuerySpec(Guid.NewGuid());

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
        var builder = new SpecificationSqlBuilder<TestOrderEntity>(_columnMappings);
        var spec = new NoOrderingQuerySpec();

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders", spec);

        // Assert
        sql.ShouldNotContain("ORDER BY");
    }

    #endregion
}

#region Test Entity

/// <summary>
/// Test entity for QuerySpecification tests.
/// </summary>
public class TestOrderEntity
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
public class OrderByNameQuerySpec : QuerySpecification<TestOrderEntity>
{
    public OrderByNameQuerySpec()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
    }
}

/// <summary>
/// QuerySpecification with OrderByDescending.
/// </summary>
public class OrderByTotalDescQuerySpec : QuerySpecification<TestOrderEntity>
{
    public OrderByTotalDescQuerySpec()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderByDescending(e => e.Total);
    }
}

/// <summary>
/// QuerySpecification with OrderBy and ThenBy.
/// </summary>
public class OrderByNameThenByTotalQuerySpec : QuerySpecification<TestOrderEntity>
{
    public OrderByNameThenByTotalQuerySpec()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
        ApplyThenBy(e => e.Total);
    }
}

/// <summary>
/// QuerySpecification with OrderBy and ThenByDescending.
/// </summary>
public class OrderByNameThenByTotalDescQuerySpec : QuerySpecification<TestOrderEntity>
{
    public OrderByNameThenByTotalDescQuerySpec()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
        ApplyThenByDescending(e => e.Total);
    }
}

/// <summary>
/// QuerySpecification with complex multi-column ordering.
/// </summary>
public class ComplexOrderQuerySpec : QuerySpecification<TestOrderEntity>
{
    public ComplexOrderQuerySpec()
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
public class NoOrderingQuerySpec : QuerySpecification<TestOrderEntity>
{
    public NoOrderingQuerySpec()
    {
        AddCriteria(e => e.IsActive);
    }
}

/// <summary>
/// QuerySpecification with paging (skip and take).
/// </summary>
public class PagedQuerySpec : QuerySpecification<TestOrderEntity>
{
    public PagedQuerySpec(int skip, int take)
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
public class SkipOnlyQuerySpec : QuerySpecification<TestOrderEntity>
{
    public SkipOnlyQuerySpec(int skip)
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
public class TakeOnlyQuerySpec : QuerySpecification<TestOrderEntity>
{
    public TakeOnlyQuerySpec(int take)
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
        ApplyPaging(0, take);
    }
}

/// <summary>
/// QuerySpecification without paging.
/// </summary>
public class NoPagingQuerySpec : QuerySpecification<TestOrderEntity>
{
    public NoPagingQuerySpec()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
    }
}

/// <summary>
/// QuerySpecification with keyset pagination.
/// </summary>
public class KeysetPaginatedQuerySpec : QuerySpecification<TestOrderEntity>
{
    public KeysetPaginatedQuerySpec(Guid lastId)
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Id);
        ApplyKeysetPagination(e => e.Id, lastId, 10);
    }
}

/// <summary>
/// QuerySpecification with keyset pagination but no last key value.
/// </summary>
public class KeysetPaginatedNoLastKeyQuerySpec : QuerySpecification<TestOrderEntity>
{
    public KeysetPaginatedNoLastKeyQuerySpec()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Id);
        ApplyKeysetPagination(e => e.Id, null, 10);
    }
}

/// <summary>
/// QuerySpecification with all features enabled.
/// </summary>
public class FullFeaturedQuerySpec : QuerySpecification<TestOrderEntity>
{
    public FullFeaturedQuerySpec()
    {
        AddCriteria(e => e.IsActive);
        AddCriteria(e => e.Total > 100);
        ApplyOrderBy(e => e.Name);
        ApplyThenByDescending(e => e.CreatedAtUtc);
        ApplyPaging(5, 10);
    }
}

#endregion
