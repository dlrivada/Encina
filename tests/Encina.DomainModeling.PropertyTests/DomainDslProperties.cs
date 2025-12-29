using System.Linq.Expressions;
using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for domain DSL patterns.
/// </summary>
public class DomainDslProperties
{
    // === Quantity Properties ===

    [Property(MaxTest = 100)]
    public bool Quantity_Create_ValidValue_ReturnsRight(PositiveInt value)
    {
        var result = Quantity.Create(value.Get);
        return result.IsRight;
    }

    [Property(MaxTest = 100)]
    public bool Quantity_Create_NegativeValue_ReturnsLeft(NegativeInt value)
    {
        var result = Quantity.Create(value.Get);
        return result.IsLeft;
    }

    [Property(MaxTest = 100)]
    public bool Quantity_From_ValidValue_ReturnsQuantity(PositiveInt value)
    {
        var quantity = Quantity.From(value.Get);
        return quantity.Value == value.Get;
    }

    [Property(MaxTest = 100)]
    public bool Quantity_Add_CommutativeProperty(PositiveInt a, PositiveInt b)
    {
        var qa = Quantity.From(a.Get);
        var qb = Quantity.From(b.Get);
        return (qa + qb) == (qb + qa);
    }

    [Property(MaxTest = 100)]
    public bool Quantity_Add_CorrectResult(PositiveInt a, PositiveInt b)
    {
        var qa = Quantity.From(a.Get);
        var qb = Quantity.From(b.Get);
        var sum = qa + qb;
        return sum.Value == a.Get + b.Get;
    }

    [Property(MaxTest = 100)]
    public bool Quantity_Subtract_FloorsAtZero(PositiveInt a, PositiveInt b)
    {
        var qa = Quantity.From(a.Get);
        var qb = Quantity.From(b.Get);
        var diff = qa - qb;
        return diff.Value >= 0;
    }

    [Property(MaxTest = 100)]
    public bool Quantity_Multiply_CorrectResult(PositiveInt a, PositiveInt factor)
    {
        // Limit to avoid overflow
        var safeValue = a.Get % 1000;
        var safeFactor = factor.Get % 100;
        var qa = Quantity.From(safeValue);
        var product = qa * safeFactor;
        return product.Value == safeValue * safeFactor;
    }

    [Property(MaxTest = 100)]
    public bool Quantity_Zero_HasValueZero()
    {
        return Quantity.Zero.Value == 0 && Quantity.Zero.IsZero;
    }

    [Property(MaxTest = 100)]
    public bool Quantity_One_HasValueOne()
    {
        return Quantity.One.Value == 1 && Quantity.One.IsPositive;
    }

    [Property(MaxTest = 100)]
    public bool Quantity_IsGreaterThan_WorksCorrectly(PositiveInt a, PositiveInt b)
    {
        var qa = Quantity.From(a.Get);
        var qb = Quantity.From(b.Get);
        return qa.IsGreaterThan(qb) == (a.Get > b.Get);
    }

    [Property(MaxTest = 100)]
    public bool Quantity_Equality_WorksCorrectly(PositiveInt value)
    {
        var q1 = Quantity.From(value.Get);
        var q2 = Quantity.From(value.Get);
        return q1 == q2 && q1.Equals(q2);
    }

    // === Percentage Properties ===

    [Property(MaxTest = 100)]
    public bool Percentage_Create_ValidValue_ReturnsRight(PositiveInt value)
    {
        var safeValue = (decimal)(value.Get % 101); // 0-100
        var result = Percentage.Create(safeValue);
        return result.IsRight;
    }

    [Property(MaxTest = 100)]
    public bool Percentage_Create_ValueOver100_ReturnsLeft()
    {
        var result = Percentage.Create(101);
        return result.IsLeft;
    }

    [Property(MaxTest = 100)]
    public bool Percentage_Create_NegativeValue_ReturnsLeft()
    {
        var result = Percentage.Create(-1);
        return result.IsLeft;
    }

    [Property(MaxTest = 100)]
    public bool Percentage_ApplyTo_CorrectCalculation(PositiveInt pctValue, PositiveInt amount)
    {
        var safePct = (decimal)(pctValue.Get % 101);
        var safeAmount = (decimal)(amount.Get % 10000);
        var pct = Percentage.From(safePct);
        var expected = safeAmount * (safePct / 100m);
        return pct.ApplyTo(safeAmount) == expected;
    }

    [Property(MaxTest = 100)]
    public bool Percentage_AsFraction_CorrectValue(PositiveInt value)
    {
        var safePct = (decimal)(value.Get % 101);
        var pct = Percentage.From(safePct);
        return pct.AsFraction == safePct / 100m;
    }

    [Property(MaxTest = 100)]
    public bool Percentage_Complement_SumsTo100(PositiveInt value)
    {
        var safePct = (decimal)(value.Get % 101);
        var pct = Percentage.From(safePct);
        return pct.Value + pct.Complement.Value == 100;
    }

