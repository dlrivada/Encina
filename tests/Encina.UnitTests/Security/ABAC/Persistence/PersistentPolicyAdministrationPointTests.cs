#pragma warning disable CA2012 // Use ValueTasks correctly — NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.Persistence;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Security.ABAC.Persistence;

/// <summary>
/// Unit tests for <see cref="PersistentPolicyAdministrationPoint"/>: verifies
/// CRUD operations, duplicate detection, parent-child policy management, and
/// error propagation using a mocked <see cref="IPolicyStore"/>.
/// </summary>
public sealed class PersistentPolicyAdministrationPointTests
{
    private readonly IPolicyStore _store;
    private readonly PersistentPolicyAdministrationPoint _sut;

    public PersistentPolicyAdministrationPointTests()
    {
        _store = Substitute.For<IPolicyStore>();
        var logger = NullLoggerFactory.Instance.CreateLogger<PersistentPolicyAdministrationPoint>();
        _sut = new PersistentPolicyAdministrationPoint(_store, logger);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static PolicySet CreatePolicySet(string id = "ps-1") => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies = [],
        PolicySets = [],
        Obligations = [],
        Advice = []
    };

    private static Policy CreatePolicy(string id = "p-1") => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    private static EncinaError CreateError(string message = "test error") =>
        EncinaError.New(message);

    private void SetupStoreExistsPolicySet(string id, bool exists)
    {
        _store.ExistsPolicySetAsync(id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(
                Either<EncinaError, bool>.Right(exists)));
    }

    private void SetupStoreExistsPolicy(string id, bool exists)
    {
        _store.ExistsPolicyAsync(id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(
                Either<EncinaError, bool>.Right(exists)));
    }

