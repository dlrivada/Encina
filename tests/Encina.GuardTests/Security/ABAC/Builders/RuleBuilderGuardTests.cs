using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;
using Shouldly;
using Target = Encina.Security.ABAC.Target;

namespace Encina.GuardTests.Security.ABAC.Builders;

/// <summary>
/// Guard clause tests for <see cref="RuleBuilder"/>.
/// </summary>
public class RuleBuilderGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
        var act = () => new RuleBuilder(null!, Effect.Permit);
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var act = () => new RuleBuilder("", Effect.Permit);
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhitespaceId_ThrowsArgumentException()
    {
        var act = () => new RuleBuilder("   ", Effect.Permit);
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Constructor_InvalidEffect_NotApplicable_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new RuleBuilder("rule-1", Effect.NotApplicable);
        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("effect");
    }

    [Fact]
    public void Constructor_InvalidEffect_Indeterminate_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new RuleBuilder("rule-1", Effect.Indeterminate);
        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("effect");
    }

    [Fact]
    public void Constructor_ValidEffect_Permit_DoesNotThrow()
    {
        Should.NotThrow(() => new RuleBuilder("rule-1", Effect.Permit));
    }

    [Fact]
    public void Constructor_ValidEffect_Deny_DoesNotThrow()
    {
        Should.NotThrow(() => new RuleBuilder("rule-1", Effect.Deny));
    }

    #endregion

    #region WithDescription Guards

    [Fact]
    public void WithDescription_NullDescription_ThrowsArgumentException()
    {
        var builder = new RuleBuilder("rule-1", Effect.Permit);
        var act = () => builder.WithDescription(null!);
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void WithDescription_EmptyDescription_ThrowsArgumentException()
    {
        var builder = new RuleBuilder("rule-1", Effect.Permit);
        var act = () => builder.WithDescription("");
        act.ShouldThrow<ArgumentException>();
    }

    #endregion

    #region WithTarget Guards

    [Fact]
    public void WithTarget_NullTarget_ThrowsArgumentNullException()
    {
        var builder = new RuleBuilder("rule-1", Effect.Permit);
        var act = () => builder.WithTarget((Target)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("target");
    }

    [Fact]
    public void WithTarget_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new RuleBuilder("rule-1", Effect.Permit);
        var act = () => builder.WithTarget((Action<TargetBuilder>)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region WithCondition Guards

    [Fact]
    public void WithCondition_NullCondition_ThrowsArgumentNullException()
    {
        var builder = new RuleBuilder("rule-1", Effect.Permit);
        var act = () => builder.WithCondition(null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("condition");
    }

    #endregion

    #region AddObligation Guards

    [Fact]
    public void AddObligation_NullObligation_ThrowsArgumentNullException()
    {
        var builder = new RuleBuilder("rule-1", Effect.Permit);
        var act = () => builder.AddObligation((Obligation)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("obligation");
    }

    [Fact]
    public void AddObligation_NullObligationId_ThrowsArgumentException()
    {
        var builder = new RuleBuilder("rule-1", Effect.Permit);
        var act = () => builder.AddObligation(null!, _ => { });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddObligation_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new RuleBuilder("rule-1", Effect.Permit);
        var act = () => builder.AddObligation("ob-1", null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region AddAdvice Guards

    [Fact]
    public void AddAdvice_NullAdvice_ThrowsArgumentNullException()
    {
        var builder = new RuleBuilder("rule-1", Effect.Permit);
        var act = () => builder.AddAdvice((AdviceExpression)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("advice");
    }

    [Fact]
    public void AddAdvice_NullAdviceId_ThrowsArgumentException()
    {
        var builder = new RuleBuilder("rule-1", Effect.Permit);
        var act = () => builder.AddAdvice(null!, _ => { });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddAdvice_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new RuleBuilder("rule-1", Effect.Permit);
        var act = () => builder.AddAdvice("adv-1", null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region Build

    [Fact]
    public void Build_ReturnsRuleWithCorrectProperties()
    {
        var rule = new RuleBuilder("rule-1", Effect.Deny).Build();
        rule.Id.ShouldBe("rule-1");
        rule.Effect.ShouldBe(Effect.Deny);
    }

    #endregion
}
