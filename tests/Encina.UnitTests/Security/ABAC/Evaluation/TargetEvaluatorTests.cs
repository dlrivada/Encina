using Encina.Security.ABAC;
using Encina.Security.ABAC.Evaluation;

using Shouldly;

using Target = Encina.Security.ABAC.Target;

namespace Encina.UnitTests.Security.ABAC.Evaluation;

/// <summary>
/// Unit tests for <see cref="TargetEvaluator"/>: XACML 3.0 target matching
/// with triple-nesting structure (Target → AnyOf → AllOf → Match).
/// </summary>
public sealed class TargetEvaluatorTests
{
    private readonly DefaultFunctionRegistry _registry = new();
    private readonly TargetEvaluator _sut;

    public TargetEvaluatorTests()
    {
        _sut = new TargetEvaluator(_registry);
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

    private static Match MakeMatch(
        string functionId,
        AttributeCategory category,
        string attributeId,
        string dataType,
        object? matchValue,
        bool mustBePresent = false) =>
        new()
        {
            FunctionId = functionId,
            AttributeDesignator = new AttributeDesignator
            {
                Category = category,
                AttributeId = attributeId,
                DataType = dataType,
                MustBePresent = mustBePresent
            },
            AttributeValue = new AttributeValue { DataType = dataType, Value = matchValue }
        };

    #region Null and Empty Target

    [Fact]
    public void EvaluateTarget_NullTarget_ReturnsPermit()
    {
        var ctx = MakeContext();

        var result = _sut.EvaluateTarget(null, ctx);

        result.ShouldBe(Effect.Permit, "null target matches all requests");
    }

    [Fact]
    public void EvaluateTarget_EmptyAnyOfElements_ReturnsPermit()
    {
        var target = new Target { AnyOfElements = [] };
        var ctx = MakeContext();

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Permit, "empty AnyOfElements matches all requests");
    }

    #endregion

    #region Single Match

