using Encina.Security.ABAC;
using Encina.Security.ABAC.Evaluation;

using NSubstitute;

using Shouldly;

using Target = Encina.Security.ABAC.Target;

namespace Encina.GuardTests.Security.ABAC.Evaluation;

/// <summary>
/// Guard clause tests for <see cref="TargetEvaluator"/>.
/// </summary>
public class TargetEvaluatorGuardTests
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
        var act = () => new TargetEvaluator(null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("functionRegistry");
    }

    #endregion

    #region EvaluateTarget Guards

    [Fact]
    public void EvaluateTarget_NullContext_ThrowsArgumentNullException()
    {
        var evaluator = new TargetEvaluator(Substitute.For<IFunctionRegistry>());
        Action act = () => evaluator.EvaluateTarget(null, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("context");
    }

    #endregion

    #region EvaluateTarget Behavior

    [Fact]
    public void EvaluateTarget_NullTarget_ReturnsPermit()
    {
        var evaluator = new TargetEvaluator(new DefaultFunctionRegistry());
        var result = evaluator.EvaluateTarget(null, CreateContext());
        result.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void EvaluateTarget_EmptyAnyOfElements_ReturnsPermit()
    {
        var evaluator = new TargetEvaluator(new DefaultFunctionRegistry());
        var target = new Target { AnyOfElements = [] };
        var result = evaluator.EvaluateTarget(target, CreateContext());
        result.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void EvaluateTarget_EmptyAllOf_ReturnsPermit()
    {
        var evaluator = new TargetEvaluator(new DefaultFunctionRegistry());
        var target = new Target
        {
            AnyOfElements = [new AnyOf { AllOfElements = [new AllOf { Matches = [] }] }]
        };
        var result = evaluator.EvaluateTarget(target, CreateContext());
        result.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void EvaluateTarget_EmptyAnyOf_ReturnsPermit()
    {
        var evaluator = new TargetEvaluator(new DefaultFunctionRegistry());
        var target = new Target
        {
            AnyOfElements = [new AnyOf { AllOfElements = [] }]
        };
        var result = evaluator.EvaluateTarget(target, CreateContext());
        result.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void EvaluateTarget_MatchWithMissingAttribute_MustBePresent_ReturnsIndeterminate()
    {
        var evaluator = new TargetEvaluator(new DefaultFunctionRegistry());
        var target = new Target
        {
            AnyOfElements = [new AnyOf
            {
                AllOfElements = [new AllOf
                {
                    Matches = [new Match
                    {
                        FunctionId = XACMLFunctionIds.StringEqual,
                        AttributeDesignator = new AttributeDesignator
                        {
                            Category = AttributeCategory.Subject,
                            AttributeId = "role",
                            DataType = XACMLDataTypes.String,
                            MustBePresent = true
                        },
                        AttributeValue = new AttributeValue { DataType = XACMLDataTypes.String, Value = "Admin" }
                    }]
                }]
            }]
        };
        var result = evaluator.EvaluateTarget(target, CreateContext());
        result.ShouldBe(Effect.Indeterminate);
    }

    [Fact]
    public void EvaluateTarget_MatchWithMissingAttribute_NotMustBePresent_ReturnsNotApplicable()
    {
        var evaluator = new TargetEvaluator(new DefaultFunctionRegistry());
        var target = new Target
        {
            AnyOfElements = [new AnyOf
            {
                AllOfElements = [new AllOf
                {
                    Matches = [new Match
                    {
                        FunctionId = XACMLFunctionIds.StringEqual,
                        AttributeDesignator = new AttributeDesignator
                        {
                            Category = AttributeCategory.Subject,
                            AttributeId = "role",
                            DataType = XACMLDataTypes.String,
                            MustBePresent = false
                        },
                        AttributeValue = new AttributeValue { DataType = XACMLDataTypes.String, Value = "Admin" }
                    }]
                }]
            }]
        };
        var result = evaluator.EvaluateTarget(target, CreateContext());
        result.ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public void EvaluateTarget_UnknownFunction_ReturnsIndeterminate()
    {
        var registry = Substitute.For<IFunctionRegistry>();
        registry.GetFunction(Arg.Any<string>()).Returns((IXACMLFunction?)null);

        var evaluator = new TargetEvaluator(registry);
        var target = new Target
        {
            AnyOfElements = [new AnyOf
            {
                AllOfElements = [new AllOf
                {
                    Matches = [new Match
                    {
                        FunctionId = "unknown-func",
                        AttributeDesignator = new AttributeDesignator
                        {
                            Category = AttributeCategory.Subject,
                            AttributeId = "role",
                            DataType = XACMLDataTypes.String
                        },
                        AttributeValue = new AttributeValue { DataType = XACMLDataTypes.String, Value = "Admin" }
                    }]
                }]
            }]
        };

        var context = new PolicyEvaluationContext
        {
            SubjectAttributes = AttributeBag.Of(new AttributeValue { DataType = XACMLDataTypes.String, Value = "Admin" }),
            ResourceAttributes = AttributeBag.Empty,
            EnvironmentAttributes = AttributeBag.Empty,
            ActionAttributes = AttributeBag.Empty,
            RequestType = typeof(object)
        };

        var result = evaluator.EvaluateTarget(target, context);
        result.ShouldBe(Effect.Indeterminate);
    }

    #endregion
}
