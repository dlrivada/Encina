using System.Linq.Expressions;
using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;
using Shouldly;

namespace Encina.PropertyTests.Database.Specification;

/// <summary>
/// Property-based tests for the Specification pattern.
/// Verifies invariants that MUST hold for all specification operations.
/// </summary>
[Trait("Category", "Property")]
public sealed class SpecificationPropertyTests
{
    #region Specification Composition Invariants

    [Property(MaxTest = 100)]
    public bool Property_AndSpecification_BothConditionsMustBeSatisfied(int value)
    {
        // Property: And composition requires BOTH conditions to be true
        var greaterThan5 = new ValueGreaterThanSpec(5);
        var lessThan10 = new ValueLessThanSpec(10);
        var combined = greaterThan5.And(lessThan10);

        var entity = new SpecTestEntity { Value = value };
        var expected = value > 5 && value < 10;
        return combined.IsSatisfiedBy(entity) == expected;
    }

    [Property(MaxTest = 100)]
    public bool Property_OrSpecification_EitherConditionCanBeSatisfied(int value)
    {
        // Property: Or composition requires AT LEAST ONE condition to be true
        var lessThan0 = new ValueLessThanSpec(0);
        var greaterThan100 = new ValueGreaterThanSpec(100);
        var combined = lessThan0.Or(greaterThan100);

        var entity = new SpecTestEntity { Value = value };
        var expected = value < 0 || value > 100;
        return combined.IsSatisfiedBy(entity) == expected;
    }

    [Property(MaxTest = 100)]
    public bool Property_NotSpecification_InvertsCondition(int value)
    {
        // Property: Not composition inverts the condition
        var greaterThan50 = new ValueGreaterThanSpec(50);
        var notGreaterThan50 = greaterThan50.Not();

        var entity = new SpecTestEntity { Value = value };
        var expected = !(value > 50);
        return notGreaterThan50.IsSatisfiedBy(entity) == expected;
    }

    [Fact]
    public void Property_DoubleNegation_EqualsOriginal()
    {
        // Property: Not(Not(spec)) == spec for any value
        var spec = new ValueGreaterThanSpec(25);
        var doubleNegated = spec.Not().Not();

        for (var i = 0; i <= 50; i++)
        {
            var entity = new SpecTestEntity { Value = i };
            doubleNegated.IsSatisfiedBy(entity).ShouldBe(spec.IsSatisfiedBy(entity),
                $"Double negation should equal original for value {i}");
        }
    }

    [Fact]
    public void Property_AndComposition_IsCommutative()
    {
        // Property: A.And(B) == B.And(A) for all values
        var spec1 = new ValueGreaterThanSpec(10);
        var spec2 = new ValueLessThanSpec(50);

        var and1 = spec1.And(spec2);
        var and2 = spec2.And(spec1);

        for (var i = 0; i <= 60; i++)
        {
            var entity = new SpecTestEntity { Value = i };
            and1.IsSatisfiedBy(entity).ShouldBe(and2.IsSatisfiedBy(entity),
                $"And composition should be commutative for value {i}");
        }
    }

    [Fact]
    public void Property_OrComposition_IsCommutative()
    {
        // Property: A.Or(B) == B.Or(A) for all values
        var spec1 = new ValueLessThanSpec(10);
        var spec2 = new ValueGreaterThanSpec(50);

        var or1 = spec1.Or(spec2);
        var or2 = spec2.Or(spec1);

        for (var i = 0; i <= 60; i++)
        {
            var entity = new SpecTestEntity { Value = i };
            or1.IsSatisfiedBy(entity).ShouldBe(or2.IsSatisfiedBy(entity),
                $"Or composition should be commutative for value {i}");
        }
    }

    [Fact]
    public void Property_AndComposition_IsAssociative()
    {
        // Property: (A.And(B)).And(C) == A.And(B.And(C))
        var spec1 = new ValueGreaterThanSpec(5);
        var spec2 = new ValueLessThanSpec(50);
        var spec3 = new ValueGreaterThanSpec(10);

        var leftGrouped = spec1.And(spec2).And(spec3);
        var rightGrouped = spec1.And(spec2.And(spec3));

        for (var i = 0; i <= 60; i++)
        {
            var entity = new SpecTestEntity { Value = i };
            leftGrouped.IsSatisfiedBy(entity).ShouldBe(rightGrouped.IsSatisfiedBy(entity),
                $"And composition should be associative for value {i}");
        }
    }

