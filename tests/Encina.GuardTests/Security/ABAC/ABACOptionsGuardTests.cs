using Encina.Security.ABAC;

using FluentAssertions;

using NSubstitute;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard clause tests for <see cref="ABACOptions"/>.
/// Covers defaults, property setting, AddFunction guards, fluent API chaining,
/// and seed collection behavior.
/// </summary>
public class ABACOptionsGuardTests
{
    #region Default Values

    [Fact]
    public void Defaults_EnforcementMode_IsBlock()
    {
        var options = new ABACOptions();

        options.EnforcementMode.Should().Be(ABACEnforcementMode.Block);
    }

    [Fact]
    public void Defaults_DefaultNotApplicableEffect_IsDeny()
    {
        var options = new ABACOptions();

        options.DefaultNotApplicableEffect.Should().Be(Effect.Deny);
    }

    [Fact]
    public void Defaults_IncludeAdvice_IsTrue()
    {
        var options = new ABACOptions();

        options.IncludeAdvice.Should().BeTrue();
    }

    [Fact]
    public void Defaults_FailOnMissingObligationHandler_IsTrue()
    {
        var options = new ABACOptions();

        options.FailOnMissingObligationHandler.Should().BeTrue();
    }

    [Fact]
    public void Defaults_AddHealthCheck_IsFalse()
    {
        var options = new ABACOptions();

        options.AddHealthCheck.Should().BeFalse();
    }

    [Fact]
    public void Defaults_ValidateExpressionsAtStartup_IsFalse()
    {
        var options = new ABACOptions();

        options.ValidateExpressionsAtStartup.Should().BeFalse();
    }

    [Fact]
    public void Defaults_UsePersistentPAP_IsFalse()
    {
        var options = new ABACOptions();

        options.UsePersistentPAP.Should().BeFalse();
    }

    [Fact]
    public void Defaults_PolicyCaching_IsNotNull()
    {
        var options = new ABACOptions();

        options.PolicyCaching.Should().NotBeNull();
        options.PolicyCaching.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Defaults_CustomFunctions_IsEmpty()
    {
        var options = new ABACOptions();

        options.CustomFunctions.Should().BeEmpty();
    }

    [Fact]
    public void Defaults_SeedPolicySets_IsEmpty()
    {
        var options = new ABACOptions();

        options.SeedPolicySets.Should().BeEmpty();
    }

    [Fact]
    public void Defaults_SeedPolicies_IsEmpty()
    {
        var options = new ABACOptions();

        options.SeedPolicies.Should().BeEmpty();
    }

    [Fact]
    public void Defaults_ExpressionScanAssemblies_IsEmpty()
    {
        var options = new ABACOptions();

        options.ExpressionScanAssemblies.Should().BeEmpty();
    }

    #endregion

    #region Property Setting

    [Fact]
    public void EnforcementMode_CanBeSetToWarn()
    {
        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Warn };

        options.EnforcementMode.Should().Be(ABACEnforcementMode.Warn);
    }

    [Fact]
    public void EnforcementMode_CanBeSetToDisabled()
    {
        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Disabled };

        options.EnforcementMode.Should().Be(ABACEnforcementMode.Disabled);
    }

    [Fact]
    public void DefaultNotApplicableEffect_CanBeSetToPermit()
    {
        var options = new ABACOptions { DefaultNotApplicableEffect = Effect.Permit };

        options.DefaultNotApplicableEffect.Should().Be(Effect.Permit);
    }

    [Fact]
    public void IncludeAdvice_CanBeSetToFalse()
    {
        var options = new ABACOptions { IncludeAdvice = false };

        options.IncludeAdvice.Should().BeFalse();
    }

    [Fact]
    public void FailOnMissingObligationHandler_CanBeSetToFalse()
    {
        var options = new ABACOptions { FailOnMissingObligationHandler = false };

        options.FailOnMissingObligationHandler.Should().BeFalse();
    }

    [Fact]
    public void UsePersistentPAP_CanBeSetToTrue()
    {
        var options = new ABACOptions { UsePersistentPAP = true };

        options.UsePersistentPAP.Should().BeTrue();
    }

    #endregion

    #region AddFunction — Guards

    [Fact]
    public void AddFunction_NullFunctionId_ThrowsArgumentException()
    {
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        var act = () => options.AddFunction(null!, function);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddFunction_EmptyFunctionId_ThrowsArgumentException()
    {
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        var act = () => options.AddFunction("", function);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddFunction_WhitespaceFunctionId_ThrowsArgumentException()
    {
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        var act = () => options.AddFunction("   ", function);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddFunction_NullFunction_ThrowsArgumentNullException()
    {
        var options = new ABACOptions();

        var act = () => options.AddFunction("custom:test", null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("function");
    }

    [Fact]
    public void AddFunction_ValidParams_AddsToCollection()
    {
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        options.AddFunction("custom:test", function);

        options.CustomFunctions.Should().ContainSingle()
            .Which.FunctionId.Should().Be("custom:test");
    }

    [Fact]
    public void AddFunction_ReturnsSameInstance_ForChaining()
    {
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        var result = options.AddFunction("custom:test", function);

        result.Should().BeSameAs(options);
    }

    [Fact]
    public void AddFunction_MultipleFunctions_AllAdded()
    {
        var options = new ABACOptions();
        var f1 = Substitute.For<IXACMLFunction>();
        var f2 = Substitute.For<IXACMLFunction>();

        options.AddFunction("custom:func1", f1)
               .AddFunction("custom:func2", f2);

        options.CustomFunctions.Should().HaveCount(2);
    }

    #endregion

    #region UseXacmlXmlSerializer — Fluent API

    [Fact]
    public void UseXacmlXmlSerializer_SetsUseXacmlXml()
    {
        var options = new ABACOptions();

        var result = options.UseXacmlXmlSerializer();

        options.UseXacmlXml.Should().BeTrue();
        result.Should().BeSameAs(options);
    }

    #endregion

    #region RegisterXacmlXmlSerializer — Fluent API

    [Fact]
    public void RegisterXacmlXmlSerializer_SetsFlag()
    {
        var options = new ABACOptions();

        var result = options.RegisterXacmlXmlSerializer();

        options.RegisterXacmlXmlAsKeyed.Should().BeTrue();
        result.Should().BeSameAs(options);
    }

    #endregion

    #region Seed Collections — Mutability

    [Fact]
    public void SeedPolicySets_CanAddItems()
    {
        var options = new ABACOptions();
        var ps = new PolicySet
        {
            Id = "seed-ps",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Policies = [],
            PolicySets = [],
            Obligations = [],
            Advice = []
        };

        options.SeedPolicySets.Add(ps);

        options.SeedPolicySets.Should().ContainSingle()
            .Which.Id.Should().Be("seed-ps");
    }

    [Fact]
    public void SeedPolicies_CanAddItems()
    {
        var options = new ABACOptions();
        var pol = new Policy
        {
            Id = "seed-pol",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules = [],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        };

        options.SeedPolicies.Add(pol);

        options.SeedPolicies.Should().ContainSingle()
            .Which.Id.Should().Be("seed-pol");
    }

    [Fact]
    public void ExpressionScanAssemblies_CanAddAssembly()
    {
        var options = new ABACOptions();

        options.ExpressionScanAssemblies.Add(typeof(ABACOptions).Assembly);

        options.ExpressionScanAssemblies.Should().ContainSingle();
    }

    #endregion
}
