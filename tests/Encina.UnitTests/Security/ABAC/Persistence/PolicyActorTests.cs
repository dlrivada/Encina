using Encina.Security.ABAC.Administration;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Persistence;

/// <summary>
/// The actor of a persistent PAP change never prints its user id, tenant or correlation id
/// (#1705 Phase 4, PR #1982 review F2; AGENTS.md: no user id in logs or diagnostics).
/// </summary>
public sealed class PolicyActorTests
{
    [Theory]
    [InlineData(IdentityKind.User, "SENTINEL-user-1982")]
    [InlineData(IdentityKind.Service, "service:SENTINEL-job-1982")]
    public void ToString_PrintsTheKindOnly(IdentityKind kind, string userId)
    {
        var actor = new PersistentPolicyAdministrationPoint.PolicyActor(userId, "SENTINEL-tenant", "SENTINEL-corr", kind);

        var text = actor.ToString();

        text.ShouldBe($"PolicyActor {{ Kind = {kind} }}");
        text.ShouldNotContain("SENTINEL");
        $"{actor}".ShouldNotContain(userId);
    }
}