    [Fact]
    public void Property_OrComposition_IsAssociative()
    {
        // Property: (A.Or(B)).Or(C) == A.Or(B.Or(C))
        var spec1 = new ValueLessThanSpec(5);
        var spec2 = new ValueGreaterThanSpec(50);
        var spec3 = new ValueEqualSpec(25);

        var leftGrouped = spec1.Or(spec2).Or(spec3);
        var rightGrouped = spec1.Or(spec2.Or(spec3));

        for (var i = 0; i <= 60; i++)
        {
            var entity = new SpecTestEntity { Value = i };
            leftGrouped.IsSatisfiedBy(entity).ShouldBe(rightGrouped.IsSatisfiedBy(entity),
                $"Or composition should be associative for value {i}");
        }
    }

    #endregion

    #region IsSatisfiedBy and ToExpression Consistency

    [Property(MaxTest = 100)]
    public bool Property_IsSatisfiedBy_ConsistentWithToExpression(int value)
    {
        // Property: IsSatisfiedBy MUST return the same result as compiling and invoking ToExpression
        var spec = new ValueGreaterThanSpec(25);
        var entity = new SpecTestEntity { Value = value };

        var compiledResult = spec.ToExpression().Compile()(entity);
        var methodResult = spec.IsSatisfiedBy(entity);

        return compiledResult == methodResult;
    }

    [Property(MaxTest = 100)]
    public bool Property_ToFunc_ConsistentWithToExpression(int value)
    {
        // Property: ToFunc MUST return the same result as ToExpression compiled
        var spec = new ValueGreaterThanSpec(25);
        var entity = new SpecTestEntity { Value = value };

        var funcResult = spec.ToFunc()(entity);
        var expressionResult = spec.ToExpression().Compile()(entity);

        return funcResult == expressionResult;
    }

    [Property(MaxTest = 100)]
    public bool Property_ComposedSpecification_IsSatisfiedBy_ConsistentWithToExpression(int value)
    {
        // Property: Composed specifications maintain consistency between IsSatisfiedBy and ToExpression
        var spec1 = new ValueGreaterThanSpec(10);
        var spec2 = new ValueLessThanSpec(90);
        var composed = spec1.And(spec2);

        var entity = new SpecTestEntity { Value = value };
        var compiledResult = composed.ToExpression().Compile()(entity);
        var methodResult = composed.IsSatisfiedBy(entity);

        return compiledResult == methodResult;
    }

    #endregion

    #region QuerySpecification Invariants

    [Fact]
    public void Property_QuerySpecification_CriteriaIsPreserved()
    {
        // Property: Criteria added via AddCriteria MUST be preserved
        var spec = new TestQuerySpecification();
        spec.TestAddCriteria(e => e.Value > 10);

        var entity1 = new SpecTestEntity { Value = 5 };
        var entity2 = new SpecTestEntity { Value = 15 };

        spec.IsSatisfiedBy(entity1).ShouldBeFalse();
        spec.IsSatisfiedBy(entity2).ShouldBeTrue();
    }

