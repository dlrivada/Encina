using LanguageExt;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Unit tests for the Business Rule pattern implementation.
/// </summary>
public class BusinessRuleTests
{
    private sealed class Order
    {
        public List<OrderItem> Items { get; } = [];
        public decimal Total => Items.Sum(i => i.Price);
    }

    private sealed class OrderItem
    {
        public decimal Price { get; init; }
    }

    private sealed class OrderMustHaveItemsRule : BusinessRule
    {
        private readonly Order _order;

        public OrderMustHaveItemsRule(Order order) => _order = order;

        public override string ErrorCode => "ORDER_NO_ITEMS";
        public override string ErrorMessage => "Order must have at least one item";
        public override bool IsSatisfied() => _order.Items.Count > 0;
    }

    private sealed class OrderTotalMustBePositiveRule : BusinessRule
    {
        private readonly Order _order;

        public OrderTotalMustBePositiveRule(Order order) => _order = order;

        public override string ErrorCode => "ORDER_INVALID_TOTAL";
        public override string ErrorMessage => "Order total must be greater than zero";
        public override bool IsSatisfied() => _order.Total > 0;
    }

    #region IsSatisfied Tests

    [Fact]
    public void IsSatisfied_WhenRulePasses_ReturnsTrue()
    {
        // Arrange
        var order = new Order();
        order.Items.Add(new OrderItem { Price = 100 });
        var rule = new OrderMustHaveItemsRule(order);

        // Act
        var result = rule.IsSatisfied();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfied_WhenRuleFails_ReturnsFalse()
    {
        // Arrange
        var order = new Order();
        var rule = new OrderMustHaveItemsRule(order);

        // Act
        var result = rule.IsSatisfied();

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Check Extension Tests

    [Fact]
    public void Check_WhenSatisfied_ReturnsRightUnit()
    {
        // Arrange
        var order = new Order();
        order.Items.Add(new OrderItem { Price = 100 });
        var rule = new OrderMustHaveItemsRule(order);

        // Act
        var result = rule.Check();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Check_WhenNotSatisfied_ReturnsLeftBusinessRuleError()
    {
        // Arrange
        var order = new Order();
        var rule = new OrderMustHaveItemsRule(order);

        // Act
        var result = rule.Check();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.ErrorCode.ShouldBe("ORDER_NO_ITEMS");
            error.ErrorMessage.ShouldBe("Order must have at least one item");
        });
    }

    [Fact]
    public void Check_NullRule_ThrowsArgumentNullException()
    {
        // Arrange
        IBusinessRule rule = null!;

        // Act
        Action act = () => _ = rule.Check();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("rule");
    }

    #endregion

    #region CheckFirst Extension Tests

    [Fact]
    public void CheckFirst_AllRulesPass_ReturnsRightUnit()
    {
        // Arrange
        var order = new Order();
        order.Items.Add(new OrderItem { Price = 100 });
        var rules = new IBusinessRule[]
        {
            new OrderMustHaveItemsRule(order),
            new OrderTotalMustBePositiveRule(order)
        };

        // Act
        var result = rules.CheckFirst();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void CheckFirst_FirstRuleFails_ReturnsFirstError()
    {
        // Arrange
        var order = new Order();
        var rules = new IBusinessRule[]
        {
            new OrderMustHaveItemsRule(order),
            new OrderTotalMustBePositiveRule(order)
        };

        // Act
        var result = rules.CheckFirst();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.ErrorCode.ShouldBe("ORDER_NO_ITEMS");
        });
    }

    [Fact]
    public void CheckFirst_EmptyRules_ReturnsRightUnit()
    {
        // Arrange
        var rules = Array.Empty<IBusinessRule>();

        // Act
        var result = rules.CheckFirst();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void CheckFirst_NullRules_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<IBusinessRule> rules = null!;

        // Act
        Action act = () => _ = rules.CheckFirst();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("rules");
    }

    #endregion

    #region CheckAll Extension Tests

    [Fact]
    public void CheckAll_AllRulesPass_ReturnsRightUnit()
    {
        // Arrange
        var order = new Order();
        order.Items.Add(new OrderItem { Price = 100 });
        var rules = new IBusinessRule[]
        {
            new OrderMustHaveItemsRule(order),
            new OrderTotalMustBePositiveRule(order)
        };

        // Act
        var result = rules.CheckAll();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void CheckAll_MultipleRulesFail_ReturnsAllErrors()
    {
        // Arrange
        var order = new Order();
        var rules = new IBusinessRule[]
        {
            new OrderMustHaveItemsRule(order),
            new OrderTotalMustBePositiveRule(order)
        };

        // Act
        var result = rules.CheckAll();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(aggregate =>
        {
            aggregate.Errors.Count.ShouldBe(2);
            aggregate.Errors.ShouldContain(e => e.ErrorCode == "ORDER_NO_ITEMS");
            aggregate.Errors.ShouldContain(e => e.ErrorCode == "ORDER_INVALID_TOTAL");
        });
    }

    [Fact]
    public void CheckAll_NullRules_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<IBusinessRule> rules = null!;

        // Act
        Action act = () => _ = rules.CheckAll();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("rules");
    }

