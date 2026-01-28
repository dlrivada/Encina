using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using Encina.ADO.Benchmarks.Infrastructure;
using Encina.ADO.Sqlite.Repository;
using Encina.DomainModeling;

namespace Encina.ADO.Benchmarks.Repository;

/// <summary>
/// Benchmarks for SpecificationSqlBuilder SQL generation (not execution) for ADO.NET.
/// </summary>
/// <remarks>
/// Since all providers share the same expression translation logic,
/// only the SQL syntax differs (identifier quoting, pagination syntax).
/// We benchmark SQLite as the representative provider.
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class SpecificationSqlBuilderBenchmarks
{
    private readonly IReadOnlyDictionary<string, string> _columnMappings = new Dictionary<string, string>
    {
        ["Id"] = "Id",
        ["Name"] = "Name",
        ["Description"] = "Description",
        ["Price"] = "Price",
        ["Quantity"] = "Quantity",
        ["IsActive"] = "IsActive",
        ["Category"] = "Category",
        ["CreatedAtUtc"] = "CreatedAtUtc",
        ["UpdatedAtUtc"] = "UpdatedAtUtc"
    };

    private SpecificationSqlBuilder<BenchmarkRepositoryEntity> _sqlBuilder = null!;

    // Pre-built specifications for benchmarking
    private SimpleEqualitySpec _simpleEquality = null!;
    private MultipleAndConditionsSpec _multipleAnd = null!;
    private OrCombinationSpec _orCombination = null!;
    private StringContainsSpec _stringContains = null!;
    private StringStartsWithSpec _stringStartsWith = null!;
    private StringEndsWithSpec _stringEndsWith = null!;
    private PaginatedQuerySpec _paginatedQuery = null!;
    private MultiColumnOrderSpec _multiColumnOrder = null!;

    /// <summary>
    /// Global setup - initializes builder and specifications.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _sqlBuilder = new SpecificationSqlBuilder<BenchmarkRepositoryEntity>(_columnMappings);

        // Pre-build specifications
        _simpleEquality = new SimpleEqualitySpec(Guid.NewGuid());
        _multipleAnd = new MultipleAndConditionsSpec("Electronics", true, 50);
        _orCombination = new OrCombinationSpec(Guid.NewGuid(), Guid.NewGuid());
        _stringContains = new StringContainsSpec("Product");
        _stringStartsWith = new StringStartsWithSpec("Premium");
        _stringEndsWith = new StringEndsWithSpec("Sale");
        _paginatedQuery = new PaginatedQuerySpec("Electronics", 20, 100);
        _multiColumnOrder = new MultiColumnOrderSpec("Electronics");
    }

    #region WHERE Clause - Single Equality Condition

    /// <summary>
    /// Benchmarks WHERE clause generation with single equality condition.
    /// </summary>
    [Benchmark(Baseline = true)]
    public string BuildWhereClause_SingleEquality()
    {
        var (whereClause, _) = _sqlBuilder.BuildWhereClause(_simpleEquality);
        return whereClause;
    }

    #endregion

    #region WHERE Clause - Multiple AND Conditions

    /// <summary>
    /// Benchmarks WHERE clause generation with multiple AND conditions.
    /// </summary>
    [Benchmark]
    public string BuildWhereClause_MultipleAnd()
    {
        var (whereClause, _) = _sqlBuilder.BuildWhereClause(_multipleAnd);
        return whereClause;
    }

    #endregion

    #region WHERE Clause - OR Combinations

    /// <summary>
    /// Benchmarks WHERE clause generation with OR combinations.
    /// </summary>
    [Benchmark]
    public string BuildWhereClause_OrCombination()
    {
        var (whereClause, _) = _sqlBuilder.BuildWhereClause(_orCombination);
        return whereClause;
    }

    #endregion

    #region WHERE Clause - String Operations

    /// <summary>
    /// Benchmarks WHERE clause generation with string Contains.
    /// </summary>
    [Benchmark]
    public string BuildWhereClause_StringContains()
    {
        var (whereClause, _) = _sqlBuilder.BuildWhereClause(_stringContains);
        return whereClause;
    }

    /// <summary>
    /// Benchmarks WHERE clause generation with string StartsWith.
    /// </summary>
    [Benchmark]
    public string BuildWhereClause_StringStartsWith()
    {
        var (whereClause, _) = _sqlBuilder.BuildWhereClause(_stringStartsWith);
        return whereClause;
    }

    /// <summary>
    /// Benchmarks WHERE clause generation with string EndsWith.
    /// </summary>
    [Benchmark]
    public string BuildWhereClause_StringEndsWith()
    {
        var (whereClause, _) = _sqlBuilder.BuildWhereClause(_stringEndsWith);
        return whereClause;
    }

    #endregion

    #region ORDER BY Clause

    /// <summary>
    /// Benchmarks ORDER BY clause generation with single column.
    /// </summary>
    [Benchmark]
    public string BuildOrderByClause_SingleColumn()
    {
        var spec = new SingleColumnOrderSpec("Electronics");
        return _sqlBuilder.BuildOrderByClause(spec);
    }

    /// <summary>
    /// Benchmarks ORDER BY clause generation with multiple columns.
    /// </summary>
    [Benchmark]
    public string BuildOrderByClause_MultipleColumns()
    {
        return _sqlBuilder.BuildOrderByClause(_multiColumnOrder);
    }

    #endregion

    #region Pagination Clause

    /// <summary>
    /// Benchmarks pagination clause generation (LIMIT/OFFSET for SQLite).
    /// </summary>
    [Benchmark]
    public string BuildPaginationClause()
    {
        return _sqlBuilder.BuildPaginationClause(_paginatedQuery);
    }

    #endregion

    #region Complete SELECT Statement

    /// <summary>
    /// Benchmarks complete SELECT statement generation.
    /// </summary>
    [Benchmark]
    public string BuildSelectStatement_Complete()
    {
        var (sql, _) = _sqlBuilder.BuildSelectStatement("BenchmarkEntities", _paginatedQuery);
        return sql;
    }

    #endregion

    #region Specification Reuse Pattern

    /// <summary>
    /// Benchmarks using pre-built specification (reuse pattern).
    /// </summary>
    [Benchmark]
    public string SpecificationReuse_PreBuilt()
    {
        var (whereClause, _) = _sqlBuilder.BuildWhereClause(_multipleAnd);
        return whereClause;
    }

    /// <summary>
    /// Benchmarks creating new specification each time (dynamic creation).
    /// </summary>
    [Benchmark]
    public string SpecificationReuse_DynamicCreation()
    {
        var dynamicSpec = new MultipleAndConditionsSpec("Electronics", true, 50);
        var (whereClause, _) = _sqlBuilder.BuildWhereClause(dynamicSpec);
        return whereClause;
    }

    #endregion
}

