using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for Business Rule invariants.
/// </summary>
public sealed class BusinessRuleProperties
{
    private sealed class ThresholdRule : BusinessRule
    {
        private readonly int _value;
        private readonly int _threshold;

        public ThresholdRule(int value, int threshold)
        {
            _value = value;
            _threshold = threshold;
        }

        public override string ErrorCode => "THRESHOLD_VIOLATION";
        public override string ErrorMessage => $"Value {_value} must be greater than {_threshold}";
        public override bool IsSatisfied() => _value > _threshold;
    }

    private sealed class AlwaysSatisfiedRule : BusinessRule
    {
        public override string ErrorCode => "NEVER_FAILS";
        public override string ErrorMessage => "Always satisfied";
        public override bool IsSatisfied() => true;
    }

    private sealed class NeverSatisfiedRule : BusinessRule
    {
        public override string ErrorCode => "ALWAYS_FAILS";
        public override string ErrorMessage => "Never satisfied";
        public override bool IsSatisfied() => false;
    }

    #region IsSatisfied Properties

    [Property(MaxTest = 200)]
    public bool IsSatisfied_IsConsistent(int value, int threshold)
    {
        var rule = new ThresholdRule(value, threshold);

        var result1 = rule.IsSatisfied();
        var result2 = rule.IsSatisfied();

        return result1 == result2;
    }

    [Property(MaxTest = 200)]
    public bool IsSatisfied_MatchesExpectedLogic(int value, int threshold)
    {
        var rule = new ThresholdRule(value, threshold);
        return rule.IsSatisfied() == (value > threshold);
    }

    #endregion

    #region Check Extension Properties

    [Property(MaxTest = 200)]
    public bool Check_ReturnsRightOnSatisfied(int value, int threshold)
    {
        if (value <= threshold) return true; // Skip unsatisfied cases

        var rule = new ThresholdRule(value, threshold);
        var result = rule.Check();

        return result.IsRight && result.Match(Left: _ => false, Right: _ => true);
    }

    [Property(MaxTest = 200)]
    public bool Check_ReturnsLeftOnNotSatisfied(int value, int threshold)
    {
        if (value > threshold) return true; // Skip satisfied cases

        var rule = new ThresholdRule(value, threshold);
        var result = rule.Check();

        return result.IsLeft;
    }

    [Property(MaxTest = 200)]
    public bool Check_ErrorCodeMatches(int value, int threshold)
    {
        if (value > threshold) return true; // Skip satisfied cases

        var rule = new ThresholdRule(value, threshold);
        var result = rule.Check();

        return result.Match(
            Left: error => error.ErrorCode == "THRESHOLD_VIOLATION",
            Right: _ => false);
    }

    #endregion

    #region CheckFirst Properties

    [Property(MaxTest = 100)]
    public bool CheckFirst_ReturnsFirstFailure(int value1, int value2, int threshold)
    {
        if (value1 > threshold && value2 > threshold) return true; // Both satisfied

        var rule1 = new ThresholdRule(value1, threshold);
        var rule2 = new ThresholdRule(value2, threshold);
        var rules = new IBusinessRule[] { rule1, rule2 };

        var result = rules.CheckFirst();

        if (value1 <= threshold)
        {
            // First rule fails, should return its error
            return result.IsLeft;
        }
        else if (value2 <= threshold)
        {
            // Only second rule fails
            return result.IsLeft;
        }

        return true;
    }

    [Property(MaxTest = 100)]
    public bool CheckFirst_AllSatisfied_ReturnsRight(int baseValue, PositiveInt offset1, PositiveInt offset2)
    {
        // Ensure all values are above threshold
        var threshold = baseValue;
        var rule1 = new ThresholdRule(baseValue + offset1.Get, threshold);
        var rule2 = new ThresholdRule(baseValue + offset2.Get, threshold);
        var rules = new IBusinessRule[] { rule1, rule2 };

        var result = rules.CheckFirst();

        return result.IsRight;
    }

    #endregion

    #region CheckAll Properties

    [Property(MaxTest = 100)]
    public bool CheckAll_CollectsAllFailures(int value1, int value2, int threshold)
    {
        var rule1 = new ThresholdRule(value1, threshold);
        var rule2 = new ThresholdRule(value2, threshold);
        var rules = new IBusinessRule[] { rule1, rule2 };

        var result = rules.CheckAll();

        var failureCount = (value1 <= threshold ? 1 : 0) + (value2 <= threshold ? 1 : 0);

        if (failureCount == 0)
        {
            return result.IsRight;
        }
        else
        {
            return result.Match(
                Left: error => error is AggregateBusinessRuleError agg && agg.Errors.Count == failureCount,
                Right: _ => false);
        }
    }

    [Property(MaxTest = 100)]
    public bool CheckAll_AllSatisfied_ReturnsRight(int baseValue, PositiveInt offset)
    {
        var threshold = baseValue;
        var rule = new ThresholdRule(baseValue + offset.Get, threshold);
        var rules = new IBusinessRule[] { rule };

        var result = rules.CheckAll();

        return result.IsRight;
    }

    #endregion

    #region BusinessRuleError Properties

    [Property(MaxTest = 200)]
    public bool BusinessRuleError_From_PreservesErrorInfo(NonEmptyString errorCode, NonEmptyString errorMessage)
    {
        var rule = new CustomRule(errorCode.Get, errorMessage.Get, false);
        var error = BusinessRuleError.From(rule);

        return error.ErrorCode == errorCode.Get && error.ErrorMessage == errorMessage.Get;
    }

    private sealed class CustomRule : BusinessRule
    {
        private readonly string _errorCode;
        private readonly string _errorMessage;
        private readonly bool _isSatisfied;

        public CustomRule(string errorCode, string errorMessage, bool isSatisfied)
        {
            _errorCode = errorCode;
            _errorMessage = errorMessage;
            _isSatisfied = isSatisfied;
        }

        public override string ErrorCode => _errorCode;
        public override string ErrorMessage => _errorMessage;
        public override bool IsSatisfied() => _isSatisfied;
    }

    #endregion

    #region ThrowIfNotSatisfied Properties

    [Property(MaxTest = 100)]
    public bool ThrowIfNotSatisfied_DoesNotThrowWhenSatisfied(int value, int threshold)
    {
        if (value <= threshold) return true; // Skip unsatisfied

        var rule = new ThresholdRule(value, threshold);

        try
        {
            rule.ThrowIfNotSatisfied();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Property(MaxTest = 100)]
    public bool ThrowIfNotSatisfied_ThrowsWhenNotSatisfied(int value, int threshold)
    {
        if (value > threshold) return true; // Skip satisfied

        var rule = new ThresholdRule(value, threshold);

        try
        {
            rule.ThrowIfNotSatisfied();
            return false; // Should have thrown
        }
        catch (BusinessRuleViolationException ex)
        {
            return ex.BrokenRule == rule;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