    [Fact]
    public void EvaluateTarget_SingleMatch_AttributeMatches_ReturnsPermit()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "admin")
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(subject: SingleBag(XACMLDataTypes.String, "admin"));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void EvaluateTarget_SingleMatch_AttributeDoesNotMatch_ReturnsNotApplicable()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "admin")
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(subject: SingleBag(XACMLDataTypes.String, "viewer"));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.NotApplicable);
    }

    #endregion

    #region AnyOf (OR) Logic

    [Fact]
    public void EvaluateTarget_AnyOf_FirstMatches_ReturnsPermit()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "admin")
                            ]
                        },
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "manager")
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(subject: SingleBag(XACMLDataTypes.String, "admin"));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Permit, "first AllOf matches — OR satisfied");
    }

    [Fact]
    public void EvaluateTarget_AnyOf_SecondMatches_ReturnsPermit()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "admin")
                            ]
                        },
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "viewer")
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(subject: SingleBag(XACMLDataTypes.String, "viewer"));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Permit, "second AllOf matches — OR satisfied");
    }

    [Fact]
    public void EvaluateTarget_AnyOf_NoneMatch_ReturnsNotApplicable()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "admin")
                            ]
                        },
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "manager")
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(subject: SingleBag(XACMLDataTypes.String, "viewer"));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public void EvaluateTarget_AnyOf_EmptyAllOfElements_ReturnsPermit()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf { AllOfElements = [] }
            ]
        };
        var ctx = MakeContext();

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Permit, "empty AllOf list matches all requests");
    }

    #endregion

    #region AllOf (AND) Logic

    [Fact]
    public void EvaluateTarget_AllOf_BothMatch_ReturnsPermit()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "admin"),
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Action, "name", XACMLDataTypes.String, "read")
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(
            subject: SingleBag(XACMLDataTypes.String, "admin"),
            action: SingleBag(XACMLDataTypes.String, "read"));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Permit, "both matches satisfied — AND logic");
    }

    [Fact]
    public void EvaluateTarget_AllOf_OneDoesNotMatch_ReturnsNotApplicable()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "admin"),
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Action, "name", XACMLDataTypes.String, "delete")
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(
            subject: SingleBag(XACMLDataTypes.String, "admin"),
            action: SingleBag(XACMLDataTypes.String, "read"));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.NotApplicable, "action does not match — AND fails");
    }

    [Fact]
    public void EvaluateTarget_AllOf_EmptyMatches_ReturnsPermit()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf { Matches = [] }
                    ]
                }
            ]
        };
        var ctx = MakeContext();

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Permit, "empty matches list matches all requests");
    }

    #endregion

    #region Multiple AnyOf (top-level AND)

    [Fact]
    public void EvaluateTarget_MultipleAnyOf_AllMatch_ReturnsPermit()
    {
        // Two AnyOf elements: subject must be admin AND action must be read
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "admin")
                            ]
                        }
                    ]
                },
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Action, "name", XACMLDataTypes.String, "read")
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(
            subject: SingleBag(XACMLDataTypes.String, "admin"),
            action: SingleBag(XACMLDataTypes.String, "read"));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Permit, "both AnyOf elements match — top-level AND");
    }

    [Fact]
    public void EvaluateTarget_MultipleAnyOf_OneDoesNotMatch_ReturnsNotApplicable()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "admin")
                            ]
                        }
                    ]
                },
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Action, "name", XACMLDataTypes.String, "delete")
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(
            subject: SingleBag(XACMLDataTypes.String, "admin"),
            action: SingleBag(XACMLDataTypes.String, "read"));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.NotApplicable, "second AnyOf does not match — top-level AND fails");
    }

    #endregion

    #region Missing Attribute and Indeterminate

    [Fact]
    public void EvaluateTarget_MissingAttribute_MustBePresent_ReturnsIndeterminate()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "admin",
                                    mustBePresent: true)
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(); // No subject attributes

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Indeterminate, "MustBePresent attribute is missing");
    }

    [Fact]
    public void EvaluateTarget_MissingAttribute_NotMustBePresent_ReturnsNotApplicable()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "admin",
                                    mustBePresent: false)
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(); // No subject attributes

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.NotApplicable, "optional attribute missing → no match");
    }

    [Fact]
    public void EvaluateTarget_UnregisteredFunction_ReturnsIndeterminate()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    "nonexistent-function",
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String, "admin")
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(subject: SingleBag(XACMLDataTypes.String, "admin"));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Indeterminate, "unregistered function should produce indeterminate");
    }

    #endregion

    #region Multi-valued Bag (any match)

    [Fact]
    public void EvaluateTarget_MultiValuedBag_AnyMatches_ReturnsPermit()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "roles", XACMLDataTypes.String, "admin")
                            ]
                        }
                    ]
                }
            ]
        };
        var bag = AttributeBag.Of(
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "viewer" },
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "admin" });
        var ctx = MakeContext(subject: bag);

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Permit, "one of the bag values matches");
    }

    [Fact]
    public void EvaluateTarget_MultiValuedBag_NoneMatch_ReturnsNotApplicable()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "roles", XACMLDataTypes.String, "admin")
                            ]
                        }
                    ]
                }
            ]
        };
        var bag = AttributeBag.Of(
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "viewer" },
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "editor" });
        var ctx = MakeContext(subject: bag);

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.NotApplicable, "no bag values match");
    }

    #endregion

    #region Indeterminate Propagation in AnyOf

    [Fact]
    public void EvaluateTarget_AnyOf_IndeterminateAndNotApplicable_ReturnsIndeterminate()
    {
        // First AllOf is indeterminate (MustBePresent missing), second is NotApplicable
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "missing", XACMLDataTypes.String, "x",
                                    mustBePresent: true)
                            ]
                        },
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Action, "name", XACMLDataTypes.String, "delete")
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(action: SingleBag(XACMLDataTypes.String, "read"));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Indeterminate,
            "indeterminate propagates when no AllOf matches");
    }

    [Fact]
    public void EvaluateTarget_AnyOf_IndeterminateAndPermit_ReturnsPermit()
    {
        // First AllOf is indeterminate (MustBePresent missing), second AllOf matches
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Subject, "missing", XACMLDataTypes.String, "x",
                                    mustBePresent: true)
                            ]
                        },
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.StringEqual,
                                    AttributeCategory.Action, "name", XACMLDataTypes.String, "read")
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(action: SingleBag(XACMLDataTypes.String, "read"));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Permit,
            "at least one AllOf matches, so indeterminate does not propagate");
    }

    #endregion

    #region Integer Comparison

    [Fact]
    public void EvaluateTarget_IntegerGreaterThan_Matches_ReturnsPermit()
    {
        var target = new Target
        {
            AnyOfElements =
            [
                new AnyOf
                {
                    AllOfElements =
                    [
                        new AllOf
                        {
                            Matches =
                            [
                                MakeMatch(
                                    XACMLFunctionIds.IntegerGreaterThan,
                                    AttributeCategory.Resource, "amount",
                                    XACMLDataTypes.Integer, 1000)
                            ]
                        }
                    ]
                }
            ]
        };
        var ctx = MakeContext(resource: SingleBag(XACMLDataTypes.Integer, 5000));

        var result = _sut.EvaluateTarget(target, ctx);

        result.ShouldBe(Effect.Permit);
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public void EvaluateTarget_NullContext_Throws()
    {
        Action act = () => { _sut.EvaluateTarget(null, null!); };

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_NullFunctionRegistry_Throws()
    {
        var act = () => new TargetEvaluator(null!);

        Should.Throw<ArgumentNullException>(act);
    }

    #endregion
}
