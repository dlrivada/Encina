#pragma warning disable CA2012 // Use ValueTasks correctly -- async test setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;

using Shouldly;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Security.ABAC.Administration;

/// <summary>
/// Guard clause tests for <see cref="InMemoryPolicyAdministrationPoint"/>.
/// Covers constructor guards and all CRUD method null/whitespace parameter validation.
/// </summary>
public class InMemoryPolicyAdminGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryPolicyAdministrationPoint(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidLogger_DoesNotThrow()
    {
        var act = () => CreatePAP();

        Should.NotThrow(act);
    }

    #endregion

    #region GetPolicySetAsync — Parameter Guards

    [Fact]
    public void GetPolicySetAsync_NullId_ThrowsArgumentException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.GetPolicySetAsync(null!);

        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void GetPolicySetAsync_EmptyId_ThrowsArgumentException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.GetPolicySetAsync("");

        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void GetPolicySetAsync_WhitespaceId_ThrowsArgumentException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.GetPolicySetAsync("   ");

        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetPolicySetAsync_NonExistentId_ReturnsNone()
    {
        var sut = CreatePAP();

        var result = await sut.GetPolicySetAsync("non-existent");

        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Left: _ => throw new InvalidOperationException(),
            Right: opt => { opt.IsNone.ShouldBeTrue(); return 0; });
    }

    #endregion

    #region AddPolicySetAsync — Parameter Guards

    [Fact]
    public void AddPolicySetAsync_NullPolicySet_ThrowsArgumentNullException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.AddPolicySetAsync(null!);

        Should.ThrowAsync<ArgumentNullException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AddPolicySetAsync_DuplicateId_ReturnsError()
    {
        var sut = CreatePAP();
        var ps = CreateMinimalPolicySet("dup-ps");

        await sut.AddPolicySetAsync(ps);
        var result = await sut.AddPolicySetAsync(ps);

        result.IsLeft.ShouldBeTrue("duplicate policy set ID should be rejected");
    }

    [Fact]
    public async Task AddPolicySetAsync_ValidPolicySet_Succeeds()
    {
        var sut = CreatePAP();
        var ps = CreateMinimalPolicySet("valid-ps");

        var result = await sut.AddPolicySetAsync(ps);

        result.IsRight.ShouldBeTrue();
        sut.PolicySetCount.ShouldBe(1);
    }

    #endregion

    #region UpdatePolicySetAsync — Parameter Guards

    [Fact]
    public void UpdatePolicySetAsync_NullPolicySet_ThrowsArgumentNullException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.UpdatePolicySetAsync(null!);

        Should.ThrowAsync<ArgumentNullException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task UpdatePolicySetAsync_NonExistentPolicySet_ReturnsError()
    {
        var sut = CreatePAP();
        var ps = CreateMinimalPolicySet("non-existent");

        var result = await sut.UpdatePolicySetAsync(ps);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdatePolicySetAsync_ExistingPolicySet_Succeeds()
    {
        var sut = CreatePAP();
        var ps = CreateMinimalPolicySet("update-ps");
        await sut.AddPolicySetAsync(ps);

        var updated = ps with { Description = "Updated" };
        var result = await sut.UpdatePolicySetAsync(updated);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region RemovePolicySetAsync — Parameter Guards

    [Fact]
    public void RemovePolicySetAsync_NullId_ThrowsArgumentException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.RemovePolicySetAsync(null!);

        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void RemovePolicySetAsync_EmptyId_ThrowsArgumentException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.RemovePolicySetAsync("");

        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void RemovePolicySetAsync_WhitespaceId_ThrowsArgumentException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.RemovePolicySetAsync("   ");

        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task RemovePolicySetAsync_NonExistentId_ReturnsError()
    {
        var sut = CreatePAP();

        var result = await sut.RemovePolicySetAsync("non-existent");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RemovePolicySetAsync_ExistingId_Succeeds()
    {
        var sut = CreatePAP();
        await sut.AddPolicySetAsync(CreateMinimalPolicySet("rm-ps"));

        var result = await sut.RemovePolicySetAsync("rm-ps");

        result.IsRight.ShouldBeTrue();
        sut.PolicySetCount.ShouldBe(0);
    }

    #endregion

    #region GetPolicyAsync — Parameter Guards

    [Fact]
    public void GetPolicyAsync_NullId_ThrowsArgumentException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.GetPolicyAsync(null!);

        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void GetPolicyAsync_EmptyId_ThrowsArgumentException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.GetPolicyAsync("");

        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void GetPolicyAsync_WhitespaceId_ThrowsArgumentException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.GetPolicyAsync("  ");

        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetPolicyAsync_NonExistentId_ReturnsNone()
    {
        var sut = CreatePAP();

        var result = await sut.GetPolicyAsync("non-existent");

        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Left: _ => throw new InvalidOperationException(),
            Right: opt => { opt.IsNone.ShouldBeTrue(); return 0; });
    }

    #endregion

    #region AddPolicyAsync — Parameter Guards

    [Fact]
    public void AddPolicyAsync_NullPolicy_ThrowsArgumentNullException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.AddPolicyAsync(null!, null);

        Should.ThrowAsync<ArgumentNullException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AddPolicyAsync_DuplicateId_ReturnsError()
    {
        var sut = CreatePAP();
        var policy = CreateMinimalPolicy("dup-pol");

        await sut.AddPolicyAsync(policy, null);
        var result = await sut.AddPolicyAsync(policy, null);

        result.IsLeft.ShouldBeTrue("duplicate policy ID should be rejected");
    }

    [Fact]
    public async Task AddPolicyAsync_Standalone_Succeeds()
    {
        var sut = CreatePAP();
        var policy = CreateMinimalPolicy("standalone-pol");

        var result = await sut.AddPolicyAsync(policy, null);

        result.IsRight.ShouldBeTrue();
        sut.StandalonePolicyCount.ShouldBe(1);
    }

    [Fact]
    public async Task AddPolicyAsync_ToNonExistentPolicySet_ReturnsError()
    {
        var sut = CreatePAP();
        var policy = CreateMinimalPolicy("orphan-pol");

        var result = await sut.AddPolicyAsync(policy, "non-existent-ps");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task AddPolicyAsync_ToPolicySet_Succeeds()
    {
        var sut = CreatePAP();
        await sut.AddPolicySetAsync(CreateMinimalPolicySet("parent-ps"));
        var policy = CreateMinimalPolicy("child-pol");

        var result = await sut.AddPolicyAsync(policy, "parent-ps");

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region UpdatePolicyAsync — Parameter Guards

    [Fact]
    public void UpdatePolicyAsync_NullPolicy_ThrowsArgumentNullException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.UpdatePolicyAsync(null!);

        Should.ThrowAsync<ArgumentNullException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task UpdatePolicyAsync_NonExistentPolicy_ReturnsError()
    {
        var sut = CreatePAP();
        var policy = CreateMinimalPolicy("ghost");

        var result = await sut.UpdatePolicyAsync(policy);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdatePolicyAsync_ExistingStandalonePolicy_Succeeds()
    {
        var sut = CreatePAP();
        var policy = CreateMinimalPolicy("update-pol");
        await sut.AddPolicyAsync(policy, null);

        var updated = policy with { Description = "Updated" };
        var result = await sut.UpdatePolicyAsync(updated);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdatePolicyAsync_ExistingNestedPolicy_Succeeds()
    {
        var sut = CreatePAP();
        await sut.AddPolicySetAsync(CreateMinimalPolicySet("ps-for-update"));
        var policy = CreateMinimalPolicy("nested-pol");
        await sut.AddPolicyAsync(policy, "ps-for-update");

        var updated = policy with { Description = "Updated nested" };
        var result = await sut.UpdatePolicyAsync(updated);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region RemovePolicyAsync — Parameter Guards

    [Fact]
    public void RemovePolicyAsync_NullId_ThrowsArgumentException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.RemovePolicyAsync(null!);

        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void RemovePolicyAsync_EmptyId_ThrowsArgumentException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.RemovePolicyAsync("");

        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void RemovePolicyAsync_WhitespaceId_ThrowsArgumentException()
    {
        var sut = CreatePAP();

        var act = async () => await sut.RemovePolicyAsync("   ");

        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task RemovePolicyAsync_NonExistentId_ReturnsError()
    {
        var sut = CreatePAP();

        var result = await sut.RemovePolicyAsync("non-existent");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RemovePolicyAsync_ExistingStandalone_Succeeds()
    {
        var sut = CreatePAP();
        await sut.AddPolicyAsync(CreateMinimalPolicy("rm-pol"), null);

        var result = await sut.RemovePolicyAsync("rm-pol");

        result.IsRight.ShouldBeTrue();
        sut.StandalonePolicyCount.ShouldBe(0);
    }

    [Fact]
    public async Task RemovePolicyAsync_ExistingNested_Succeeds()
    {
        var sut = CreatePAP();
        await sut.AddPolicySetAsync(CreateMinimalPolicySet("ps-for-rm"));
        await sut.AddPolicyAsync(CreateMinimalPolicy("nested-rm"), "ps-for-rm");

        var result = await sut.RemovePolicyAsync("nested-rm");

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region GetPoliciesAsync — Null vs Non-Null PolicySetId

    [Fact]
    public async Task GetPoliciesAsync_NullPolicySetId_ReturnsStandalonePolicies()
    {
        var sut = CreatePAP();
        await sut.AddPolicyAsync(CreateMinimalPolicy("s1"), null);

        var result = await sut.GetPoliciesAsync(null);

        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Left: _ => throw new InvalidOperationException(),
            Right: policies => { policies.Count.ShouldBe(1); return 0; });
    }

    [Fact]
    public async Task GetPoliciesAsync_NonExistentPolicySetId_ReturnsError()
    {
        var sut = CreatePAP();

        var result = await sut.GetPoliciesAsync("non-existent");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Clear — Resets State

    [Fact]
    public async Task Clear_RemovesAllData()
    {
        var sut = CreatePAP();
        await sut.AddPolicySetAsync(CreateMinimalPolicySet("ps-1"));
        await sut.AddPolicyAsync(CreateMinimalPolicy("pol-1"), null);

        sut.Clear();

        sut.PolicySetCount.ShouldBe(0);
        sut.StandalonePolicyCount.ShouldBe(0);
    }

    #endregion

    // ── Helpers ──────────────────────────────────────────────────────

    private static InMemoryPolicyAdministrationPoint CreatePAP() =>
        new(NullLoggerFactory.Instance.CreateLogger<InMemoryPolicyAdministrationPoint>());

    private static PolicySet CreateMinimalPolicySet(string id) => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies = [],
        PolicySets = [],
        Obligations = [],
        Advice = []
    };

    private static Policy CreateMinimalPolicy(string id) => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };
}
