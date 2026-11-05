using Encina.Audit.Marten;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for the resolution error codes and factories of <see cref="MartenAuditErrors"/> (issue #3049, phase 1).
/// The errors carry codes only: no reason, event or identity.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class AuditResolutionErrorsTests
{
    public static TheoryData<string, Func<EncinaError>> Cases => new()
    {
        { MartenAuditErrors.ResolutionForbiddenCode, MartenAuditErrors.ResolutionForbidden },
        { MartenAuditErrors.ResolutionPrincipalRequiredCode, MartenAuditErrors.ResolutionPrincipalRequired },
        { MartenAuditErrors.InvalidResolutionReasonCode, MartenAuditErrors.InvalidResolutionReason },
        { MartenAuditErrors.ResolutionEventNotFoundCode, MartenAuditErrors.ResolutionEventNotFound },
        { MartenAuditErrors.InvalidResolutionFieldCode, MartenAuditErrors.InvalidResolutionField },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Factory_ReturnsItsCodeAndStage(string code, Func<EncinaError> factory)
    {
        var error = factory();

        error.GetCode().IfNone(string.Empty).ShouldBe(code);
        error.Message.ShouldNotBeNullOrWhiteSpace();
        error.GetDetails().ShouldContainKeyAndValue("stage", "audit-marten");
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Factory_CarriesNoIdentityOrReasonDetails(string code, Func<EncinaError> factory)
    {
        _ = code;

        factory().GetDetails().Keys.ShouldBe(["stage"]);
    }

    [Fact]
    public void Codes_FollowTheConventionAndAreUnique()
    {
        var codes = new[]
        {
            MartenAuditErrors.ResolutionForbiddenCode,
            MartenAuditErrors.ResolutionPrincipalRequiredCode,
            MartenAuditErrors.InvalidResolutionReasonCode,
            MartenAuditErrors.ResolutionEventNotFoundCode,
            MartenAuditErrors.InvalidResolutionFieldCode,
        };

        MartenAuditErrors.ResolutionForbiddenCode.ShouldBe("audit.marten.resolution_forbidden");
        codes.ShouldAllBe(c => c.StartsWith("audit.marten.", StringComparison.Ordinal));
        codes.Distinct().Count().ShouldBe(codes.Length);
    }
}
