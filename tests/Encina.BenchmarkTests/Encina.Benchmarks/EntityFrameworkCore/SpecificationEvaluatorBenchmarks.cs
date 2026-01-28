using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using Encina.Benchmarks.EntityFrameworkCore.Infrastructure;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using Microsoft.EntityFrameworkCore;

namespace Encina.Benchmarks.EntityFrameworkCore;

/// <summary>
/// Benchmarks for <see cref="SpecificationEvaluator"/> measuring the specification
/// evaluation pipeline that builds query expressions for every specification-based query.
/// </summary>
/// <remarks>
/// <para>
/// <b>Performance Targets:</b>
/// <list type="bullet">
///   <item><description>Simple predicate application: &lt;100ns</description></item>
///   <item><description>Keyset pagination expression building: 1-5μs</description></item>
///   <item><description>Full specification application: &lt;10μs</description></item>
/// </list>
/// </para>
/// <para>
/// <b>CA1001 Suppression:</b> BenchmarkDotNet manages lifecycle via [GlobalSetup]/[GlobalCleanup].
/// Implementing IDisposable would interfere with BenchmarkDotNet's resource management.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
[RankColumn]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class SpecificationEvaluatorBenchmarks
#pragma warning restore CA1001
{
    private EntityFrameworkBenchmarkDbContext _dbContext = null!;
    private IQueryable<BenchmarkEntity> _baseQuery = null!;

    // Pre-created specifications for consistent benchmarking
    private SimpleWhereSpec _simpleSpec = null!;
    private ComplexCriteriaSpec _complexSpec2 = null!;
    private ComplexCriteriaSpec _complexSpec5 = null!;
    private ComplexCriteriaSpec _complexSpec10 = null!;
    private KeysetPaginationSpec _keysetSpec = null!;
    private LambdaIncludeSpec _lambdaIncludeSpec = null!;
    private StringIncludeSpec _stringIncludeSpec = null!;
    private FullSpec _fullSpec = null!;
    private OrderingSpec _orderingSpec = null!;
    private OffsetPaginationSpec _offsetPaginationSpec = null!;

    /// <summary>
    /// Number of criteria to combine for complex predicate benchmarks.
    /// </summary>
    [Params(2, 5, 10)]
    public int CriteriaCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Use InMemory database for pure CPU measurement
        _dbContext = EntityFrameworkBenchmarkDbContext.CreateInMemory("SpecBenchmarks");
        _baseQuery = _dbContext.BenchmarkEntities.AsQueryable();

        // Pre-create specifications
        _simpleSpec = new SimpleWhereSpec("Test");
        _complexSpec2 = new ComplexCriteriaSpec(2);
        _complexSpec5 = new ComplexCriteriaSpec(5);
        _complexSpec10 = new ComplexCriteriaSpec(10);
        _keysetSpec = new KeysetPaginationSpec(Guid.NewGuid(), 10);
        _lambdaIncludeSpec = new LambdaIncludeSpec();
        _stringIncludeSpec = new StringIncludeSpec();
        _fullSpec = new FullSpec("Electronics", 100m, 10);
        _orderingSpec = new OrderingSpec();
        _offsetPaginationSpec = new OffsetPaginationSpec(0, 10);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _dbContext?.Dispose();
    }

    #region Simple Predicate Benchmarks

    /// <summary>
    /// Simple predicate application (single Where criterion).
    /// Target: &lt;100ns
    /// </summary>
    /// <remarks>
    /// Returns List to force query materialization for BenchmarkDotNet.
    /// We measure expression tree building + query execution combined.
    /// </remarks>
    [Benchmark(Baseline = true, Description = "Simple Where (single criterion)")]
    public List<BenchmarkEntity> SimplePredicate_SingleWhere()
    {
        return SpecificationEvaluator.GetQuery(_baseQuery, _simpleSpec).ToList();
    }

    /// <summary>
    /// Baseline: Direct LINQ Where without specification pattern.
    /// </summary>
    [Benchmark(Description = "Direct LINQ Where (baseline)")]
    public List<BenchmarkEntity> DirectLinq_Where()
    {
        return _baseQuery.Where(e => e.Name.StartsWith("Test")).ToList();
    }

    #endregion

    #region Complex Predicate Benchmarks

    /// <summary>
    /// Complex predicates with parameterized criteria count.
    /// Measures expression building overhead for multiple AND-combined criteria.
    /// </summary>
    [Benchmark(Description = "Complex predicates (parameterized)")]
    public List<BenchmarkEntity> ComplexPredicates_Parameterized()
    {
        return CriteriaCount switch
        {
            2 => SpecificationEvaluator.GetQuery(_baseQuery, _complexSpec2).ToList(),
            5 => SpecificationEvaluator.GetQuery(_baseQuery, _complexSpec5).ToList(),
            10 => SpecificationEvaluator.GetQuery(_baseQuery, _complexSpec10).ToList(),
            _ => SpecificationEvaluator.GetQuery(_baseQuery, _complexSpec2).ToList()
        };
    }

    /// <summary>
    /// Two criteria combined with AND logic.
    /// </summary>
    [Benchmark(Description = "Two criteria (AND)")]
    public List<BenchmarkEntity> TwoCriteria_And()
    {
        return SpecificationEvaluator.GetQuery(_baseQuery, _complexSpec2).ToList();
    }

    /// <summary>
    /// Five criteria combined with AND logic.
    /// </summary>
    [Benchmark(Description = "Five criteria (AND)")]
    public List<BenchmarkEntity> FiveCriteria_And()
    {
        return SpecificationEvaluator.GetQuery(_baseQuery, _complexSpec5).ToList();
    }

    /// <summary>
    /// Ten criteria combined with AND logic.
    /// </summary>
    [Benchmark(Description = "Ten criteria (AND)")]
    public List<BenchmarkEntity> TenCriteria_And()
    {
        return SpecificationEvaluator.GetQuery(_baseQuery, _complexSpec10).ToList();
    }

    #endregion

    #region Keyset Pagination Benchmarks

    /// <summary>
    /// Keyset pagination expression building.
    /// Target: 1-5μs
    /// </summary>
    [Benchmark(Description = "Keyset pagination")]
    public List<BenchmarkEntity> KeysetPagination_ExpressionBuilding()
    {
        return SpecificationEvaluator.GetQuery(_baseQuery, _keysetSpec).ToList();
    }

    /// <summary>
    /// Keyset pagination with fresh cursor value each iteration.
    /// Tests expression tree construction overhead.
    /// </summary>
    [Benchmark(Description = "Keyset pagination (fresh cursor)")]
    public List<BenchmarkEntity> KeysetPagination_FreshCursor()
    {
        var spec = new KeysetPaginationSpec(Guid.NewGuid(), 10);
        return SpecificationEvaluator.GetQuery(_baseQuery, spec).ToList();
    }

    #endregion

    #region Include Benchmarks

    /// <summary>
    /// Lambda-based include (type-safe navigation property).
    /// </summary>
    [Benchmark(Description = "Lambda Include")]
    public List<BenchmarkEntity> Include_Lambda()
    {
        return SpecificationEvaluator.GetQuery(_baseQuery, _lambdaIncludeSpec).ToList();
    }

    /// <summary>
    /// String-based include.
    /// </summary>
    [Benchmark(Description = "String Include")]
    public List<BenchmarkEntity> Include_String()
    {
        return SpecificationEvaluator.GetQuery(_baseQuery, _stringIncludeSpec).ToList();
    }

    #endregion

    #region Ordering Benchmarks

    /// <summary>
    /// Multi-column ordering (OrderBy + ThenBy).
    /// </summary>
    [Benchmark(Description = "Multi-column ordering")]
    public List<BenchmarkEntity> MultiColumnOrdering()
    {
        return SpecificationEvaluator.GetQuery(_baseQuery, _orderingSpec).ToList();
    }

    #endregion

    #region Pagination Benchmarks

    /// <summary>
    /// Offset-based pagination (Skip/Take).
    /// </summary>
    [Benchmark(Description = "Offset pagination (Skip/Take)")]
    public List<BenchmarkEntity> OffsetPagination()
    {
        return SpecificationEvaluator.GetQuery(_baseQuery, _offsetPaginationSpec).ToList();
    }

    #endregion

    #region Full Specification Benchmarks

    /// <summary>
    /// Full specification application combining predicates, ordering, pagination, and includes.
    /// Target: &lt;10μs
    /// </summary>
    [Benchmark(Description = "Full specification (all features)")]
    public List<BenchmarkEntity> FullSpecification_AllFeatures()
    {
        return SpecificationEvaluator.GetQuery(_baseQuery, _fullSpec).ToList();
    }

    #endregion

    #region Test Specification Classes

    /// <summary>
    /// Simple specification with single Where criterion.
    /// </summary>
    private sealed class SimpleWhereSpec : QuerySpecification<BenchmarkEntity>
    {
        public SimpleWhereSpec(string namePrefix)
        {
            AddCriteria(e => e.Name.StartsWith(namePrefix));
        }

        public override Expression<Func<BenchmarkEntity, bool>> ToExpression()
            => e => e.Name.StartsWith("Test");
    }

    /// <summary>
    /// Specification with varying numbers of criteria combined with AND.
    /// </summary>
    private sealed class ComplexCriteriaSpec : QuerySpecification<BenchmarkEntity>
    {
        public ComplexCriteriaSpec(int criteriaCount)
        {
            // Add specified number of criteria
            AddCriteria(e => e.IsActive);

            if (criteriaCount >= 2)
                AddCriteria(e => e.Value > 0);

            if (criteriaCount >= 3)
                AddCriteria(e => e.CreatedAtUtc > DateTime.MinValue);

            if (criteriaCount >= 4)
                AddCriteria(e => e.Category != null);

            if (criteriaCount >= 5)
                AddCriteria(e => e.Name.Length > 0);

            if (criteriaCount >= 6)
                AddCriteria(e => e.Value < 10000);

            if (criteriaCount >= 7)
                AddCriteria(e => e.CreatedAtUtc < DateTime.MaxValue);

            if (criteriaCount >= 8)
                AddCriteria(e => !string.IsNullOrEmpty(e.Name));

            if (criteriaCount >= 9)
                AddCriteria(e => e.Id != Guid.Empty);

            if (criteriaCount >= 10)
                AddCriteria(e => e.Category != "Deleted");
        }
    }

    /// <summary>
    /// Specification with keyset pagination.
    /// </summary>
    private sealed class KeysetPaginationSpec : QuerySpecification<BenchmarkEntity>
    {
        public KeysetPaginationSpec(Guid? lastId, int pageSize)
        {
            AddCriteria(e => e.IsActive);
            ApplyOrderBy(e => e.Id);

            if (lastId.HasValue)
            {
                ApplyKeysetPagination(e => e.Id, lastId.Value, pageSize);
            }
            else
            {
                ApplyPaging(0, pageSize);
            }
        }
    }

    /// <summary>
    /// Specification with lambda-based include.
    /// Note: BenchmarkEntity doesn't have navigation properties,
    /// so we simulate the pattern without actual navigation.
    /// </summary>
    private sealed class LambdaIncludeSpec : QuerySpecification<BenchmarkEntity>
    {
        public LambdaIncludeSpec()
        {
            AddCriteria(e => e.IsActive);
            // In a real scenario with navigation properties:
            // AddInclude(e => e.RelatedEntity);
            // For benchmark, we measure the spec overhead without actual navigation
        }
    }

    /// <summary>
    /// Specification with string-based include.
    /// Note: BenchmarkEntity has no navigation properties, so this spec
    /// measures only the criteria application overhead. The include
    /// functionality is tested in integration tests with real entities.
    /// </summary>
    private sealed class StringIncludeSpec : QuerySpecification<BenchmarkEntity>
    {
        public StringIncludeSpec()
        {
            AddCriteria(e => e.IsActive);
            // String include is not added because BenchmarkEntity has no navigation properties
            // This benchmark measures the spec infrastructure overhead only
        }
    }

    /// <summary>
    /// Specification with multi-column ordering.
    /// </summary>
    private sealed class OrderingSpec : QuerySpecification<BenchmarkEntity>
    {
        public OrderingSpec()
        {
            AddCriteria(e => e.IsActive);
            ApplyOrderByDescending(e => e.CreatedAtUtc);
            ApplyThenBy(e => e.Name);
            ApplyThenBy(e => e.Id);
        }
    }

    /// <summary>
    /// Specification with offset-based pagination.
    /// </summary>
    private sealed class OffsetPaginationSpec : QuerySpecification<BenchmarkEntity>
    {
        public OffsetPaginationSpec(int skip, int take)
        {
            AddCriteria(e => e.IsActive);
            ApplyOrderBy(e => e.CreatedAtUtc);
            ApplyPaging(skip, take);
        }
    }

    /// <summary>
    /// Full specification combining all features.
    /// </summary>
    private sealed class FullSpec : QuerySpecification<BenchmarkEntity>
    {
        public FullSpec(string category, decimal minValue, int pageSize)
        {
            // Multiple criteria
            AddCriteria(e => e.IsActive);
            AddCriteria(e => e.Category == category);
            AddCriteria(e => e.Value >= minValue);
            AddCriteria(e => e.CreatedAtUtc > DateTime.UtcNow.AddYears(-1));

            // Multi-column ordering
            ApplyOrderByDescending(e => e.Value);
            ApplyThenByDescending(e => e.CreatedAtUtc);
            ApplyThenBy(e => e.Id);

            // Pagination
            ApplyPaging(0, pageSize);

            // Query options
            AsNoTracking = true;
        }
    }

    #endregion
}