    #endregion

    #region ThrowIfNotSatisfied Tests

    [Fact]
    public void ThrowIfNotSatisfied_WhenSatisfied_DoesNotThrow()
    {
        // Arrange
        var order = new Order();
        order.Items.Add(new OrderItem { Price = 100 });
        var rule = new OrderMustHaveItemsRule(order);

        // Act
        var act = () => rule.ThrowIfNotSatisfied();

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void ThrowIfNotSatisfied_WhenNotSatisfied_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var order = new Order();
        var rule = new OrderMustHaveItemsRule(order);

        // Act
        var act = () => rule.ThrowIfNotSatisfied();

        // Assert
        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.ErrorCode.ShouldBe("ORDER_NO_ITEMS");
        ex.BrokenRule.ShouldBe(rule);
    }

    [Fact]
    public void ThrowIfNotSatisfied_NullRule_ThrowsArgumentNullException()
    {
        // Arrange
        IBusinessRule rule = null!;

        // Act
        var act = () => rule.ThrowIfNotSatisfied();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("rule");
    }

    #endregion

    #region ThrowIfAnyNotSatisfied Tests

    [Fact]
    public void ThrowIfAnyNotSatisfied_AllPass_DoesNotThrow()
    {
        // Arrange
        var order = new Order();
        order.Items.Add(new OrderItem { Price = 100 });
        var rules = new IBusinessRule[]
        {
            new OrderMustHaveItemsRule(order),
            new OrderTotalMustBePositiveRule(order)
        };

        // Act
        var act = () => rules.ThrowIfAnyNotSatisfied();

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void ThrowIfAnyNotSatisfied_FirstFails_ThrowsImmediately()
    {
        // Arrange
        var order = new Order();
        var rules = new IBusinessRule[]
        {
            new OrderMustHaveItemsRule(order),
            new OrderTotalMustBePositiveRule(order)
        };

        // Act
        var act = () => rules.ThrowIfAnyNotSatisfied();

        // Assert
        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.ErrorCode.ShouldBe("ORDER_NO_ITEMS");
    }

    [Fact]
    public void ThrowIfAnyNotSatisfied_NullRules_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<IBusinessRule> rules = null!;

        // Act
        var act = () => rules.ThrowIfAnyNotSatisfied();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("rules");
    }

    #endregion

    #region BusinessRuleError Tests

    [Fact]
    public void BusinessRuleError_From_CreatesErrorFromRule()
    {
        // Arrange
        var order = new Order();
        var rule = new OrderMustHaveItemsRule(order);

        // Act
        var error = BusinessRuleError.From(rule);

        // Assert
        error.ErrorCode.ShouldBe("ORDER_NO_ITEMS");
        error.ErrorMessage.ShouldBe("Order must have at least one item");
    }

    [Fact]
    public void BusinessRuleError_From_NullRule_ThrowsArgumentNullException()
    {
        // Act
        var act = () => BusinessRuleError.From(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("rule");
    }

    #endregion

    #region AggregateBusinessRuleError Tests

    [Fact]
    public void AggregateBusinessRuleError_FromRules_CreatesAggregate()
    {
        // Arrange
        var order = new Order();
        var rules = new IBusinessRule[]
        {
            new OrderMustHaveItemsRule(order),
            new OrderTotalMustBePositiveRule(order)
        };

        // Act
        var aggregate = AggregateBusinessRuleError.FromRules(rules);

        // Assert
        aggregate.Errors.Count.ShouldBe(2);
    }

    [Fact]
    public void AggregateBusinessRuleError_From_CreatesFromErrors()
    {
        // Arrange
        var error1 = new BusinessRuleError("E1", "Error 1");
        var error2 = new BusinessRuleError("E2", "Error 2");

        // Act
        var aggregate = AggregateBusinessRuleError.From(error1, error2);

        // Assert
        aggregate.Errors.Count.ShouldBe(2);
    }

    #endregion

    #region BusinessRuleViolationException Tests

    [Fact]
    public void BusinessRuleViolationException_Constructor_SetsProperties()
    {
        // Arrange
        var order = new Order();
        var rule = new OrderMustHaveItemsRule(order);

        // Act
        var exception = new BusinessRuleViolationException(rule);

        // Assert
        exception.BrokenRule.ShouldBe(rule);
        exception.ErrorCode.ShouldBe("ORDER_NO_ITEMS");
        exception.Message.ShouldBe("Order must have at least one item");
    }

    [Fact]
    public void BusinessRuleViolationException_Constructor_NullRule_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new BusinessRuleViolationException(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("brokenRule");
    }

    [Fact]
    public void BusinessRuleViolationException_WithInnerException_PreservesInner()
    {
        // Arrange
        var order = new Order();
        var rule = new OrderMustHaveItemsRule(order);
        var inner = new InvalidOperationException("Inner");

        // Act
        var exception = new BusinessRuleViolationException(rule, inner);

        // Assert
        exception.InnerException.ShouldBe(inner);
    }

    #endregion
}
