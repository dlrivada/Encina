using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;
using Shouldly;
using Rule = Encina.Security.ABAC.Rule;
using Target = Encina.Security.ABAC.Target;

namespace Encina.GuardTests.Security.ABAC.Builders;

/// <summary>
/// Guard clause tests for <see cref="PolicyBuilder"/>.
/// </summary>
public class PolicyBuilderGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
        var act = () => new PolicyBuilder(null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("id");
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var act = () => new PolicyBuilder("");
        act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("id");
    }

    [Fact]
    public void Constructor_WhitespaceId_ThrowsArgumentException()
    {
        var act = () => new PolicyBuilder("   ");
        act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("id");
    }

    #endregion

    #region WithVersion Guards

    [Fact]
    public void WithVersion_NullVersion_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.WithVersion(null!);
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void WithVersion_EmptyVersion_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.WithVersion("");
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void WithVersion_WhitespaceVersion_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.WithVersion("   ");
        act.ShouldThrow<ArgumentException>();
    }

    #endregion

    #region WithDescription Guards

    [Fact]
    public void WithDescription_NullDescription_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.WithDescription(null!);
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void WithDescription_EmptyDescription_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.WithDescription("");
        act.ShouldThrow<ArgumentException>();
    }

    #endregion

    #region WithTarget Guards

    [Fact]
    public void WithTarget_NullTarget_ThrowsArgumentNullException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.WithTarget((Target)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("target");
    }

    [Fact]
    public void WithTarget_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.WithTarget((Action<TargetBuilder>)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region AddRule Guards

    [Fact]
    public void AddRule_NullRule_ThrowsArgumentNullException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.AddRule((Rule)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("rule");
    }

    [Fact]
    public void AddRule_NullRuleId_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.AddRule(null!, Effect.Permit, _ => { });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddRule_EmptyRuleId_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.AddRule("", Effect.Permit, _ => { });
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AddRule_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.AddRule("rule-1", Effect.Permit, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region AddObligation Guards

    [Fact]
    public void AddObligation_NullObligation_ThrowsArgumentNullException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.AddObligation((Obligation)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("obligation");
    }

    [Fact]
    public void AddObligation_NullObligationId_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.AddObligation(null!, _ => { });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddObligation_EmptyObligationId_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.AddObligation("", _ => { });
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AddObligation_NullObligationConfigure_ThrowsArgumentNullException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.AddObligation("ob-1", null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region AddAdvice Guards

    [Fact]
    public void AddAdvice_NullAdvice_ThrowsArgumentNullException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.AddAdvice((AdviceExpression)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("advice");
    }

    [Fact]
    public void AddAdvice_NullAdviceId_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.AddAdvice(null!, _ => { });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddAdvice_EmptyAdviceId_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.AddAdvice("", _ => { });
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AddAdvice_NullAdviceConfigure_ThrowsArgumentNullException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.AddAdvice("adv-1", null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region DefineVariable Guards

    [Fact]
    public void DefineVariable_NullVariableId_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.DefineVariable(null!, new AttributeValue { DataType = XACMLDataTypes.String, Value = "test" });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void DefineVariable_EmptyVariableId_ThrowsArgumentException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.DefineVariable("", new AttributeValue { DataType = XACMLDataTypes.String, Value = "test" });
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void DefineVariable_NullExpression_ThrowsArgumentNullException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.DefineVariable("var-1", null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("expression");
    }

    #endregion

    #region Build Guards

    [Fact]
    public void Build_NoRules_ThrowsInvalidOperationException()
    {
        var builder = new PolicyBuilder("test-policy");
        var act = () => builder.Build();
        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("at least one rule");
    }

    [Fact]
    public void Build_WithRules_Succeeds()
    {
        var builder = new PolicyBuilder("test-policy");
        builder.AddRule("rule-1", Effect.Permit, _ => { });
        var policy = builder.Build();
        policy.Id.ShouldBe("test-policy");
        policy.Rules.Count.ShouldBe(1);
    }

    #endregion

    #region Fluent Chaining

    [Fact]
    public void FluentChaining_AllMethods_ReturnSameBuilder()
    {
        var builder = new PolicyBuilder("test-policy");

        builder.WithVersion("1.0").ShouldBeSameAs(builder);
        builder.WithDescription("desc").ShouldBeSameAs(builder);
        builder.WithAlgorithm(CombiningAlgorithmId.PermitOverrides).ShouldBeSameAs(builder);
        builder.WithPriority(5).ShouldBeSameAs(builder);
        builder.Disabled().ShouldBeSameAs(builder);
        builder.ForResourceType<string>().ShouldBeSameAs(builder);
    }

    #endregion
}
