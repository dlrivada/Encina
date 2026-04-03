using Encina.Security.ABAC;
using Encina.Security.ABAC.Evaluation;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC.Evaluation;

/// <summary>
/// Guard clause tests for <see cref="ConditionEvaluator"/>.
/// </summary>
public class ConditionEvaluatorGuardTests
{
    private static PolicyEvaluationContext CreateContext() => new()
    {
        SubjectAttributes = AttributeBag.Empty,
        ResourceAttributes = AttributeBag.Empty,
        EnvironmentAttributes = AttributeBag.Empty,
        ActionAttributes = AttributeBag.Empty,
        RequestType = typeof(object)
    };

    #region Constructor Guards

    [Fact]
    public void Constructor_NullFunctionRegistry_ThrowsArgumentNullException()
    {
        var act = () => new ConditionEvaluator(null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("functionRegistry");
    }

    #endregion

    #region Evaluate Guards

    [Fact]
    public void Evaluate_NullExpression_ThrowsArgumentNullException()
    {
        var evaluator = new ConditionEvaluator(Substitute.For<IFunctionRegistry>());
        Action act = () => evaluator.Evaluate(null!, CreateContext());
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("expression");
    }

    [Fact]
    public void Evaluate_NullContext_ThrowsArgumentNullException()
    {
        var evaluator = new ConditionEvaluator(Substitute.For<IFunctionRegistry>());
        var expression = new AttributeValue { DataType = XACMLDataTypes.String, Value = "test" };
        Action act = () => evaluator.Evaluate(expression, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("context");
    }

    #endregion

    #region Evaluate AttributeValue

    [Fact]
    public void Evaluate_AttributeValue_ReturnsRightWithValue()
    {
        var evaluator = new ConditionEvaluator(new DefaultFunctionRegistry());
        var expression = new AttributeValue { DataType = XACMLDataTypes.String, Value = "hello" };
        var result = evaluator.Evaluate(expression, CreateContext());
        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => null, Right: v => v).ShouldBe("hello");
    }

    #endregion

    #region Evaluate AttributeDesignator

    [Fact]
    public void Evaluate_AttributeDesignator_EmptyBag_MustBePresent_ReturnsLeft()
    {
        var evaluator = new ConditionEvaluator(new DefaultFunctionRegistry());
        var designator = new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "role",
            DataType = XACMLDataTypes.String,
            MustBePresent = true
        };
        var result = evaluator.Evaluate(designator, CreateContext());
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_AttributeDesignator_EmptyBag_NotMustBePresent_ReturnsEmptyBag()
    {
        var evaluator = new ConditionEvaluator(new DefaultFunctionRegistry());
        var designator = new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "role",
            DataType = XACMLDataTypes.String,
            MustBePresent = false
        };
        var result = evaluator.Evaluate(designator, CreateContext());
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Evaluate VariableReference

    [Fact]
    public void Evaluate_VariableReference_NullVariables_ReturnsLeft()
    {
        var evaluator = new ConditionEvaluator(new DefaultFunctionRegistry());
        var varRef = new VariableReference { VariableId = "myVar" };
        var result = evaluator.Evaluate(varRef, CreateContext(), variables: null);
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_VariableReference_UndefinedVariable_ReturnsLeft()
    {
        var evaluator = new ConditionEvaluator(new DefaultFunctionRegistry());
        var varRef = new VariableReference { VariableId = "myVar" };
        var variables = new Dictionary<string, VariableDefinition>();
        var result = evaluator.Evaluate(varRef, CreateContext(), variables);
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_VariableReference_DefinedVariable_EvaluatesExpression()
    {
        var evaluator = new ConditionEvaluator(new DefaultFunctionRegistry());
        var varRef = new VariableReference { VariableId = "myVar" };
        var variables = new Dictionary<string, VariableDefinition>
        {
            ["myVar"] = new() { VariableId = "myVar", Expression = new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 42 } }
        };
        var result = evaluator.Evaluate(varRef, CreateContext(), variables);
        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => null, Right: v => v).ShouldBe(42);
    }

    #endregion

    #region Evaluate Apply

    [Fact]
    public void Evaluate_Apply_UnknownFunction_ReturnsLeft()
    {
        var registry = Substitute.For<IFunctionRegistry>();
        registry.GetFunction("unknown-func").Returns((IXACMLFunction?)null);

        var evaluator = new ConditionEvaluator(registry);
        var apply = new Apply
        {
            FunctionId = "unknown-func",
            Arguments = [new AttributeValue { DataType = XACMLDataTypes.String, Value = "a" }]
        };
        var result = evaluator.Evaluate(apply, CreateContext());
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_Apply_FunctionThrows_ReturnsLeft()
    {
        var func = Substitute.For<IXACMLFunction>();
        func.Evaluate(Arg.Any<IReadOnlyList<object?>>()).Returns(_ => throw new InvalidOperationException("boom"));

        var registry = Substitute.For<IFunctionRegistry>();
        registry.GetFunction("test-func").Returns(func);

        var evaluator = new ConditionEvaluator(registry);
        var apply = new Apply
        {
            FunctionId = "test-func",
            Arguments = [new AttributeValue { DataType = XACMLDataTypes.String, Value = "a" }]
        };
        var result = evaluator.Evaluate(apply, CreateContext());
        result.IsLeft.ShouldBeTrue();
    }

    #endregion
}