    [Property(MaxTest = 100)]
    public bool Percentage_Zero_HasValueZero()
    {
        return Percentage.Zero.Value == 0;
    }

    [Property(MaxTest = 100)]
    public bool Percentage_Full_HasValue100()
    {
        return Percentage.Full.Value == 100;
    }

    [Property(MaxTest = 100)]
    public bool Percentage_Half_HasValue50()
    {
        return Percentage.Half.Value == 50;
    }

    // === DateRange Properties ===

    [Property(MaxTest = 100)]
    public bool DateRange_Create_ValidRange_ReturnsRight(PositiveInt days)
    {
        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = start.AddDays(days.Get % 365);
        var result = DateRange.Create(start, end);
        return result.IsRight;
    }

    [Property(MaxTest = 100)]
    public bool DateRange_Create_EndBeforeStart_ReturnsLeft()
    {
        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = start.AddDays(-1);
        var result = DateRange.Create(start, end);
        return result.IsLeft;
    }

    [Property(MaxTest = 100)]
    public bool DateRange_SingleDay_HasTotalDaysOne()
    {
        var date = DateOnly.FromDateTime(DateTime.Today);
        var range = DateRange.SingleDay(date);
        return range.TotalDays == 1;
    }

    [Property(MaxTest = 100)]
    public bool DateRange_Days_HasCorrectTotalDays(PositiveInt count)
    {
        var safeCount = (count.Get % 365) + 1; // 1-365
        var start = DateOnly.FromDateTime(DateTime.Today);
        var range = DateRange.Days(start, safeCount);
        return range.TotalDays == safeCount;
    }

    [Property(MaxTest = 100)]
    public bool DateRange_Contains_StartAndEnd(PositiveInt days)
    {
        var safeDays = (days.Get % 365) + 1;
        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = start.AddDays(safeDays);
        var range = DateRange.From(start, end);
        return range.Contains(start) && range.Contains(end);
    }

    [Property(MaxTest = 100)]
    public bool DateRange_Overlaps_SameRange()
    {
        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = start.AddDays(10);
        var range = DateRange.From(start, end);
        return range.Overlaps(range);
    }

    [Property(MaxTest = 100)]
    public bool DateRange_FullyContains_SameRange()
    {
        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = start.AddDays(10);
        var range = DateRange.From(start, end);
        return range.FullyContains(range);
    }

    [Property(MaxTest = 100)]
    public bool DateRange_ExtendBy_IncreasesTotalDays(PositiveInt days, PositiveInt extension)
    {
        var safeDays = (days.Get % 100) + 1;
        var safeExtension = extension.Get % 100;
        var start = DateOnly.FromDateTime(DateTime.Today);
        var range = DateRange.Days(start, safeDays);
        var extended = range.ExtendBy(safeExtension);
        return extended.TotalDays == safeDays + safeExtension;
    }

    [Property(MaxTest = 100)]
    public bool DateRange_Equality_WorksCorrectly()
    {
        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = start.AddDays(10);
        var range1 = DateRange.From(start, end);
        var range2 = DateRange.From(start, end);
        return range1 == range2 && range1.Equals(range2);
    }

    // === TimeRange Properties ===

    [Property(MaxTest = 100)]
    public bool TimeRange_Create_ValidRange_ReturnsRight(PositiveInt hours)
    {
        var safeHours = (hours.Get % 12) + 1; // 1-12 hours later
        var start = new TimeOnly(8, 0);
        var end = start.AddHours(safeHours);
        var result = TimeRange.Create(start, end);
        return result.IsRight;
    }

    [Property(MaxTest = 100)]
    public bool TimeRange_Create_EndBeforeStart_ReturnsLeft()
    {
        var start = new TimeOnly(10, 0);
        var end = new TimeOnly(8, 0);
        var result = TimeRange.Create(start, end);
        return result.IsLeft;
    }

    [Property(MaxTest = 100)]
    public bool TimeRange_Duration_CorrectValue(PositiveInt hours)
    {
        var safeHours = (hours.Get % 12) + 1;
        var start = new TimeOnly(8, 0);
        var end = start.AddHours(safeHours);
        var range = TimeRange.From(start, end);
        return range.Duration.TotalHours == safeHours;
    }

    [Property(MaxTest = 100)]
    public bool TimeRange_Contains_StartAndEnd(PositiveInt hours)
    {
        var safeHours = (hours.Get % 12) + 1;
        var start = new TimeOnly(8, 0);
        var end = start.AddHours(safeHours);
        var range = TimeRange.From(start, end);
        return range.Contains(start) && range.Contains(end);
    }