    private void SetupStoreSavePolicySetSuccess()
    {
        _store.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, LanguageExt.Unit>>(
                Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Prelude.unit)));
    }

    private void SetupStoreSavePolicySuccess()
    {
        _store.SavePolicyAsync(Arg.Any<Policy>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, LanguageExt.Unit>>(
                Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Prelude.unit)));
    }

    private void SetupStoreDeletePolicySetSuccess()
    {
        _store.DeletePolicySetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, LanguageExt.Unit>>(
                Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Prelude.unit)));
    }

    private void SetupStoreDeletePolicySuccess()
    {
        _store.DeletePolicyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, LanguageExt.Unit>>(
                Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Prelude.unit)));
    }

    private void SetupStoreGetAllPolicySets(params PolicySet[] policySets)
    {
        _store.GetAllPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>>(
                Either<EncinaError, IReadOnlyList<PolicySet>>.Right(
                    (IReadOnlyList<PolicySet>)policySets.ToList())));
    }

    private void SetupStoreGetPolicySet(string id, PolicySet? policySet)
    {
        var option = policySet is not null
            ? LanguageExt.Prelude.Some(policySet)
            : Option<PolicySet>.None;

        _store.GetPolicySetAsync(id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<PolicySet>>>(
                Either<EncinaError, Option<PolicySet>>.Right(option)));
    }

    private void SetupStoreGetPolicy(string id, Policy? policy)
    {
        var option = policy is not null
            ? LanguageExt.Prelude.Some(policy)
            : Option<Policy>.None;

        _store.GetPolicyAsync(id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<Policy>>>(
                Either<EncinaError, Option<Policy>>.Right(option)));
    }

    // ── GetPolicySetsAsync ──────────────────────────────────────────

    #region GetPolicySetsAsync

    [Fact]
    public async Task GetPolicySetsAsync_DelegatesToStore()
    {
        // Arrange
        var ps = CreatePolicySet();
        SetupStoreGetAllPolicySets(ps);

        // Act
        var result = await _sut.GetPolicySetsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var sets = result.Match(Right: s => s, Left: _ => []);
        sets.Should().HaveCount(1);
        sets[0].Id.Should().Be("ps-1");
    }

    [Fact]
    public async Task GetPolicySetsAsync_StoreError_PropagatesLeft()
    {
        // Arrange
        _store.GetAllPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>>(
                Either<EncinaError, IReadOnlyList<PolicySet>>.Left(CreateError())));

        // Act
        var result = await _sut.GetPolicySetsAsync();

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    // ── GetPolicySetAsync ───────────────────────────────────────────

    #region GetPolicySetAsync

    [Fact]
    public async Task GetPolicySetAsync_Existing_ReturnsSome()
    {
        // Arrange
        var ps = CreatePolicySet("ps-found");
        SetupStoreGetPolicySet("ps-found", ps);

        // Act
        var result = await _sut.GetPolicySetAsync("ps-found");

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task GetPolicySetAsync_NotFound_ReturnsNone()
    {
        // Arrange
        SetupStoreGetPolicySet("ps-missing", null);

        // Act
        var result = await _sut.GetPolicySetAsync("ps-missing");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = result.Match(Right: o => o, Left: _ => Option<PolicySet>.None);
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public void GetPolicySetAsync_NullId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _sut.GetPolicySetAsync(null!);

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    // ── AddPolicySetAsync ───────────────────────────────────────────

    #region AddPolicySetAsync

    [Fact]
    public async Task AddPolicySetAsync_NewPolicySet_CallsSaveOnStore()
    {
        // Arrange
        var ps = CreatePolicySet("ps-new");
        SetupStoreExistsPolicySet("ps-new", false);
        SetupStoreSavePolicySetSuccess();

        // Act
        var result = await _sut.AddPolicySetAsync(ps);

        // Assert
        result.IsRight.Should().BeTrue();
        await _store.Received(1).SavePolicySetAsync(ps, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddPolicySetAsync_DuplicateId_ReturnsLeft()
    {
        // Arrange
        var ps = CreatePolicySet("ps-dup");
        SetupStoreExistsPolicySet("ps-dup", true);

        // Act
        var result = await _sut.AddPolicySetAsync(ps);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AddPolicySetAsync_ExistsCheckError_PropagatesLeft()
    {
        // Arrange
        var ps = CreatePolicySet("ps-err");
        _store.ExistsPolicySetAsync("ps-err", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(
                Either<EncinaError, bool>.Left(CreateError())));

        // Act
        var result = await _sut.AddPolicySetAsync(ps);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void AddPolicySetAsync_NullPolicySet_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.AddPolicySetAsync(null!);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    // ── UpdatePolicySetAsync ────────────────────────────────────────

    #region UpdatePolicySetAsync

    [Fact]
    public async Task UpdatePolicySetAsync_Existing_CallsSaveOnStore()
    {
        // Arrange
        var ps = CreatePolicySet("ps-update");
        SetupStoreExistsPolicySet("ps-update", true);
        SetupStoreSavePolicySetSuccess();

        // Act
        var result = await _sut.UpdatePolicySetAsync(ps);

        // Assert
        result.IsRight.Should().BeTrue();
        await _store.Received(1).SavePolicySetAsync(ps, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePolicySetAsync_NotFound_ReturnsLeft()
    {
        // Arrange
        var ps = CreatePolicySet("ps-missing");
        SetupStoreExistsPolicySet("ps-missing", false);

        // Act
        var result = await _sut.UpdatePolicySetAsync(ps);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void UpdatePolicySetAsync_NullPolicySet_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.UpdatePolicySetAsync(null!);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    // ── RemovePolicySetAsync ────────────────────────────────────────

    #region RemovePolicySetAsync

    [Fact]
    public async Task RemovePolicySetAsync_Existing_CallsDeleteOnStore()
    {
        // Arrange
        SetupStoreExistsPolicySet("ps-del", true);
        SetupStoreDeletePolicySetSuccess();

        // Act
        var result = await _sut.RemovePolicySetAsync("ps-del");

        // Assert
        result.IsRight.Should().BeTrue();
        await _store.Received(1).DeletePolicySetAsync("ps-del", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemovePolicySetAsync_NotFound_ReturnsLeft()
    {
        // Arrange
        SetupStoreExistsPolicySet("ps-missing", false);

        // Act
        var result = await _sut.RemovePolicySetAsync("ps-missing");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void RemovePolicySetAsync_NullId_ThrowsArgumentException()
    {
        var act = async () => await _sut.RemovePolicySetAsync(null!);

        act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    // ── GetPoliciesAsync ────────────────────────────────────────────

    #region GetPoliciesAsync

    [Fact]
    public async Task GetPoliciesAsync_NullPolicySetId_ReturnsStandalonePolicies()
    {
        // Arrange
        var policy = CreatePolicy("standalone-p");
        _store.GetAllStandalonePoliciesAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<Policy>>>(
                Either<EncinaError, IReadOnlyList<Policy>>.Right(
                    (IReadOnlyList<Policy>)[policy])));

        // Act
        var result = await _sut.GetPoliciesAsync(null);

        // Assert
        result.IsRight.Should().BeTrue();
        var policies = result.Match(Right: p => p, Left: _ => []);
        policies.Should().HaveCount(1);
        policies[0].Id.Should().Be("standalone-p");
    }

    [Fact]
    public async Task GetPoliciesAsync_WithPolicySetId_ReturnsPoliciesFromSet()
    {
        // Arrange
        var nestedPolicy = CreatePolicy("nested-p");
        var ps = CreatePolicySet("ps-parent") with
        {
            Policies = [nestedPolicy]
        };
        SetupStoreGetPolicySet("ps-parent", ps);

        // Act
        var result = await _sut.GetPoliciesAsync("ps-parent");

        // Assert
        result.IsRight.Should().BeTrue();
        var policies = result.Match(Right: p => p, Left: _ => []);
        policies.Should().HaveCount(1);
        policies[0].Id.Should().Be("nested-p");
    }

    [Fact]
    public async Task GetPoliciesAsync_NonExistentPolicySetId_ReturnsLeft()
    {
        // Arrange
        SetupStoreGetPolicySet("ps-nope", null);

        // Act
        var result = await _sut.GetPoliciesAsync("ps-nope");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    // ── GetPolicyAsync ──────────────────────────────────────────────

    #region GetPolicyAsync

    [Fact]
    public async Task GetPolicyAsync_StandalonePolicy_ReturnsSome()
    {
        // Arrange
        var policy = CreatePolicy("p-standalone");
        SetupStoreGetPolicy("p-standalone", policy);

        // Act
        var result = await _sut.GetPolicyAsync("p-standalone");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = result.Match(Right: o => o, Left: _ => Option<Policy>.None);
        option.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task GetPolicyAsync_NestedInPolicySet_ReturnsSome()
    {
        // Arrange — standalone not found
        SetupStoreGetPolicy("p-nested", null);

        // Arrange — found in a policy set
        var nestedPolicy = CreatePolicy("p-nested");
        var ps = CreatePolicySet("ps-parent") with { Policies = [nestedPolicy] };
        SetupStoreGetAllPolicySets(ps);

        // Act
        var result = await _sut.GetPolicyAsync("p-nested");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = result.Match(Right: o => o, Left: _ => Option<Policy>.None);
        option.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task GetPolicyAsync_NotFoundAnywhere_ReturnsNone()
    {
        // Arrange
        SetupStoreGetPolicy("p-ghost", null);
        SetupStoreGetAllPolicySets();

        // Act
        var result = await _sut.GetPolicyAsync("p-ghost");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = result.Match(Right: o => o, Left: _ => Option<Policy>.None);
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public void GetPolicyAsync_NullId_ThrowsArgumentException()
    {
        var act = async () => await _sut.GetPolicyAsync(null!);

        act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    // ── AddPolicyAsync — Standalone ─────────────────────────────────

    #region AddPolicyAsync — Standalone

    [Fact]
    public async Task AddPolicyAsync_Standalone_NewPolicy_CallsSaveOnStore()
    {
        // Arrange
        var policy = CreatePolicy("p-new");
        SetupStoreExistsPolicy("p-new", false);
        SetupStoreGetAllPolicySets(); // No nested duplicates
        SetupStoreSavePolicySuccess();

        // Act
        var result = await _sut.AddPolicyAsync(policy, parentPolicySetId: null);

        // Assert
        result.IsRight.Should().BeTrue();
        await _store.Received(1).SavePolicyAsync(policy, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddPolicyAsync_DuplicateStandalone_ReturnsLeft()
    {
        // Arrange
        var policy = CreatePolicy("p-dup");
        SetupStoreExistsPolicy("p-dup", true);

        // Act
        var result = await _sut.AddPolicyAsync(policy, parentPolicySetId: null);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AddPolicyAsync_DuplicateNested_ReturnsLeft()
    {
        // Arrange
        var policy = CreatePolicy("p-dup-nested");
        SetupStoreExistsPolicy("p-dup-nested", false);

        // Duplicate exists inside a policy set
        var existingNested = CreatePolicy("p-dup-nested");
        var ps = CreatePolicySet("ps-has-dup") with { Policies = [existingNested] };
        SetupStoreGetAllPolicySets(ps);

        // Act
        var result = await _sut.AddPolicyAsync(policy, parentPolicySetId: null);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void AddPolicyAsync_NullPolicy_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.AddPolicyAsync(null!, null);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    // ── AddPolicyAsync — Nested (with parent) ───────────────────────

    #region AddPolicyAsync — Nested

    [Fact]
    public async Task AddPolicyAsync_WithParent_AddsToParentPolicySet()
    {
        // Arrange
        var policy = CreatePolicy("p-child");
        SetupStoreExistsPolicy("p-child", false);
        SetupStoreGetAllPolicySets(); // No duplicates in sets

        var parentPs = CreatePolicySet("ps-parent");
        SetupStoreGetPolicySet("ps-parent", parentPs);
        SetupStoreSavePolicySetSuccess();

        // Act
        var result = await _sut.AddPolicyAsync(policy, parentPolicySetId: "ps-parent");

        // Assert
        result.IsRight.Should().BeTrue();
        await _store.Received(1).SavePolicySetAsync(
            Arg.Is<PolicySet>(ps =>
                ps.Id == "ps-parent" &&
                ps.Policies.Count == 1 &&
                ps.Policies[0].Id == "p-child"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddPolicyAsync_WithParent_ParentNotFound_ReturnsLeft()
    {
        // Arrange
        var policy = CreatePolicy("p-orphan");
        SetupStoreExistsPolicy("p-orphan", false);
        SetupStoreGetAllPolicySets(); // No duplicates
        SetupStoreGetPolicySet("ps-missing", null);

        // Act
        var result = await _sut.AddPolicyAsync(policy, parentPolicySetId: "ps-missing");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    // ── UpdatePolicyAsync ───────────────────────────────────────────

    #region UpdatePolicyAsync

    [Fact]
    public async Task UpdatePolicyAsync_StandaloneExists_CallsSavePolicyOnStore()
    {
        // Arrange
        var policy = CreatePolicy("p-update");
        SetupStoreExistsPolicy("p-update", true);
        SetupStoreSavePolicySuccess();

        // Act
        var result = await _sut.UpdatePolicyAsync(policy);

        // Assert
        result.IsRight.Should().BeTrue();
        await _store.Received(1).SavePolicyAsync(policy, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePolicyAsync_NestedInPolicySet_UpdatesParentPolicySet()
    {
        // Arrange
        var policy = CreatePolicy("p-nested-update") with { Version = "2.0" };
        SetupStoreExistsPolicy("p-nested-update", false);

        var originalPolicy = CreatePolicy("p-nested-update") with { Version = "1.0" };
        var ps = CreatePolicySet("ps-parent") with { Policies = [originalPolicy] };
        SetupStoreGetAllPolicySets(ps);
        SetupStoreSavePolicySetSuccess();

        // Act
        var result = await _sut.UpdatePolicyAsync(policy);

        // Assert
        result.IsRight.Should().BeTrue();
        await _store.Received(1).SavePolicySetAsync(
            Arg.Is<PolicySet>(saved =>
                saved.Id == "ps-parent" &&
                saved.Policies[0].Version == "2.0"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePolicyAsync_NotFound_ReturnsLeft()
    {
        // Arrange
        var policy = CreatePolicy("p-ghost");
        SetupStoreExistsPolicy("p-ghost", false);
        SetupStoreGetAllPolicySets();

        // Act
        var result = await _sut.UpdatePolicyAsync(policy);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void UpdatePolicyAsync_NullPolicy_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.UpdatePolicyAsync(null!);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    // ── RemovePolicyAsync ───────────────────────────────────────────

    #region RemovePolicyAsync

    [Fact]
    public async Task RemovePolicyAsync_StandaloneExists_CallsDeleteOnStore()
    {
        // Arrange
        SetupStoreExistsPolicy("p-del", true);
        SetupStoreDeletePolicySuccess();

        // Act
        var result = await _sut.RemovePolicyAsync("p-del");

        // Assert
        result.IsRight.Should().BeTrue();
        await _store.Received(1).DeletePolicyAsync("p-del", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemovePolicyAsync_NestedInPolicySet_RemovesFromParent()
    {
        // Arrange
        SetupStoreExistsPolicy("p-nested-del", false);

        var nestedPolicy = CreatePolicy("p-nested-del");
        var otherPolicy = CreatePolicy("p-keep");
        var ps = CreatePolicySet("ps-parent") with { Policies = [nestedPolicy, otherPolicy] };
        SetupStoreGetAllPolicySets(ps);
        SetupStoreSavePolicySetSuccess();

        // Act
        var result = await _sut.RemovePolicyAsync("p-nested-del");

        // Assert
        result.IsRight.Should().BeTrue();
        await _store.Received(1).SavePolicySetAsync(
            Arg.Is<PolicySet>(saved =>
                saved.Id == "ps-parent" &&
                saved.Policies.Count == 1 &&
                saved.Policies[0].Id == "p-keep"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemovePolicyAsync_NotFound_ReturnsLeft()
    {
        // Arrange
        SetupStoreExistsPolicy("p-ghost", false);
        SetupStoreGetAllPolicySets();

        // Act
        var result = await _sut.RemovePolicyAsync("p-ghost");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void RemovePolicyAsync_NullId_ThrowsArgumentException()
    {
        var act = async () => await _sut.RemovePolicyAsync(null!);

        act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    // ── Constructor Guard Clauses ────────────────────────────────────

    #region Constructor Guard Clauses

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new PersistentPolicyAdministrationPoint(
            null!,
            NullLoggerFactory.Instance.CreateLogger<PersistentPolicyAdministrationPoint>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var store = Substitute.For<IPolicyStore>();
        var act = () => new PersistentPolicyAdministrationPoint(store, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion
}
