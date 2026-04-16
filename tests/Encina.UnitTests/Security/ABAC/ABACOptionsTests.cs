using Encina.Security.ABAC;
using Shouldly;
using NSubstitute;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Unit tests for <see cref="ABACOptions"/> configuration.
/// Verifies defaults, property setters, and AddFunction fluent API.
/// </summary>
public sealed class ABACOptionsTests
{
    #region Default Values

    [Fact]
    public void EnforcementMode_DefaultsToBlock()
    {
        var options = new ABACOptions();

        options.EnforcementMode.ShouldBe(ABACEnforcementMode.Block);
    }

    [Fact]
    public void DefaultNotApplicableEffect_DefaultsToDeny()
    {
        var options = new ABACOptions();

        options.DefaultNotApplicableEffect.ShouldBe(Effect.Deny,
            "Closed-world assumption: unmatched requests denied by default");
    }

    [Fact]
    public void IncludeAdvice_DefaultsToTrue()
    {
        var options = new ABACOptions();

        options.IncludeAdvice.ShouldBeTrue();
    }

    [Fact]
    public void FailOnMissingObligationHandler_DefaultsToTrue()
    {
        var options = new ABACOptions();

        options.FailOnMissingObligationHandler.ShouldBeTrue(
            "XACML 7.18 mandates denying access if obligation cannot be fulfilled");
    }

    [Fact]
    public void AddHealthCheck_DefaultsToFalse()
    {
        var options = new ABACOptions();

        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void ValidateExpressionsAtStartup_DefaultsToFalse()
    {
        var options = new ABACOptions();

        options.ValidateExpressionsAtStartup.ShouldBeFalse();
    }

    [Fact]
    public void ExpressionScanAssemblies_DefaultsToEmpty()
    {
        var options = new ABACOptions();

        options.ExpressionScanAssemblies.ShouldBeEmpty();
    }

    [Fact]
    public void CustomFunctions_DefaultsToEmpty()
    {
        var options = new ABACOptions();

        options.CustomFunctions.ShouldBeEmpty();
    }

    [Fact]
    public void SeedPolicySets_DefaultsToEmpty()
    {
        var options = new ABACOptions();

        options.SeedPolicySets.ShouldBeEmpty();
    }

    [Fact]
    public void SeedPolicies_DefaultsToEmpty()
    {
        var options = new ABACOptions();

        options.SeedPolicies.ShouldBeEmpty();
    }

    #endregion

    #region Property Setters

    [Theory]
    [InlineData(ABACEnforcementMode.Block)]
    [InlineData(ABACEnforcementMode.Warn)]
    [InlineData(ABACEnforcementMode.Disabled)]
    public void EnforcementMode_CanBeSet(ABACEnforcementMode mode)
    {
        var options = new ABACOptions { EnforcementMode = mode };

        options.EnforcementMode.ShouldBe(mode);
    }

    [Theory]
    [InlineData(Effect.Deny)]
    [InlineData(Effect.Permit)]
    public void DefaultNotApplicableEffect_CanBeSet(Effect effect)
    {
        var options = new ABACOptions { DefaultNotApplicableEffect = effect };

        options.DefaultNotApplicableEffect.ShouldBe(effect);
    }

    #endregion

    #region AddFunction

    [Fact]
    public void AddFunction_ValidInput_AddsToCustomFunctions()
    {
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        options.AddFunction("custom:test", function);

        options.CustomFunctions.ShouldHaveSingleItem()
            .Which.FunctionId.ShouldBe("custom:test");
    }

    [Fact]
    public void AddFunction_ReturnsSameInstance_ForChaining()
    {
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        var result = options.AddFunction("custom:test", function);

        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddFunction_MultipleFunctions_AllAdded()
    {
        var options = new ABACOptions();
        var f1 = Substitute.For<IXACMLFunction>();
        var f2 = Substitute.For<IXACMLFunction>();

        options
            .AddFunction("custom:first", f1)
            .AddFunction("custom:second", f2);

        options.CustomFunctions.Count.ShouldBe(2);
    }

    [Fact]
    public void AddFunction_NullFunctionId_ThrowsArgumentException()
    {
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        var act = () => options.AddFunction(null!, function);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void AddFunction_EmptyFunctionId_ThrowsArgumentException()
    {
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        var act = () => options.AddFunction("", function);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void AddFunction_WhitespaceFunctionId_ThrowsArgumentException()
    {
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        var act = () => options.AddFunction("   ", function);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void AddFunction_NullFunction_ThrowsArgumentNullException()
    {
        var options = new ABACOptions();

        var act = () => options.AddFunction("custom:test", null!);

        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("function");
    }

    #endregion
}
