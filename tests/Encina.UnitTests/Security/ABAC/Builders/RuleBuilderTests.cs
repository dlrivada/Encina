using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;
using FluentAssertions;
using Target = Encina.Security.ABAC.Target;

namespace Encina.UnitTests.Security.ABAC.Builders;

/// <summary>
/// Unit tests for <see cref="RuleBuilder"/>.
/// Verifies XACML 3.0 §7.9: rule construction, effect validation, and fluent API.
/// </summary>
public sealed class RuleBuilderTests
{
    #region Constructor

    [Fact]
    public void Constructor_ValidIdAndPermit_DoesNotThrow()
    {
        var act = () => new RuleBuilder("rule-1", Effect.Permit);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ValidIdAndDeny_DoesNotThrow()
    {
        var act = () => new RuleBuilder("rule-1", Effect.Deny);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentException()
    {
        var act = () => new RuleBuilder(null!, Effect.Permit);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var act = () => new RuleBuilder("", Effect.Permit);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(Effect.NotApplicable)]
    [InlineData(Effect.Indeterminate)]
    public void Constructor_InvalidEffect_ThrowsArgumentOutOfRangeException(Effect invalidEffect)
    {
        var act = () => new RuleBuilder("rule-1", invalidEffect);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("effect");
    }

    #endregion

    #region Build — Minimal

    [Fact]
    public void Build_MinimalRule_SetsId()
    {
        var rule = new RuleBuilder("test-rule", Effect.Permit).Build();

        rule.Id.Should().Be("test-rule");
    }

    [Fact]
    public void Build_MinimalRule_SetsEffect()
    {
        var rule = new RuleBuilder("test-rule", Effect.Deny).Build();

        rule.Effect.Should().Be(Effect.Deny);
    }

    [Fact]
    public void Build_MinimalRule_NullDescription()
    {
        var rule = new RuleBuilder("test-rule", Effect.Permit).Build();

        rule.Description.Should().BeNull();
    }

    [Fact]
    public void Build_MinimalRule_NullTarget()
    {
        var rule = new RuleBuilder("test-rule", Effect.Permit).Build();

        rule.Target.Should().BeNull();
    }

    [Fact]
    public void Build_MinimalRule_NullCondition()
    {
        var rule = new RuleBuilder("test-rule", Effect.Permit).Build();

        rule.Condition.Should().BeNull();
    }

    [Fact]
    public void Build_MinimalRule_EmptyObligations()
    {
        var rule = new RuleBuilder("test-rule", Effect.Permit).Build();

        rule.Obligations.Should().BeEmpty();
    }

    [Fact]
    public void Build_MinimalRule_EmptyAdvice()
    {
        var rule = new RuleBuilder("test-rule", Effect.Permit).Build();

        rule.Advice.Should().BeEmpty();
    }

    #endregion

    #region Fluent API

    [Fact]
    public void WithDescription_SetsDescription()
    {
        var rule = new RuleBuilder("rule-1", Effect.Permit)
            .WithDescription("Test description")
            .Build();

        rule.Description.Should().Be("Test description");
    }

    [Fact]
    public void WithDescription_NullDescription_ThrowsArgumentException()
    {
        var act = () => new RuleBuilder("rule-1", Effect.Permit)
            .WithDescription(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithTarget_PrebuiltTarget_SetsTarget()
    {
        var target = new Target { AnyOfElements = [] };
        var rule = new RuleBuilder("rule-1", Effect.Permit)
            .WithTarget(target)
            .Build();

        rule.Target.Should().BeSameAs(target);
    }

    [Fact]
    public void WithTarget_NullTarget_ThrowsArgumentNullException()
    {
        var act = () => new RuleBuilder("rule-1", Effect.Permit)
            .WithTarget((Target)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithTarget_BuilderDelegate_SetsTarget()
    {
        var rule = new RuleBuilder("rule-1", Effect.Permit)
            .WithTarget(t => t.AnyOf(any => any
                .AllOf(all => all.MatchAttribute(
                    AttributeCategory.Subject, "role",
                    ConditionOperator.Equals, "admin"))))
            .Build();

        rule.Target.Should().NotBeNull();
        rule.Target!.AnyOfElements.Should().HaveCount(1);
    }

    [Fact]
    public void WithCondition_SetsCondition()
    {
        var condition = new Apply
        {
            FunctionId = "string-equal",
            Arguments = []
        };
        var rule = new RuleBuilder("rule-1", Effect.Permit)
            .WithCondition(condition)
            .Build();

        rule.Condition.Should().BeSameAs(condition);
    }

    [Fact]
    public void AddObligation_PrebuiltObligation_AddsToList()
    {
        var obligation = new Obligation
        {
            Id = "log-access",
            FulfillOn = FulfillOn.Permit,
            AttributeAssignments = []
        };

        var rule = new RuleBuilder("rule-1", Effect.Permit)
            .AddObligation(obligation)
            .Build();

        rule.Obligations.Should().ContainSingle()
            .Which.Id.Should().Be("log-access");
    }

    [Fact]
    public void AddObligation_BuilderDelegate_AddsToList()
    {
        var rule = new RuleBuilder("rule-1", Effect.Permit)
            .AddObligation("audit", ob => ob.OnPermit())
            .Build();

        rule.Obligations.Should().ContainSingle()
            .Which.Id.Should().Be("audit");
    }

    [Fact]
    public void AddAdvice_PrebuiltAdvice_AddsToList()
    {
        var advice = new AdviceExpression
        {
            Id = "notify",
            AppliesTo = FulfillOn.Deny,
            AttributeAssignments = []
        };

        var rule = new RuleBuilder("rule-1", Effect.Permit)
            .AddAdvice(advice)
            .Build();

        rule.Advice.Should().ContainSingle()
            .Which.Id.Should().Be("notify");
    }

    [Fact]
    public void AddAdvice_BuilderDelegate_AddsToList()
    {
        var rule = new RuleBuilder("rule-1", Effect.Permit)
            .AddAdvice("suggest", a => a.OnDeny())
            .Build();

        rule.Advice.Should().ContainSingle()
            .Which.Id.Should().Be("suggest");
    }

    [Fact]
    public void FluentChaining_AllMethods_ReturnSameBuilder()
    {
        var builder = new RuleBuilder("rule-1", Effect.Permit);

        var result = builder
            .WithDescription("desc")
            .WithTarget(new Target { AnyOfElements = [] })
            .AddObligation(new Obligation { Id = "ob", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] })
            .AddAdvice(new AdviceExpression { Id = "ad", AppliesTo = FulfillOn.Permit, AttributeAssignments = [] });

        // All methods return the builder for chaining — verify the build succeeds
        var rule = result.Build();
        rule.Id.Should().Be("rule-1");
        rule.Obligations.Should().HaveCount(1);
        rule.Advice.Should().HaveCount(1);
    }

    #endregion
}
