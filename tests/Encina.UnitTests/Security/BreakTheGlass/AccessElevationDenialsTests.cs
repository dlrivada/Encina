using Encina.Security.BreakTheGlass;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>Unit tests for <see cref="AccessElevationDenials"/>, <see cref="AccessElevation"/> and the core default gate.</summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class AccessElevationDenialsTests
{
    [Theory]
    [InlineData("encina.authorization.forbidden")]
    [InlineData("encina.authorization.policy_failed")]
    [InlineData("encina.authorization.resource_denied")]
    [InlineData("encina.authorization.insufficient_roles")]
    [InlineData("encina.authorization.permission_denied")]
    [InlineData("encina.authorization.claim_missing")]
    [InlineData("encina.authorization.not_owner")]
    [InlineData("encina.authorization.abac_access_denied")]
    [InlineData("encina.authorization.abac_condition_not_met")]
    public void IsOverridable_DefiniteDenialCode_ReturnsTrue(string code)
    {
        AccessElevationDenials.IsOverridable(EncinaErrors.Create(code, "denied")).ShouldBeTrue();
    }

    [Theory]
    [InlineData("encina.authorization.unauthenticated")]
    [InlineData("abac.missing_context")]
    [InlineData("abac.unauthenticated_caller")]
    [InlineData("abac.indeterminate")]
    [InlineData("abac.evaluation_failed")]
    [InlineData("encina.authorization.abac_obligation_failed")]
    [InlineData("encina.authorization.abac_policy_not_found")]
    [InlineData("encina.authorization.made_up_code")]
    [InlineData("encina.authorization.")]
    [InlineData("ENCINA.AUTHORIZATION.FORBIDDEN")]
    [InlineData("encina.authorization.forbidden ")]
    [InlineData("break_glass.audit_failed")]
    public void IsOverridable_AnyOtherCode_ReturnsFalse(string code)
    {
        AccessElevationDenials.IsOverridable(EncinaErrors.Create(code, "denied")).ShouldBeFalse();
    }

    [Fact]
    public void IsOverridable_ErrorWithoutCode_ReturnsFalse()
    {
        AccessElevationDenials.IsOverridable(EncinaError.New("plain error")).ShouldBeFalse();
    }

    [Fact]
    public async Task AddEncina_RegistersNoAccessElevationGate_ThatKeepsEveryDenial()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(typeof(IAccessElevationGate).Assembly);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var gate = provider.GetRequiredService<IAccessElevationGate>();
        var context = BreakTheGlassTestKit.Context(BreakTheGlassTestKit.Clinician());
        var result = await gate.TryElevateAsync("request", context, EncinaErrors.Forbidden(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        result.IfLeft(default(LanguageExt.Option<AccessElevation>)).IsNone.ShouldBeTrue();
    }

    [Fact]
    public void AccessElevation_HasValueEquality()
    {
        var at = DateTimeOffset.UnixEpoch;
        var id = Guid.NewGuid();

        new AccessElevation(id, "s", at).ShouldBe(new AccessElevation(id, "s", at));
    }
}