    [Property(MaxTest = 100)]
    public bool TimeRange_Overlaps_SameRange()
    {
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(17, 0);
        var range = TimeRange.From(start, end);
        return range.Overlaps(range);
    }

    [Property(MaxTest = 100)]
    public bool TimeRange_Equality_WorksCorrectly()
    {
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(17, 0);
        var range1 = TimeRange.From(start, end);
        var range2 = TimeRange.From(start, end);
        return range1 == range2 && range1.Equals(range2);
    }

    // === DomainDslExtensions Properties ===

    [Property(MaxTest = 100)]
    public bool Is_ReturnsTrueForSatisfyingSpec()
    {
        var entity = new TestEntity(10);
        var spec = new GreaterThanFiveSpec();
        return entity.Is(spec);
    }

    [Property(MaxTest = 100)]
    public bool Satisfies_IsSameAsIs()
    {
        var entity = new TestEntity(10);
        var spec = new GreaterThanFiveSpec();
        return entity.Is(spec) == entity.Satisfies(spec);
    }

    [Property(MaxTest = 100)]
    public bool Violates_IsOppositeOfIs()
    {
        var entity = new TestEntity(3);
        var spec = new GreaterThanFiveSpec();
        return entity.Violates(spec) == !entity.Is(spec);
    }

    [Property(MaxTest = 100)]
    public bool Passes_ReturnsTrueForSatisfiedRule()
    {
        var rule = new AlwaysTrueRule();
        return rule.Passes();
    }

    [Property(MaxTest = 100)]
    public bool Fails_ReturnsTrueForViolatedRule()
    {
        var rule = new AlwaysFalseRule();
        return rule.Fails();
    }

    [Property(MaxTest = 100)]
    public bool EnsureValid_ValidCondition_ReturnsRight(NonEmptyString value)
    {
        var result = value.Get.EnsureValid(s => s.Length > 0, "Value cannot be empty");
        return result.IsRight;
    }

    [Property(MaxTest = 100)]
    public bool EnsureValid_InvalidCondition_ReturnsLeft()
    {
        var result = "".EnsureValid(s => s.Length > 0, "Value cannot be empty");
        return result.IsLeft;
    }

    [Property(MaxTest = 100)]
    public bool EnsureNotNull_NonNullValue_ReturnsRight(NonEmptyString value)
    {
        string? nullableValue = value.Get;
        var result = nullableValue.EnsureNotNull("Value");
        return result.IsRight;
    }

    [Property(MaxTest = 100)]
    public bool EnsureNotNull_NullValue_ReturnsLeft()
    {
        string? nullableValue = null;
        var result = nullableValue.EnsureNotNull("Value");
        return result.IsLeft;
    }

    [Property(MaxTest = 100)]
    public bool EnsureNotEmpty_NonEmptyString_ReturnsRight(NonEmptyString value)
    {
        var result = value.Get.EnsureNotEmpty("Value");
        return result.IsRight;
    }

    [Property(MaxTest = 100)]
    public bool EnsureNotEmpty_EmptyString_ReturnsLeft()
    {
        var result = "".EnsureNotEmpty("Value");
        return result.IsLeft;
    }

    // === DomainBuilderError Properties ===

    [Property(MaxTest = 100)]
    public bool DomainBuilderError_MissingValue_HasCorrectCode(NonEmptyString propertyName)
    {
        var error = DomainBuilderError.MissingValue(propertyName.Get);
        return error.ErrorCode == "BUILDER_MISSING_VALUE"
            && error.Message.Contains(propertyName.Get);
    }

    [Property(MaxTest = 100)]
    public bool DomainBuilderError_ValidationFailed_HasCorrectCode(NonEmptyString reason)
    {
        var error = DomainBuilderError.ValidationFailed(reason.Get);
        return error.ErrorCode == "BUILDER_VALIDATION_FAILED"
            && error.Message.Contains(reason.Get);
    }

    [Property(MaxTest = 100)]
    public bool DomainBuilderError_InvalidState_HasCorrectCode(NonEmptyString reason)
    {
        var error = DomainBuilderError.InvalidState(reason.Get);
        return error.ErrorCode == "BUILDER_INVALID_STATE"
            && error.Message.Contains(reason.Get);
    }

    // Test helpers
    private sealed record TestEntity(int Value);

    private sealed class GreaterThanFiveSpec : Specification<TestEntity>
    {
        public override Expression<Func<TestEntity, bool>> ToExpression()
            => e => e.Value > 5;
    }

    private sealed class AlwaysTrueRule : IBusinessRule
    {
        public string ErrorCode => "ALWAYS_TRUE";
        public string ErrorMessage => "Always true";
        public bool IsSatisfied() => true;
    }

    private sealed class AlwaysFalseRule : IBusinessRule
    {
        public string ErrorCode => "ALWAYS_FALSE";
        public string ErrorMessage => "Always false";
        public bool IsSatisfied() => false;
    }
}
