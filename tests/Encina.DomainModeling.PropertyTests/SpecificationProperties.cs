using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for Specification pattern invariants.
/// </summary>
public sealed class SpecificationProperties
{
    private sealed class NumberSpec : Specification<int>
    {
        private readonly Func<int, bool> _predicate;

        public NumberSpec(Func<int, bool> predicate) => _predicate = predicate;

        public override System.Linq.Expressions.Expression<Func<int, bool>> ToExpression()
            => x => _predicate(x);
    }

    #region Specification Composition Properties

    [Property(MaxTest = 200)]
    public bool And_SatisfiesBothConditions(int value, int threshold1, int threshold2)
    {
        var spec1 = new NumberSpec(x => x > threshold1);
        var spec2 = new NumberSpec(x => x < threshold2);
        var combined = spec1.And(spec2);

        var result = combined.IsSatisfiedBy(value);
        var expected = value > threshold1 && value < threshold2;

        return result == expected;
    }

    [Property(MaxTest = 200)]
    public bool Or_SatisfiesEitherCondition(int value, int threshold1, int threshold2)
    {
        var spec1 = new NumberSpec(x => x < threshold1);
        var spec2 = new NumberSpec(x => x > threshold2);
        var combined = spec1.Or(spec2);

        var result = combined.IsSatisfiedBy(value);
        var expected = value < threshold1 || value > threshold2;

        return result == expected;
    }

    [Property(MaxTest = 200)]
    public bool Not_InvertsCondition(int value, int threshold)
    {
        var spec = new NumberSpec(x => x > threshold);
        var notSpec = spec.Not();

        var result = notSpec.IsSatisfiedBy(value);
        var expected = !(value > threshold);

        return result == expected;
    }

    [Property(MaxTest = 200)]
    public bool DoubleNegation_EqualsOriginal(int value, int threshold)
    {
        var spec = new NumberSpec(x => x > threshold);
        var doubleNot = spec.Not().Not();

        return spec.IsSatisfiedBy(value) == doubleNot.IsSatisfiedBy(value);
    }

    [Property(MaxTest = 200)]
    public bool And_IsCommutative_ForSameInput(int value, int t1, int t2)
    {
        var spec1 = new NumberSpec(x => x > t1);
        var spec2 = new NumberSpec(x => x < t2);

        var and1 = spec1.And(spec2);
        var and2 = spec2.And(spec1);

        return and1.IsSatisfiedBy(value) == and2.IsSatisfiedBy(value);
    }

    [Property(MaxTest = 200)]
    public bool Or_IsCommutative_ForSameInput(int value, int t1, int t2)
    {
        var spec1 = new NumberSpec(x => x > t1);
        var spec2 = new NumberSpec(x => x < t2);

        var or1 = spec1.Or(spec2);
        var or2 = spec2.Or(spec1);

        return or1.IsSatisfiedBy(value) == or2.IsSatisfiedBy(value);
    }

    [Property(MaxTest = 200)]
    public bool And_IsAssociative(int value, int t1, int t2, int t3)
    {
        var spec1 = new NumberSpec(x => x > t1);
        var spec2 = new NumberSpec(x => x < t2);
        var spec3 = new NumberSpec(x => x != t3);

        var leftAssoc = spec1.And(spec2).And(spec3);
        var rightAssoc = spec1.And(spec2.And(spec3));

        return leftAssoc.IsSatisfiedBy(value) == rightAssoc.IsSatisfiedBy(value);
    }

    [Property(MaxTest = 200)]
    public bool Or_IsAssociative(int value, int t1, int t2, int t3)
    {
        var spec1 = new NumberSpec(x => x > t1);
        var spec2 = new NumberSpec(x => x < t2);
        var spec3 = new NumberSpec(x => x != t3);

        var leftAssoc = spec1.Or(spec2).Or(spec3);
        var rightAssoc = spec1.Or(spec2.Or(spec3));

        return leftAssoc.IsSatisfiedBy(value) == rightAssoc.IsSatisfiedBy(value);
    }

    [Property(MaxTest = 200)]
    public bool DeMorgan_NotAnd_EqualsOrNot(int value, int t1, int t2)
    {
        var spec1 = new NumberSpec(x => x > t1);
        var spec2 = new NumberSpec(x => x < t2);

        // !(A && B) == (!A || !B)
        var notAnd = spec1.And(spec2).Not();
        var orNot = spec1.Not().Or(spec2.Not());

        return notAnd.IsSatisfiedBy(value) == orNot.IsSatisfiedBy(value);
    }

    [Property(MaxTest = 200)]
    public bool DeMorgan_NotOr_EqualsAndNot(int value, int t1, int t2)
    {
        var spec1 = new NumberSpec(x => x > t1);
        var spec2 = new NumberSpec(x => x < t2);

        // !(A || B) == (!A && !B)
        var notOr = spec1.Or(spec2).Not();
        var andNot = spec1.Not().And(spec2.Not());

        return notOr.IsSatisfiedBy(value) == andNot.IsSatisfiedBy(value);
    }

    #endregion

    #region Expression Composition Properties

    [Property(MaxTest = 200)]
    public bool ToExpression_ProducesSameResultAsIsSatisfiedBy(int value, int threshold)
    {
        var spec = new NumberSpec(x => x > threshold);

        var isSatisfiedResult = spec.IsSatisfiedBy(value);
        var expressionResult = spec.ToExpression().Compile()(value);

        return isSatisfiedResult == expressionResult;
    }

    [Property(MaxTest = 200)]
    public bool ComposedExpression_CanBeCompiled(int value, int t1, int t2)
    {
        var spec1 = new NumberSpec(x => x > t1);
        var spec2 = new NumberSpec(x => x < t2);
        var combined = spec1.And(spec2);

        // Should not throw when compiling
        var compiled = combined.ToExpression().Compile();
        var result = compiled(value);
        var expected = value > t1 && value < t2;

        return result == expected;
    }

    [Property(MaxTest = 200)]
    public bool ToFunc_ProducesSameResultAsToExpression(int value, int threshold)
    {
        var spec = new NumberSpec(x => x > threshold);

        var funcResult = spec.ToFunc()(value);
        var expressionResult = spec.ToExpression().Compile()(value);

        return funcResult == expressionResult;
    }

    [Property(MaxTest = 200)]
    public bool ImplicitConversion_ProducesCorrectExpression(int value, int threshold)
    {
        var spec = new NumberSpec(x => x > threshold);

        // Implicit conversion to expression
        System.Linq.Expressions.Expression<Func<int, bool>> expression = spec;
        var result = expression.Compile()(value);
        var expected = value > threshold;

        return result == expected;
    }

    #endregion
}
