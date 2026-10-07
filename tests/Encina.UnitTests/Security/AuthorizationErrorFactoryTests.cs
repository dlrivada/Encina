using Encina;
using Encina.Security;
using Shouldly;

namespace Encina.UnitTests.Security;

/// <summary>
/// Tests for the authorization error factories: <see cref="EncinaErrors.Unauthorized"/> (denial, 403),
/// <see cref="EncinaErrors.Unauthenticated"/> (401) and <see cref="SecurityErrors"/> (#1918).
/// </summary>
public sealed class AuthorizationErrorFactoryTests
{
    [Fact]
    public void Unauthorized_IsADenial()
    {
        var error = EncinaErrors.Unauthorized();

        error.GetCode().IfNone("").ShouldBe(EncinaErrorCodes.AuthorizationUnauthorized);
        error.Message.ShouldBe("Access denied.");
    }

    [Fact]
    public void Unauthenticated_RequiresAuthentication()
    {
        var error = EncinaErrors.Unauthenticated();

        error.GetCode().IfNone("").ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        error.Message.ShouldBe("Authentication is required.");
    }

    [Fact]
    public void Unauthenticated_KeepsDetails()
    {
        var details = new Dictionary<string, object?> { ["k"] = "v" };

        var error = EncinaErrors.Unauthenticated(details);

        error.GetDetails().ShouldContainKeyAndValue("k", "v");
    }

    [Fact]
    public void SecurityErrors_Unauthenticated_UsesAuthorizationFamily()
    {
        SecurityErrors.Unauthenticated(typeof(string)).GetCode().IfNone("")
            .ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
    }

    [Fact]
    public void SecurityErrors_MissingContext_IsUnauthenticated()
    {
        SecurityErrors.MissingContext(typeof(string)).GetCode().IfNone("")
            .ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
    }

    [Fact]
    public void SecurityErrors_InsufficientRoles_UsesAuthorizationFamily()
    {
        SecurityErrors.InsufficientRoles(typeof(string), ["Admin"], "u1").GetCode().IfNone("")
            .ShouldBe(EncinaErrorCodes.AuthorizationInsufficientRoles);
    }

    [Fact]
    public void SecurityErrors_PermissionDenied_UsesAuthorizationFamily()
    {
        SecurityErrors.PermissionDenied(typeof(string), ["read"], "u1").GetCode().IfNone("")
            .ShouldBe(EncinaErrorCodes.AuthorizationPermissionDenied);
    }

    [Fact]
    public void SecurityErrors_ClaimMissing_UsesAuthorizationFamily()
    {
        SecurityErrors.ClaimMissing(typeof(string), "dept", null, "u1").GetCode().IfNone("")
            .ShouldBe(EncinaErrorCodes.AuthorizationClaimMissing);
    }

    [Fact]
    public void SecurityErrors_NotOwner_UsesAuthorizationFamily()
    {
        SecurityErrors.NotOwner(typeof(string), "OwnerId", "u1").GetCode().IfNone("")
            .ShouldBe(EncinaErrorCodes.AuthorizationNotOwner);
    }
}