    [Fact]
    public void Property_QuerySpecification_MultipleCriteriaAreAndCombined()
    {
        // Property: Multiple criteria MUST be AND-combined
        var spec = new TestQuerySpecification();
        spec.TestAddCriteria(e => e.Value > 10);
        spec.TestAddCriteria(e => e.Value < 50);

        var entity1 = new SpecTestEntity { Value = 5 };  // fails first
        var entity2 = new SpecTestEntity { Value = 60 }; // fails second
        var entity3 = new SpecTestEntity { Value = 25 }; // passes both

        spec.IsSatisfiedBy(entity1).ShouldBeFalse();
        spec.IsSatisfiedBy(entity2).ShouldBeFalse();
        spec.IsSatisfiedBy(entity3).ShouldBeTrue();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public void Property_QuerySpecification_TakeIsPreserved(int takeValue)
    {
        // Property: ApplyPaging with Take MUST preserve the value
        var spec = new TestQuerySpecification();
        spec.TestApplyPaging(0, takeValue);

        spec.Take.ShouldBe(takeValue);
        spec.IsPagingEnabled.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    public void Property_QuerySpecification_SkipIsPreserved(int skipValue)
    {
        // Property: ApplyPaging with Skip MUST preserve the value
        var spec = new TestQuerySpecification();
        spec.TestApplyPaging(skipValue, 10);

        spec.Skip.ShouldBe(skipValue);
        spec.IsPagingEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Property_QuerySpecification_OrderByIsPreserved()
    {
        // Property: ApplyOrderBy MUST preserve the ordering expression
        var spec = new TestQuerySpecification();
        spec.TestApplyOrderBy(e => e.Value);

        spec.OrderBy.ShouldNotBeNull();
    }

    [Fact]
    public void Property_QuerySpecification_OrderByDescendingIsPreserved()
    {
        // Property: ApplyOrderByDescending MUST preserve the ordering expression
        var spec = new TestQuerySpecification();
        spec.TestApplyOrderByDescending(e => e.Value);

        spec.OrderByDescending.ShouldNotBeNull();
    }

    [Fact]
    public void Property_QuerySpecification_ThenByExpressionsArePreserved()
    {
        // Property: ThenBy expressions MUST be accumulated in order
        var spec = new TestQuerySpecification();
        spec.TestApplyOrderBy(e => e.Value);
        spec.TestApplyThenBy(e => e.Name);

        spec.ThenByExpressions.Count.ShouldBe(1);
    }

    [Property(MaxTest = 50)]
    public bool Property_QuerySpecification_KeysetPaginationPreservesState(int pageSize)
    {
        // Property: Keyset pagination state is properly preserved
        if (pageSize <= 0) return true; // Skip invalid page sizes

        var spec = new TestQuerySpecification();
        spec.TestApplyKeysetPagination(e => e.Value, null, pageSize);

        return spec.KeysetPaginationEnabled
            && spec.KeysetProperty is not null
            && spec.Take == pageSize;
    }

    #endregion

    #region Specification Equality by Result

    [Property(MaxTest = 100)]
    public bool Property_SameCondition_ProducesSameResults(int value)
    {
        // Property: Specifications with same condition MUST produce same results
        var spec1 = new ValueGreaterThanSpec(50);
        var spec2 = new ValueGreaterThanSpec(50);

        var entity = new SpecTestEntity { Value = value };
        return spec1.IsSatisfiedBy(entity) == spec2.IsSatisfiedBy(entity);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Property_EmptyAnd_AlwaysTrue()
    {
        // Property: An "always true" specification AND another should equal the other
        var alwaysTrue = new AlwaysTrueSpec();
        var greaterThan5 = new ValueGreaterThanSpec(5);
        var combined = alwaysTrue.And(greaterThan5);

        for (var i = 0; i <= 10; i++)
        {
            var entity = new SpecTestEntity { Value = i };
            combined.IsSatisfiedBy(entity).ShouldBe(greaterThan5.IsSatisfiedBy(entity),
                $"True AND X should equal X for value {i}");
        }
    }

    [Fact]
    public void Property_FalseAnd_AlwaysFalse()
    {
        // Property: A "always false" specification AND anything should be false
        var alwaysFalse = new AlwaysFalseSpec();
        var greaterThan5 = new ValueGreaterThanSpec(5);
        var combined = alwaysFalse.And(greaterThan5);

        for (var i = 0; i <= 10; i++)
        {
            var entity = new SpecTestEntity { Value = i };
            combined.IsSatisfiedBy(entity).ShouldBeFalse($"False AND X should be false for value {i}");
        }
    }

    [Fact]
    public void Property_TrueOr_AlwaysTrue()
    {
        // Property: A "always true" specification OR anything should be true
        var alwaysTrue = new AlwaysTrueSpec();
        var greaterThan5 = new ValueGreaterThanSpec(5);
        var combined = alwaysTrue.Or(greaterThan5);

        for (var i = 0; i <= 10; i++)
        {
            var entity = new SpecTestEntity { Value = i };
            combined.IsSatisfiedBy(entity).ShouldBeTrue($"True OR X should be true for value {i}");
        }
    }

    [Fact]
    public void Property_EmptyOr_EqualsOther()
    {
        // Property: A "always false" specification OR another should equal the other
        var alwaysFalse = new AlwaysFalseSpec();
        var greaterThan5 = new ValueGreaterThanSpec(5);
        var combined = alwaysFalse.Or(greaterThan5);

        for (var i = 0; i <= 10; i++)
        {
            var entity = new SpecTestEntity { Value = i };
            combined.IsSatisfiedBy(entity).ShouldBe(greaterThan5.IsSatisfiedBy(entity),
                $"False OR X should equal X for value {i}");
        }
    }

    #endregion
}

#region Test Entities and Specifications

/// <summary>
/// Test entity for specification property tests.
/// </summary>
public sealed class SpecTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Specification that checks if Value is greater than a threshold.
/// </summary>
public sealed class ValueGreaterThanSpec : Specification<SpecTestEntity>
{
    private readonly int _threshold;

    public ValueGreaterThanSpec(int threshold) => _threshold = threshold;

    public override Expression<Func<SpecTestEntity, bool>> ToExpression()
        => entity => entity.Value > _threshold;
}

/// <summary>
/// Specification that checks if Value is less than a threshold.
/// </summary>
public sealed class ValueLessThanSpec : Specification<SpecTestEntity>
{
    private readonly int _threshold;

    public ValueLessThanSpec(int threshold) => _threshold = threshold;

    public override Expression<Func<SpecTestEntity, bool>> ToExpression()
        => entity => entity.Value < _threshold;
}

/// <summary>
/// Specification that checks if Value equals a specific value.
/// </summary>
public sealed class ValueEqualSpec : Specification<SpecTestEntity>
{
    private readonly int _value;

    public ValueEqualSpec(int value) => _value = value;

    public override Expression<Func<SpecTestEntity, bool>> ToExpression()
        => entity => entity.Value == _value;
}

/// <summary>
/// Specification that always returns true.
/// </summary>
public sealed class AlwaysTrueSpec : Specification<SpecTestEntity>
{
    public override Expression<Func<SpecTestEntity, bool>> ToExpression()
        => entity => true;
}

/// <summary>
/// Specification that always returns false.
/// </summary>
public sealed class AlwaysFalseSpec : Specification<SpecTestEntity>
{
    public override Expression<Func<SpecTestEntity, bool>> ToExpression()
        => entity => false;
}

/// <summary>
/// Test query specification with exposed builder methods for testing.
/// Uses the base class ToExpression() which combines criteria with AND logic.
/// </summary>
public sealed class TestQuerySpecification : QuerySpecification<SpecTestEntity>
{
    public TestQuerySpecification() { }

    // Note: Do NOT override ToExpression() - let the base class handle criteria combination

    public void TestAddCriteria(Expression<Func<SpecTestEntity, bool>> criteria)
        => AddCriteria(criteria);

    public void TestApplyPaging(int skip, int take)
        => ApplyPaging(skip, take);

    public void TestApplyOrderBy(Expression<Func<SpecTestEntity, object>> orderBy)
        => ApplyOrderBy(orderBy);

    public void TestApplyOrderByDescending(Expression<Func<SpecTestEntity, object>> orderByDesc)
        => ApplyOrderByDescending(orderByDesc);

    public void TestApplyThenBy(Expression<Func<SpecTestEntity, object>> thenBy)
        => ApplyThenBy(thenBy);

    public void TestApplyThenByDescending(Expression<Func<SpecTestEntity, object>> thenByDesc)
        => ApplyThenByDescending(thenByDesc);

    public void TestApplyKeysetPagination(
        Expression<Func<SpecTestEntity, object>> keysetProperty,
        object? lastKeyValue,
        int take)
        => ApplyKeysetPagination(keysetProperty, lastKeyValue, take);
}

#endregion
