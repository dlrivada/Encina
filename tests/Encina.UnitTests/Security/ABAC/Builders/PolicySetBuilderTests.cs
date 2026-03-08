using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;
using FluentAssertions;

namespace Encina.UnitTests.Security.ABAC.Builders;

/// <summary>
/// Unit tests for <see cref="PolicySetBuilder"/>.
/// Verifies XACML 3.0 §7.11: policy set construction, recursive nesting, and validation.
/// </summary>
public sealed class PolicySetBuilderTests
{
    #region Constructor

    [Fact]
    public void Constructor_ValidId_DoesNotThrow()
    {
        var act = () => new PolicySetBuilder("test-set");

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentException()
    {
        var act = () => new PolicySetBuilder(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var act = () => new PolicySetBuilder("");

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Build — Validation

    [Fact]
    public void Build_NoPoliciesOrPolicySets_ThrowsInvalidOperationException()
    {
        var builder = new PolicySetBuilder("empty-set");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must contain at least one policy or nested policy set*");
    }

    #endregion

    #region Build — Defaults

    [Fact]
    public void Build_WithPolicy_SetsId()
    {
        var policySet = CreateMinimalPolicySet("my-set");

        policySet.Id.Should().Be("my-set");
    }

    [Fact]
    public void Build_DefaultAlgorithm_IsDenyOverrides()
    {
        var policySet = CreateMinimalPolicySet("my-set");

        policySet.Algorithm.Should().Be(CombiningAlgorithmId.DenyOverrides);
    }

    [Fact]
    public void Build_DefaultIsEnabled_True()
    {
        var policySet = CreateMinimalPolicySet("my-set");

        policySet.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Build_DefaultPriority_Zero()
    {
        var policySet = CreateMinimalPolicySet("my-set");

        policySet.Priority.Should().Be(0);
    }

    #endregion

    #region Policies

    [Fact]
    public void AddPolicy_PrebuiltPolicy_AddsToList()
    {
        var policy = new PolicyBuilder("p1")
            .AddRule("r1", Effect.Permit, _ => { })
            .Build();

        var policySet = new PolicySetBuilder("set1")
            .AddPolicy(policy)
            .Build();

        policySet.Policies.Should().ContainSingle()
            .Which.Id.Should().Be("p1");
    }

    [Fact]
    public void AddPolicy_BuilderDelegate_AddsToList()
    {
        var policySet = new PolicySetBuilder("set1")
            .AddPolicy("p1", p => p
                .AddRule("r1", Effect.Permit, _ => { }))
            .Build();

        policySet.Policies.Should().ContainSingle()
            .Which.Id.Should().Be("p1");
    }

    [Fact]
    public void AddPolicy_MultiplePolicies_AllAdded()
    {
        var policySet = new PolicySetBuilder("set1")
            .AddPolicy("p1", p => p.AddRule("r1", Effect.Permit, _ => { }))
            .AddPolicy("p2", p => p.AddRule("r2", Effect.Deny, _ => { }))
            .Build();

        policySet.Policies.Should().HaveCount(2);
    }

    #endregion

    #region Nested PolicySets

    [Fact]
    public void AddPolicySet_PrebuiltPolicySet_AddsToList()
    {
        var nested = CreateMinimalPolicySet("nested");

        var policySet = new PolicySetBuilder("parent")
            .AddPolicySet(nested)
            .Build();

        policySet.PolicySets.Should().ContainSingle()
            .Which.Id.Should().Be("nested");
    }

    [Fact]
    public void AddPolicySet_BuilderDelegate_AddsToList()
    {
        var policySet = new PolicySetBuilder("parent")
            .AddPolicySet("nested", ns => ns
                .AddPolicy("inner-p", p => p
                    .AddRule("inner-r", Effect.Permit, _ => { })))
            .Build();

        policySet.PolicySets.Should().ContainSingle()
            .Which.Id.Should().Be("nested");
    }

    [Fact]
    public void Build_OnlyNestedPolicySets_NoDirectPolicies_Succeeds()
    {
        var policySet = new PolicySetBuilder("parent")
            .AddPolicySet("child", ns => ns
                .AddPolicy("p1", p => p
                    .AddRule("r1", Effect.Permit, _ => { })))
            .Build();

        policySet.Policies.Should().BeEmpty();
        policySet.PolicySets.Should().HaveCount(1);
    }

    [Fact]
    public void Build_MixedPoliciesAndPolicySets_BothPresent()
    {
        var policySet = new PolicySetBuilder("mixed")
            .AddPolicy("p1", p => p.AddRule("r1", Effect.Permit, _ => { }))
            .AddPolicySet("nested", ns => ns
                .AddPolicy("p2", p => p.AddRule("r2", Effect.Deny, _ => { })))
            .Build();

        policySet.Policies.Should().HaveCount(1);
        policySet.PolicySets.Should().HaveCount(1);
    }

    #endregion

    #region Fluent API

    [Fact]
    public void WithVersion_SetsVersion()
    {
        var policySet = new PolicySetBuilder("set1")
            .WithVersion("1.2.3")
            .AddPolicy("p1", p => p.AddRule("r1", Effect.Permit, _ => { }))
            .Build();

        policySet.Version.Should().Be("1.2.3");
    }

    [Fact]
    public void WithDescription_SetsDescription()
    {
        var policySet = new PolicySetBuilder("set1")
            .WithDescription("Organization policies")
            .AddPolicy("p1", p => p.AddRule("r1", Effect.Permit, _ => { }))
            .Build();

        policySet.Description.Should().Be("Organization policies");
    }

    [Fact]
    public void WithAlgorithm_SetsAlgorithm()
    {
        var policySet = new PolicySetBuilder("set1")
            .WithAlgorithm(CombiningAlgorithmId.FirstApplicable)
            .AddPolicy("p1", p => p.AddRule("r1", Effect.Permit, _ => { }))
            .Build();

        policySet.Algorithm.Should().Be(CombiningAlgorithmId.FirstApplicable);
    }

    [Fact]
    public void Disabled_SetsIsEnabledFalse()
    {
        var policySet = new PolicySetBuilder("set1")
            .Disabled()
            .AddPolicy("p1", p => p.AddRule("r1", Effect.Permit, _ => { }))
            .Build();

        policySet.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void WithPriority_SetsPriority()
    {
        var policySet = new PolicySetBuilder("set1")
            .WithPriority(5)
            .AddPolicy("p1", p => p.AddRule("r1", Effect.Permit, _ => { }))
            .Build();

        policySet.Priority.Should().Be(5);
    }

    [Fact]
    public void AddObligation_BuilderDelegate_AddsToSet()
    {
        var policySet = new PolicySetBuilder("set1")
            .AddObligation("ob1", ob => ob.OnPermit())
            .AddPolicy("p1", p => p.AddRule("r1", Effect.Permit, _ => { }))
            .Build();

        policySet.Obligations.Should().ContainSingle()
            .Which.Id.Should().Be("ob1");
    }

    [Fact]
    public void AddAdvice_BuilderDelegate_AddsToSet()
    {
        var policySet = new PolicySetBuilder("set1")
            .AddAdvice("adv1", a => a.OnDeny())
            .AddPolicy("p1", p => p.AddRule("r1", Effect.Permit, _ => { }))
            .Build();

        policySet.Advice.Should().ContainSingle()
            .Which.Id.Should().Be("adv1");
    }

    #endregion

    #region Helpers

    private static PolicySet CreateMinimalPolicySet(string id) =>
        new PolicySetBuilder(id)
            .AddPolicy("default-policy", p => p
                .AddRule("default-rule", Effect.Permit, _ => { }))
            .Build();

    #endregion
}
