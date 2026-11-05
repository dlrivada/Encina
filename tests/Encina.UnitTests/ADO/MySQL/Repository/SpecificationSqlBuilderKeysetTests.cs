using System.Linq.Expressions;
using Encina.ADO.MySQL.Repository;
using Encina.DomainModeling;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.MySQL.Repository;

/// <summary>
/// Unit tests for the keyset filter direction of <see cref="SpecificationSqlBuilder{TEntity}"/>:
/// the next page holds the keys after the last one in the sort order (#3154).
/// </summary>
[Trait("Category", "Unit")]
public class SpecificationSqlBuilderKeysetTests
{
    private readonly IReadOnlyDictionary<string, string> _columnMappings = new Dictionary<string, string>
    {
        ["Id"] = "Id",
        ["Total"] = "Total",
    };

    [Fact]
    public void BuildWhereClause_KeysetSortedAscending_SelectsKeysAboveTheLastOne()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<KeysetOrder>(_columnMappings);
        var spec = new TotalKeysetPageSpec(descending: false, lastTotal: 50m);

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("`Total` > @p");
    }

    [Fact]
    public void BuildWhereClause_KeysetSortedDescending_SelectsKeysBelowTheLastOne()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<KeysetOrder>(_columnMappings);
        var spec = new TotalKeysetPageSpec(descending: true, lastTotal: 50m);

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("`Total` < @p");
    }

    [Fact]
    public void BuildWhereClause_FirstKeysetPage_HasNoKeysetFilter()
    {
        // Arrange - no last key yet: the first page is not filtered
        var builder = new SpecificationSqlBuilder<KeysetOrder>(_columnMappings);
        var spec = new TotalKeysetPageSpec(descending: true, lastTotal: null);

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldNotContain("Total");
    }

    [Fact]
    public void BuildWhereClause_WithoutKeysetPagination_HasNoKeysetFilter()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<KeysetOrder>(_columnMappings);
        var spec = new TotalOrderedWithoutKeysetSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldNotContain("Total");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(50)]
    public void BuildWhereClause_KeysetNotThePrimaryOrdering_ThrowsNotSupportedException(int? lastTotal)
    {
        // Arrange - ordered by Id with the keyset in a ThenByDescending: rejected on the first page too
        var builder = new SpecificationSqlBuilder<KeysetOrder>(_columnMappings);
        var spec = new TotalKeysetThenBySpec(lastTotal);

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void BuildSelectStatement_KeysetSortedDescending_FiltersInTheOrderDirection()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<KeysetOrder>(_columnMappings);
        var spec = new TotalKeysetPageSpec(descending: true, lastTotal: 50m);

        // Act
        var (sql, _) = builder.BuildSelectStatement("Orders", spec);

        // Assert
        sql.ShouldContain("`Total` < @p");
        sql.ShouldContain("ORDER BY `Total` DESC");
    }
}

/// <summary>
/// Entity for the keyset direction tests.
/// </summary>
public class KeysetOrder
{
    public Guid Id { get; set; }
    public decimal Total { get; set; }
}

/// <summary>
/// Page of orders by total, sorted ascending or descending, after <c>lastTotal</c> (the first page when it is null).
/// </summary>
public class TotalKeysetPageSpec : QuerySpecification<KeysetOrder>
{
    public TotalKeysetPageSpec(bool descending, decimal? lastTotal)
    {
        Expression<Func<KeysetOrder, object>> total = o => o.Total;
        if (descending)
        {
            ApplyOrderByDescending(total);
        }
        else
        {
            ApplyOrderBy(total);
        }

        ApplyKeysetPagination(total, lastTotal, 10);
    }
}

/// <summary>
/// Page of orders sorted by id, then by total descending, with the keyset on total: not the primary ordering.
/// </summary>
public class TotalKeysetThenBySpec : QuerySpecification<KeysetOrder>
{
    public TotalKeysetThenBySpec(decimal? lastTotal)
    {
        ApplyOrderBy(o => o.Id);
        ApplyThenByDescending(o => o.Total);
        ApplyKeysetPagination(o => o.Total, lastTotal, 10);
    }
}

/// <summary>
/// Orders sorted by total, without keyset pagination.
/// </summary>
public class TotalOrderedWithoutKeysetSpec : QuerySpecification<KeysetOrder>
{
    public TotalOrderedWithoutKeysetSpec() => ApplyOrderByDescending(o => o.Total);
}
