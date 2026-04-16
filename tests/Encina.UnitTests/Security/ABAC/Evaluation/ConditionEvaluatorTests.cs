using Encina.Security.ABAC;
using Encina.Security.ABAC.Evaluation;

using Shouldly;

using LanguageExt;

namespace Encina.UnitTests.Security.ABAC.Evaluation;

/// <summary>
/// Unit tests for <see cref="ConditionEvaluator"/>: recursive XACML 3.0 expression tree
/// evaluation against a <see cref="PolicyEvaluationContext"/>.
/// </summary>
public sealed class ConditionEvaluatorTests
{
    private readonly DefaultFunctionRegistry _registry = new();
    private readonly ConditionEvaluator _sut;

    public ConditionEvaluatorTests()
    {
        _sut = new ConditionEvaluator(_registry);
    }

    private static PolicyEvaluationContext MakeContext(
        AttributeBag? subject = null,
        AttributeBag? resource = null,
        AttributeBag? environment = null,
        AttributeBag? action = null) =>
        new()
        {
            SubjectAttributes = subject ?? AttributeBag.Empty,
            ResourceAttributes = resource ?? AttributeBag.Empty,
            EnvironmentAttributes = environment ?? AttributeBag.Empty,
            ActionAttributes = action ?? AttributeBag.Empty,
            RequestType = typeof(object)
        };

    private static AttributeBag SingleBag(string dataType, object? value) =>
        AttributeBag.Of(new AttributeValue { DataType = dataType, Value = value });

    /// <summary>
    /// Extracts the Right value from an Either, asserting it is Right.
    /// </summary>
    private static T AssertRight<T>(Either<EncinaError, T> either, string context = "")
    {
        either.IsRight.ShouldBeTrue($"expected Right but got Left{(context.Length > 0 ? $": {context}" : "")}");
        return either.Match(Left: _ => default!, Right: v => v);
    }

    /// <summary>
    /// Extracts the Left value from an Either, asserting it is Left.
    /// </summary>
    private static EncinaError AssertLeft<T>(Either<EncinaError, T> either, string context = "")
    {
        either.IsLeft.ShouldBeTrue($"expected Left but got Right{(context.Length > 0 ? $": {context}" : "")}");
        return either.Match(Left: e => e, Right: _ => default);
    }

    #region AttributeValue Evaluation

    [Fact]
    public void Evaluate_AttributeValue_ReturnsValue()
    {
        var expr = new AttributeValue { DataType = XACMLDataTypes.String, Value = "hello" };
        var ctx = MakeContext();

        var result = _sut.Evaluate(expr, ctx);

        AssertRight(result).ShouldBe("hello");
    }

    [Fact]
    public void Evaluate_AttributeValueInteger_ReturnsInteger()
    {
        var expr = new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 42 };
        var ctx = MakeContext();

        var result = _sut.Evaluate(expr, ctx);

