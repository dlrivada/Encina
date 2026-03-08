using Encina.Security.ABAC;
using FluentAssertions;
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

        options.EnforcementMode.Should().Be(ABACEnforcementMode.Block);
    }

    [Fact]
    public void DefaultNotApplicableEffect_DefaultsToDeny()
    {
        var options = new ABACOptions();

        options.DefaultNotApplicableEffect.Should().Be(Effect.Deny,
            "Closed-world assumption: unmatched requests denied by default");
    }

    [Fact]
    public void IncludeAdvice_DefaultsToTrue()
    {
        var options = new ABACOptions();

        options.IncludeAdvice.Should().BeTrue();
    }

    [Fact]
    public void FailOnMissingObligationHandler_DefaultsToTrue()
    {
        var options = new ABACOptions();

        options.FailOnMissingObligationHandler.Should().BeTrue(
            "XACML 7.18 mandates denying access if obligation cannot be fulfilled");
    }

    [Fact]
    public void AddHealthCheck_DefaultsToFalse()
    {
        var options = new ABACOptions();

        options.AddHealthCheck.Should().BeFalse();
    }

    [Fact]
    public void ValidateExpressionsAtStartup_DefaultsToFalse()
    {
        var options = new ABACOptions();

        options.ValidateExpressionsAtStartup.Should().BeFalse();
    }

    [Fact]
    public void ExpressionScanAssemblies_DefaultsToEmpty()
    {
        var options = new ABACOptions();

        options.ExpressionScanAssemblies.Should().BeEmpty();
    }

    [Fact]
    public void CustomFunctions_DefaultsToEmpty()
    {
        var options = new ABACOptions();

        options.CustomFunctions.Should().BeEmpty();
    }

    [Fact]
    public void SeedPolicySets_DefaultsToEmpty()
    {
        var options = new ABACOptions();

        options.SeedPolicySets.Should().BeEmpty();
    }

    [Fact]
    public void SeedPolicies_DefaultsToEmpty()
    {
        var options = new ABACOptions();

        options.SeedPolicies.Should().BeEmpty();
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

        options.EnforcementMode.Should().Be(mode);
    }

    [Theory]
    [InlineData(Effect.Deny)]
    [InlineData(Effect.Permit)]
    public void DefaultNotApplicableEffect_CanBeSet(Effect effect)
    {
        var options = new ABACOptions { DefaultNotApplicableEffect = effect };

        options.DefaultNotApplicableEffect.Should().Be(effect);
    }

    #endregion

    #region AddFunction

    [Fact]
    public void AddFunction_ValidInput_AddsToCustomFunctions()
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

        options
            .AddFunction("custom:first", f1)
            .AddFunction("custom:second", f2);

        options.CustomFunctions.Should().HaveCount(2);
    }

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

    #endregion
}
