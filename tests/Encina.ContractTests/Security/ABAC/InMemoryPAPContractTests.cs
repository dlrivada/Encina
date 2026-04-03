using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Security.ABAC;

/// <summary>
/// Behavioral contract tests for <see cref="InMemoryPolicyAdministrationPoint"/>.
/// Executes real CRUD operations to verify the PAP contract: add/get/update/remove
/// for both policy sets and standalone policies, duplicate detection, and
/// parent-child policy management.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "ABAC")]
public sealed class InMemoryPAPContractTests
{
    private readonly InMemoryPolicyAdministrationPoint _pap = new(
        NullLogger<InMemoryPolicyAdministrationPoint>.Instance);

    // ── Helpers ──────────────────────────────────────────────────────

    private static PolicySet MakePolicySet(string id) => new()
    {
        Id = id,
        IsEnabled = true,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Target = null,
        Policies = [],
        PolicySets = [],
        Obligations = [],
        Advice = []
    };

    private static Policy MakePolicy(string id) => new()
    {
        Id = id,
        IsEnabled = true,
        Algorithm = CombiningAlgorithmId.PermitOverrides,
        Target = null,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    // ── PolicySet CRUD Contracts ────────────────────────────────────

    [Fact]
    public async Task AddPolicySet_ThenGetAll_ShouldReturnAddedPolicySet()
    {
        // Arrange
        var ps = MakePolicySet("ps-crud-1");

        // Act
        var addResult = await _pap.AddPolicySetAsync(ps);
        var getResult = await _pap.GetPolicySetsAsync();

        // Assert
        addResult.IsRight.ShouldBeTrue("Add should succeed for a new policy set");

        var sets = getResult.Match(Right: v => v, Left: _ => []);
        sets.ShouldContain(s => s.Id == "ps-crud-1",
            "GetPolicySetsAsync must return the added policy set");
    }

    [Fact]
    public async Task AddPolicySet_Duplicate_ShouldReturnError()
    {
        // Arrange
        var ps = MakePolicySet("ps-dup");
        await _pap.AddPolicySetAsync(ps);

        // Act
        var result = await _pap.AddPolicySetAsync(ps);

        // Assert
        result.IsLeft.ShouldBeTrue(
            "Adding a duplicate policy set must return Left(EncinaError)");
    }

    [Fact]
    public async Task GetPolicySet_Existing_ShouldReturn_Some()
    {
        // Arrange
        var ps = MakePolicySet("ps-get-1");
        await _pap.AddPolicySetAsync(ps);

        // Act
        var result = await _pap.GetPolicySetAsync("ps-get-1");

        // Assert
        var option = result.Match(Right: v => v, Left: _ => Option<PolicySet>.None);
        option.IsSome.ShouldBeTrue("Existing policy set must return Some");
    }

    [Fact]
    public async Task GetPolicySet_NonExistent_ShouldReturn_None()
    {
        // Act
        var result = await _pap.GetPolicySetAsync("non-existent-ps");

        // Assert
        var option = result.Match(Right: v => v, Left: _ => Option<PolicySet>.None);
        option.IsNone.ShouldBeTrue(
            "Non-existent policy set must return Right(None), not Left(error)");
    }

    [Fact]
    public async Task RemovePolicySet_Existing_ShouldSucceed()
    {
        // Arrange
        var ps = MakePolicySet("ps-remove-1");
        await _pap.AddPolicySetAsync(ps);

        // Act
        var removeResult = await _pap.RemovePolicySetAsync("ps-remove-1");
        var getResult = await _pap.GetPolicySetAsync("ps-remove-1");

        // Assert
        removeResult.IsRight.ShouldBeTrue("Removing existing policy set must succeed");

        var option = getResult.Match(Right: v => v, Left: _ => Option<PolicySet>.None);
        option.IsNone.ShouldBeTrue("Removed policy set must no longer be retrievable");
    }

    // ── Standalone Policy CRUD Contracts ─────────────────────────────

    [Fact]
    public async Task AddStandalonePolicy_ThenGetPolicies_ShouldReturnIt()
    {
        // Arrange
        var policy = MakePolicy("standalone-p1");

        // Act
        var addResult = await _pap.AddPolicyAsync(policy, parentPolicySetId: null);
        var getResult = await _pap.GetPoliciesAsync(policySetId: null);

        // Assert
        addResult.IsRight.ShouldBeTrue("Add standalone policy should succeed");

        var policies = getResult.Match(Right: v => v, Left: _ => []);
        policies.ShouldContain(p => p.Id == "standalone-p1",
            "Standalone policies must appear when queried with null policySetId");
    }

    [Fact]
    public async Task AddPolicy_DuplicateId_ShouldReturnError()
    {
        // Arrange
        var policy = MakePolicy("dup-policy");
        await _pap.AddPolicyAsync(policy, parentPolicySetId: null);

        // Act
        var result = await _pap.AddPolicyAsync(policy, parentPolicySetId: null);

        // Assert
        result.IsLeft.ShouldBeTrue(
            "Adding a policy with duplicate ID must return Left(EncinaError)");
    }

    [Fact]
    public async Task AddPolicy_ToParentPolicySet_ShouldNestCorrectly()
    {
        // Arrange
        var ps = MakePolicySet("parent-ps");
        await _pap.AddPolicySetAsync(ps);
        var policy = MakePolicy("nested-p1");

        // Act
        var addResult = await _pap.AddPolicyAsync(policy, parentPolicySetId: "parent-ps");
        var getResult = await _pap.GetPoliciesAsync(policySetId: "parent-ps");

        // Assert
        addResult.IsRight.ShouldBeTrue("Adding policy to existing parent should succeed");

        var policies = getResult.Match(Right: v => v, Left: _ => []);
        policies.ShouldContain(p => p.Id == "nested-p1",
            "Nested policy must appear when querying by parent policy set ID");
    }

    [Fact]
    public async Task AddPolicy_ToNonExistentParent_ShouldReturnError()
    {
        // Arrange
        var policy = MakePolicy("orphan-policy");

        // Act
        var result = await _pap.AddPolicyAsync(policy, parentPolicySetId: "ghost-ps");

        // Assert
        result.IsLeft.ShouldBeTrue(
            "Adding policy to non-existent parent must return Left(EncinaError)");
    }
}