#region Benchmark Specifications

/// <summary>
/// Simple equality specification: e => e.Id == id
/// </summary>
public class SimpleEqualitySpec : Specification<BenchmarkRepositoryEntity>
{
    private readonly Guid _id;

    public SimpleEqualitySpec(Guid id) => _id = id;

    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => e.Id == _id;
}

/// <summary>
/// Multiple AND conditions: e => e.Category == category && e.IsActive == isActive && e.Quantity > minQuantity
/// </summary>
public class MultipleAndConditionsSpec : Specification<BenchmarkRepositoryEntity>
{
    private readonly string _category;
    private readonly bool _isActive;
    private readonly int _minQuantity;

    public MultipleAndConditionsSpec(string category, bool isActive, int minQuantity)
    {
        _category = category;
        _isActive = isActive;
        _minQuantity = minQuantity;
    }

    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => e.Category == _category && e.IsActive == _isActive && e.Quantity > _minQuantity;
}

/// <summary>
/// OR combination: e => e.Id == id1 || e.Id == id2
/// </summary>
public class OrCombinationSpec : Specification<BenchmarkRepositoryEntity>
{
    private readonly Guid _id1;
    private readonly Guid _id2;

    public OrCombinationSpec(Guid id1, Guid id2)
    {
        _id1 = id1;
        _id2 = id2;
    }

    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => e.Id == _id1 || e.Id == _id2;
}

/// <summary>
/// String Contains: e => e.Name.Contains(value)
/// </summary>
public class StringContainsSpec : Specification<BenchmarkRepositoryEntity>
{
    private readonly string _value;

    public StringContainsSpec(string value) => _value = value;

    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => e.Name.Contains(_value);
}

/// <summary>
/// String StartsWith: e => e.Name.StartsWith(value)
/// </summary>
public class StringStartsWithSpec : Specification<BenchmarkRepositoryEntity>
{
    private readonly string _value;

    public StringStartsWithSpec(string value) => _value = value;

    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => e.Name.StartsWith(_value);
}

/// <summary>
/// String EndsWith: e => e.Name.EndsWith(value)
/// </summary>
public class StringEndsWithSpec : Specification<BenchmarkRepositoryEntity>
{
    private readonly string _value;

    public StringEndsWithSpec(string value) => _value = value;

    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => e.Name.EndsWith(_value);
}

/// <summary>
/// Single column order specification.
/// </summary>
public class SingleColumnOrderSpec : QuerySpecification<BenchmarkRepositoryEntity>
{
    public SingleColumnOrderSpec(string category)
    {
        AddCriteria(e => e.Category == category);
        ApplyOrderBy(e => e.Name);
    }

    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => true;
}

/// <summary>
/// Multi-column order specification.
/// </summary>
public class MultiColumnOrderSpec : QuerySpecification<BenchmarkRepositoryEntity>
{
    public MultiColumnOrderSpec(string category)
    {
        AddCriteria(e => e.Category == category);
        ApplyOrderByDescending(e => e.Price);
        ApplyThenBy(e => e.Name);
        ApplyThenByDescending(e => e.CreatedAtUtc);
    }

    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => true;
}

/// <summary>
/// Paginated query specification.
/// </summary>
public class PaginatedQuerySpec : QuerySpecification<BenchmarkRepositoryEntity>
{
    public PaginatedQuerySpec(string category, int skip, int take)
    {
        AddCriteria(e => e.Category == category && e.IsActive);
        ApplyOrderBy(e => e.Name);
        ApplyPaging(skip, take);
    }

    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => true;
}

#endregion