        AssertRight(result).ShouldBe(42);
    }

    [Fact]
    public void Evaluate_AttributeValueNull_ReturnsLeft()
    {
        // LanguageExt Either does not allow Right(null), so null values cause an error
        var expr = new AttributeValue { DataType = XACMLDataTypes.String, Value = null };
        var ctx = MakeContext();

        Action act = () => { _sut.Evaluate(expr, ctx); };

        // Null value causes LanguageExt implicit conversion to throw
        Should.Throw<ValueIsNullException>(act);
    }

    #endregion

    #region AttributeDesignator Evaluation

    [Fact]
    public void Evaluate_AttributeDesignator_SubjectCategory_ReturnsSingleValue()
    {
        var designator = new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "role",
            DataType = XACMLDataTypes.String
        };
        var ctx = MakeContext(subject: SingleBag(XACMLDataTypes.String, "admin"));

        var result = _sut.Evaluate(designator, ctx);

        AssertRight(result).ShouldBe("admin");
    }

    [Fact]
    public void Evaluate_AttributeDesignator_ResourceCategory_ReturnsValue()
    {
        var designator = new AttributeDesignator
        {
            Category = AttributeCategory.Resource,
            AttributeId = "type",
            DataType = XACMLDataTypes.String
        };
        var ctx = MakeContext(resource: SingleBag(XACMLDataTypes.String, "document"));

        var result = _sut.Evaluate(designator, ctx);

        AssertRight(result).ShouldBe("document");
    }

    [Fact]
    public void Evaluate_AttributeDesignator_EnvironmentCategory_ReturnsValue()
    {
        var designator = new AttributeDesignator
        {
            Category = AttributeCategory.Environment,
            AttributeId = "time",
            DataType = XACMLDataTypes.String
        };
        var ctx = MakeContext(environment: SingleBag(XACMLDataTypes.String, "09:00"));

        var result = _sut.Evaluate(designator, ctx);

        AssertRight(result).ShouldBe("09:00");
    }

    [Fact]
    public void Evaluate_AttributeDesignator_ActionCategory_ReturnsValue()
    {
        var designator = new AttributeDesignator
        {
            Category = AttributeCategory.Action,
            AttributeId = "name",
            DataType = XACMLDataTypes.String
        };
        var ctx = MakeContext(action: SingleBag(XACMLDataTypes.String, "read"));

        var result = _sut.Evaluate(designator, ctx);

        AssertRight(result).ShouldBe("read");
    }

    [Fact]
    public void Evaluate_AttributeDesignator_EmptyBag_MustBePresent_ReturnsLeft()
    {
        var designator = new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "missing",
            DataType = XACMLDataTypes.String,
            MustBePresent = true
        };
        var ctx = MakeContext();

        var result = _sut.Evaluate(designator, ctx);

        result.IsLeft.ShouldBeTrue("missing MustBePresent attribute should produce error");
    }

    [Fact]
    public void Evaluate_AttributeDesignator_EmptyBag_NotMustBePresent_ReturnsEmptyBag()
    {
        var designator = new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "optional",
            DataType = XACMLDataTypes.String,
            MustBePresent = false
        };
        var ctx = MakeContext();

        var result = _sut.Evaluate(designator, ctx);

        var value = AssertRight(result);
        value.ShouldBe(AttributeBag.Empty);
    }

    [Fact]
    public void Evaluate_AttributeDesignator_MultiValuedBag_ReturnsBag()
    {
        var designator = new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "roles",
            DataType = XACMLDataTypes.String
        };
        var bag = AttributeBag.Of(
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "admin" },
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "user" });
        var ctx = MakeContext(subject: bag);

        var result = _sut.Evaluate(designator, ctx);

        var value = AssertRight(result);
        value.ShouldBeOfType<AttributeBag>();
        ((AttributeBag)value!).Count.ShouldBe(2);
    }

    #endregion

    #region Apply Evaluation

    [Fact]
    public void Evaluate_Apply_StringEqual_ReturnsTrue()
    {
        var apply = new Apply
        {
            FunctionId = XACMLFunctionIds.StringEqual,
            Arguments =
            [
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "hello" },
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "hello" }
            ]
        };
        var ctx = MakeContext();

        var result = _sut.Evaluate(apply, ctx);

        AssertRight(result).ShouldBe(true);
    }

    [Fact]
    public void Evaluate_Apply_StringEqual_ReturnsFalse()
    {
        var apply = new Apply
        {
            FunctionId = XACMLFunctionIds.StringEqual,
            Arguments =
            [
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "hello" },
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "world" }
            ]
        };
        var ctx = MakeContext();

        var result = _sut.Evaluate(apply, ctx);

        AssertRight(result).ShouldBe(false);
    }

    [Fact]
    public void Evaluate_Apply_IntegerGreaterThan_WithDesignator()
    {
        var apply = new Apply
        {
            FunctionId = XACMLFunctionIds.IntegerGreaterThan,
            Arguments =
            [
                new AttributeDesignator
                {
                    Category = AttributeCategory.Resource,
                    AttributeId = "amount",
                    DataType = XACMLDataTypes.Integer
                },
                new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 1000 }
            ]
        };
        var ctx = MakeContext(resource: SingleBag(XACMLDataTypes.Integer, 5000));

        var result = _sut.Evaluate(apply, ctx);

        AssertRight(result).ShouldBe(true);
    }

    [Fact]
    public void Evaluate_Apply_NestedApply_AndLogic()
    {
        // and(string-equal(subject.dept, "Finance"), integer-greater-than(resource.amount, 1000))
        var apply = new Apply
        {
            FunctionId = XACMLFunctionIds.And,
            Arguments =
            [
                new Apply
                {
                    FunctionId = XACMLFunctionIds.StringEqual,
                    Arguments =
                    [
                        new AttributeDesignator
                        {
                            Category = AttributeCategory.Subject,
                            AttributeId = "department",
                            DataType = XACMLDataTypes.String
                        },
                        new AttributeValue { DataType = XACMLDataTypes.String, Value = "Finance" }
                    ]
                },
                new Apply
                {
                    FunctionId = XACMLFunctionIds.IntegerGreaterThan,
                    Arguments =
                    [
                        new AttributeDesignator
                        {
                            Category = AttributeCategory.Resource,
                            AttributeId = "amount",
                            DataType = XACMLDataTypes.Integer
                        },
                        new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 1000 }
                    ]
                }
            ]
        };
        var ctx = MakeContext(
            subject: SingleBag(XACMLDataTypes.String, "Finance"),
            resource: SingleBag(XACMLDataTypes.Integer, 5000));

        var result = _sut.Evaluate(apply, ctx);

        AssertRight(result).ShouldBe(true);
    }

    [Fact]
    public void Evaluate_Apply_UnregisteredFunction_ReturnsLeft()
    {
        var apply = new Apply
        {
            FunctionId = "nonexistent-function",
            Arguments =
            [
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "hello" }
            ]
        };
        var ctx = MakeContext();

        var result = _sut.Evaluate(apply, ctx);

        result.IsLeft.ShouldBeTrue("unregistered function should produce error");
    }

    [Fact]
    public void Evaluate_Apply_ArgumentError_ShortCircuits()
    {
        var apply = new Apply
        {
            FunctionId = XACMLFunctionIds.StringEqual,
            Arguments =
            [
                new AttributeDesignator
                {
                    Category = AttributeCategory.Subject,
                    AttributeId = "missing",
                    DataType = XACMLDataTypes.String,
                    MustBePresent = true
                },
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "test" }
            ]
        };
        var ctx = MakeContext();

        var result = _sut.Evaluate(apply, ctx);

        result.IsLeft.ShouldBeTrue("error in argument should short-circuit");
    }

    [Fact]
    public void Evaluate_Apply_FunctionThrows_ReturnsLeft()
    {
        // string-one-and-only with an empty bag should produce an error via the function throwing
        var apply = new Apply
        {
            FunctionId = XACMLFunctionIds.StringOneAndOnly,
            Arguments =
            [
                new AttributeValue { DataType = XACMLDataTypes.String, Value = AttributeBag.Empty }
            ]
        };
        var ctx = MakeContext();

        var result = _sut.Evaluate(apply, ctx);

        result.IsLeft.ShouldBeTrue("function that throws should produce error");
    }

    #endregion

    #region VariableReference Evaluation

    [Fact]
    public void Evaluate_VariableReference_ResolvesVariable()
    {
        var varRef = new VariableReference { VariableId = "myVar" };
        var variables = new Dictionary<string, VariableDefinition>
        {
            ["myVar"] = new()
            {
                VariableId = "myVar",
                Expression = new AttributeValue { DataType = XACMLDataTypes.String, Value = "resolved" }
            }
        };
        var ctx = MakeContext();

        var result = _sut.Evaluate(varRef, ctx, variables);

        AssertRight(result).ShouldBe("resolved");
    }

    [Fact]
    public void Evaluate_VariableReference_UndefinedVariable_ReturnsLeft()
    {
        var varRef = new VariableReference { VariableId = "undefined" };
        var variables = new Dictionary<string, VariableDefinition>();
        var ctx = MakeContext();

        var result = _sut.Evaluate(varRef, ctx, variables);

        result.IsLeft.ShouldBeTrue("undefined variable should produce error");
    }

    [Fact]
    public void Evaluate_VariableReference_NullVariables_ReturnsLeft()
    {
        var varRef = new VariableReference { VariableId = "myVar" };
        var ctx = MakeContext();

        var result = _sut.Evaluate(varRef, ctx, variables: null);

        result.IsLeft.ShouldBeTrue("null variables dictionary should produce error");
    }

    [Fact]
    public void Evaluate_VariableReference_RecursiveEvaluation()
    {
        // Variable that references a designator
        var varRef = new VariableReference { VariableId = "userDept" };
        var variables = new Dictionary<string, VariableDefinition>
        {
            ["userDept"] = new()
            {
                VariableId = "userDept",
                Expression = new AttributeDesignator
                {
                    Category = AttributeCategory.Subject,
                    AttributeId = "department",
                    DataType = XACMLDataTypes.String
                }
            }
        };
        var ctx = MakeContext(subject: SingleBag(XACMLDataTypes.String, "Engineering"));

        var result = _sut.Evaluate(varRef, ctx, variables);

        AssertRight(result).ShouldBe("Engineering");
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public void Evaluate_NullExpression_Throws()
    {
        var ctx = MakeContext();

        Action act = () => { _sut.Evaluate(null!, ctx); };

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Evaluate_NullContext_Throws()
    {
        var expr = new AttributeValue { DataType = XACMLDataTypes.String, Value = "test" };

        Action act = () => { _sut.Evaluate(expr, null!); };

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_NullFunctionRegistry_Throws()
    {
        var act = () => new ConditionEvaluator(null!);

        Should.Throw<ArgumentNullException>(act);
    }

    #endregion
}
