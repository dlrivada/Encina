using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;
using FluentAssertions;
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

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentException()
    {
        var act = () => new PolicyBuilder(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var act = () => new PolicyBuilder("");

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Build — Validation

    [Fact]
    public void Build_NoRules_ThrowsInvalidOperationException()
    {
        var builder = new PolicyBuilder("empty-policy");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must contain at least one rule*");
    }

    #endregion

    #region Build — Defaults

    [Fact]
    public void Build_MinimalPolicy_SetsId()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Id.Should().Be("my-policy");
    }

    [Fact]
    public void Build_MinimalPolicy_DefaultAlgorithmIsDenyOverrides()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Algorithm.Should().Be(CombiningAlgorithmId.DenyOverrides);
    }

    [Fact]
    public void Build_MinimalPolicy_IsEnabled()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Build_MinimalPolicy_PriorityIsZero()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Priority.Should().Be(0);
    }

    [Fact]
    public void Build_MinimalPolicy_NullVersion()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Version.Should().BeNull();
    }

    [Fact]
    public void Build_MinimalPolicy_NullDescription()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Description.Should().BeNull();
    }

    [Fact]
    public void Build_MinimalPolicy_NullTarget()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Target.Should().BeNull();
    }

    [Fact]
    public void Build_MinimalPolicy_EmptyObligations()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Obligations.Should().BeEmpty();
    }

    [Fact]
    public void Build_MinimalPolicy_EmptyAdvice()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.Advice.Should().BeEmpty();
    }

    [Fact]
    public void Build_MinimalPolicy_EmptyVariableDefinitions()
    {
        var policy = CreateMinimalPolicy("my-policy");

        policy.VariableDefinitions.Should().BeEmpty();
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

        policy.Version.Should().Be("2.0");
    }

    [Fact]
    public void WithDescription_SetsDescription()
    {
        var policy = new PolicyBuilder("p1")
            .WithDescription("Test policy description")
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Description.Should().Be("Test policy description");
    }

    [Fact]
    public void WithAlgorithm_SetsAlgorithm()
    {
        var policy = new PolicyBuilder("p1")
            .WithAlgorithm(CombiningAlgorithmId.PermitOverrides)
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Algorithm.Should().Be(CombiningAlgorithmId.PermitOverrides);
    }

    [Fact]
    public void WithTarget_PrebuiltTarget_SetsTarget()
    {
        var target = new Target { AnyOfElements = [] };
        var policy = new PolicyBuilder("p1")
            .WithTarget(target)
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Target.Should().NotBeNull();
    }

    [Fact]
    public void ForResourceType_SetsTargetWithResourceType()
    {
        var policy = new PolicyBuilder("p1")
            .ForResourceType<string>()
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Target.Should().NotBeNull();
        policy.Target!.AnyOfElements.Should().HaveCount(1);
    }

    [Fact]
    public void AddRule_PrebuiltRule_AddsToList()
    {
        var rule = new RuleBuilder("r1", Effect.Permit).Build();
        var policy = new PolicyBuilder("p1")
            .AddRule(rule)
            .Build();

        policy.Rules.Should().ContainSingle()
            .Which.Id.Should().Be("r1");
    }

    [Fact]
    public void AddRule_BuilderDelegate_AddsToList()
    {
        var policy = new PolicyBuilder("p1")
            .AddRule("r1", Effect.Deny, rule => rule
                .WithDescription("deny rule"))
            .Build();

        policy.Rules.Should().ContainSingle();
        policy.Rules[0].Effect.Should().Be(Effect.Deny);
    }

    [Fact]
    public void AddRule_MultipleRules_AllAdded()
    {
        var policy = new PolicyBuilder("p1")
            .AddRule("r1", Effect.Permit, _ => { })
            .AddRule("r2", Effect.Deny, _ => { })
            .AddRule("r3", Effect.Permit, _ => { })
            .Build();

        policy.Rules.Should().HaveCount(3);
    }

    [Fact]
    public void WithPriority_SetsPriority()
    {
        var policy = new PolicyBuilder("p1")
            .WithPriority(10)
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Priority.Should().Be(10);
    }

    [Fact]
    public void Disabled_SetsIsEnabledFalse()
    {
        var policy = new PolicyBuilder("p1")
            .Disabled()
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void AddObligation_BuilderDelegate_AddsToPolicy()
    {
        var policy = new PolicyBuilder("p1")
            .AddObligation("ob1", ob => ob.OnPermit())
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Obligations.Should().ContainSingle()
            .Which.Id.Should().Be("ob1");
    }

    [Fact]
    public void AddAdvice_BuilderDelegate_AddsToPolicy()
    {
        var policy = new PolicyBuilder("p1")
            .AddAdvice("adv1", a => a.OnDeny())
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.Advice.Should().ContainSingle()
            .Which.Id.Should().Be("adv1");
    }

    [Fact]
    public void DefineVariable_AddsVariableDefinition()
    {
        var expression = new AttributeValue { DataType = XACMLDataTypes.String, Value = "test" };
        var policy = new PolicyBuilder("p1")
            .DefineVariable("var1", expression)
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        policy.VariableDefinitions.Should().ContainSingle()
            .Which.VariableId.Should().Be("var1");
    }

    #endregion

    #region Helpers

    private static Policy CreateMinimalPolicy(string id) =>
        new PolicyBuilder(id)
            .AddRule("default-rule", Effect.Permit, _ => { })
            .Build();

    #endregion
}
