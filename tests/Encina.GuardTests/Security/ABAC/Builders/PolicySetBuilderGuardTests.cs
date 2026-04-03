using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC.Builders;

/// <summary>
/// Guard clause tests for <see cref="PolicySetBuilder"/>.
/// </summary>
public class PolicySetBuilderGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
        var act = () => new PolicySetBuilder(null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("id");
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var act = () => new PolicySetBuilder("");
        act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("id");
    }

    [Fact]
    public void Constructor_WhitespaceId_ThrowsArgumentException()
    {
        var act = () => new PolicySetBuilder("   ");
        act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("id");
    }

    #endregion

    #region WithVersion Guards

    [Fact]
    public void WithVersion_NullVersion_ThrowsArgumentException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.WithVersion(null!);
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void WithVersion_EmptyVersion_ThrowsArgumentException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.WithVersion("");
        act.ShouldThrow<ArgumentException>();
    }

    #endregion

    #region WithDescription Guards

    [Fact]
    public void WithDescription_NullDescription_ThrowsArgumentException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.WithDescription(null!);
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void WithDescription_EmptyDescription_ThrowsArgumentException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.WithDescription("");
        act.ShouldThrow<ArgumentException>();
    }

    #endregion

    #region WithTarget Guards

    [Fact]
    public void WithTarget_NullTarget_ThrowsArgumentNullException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.WithTarget((Target)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("target");
    }

    [Fact]
    public void WithTarget_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.WithTarget((Action<TargetBuilder>)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region AddPolicy Guards

    [Fact]
    public void AddPolicy_NullPolicy_ThrowsArgumentNullException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddPolicy((Policy)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("policy");
    }

    [Fact]
    public void AddPolicy_NullPolicyId_ThrowsArgumentException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddPolicy(null!, _ => { });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddPolicy_EmptyPolicyId_ThrowsArgumentException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddPolicy("", _ => { });
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AddPolicy_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddPolicy("p-1", (Action<PolicyBuilder>)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region AddPolicySet Guards

    [Fact]
    public void AddPolicySet_NullPolicySet_ThrowsArgumentNullException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddPolicySet((PolicySet)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("policySet");
    }

    [Fact]
    public void AddPolicySet_NullPolicySetId_ThrowsArgumentException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddPolicySet(null!, _ => { });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddPolicySet_EmptyPolicySetId_ThrowsArgumentException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddPolicySet("", _ => { });
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AddPolicySet_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddPolicySet("ps-nested", (Action<PolicySetBuilder>)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region AddObligation Guards

    [Fact]
    public void AddObligation_NullObligation_ThrowsArgumentNullException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddObligation((Obligation)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("obligation");
    }

    [Fact]
    public void AddObligation_NullObligationId_ThrowsArgumentException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddObligation(null!, _ => { });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddObligation_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddObligation("ob-1", null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region AddAdvice Guards

    [Fact]
    public void AddAdvice_NullAdvice_ThrowsArgumentNullException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddAdvice((AdviceExpression)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("advice");
    }

    [Fact]
    public void AddAdvice_NullAdviceId_ThrowsArgumentException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddAdvice(null!, _ => { });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddAdvice_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.AddAdvice("adv-1", null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region Build Guards

    [Fact]
    public void Build_NoPoliciesOrPolicySets_ThrowsInvalidOperationException()
    {
        var builder = new PolicySetBuilder("test-ps");
        var act = () => builder.Build();
        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("at least one policy");
    }

    [Fact]
    public void Build_WithPolicy_Succeeds()
    {
        var builder = new PolicySetBuilder("test-ps");
        builder.AddPolicy("p-1", p => p.AddRule("r-1", Effect.Permit, _ => { }));
        var ps = builder.Build();
        ps.Id.ShouldBe("test-ps");
        ps.Policies.Count.ShouldBe(1);
    }

    #endregion
}
