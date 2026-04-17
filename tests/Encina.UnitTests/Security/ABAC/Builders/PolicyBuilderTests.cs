using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;
using Shouldly;
using Target = Encina.Security.ABAC.Target;

namespace Encina.UnitTests.Security.ABAC.Builders;

/// <summary>
/// Unit tests for <see cref="PolicyBuilder"/>.
/// Verifies XACML 3.0 §7.10: policy construction with rules, algorithm, and validation.
/// </summary>
public sealed class PolicyBuilderTests
{
    #region Constructor

    [Fact]
    public void Constructor_ValidId_DoesNotThrow()
    {
        var act = () => new PolicyBuilder("test-policy");

        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentException()
    {
        var act = () => new PolicyBuilder(null!);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var act = () => new PolicyBuilder("");

        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Build — Validation

    [Fact]
    public void Build_NoRules_ThrowsInvalidOperationException()
    {
        var builder = new PolicyBuilder("empty-policy");

        var act = () => builder.Build();

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("must contain at least one rule");
    }

    #endregion

    #region Build — Defaults

    [Fact]
    public void Build_MinimalPolicy_SetsId()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Id.ShouldBe("my-policy");
    }

    [Fact]
    public void Build_MinimalPolicy_DefaultAlgorithmIsDenyOverrides()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Algorithm.ShouldBe(CombiningAlgorithmId.DenyOverrides);
    }

    [Fact]
    public void Build_MinimalPolicy_IsEnabled()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Build_MinimalPolicy_PriorityIsZero()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Priority.ShouldBe(0);
    }

    [Fact]
    public void Build_MinimalPolicy_NullVersion()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Version.ShouldBeNull();
    }

    [Fact]
    public void Build_MinimalPolicy_NullDescription()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Description.ShouldBeNull();
    }

    [Fact]
    public void Build_MinimalPolicy_NullTarget()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Target.ShouldBeNull();
    }

    [Fact]
    public void Build_MinimalPolicy_EmptyObligations()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Obligations.ShouldBeEmpty();
    }

    [Fact]
    public void Build_MinimalPolicy_EmptyAdvice()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Advice.ShouldBeEmpty();
    }

    [Fact]
    public void Build_MinimalPolicy_EmptyVariableDefinitions()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.VariableDefinitions.ShouldBeEmpty();
    }

    #endregion

    #region Fluent API

    [Fact]
    public void WithVersion_SetsVersion()
    {
        var policy = new PolicyBuilder("p1")
            .WithVersion("2.0")
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Version.ShouldBe("2.0");
    }

    [Fact]
    public void WithDescription_SetsDescription()
    {
        var policy = new PolicyBuilder("p1")
            .WithDescription("Test policy description")
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Description.ShouldBe("Test policy description");
    }

    [Fact]
    public void WithAlgorithm_SetsAlgorithm()
    {
        var policy = new PolicyBuilder("p1")
            .WithAlgorithm(CombiningAlgorithmId.PermitOverrides)
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Algorithm.ShouldBe(CombiningAlgorithmId.PermitOverrides);
    }

    [Fact]
    public void WithTarget_PrebuiltTarget_SetsTarget()
    {
        var target = new Target { AnyOfElements = [] };
        var policy = new PolicyBuilder("p1")
            .WithTarget(target)
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Target.ShouldNotBeNull();
    }

    [Fact]
    public void ForResourceType_SetsTargetWithResourceType()
    {
        var policy = new PolicyBuilder("p1")
            .ForResourceType<string>()
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Target.ShouldNotBeNull();
        policy.Target!.AnyOfElements.Count.ShouldBe(1);
    }

    [Fact]
    public void AddRule_PrebuiltRule_AddsToList()
    {
        var rule = new RuleBuilder("r1", Effect.Permit).Build();
        var policy = new PolicyBuilder("p1")
            .AddRule(rule)
            .Build();

        policy.Rules.ShouldHaveSingleItem()
            .Id.ShouldBe("r1");
    }

    [Fact]
    public void AddRule_BuilderDelegate_AddsToList()
    {
        var policy = new PolicyBuilder("p1")
            .AddRule("r1", Effect.Deny, rule => rule
                .WithDescription("deny rule"))
            .Build();

        policy.Rules.ShouldHaveSingleItem();
        policy.Rules[0].Effect.ShouldBe(Effect.Deny);
    }

    [Fact]
    public void AddRule_MultipleRules_AllAdded()
    {
        var policy = new PolicyBuilder("p1")
            .AddRule("r1", Effect.Permit, _ => { })
            .AddRule("r2", Effect.Deny, _ => { })
            .AddRule("r3", Effect.Permit, _ => { })
            .Build();

        policy.Rules.Count.ShouldBe(3);
    }

    [Fact]
    public void WithPriority_SetsPriority()
    {
        var policy = new PolicyBuilder("p1")
            .WithPriority(10)
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Priority.ShouldBe(10);
    }

    [Fact]
    public void Disabled_SetsIsEnabledFalse()
    {
        var policy = new PolicyBuilder("p1")
            .Disabled()
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void AddObligation_BuilderDelegate_AddsToPolicy()
    {
        var policy = new PolicyBuilder("p1")
            .AddObligation("ob1", ob => ob.OnPermit())
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Obligations.ShouldHaveSingleItem()
            .Id.ShouldBe("ob1");
    }

    [Fact]
    public void AddAdvice_BuilderDelegate_AddsToPolicy()
    {
        var policy = new PolicyBuilder("p1")
            .AddAdvice("adv1", a => a.OnDeny())
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Advice.ShouldHaveSingleItem()
            .Id.ShouldBe("adv1");
    }

    [Fact]
    public void DefineVariable_AddsVariableDefinition()
    {
        var expression = new AttributeValue { DataType = XACMLDataTypes.String, Value = "test" };
        var policy = new PolicyBuilder("p1")
            .DefineVariable("var1", expression)
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.VariableDefinitions.ShouldHaveSingleItem()
            .VariableId.ShouldBe("var1");
    }

    #endregion

    #region Helpers

    private static Policy CreateMinimalPolicy(string id) =>
        new PolicyBuilder(id)
            .AddRule("default-rule", Effect.Permit, _ => { })
            .Build();

    #endregion
}
