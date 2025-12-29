namespace Encina.DomainModeling.GuardTests;

/// <summary>
/// Guard tests for the Business Rule types.
/// </summary>
public class BusinessRuleGuardTests
{
    private sealed class AlwaysSatisfiedRule : BusinessRule
    {
        public override string ErrorCode => "ALWAYS_SATISFIED";
        public override string ErrorMessage => "Always satisfied";
        public override bool IsSatisfied() => true;
    }

    private sealed class NeverSatisfiedRule : BusinessRule
    {
        public override string ErrorCode => "NEVER_SATISFIED";
        public override string ErrorMessage => "Never satisfied";
        public override bool IsSatisfied() => false;
    }

    #region BusinessRuleExtensions Guards

    [Fact]
    public void Check_NullRule_ThrowsArgumentNullException()
    {
        // Arrange
        IBusinessRule rule = null!;

        // Act
        var act = () => rule.Check();

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("rule");
    }

    [Fact]
    public void CheckFirst_NullRules_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<IBusinessRule> rules = null!;

        // Act
        var act = () => rules.CheckFirst();

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("rules");
    }

    [Fact]
    public void CheckAll_NullRules_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<IBusinessRule> rules = null!;

        // Act
        var act = () => rules.CheckAll();

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("rules");
    }

    [Fact]
    public void ThrowIfNotSatisfied_NullRule_ThrowsArgumentNullException()
    {
        // Arrange
        IBusinessRule rule = null!;

        // Act
        var act = () => rule.ThrowIfNotSatisfied();

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("rule");
    }

    [Fact]
    public void ThrowIfAnyNotSatisfied_NullRules_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<IBusinessRule> rules = null!;

        // Act
        var act = () => rules.ThrowIfAnyNotSatisfied();

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("rules");
    }

    #endregion

    #region BusinessRuleError Guards

    [Fact]
    public void BusinessRuleError_From_NullRule_ThrowsArgumentNullException()
    {
        // Act
        var act = () => BusinessRuleError.From(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("rule");
    }

    #endregion

    #region AggregateBusinessRuleError Guards

    [Fact]
    public void AggregateBusinessRuleError_FromRules_NullRules_ThrowsArgumentNullException()
    {
        // Act
        var act = () => AggregateBusinessRuleError.FromRules(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("rules");
    }

    #endregion

    #region BusinessRuleViolationException Guards

    [Fact]
    public void BusinessRuleViolationException_Constructor_NullRule_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new BusinessRuleViolationException(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("brokenRule");
    }

    [Fact]
    public void BusinessRuleViolationException_ConstructorWithInner_NullRule_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new BusinessRuleViolationException(null!, new InvalidOperationException("inner"));

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("brokenRule");
    }

    #endregion
}
