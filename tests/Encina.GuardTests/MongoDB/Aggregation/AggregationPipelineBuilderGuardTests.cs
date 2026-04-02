using Encina.MongoDB.Aggregation;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Aggregation;

public class AggregationPipelineBuilderGuardTests
{
    private static readonly IMongoCollection<TestEntity> Collection = Substitute.For<IMongoCollection<TestEntity>>();
    private readonly AggregationPipelineBuilder<TestEntity> _builder = new();

    #region BuildCountPipeline

    [Fact]
    public void BuildCountPipeline_NullCollection_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            _builder.BuildCountPipeline(null!, e => e.IsActive));

    [Fact]
    public void BuildCountPipeline_NullPredicate_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            _builder.BuildCountPipeline(Collection, null!));

    #endregion

    #region BuildSumPipeline

    [Fact]
    public void BuildSumPipeline_NullCollection_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            _builder.BuildSumPipeline<decimal>(null!, e => e.Amount));

    [Fact]
    public void BuildSumPipeline_NullSelector_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            _builder.BuildSumPipeline(Collection, (System.Linq.Expressions.Expression<Func<TestEntity, decimal>>)null!));

    #endregion

    #region BuildAvgPartialPipeline

    [Fact]
    public void BuildAvgPartialPipeline_NullCollection_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            _builder.BuildAvgPartialPipeline<decimal>(null!, e => e.Amount));

    [Fact]
    public void BuildAvgPartialPipeline_NullSelector_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            _builder.BuildAvgPartialPipeline(Collection, (System.Linq.Expressions.Expression<Func<TestEntity, decimal>>)null!));

    #endregion

    #region BuildMinPipeline

    [Fact]
    public void BuildMinPipeline_NullCollection_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            _builder.BuildMinPipeline<decimal>(null!, e => e.Amount));

    [Fact]
    public void BuildMinPipeline_NullSelector_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            _builder.BuildMinPipeline(Collection, (System.Linq.Expressions.Expression<Func<TestEntity, decimal>>)null!));

    #endregion

    #region BuildMaxPipeline

    [Fact]
    public void BuildMaxPipeline_NullCollection_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            _builder.BuildMaxPipeline<decimal>(null!, e => e.Amount));

    [Fact]
    public void BuildMaxPipeline_NullSelector_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            _builder.BuildMaxPipeline(Collection, (System.Linq.Expressions.Expression<Func<TestEntity, decimal>>)null!));

    #endregion

    public class TestEntity
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public bool IsActive { get; set; }
    }
}
