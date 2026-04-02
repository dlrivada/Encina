using Encina.MongoDB.Aggregation;
using Shouldly;

namespace Encina.UnitTests.MongoDB.Aggregation;

/// <summary>
/// Unit tests for <see cref="AggregationPipelineBuilder{TEntity}"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "MongoDB")]
public sealed class AggregationPipelineBuilderTests
{
    #region GetFieldNameFromSelector

    [Fact]
    public void GetFieldNameFromSelector_SimpleProperty_ReturnsPropertyName()
    {
        var fieldName = AggregationPipelineBuilder<TestEntity>.GetFieldNameFromSelector(
            (TestEntity e) => e.Amount);

        fieldName.ShouldBe("Amount");
    }

    [Fact]
    public void GetFieldNameFromSelector_NestedProperty_ReturnsDotNotation()
    {
        var fieldName = AggregationPipelineBuilder<TestEntity>.GetFieldNameFromSelector(
            (TestEntity e) => e.Address!.City);

        fieldName.ShouldBe("Address.City");
    }

    [Fact]
    public void GetFieldNameFromSelector_DeeplyNestedProperty_ReturnsDotNotation()
    {
        var fieldName = AggregationPipelineBuilder<TestEntity>.GetFieldNameFromSelector(
            (TestEntity e) => e.Address!.Street!.Number);

        fieldName.ShouldBe("Address.Street.Number");
    }

    [Fact]
    public void GetFieldNameFromSelector_ConvertExpression_UnwrapsAndReturnsName()
    {
        // When selecting a value type through an interface/object, a Convert expression wraps it
        var fieldName = AggregationPipelineBuilder<TestEntity>.GetFieldNameFromSelector(
            (TestEntity e) => (object)e.Amount);

        fieldName.ShouldBe("Amount");
    }

    [Fact]
    public void GetFieldNameFromSelector_UnsupportedExpression_ThrowsNotSupported()
    {
        Should.Throw<NotSupportedException>(() =>
            AggregationPipelineBuilder<TestEntity>.GetFieldNameFromSelector(
                (TestEntity e) => e.Amount + 1));
    }

    #endregion

    #region Test entities

    public sealed class TestEntity
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Name { get; set; } = string.Empty;
        public AddressInfo? Address { get; set; }
    }

    public sealed class AddressInfo
    {
        public string City { get; set; } = string.Empty;
        public StreetInfo? Street { get; set; }
    }

    public sealed class StreetInfo
    {
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
