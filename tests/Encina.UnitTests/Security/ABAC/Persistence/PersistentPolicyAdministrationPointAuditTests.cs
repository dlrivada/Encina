#pragma warning disable CA2012 // Use ValueTasks correctly — NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.Persistence;
using Encina.Security.Audit;

using Shouldly;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Security.ABAC.Persistence;

/// <summary>
/// Unit tests verifying that <see cref="PersistentPolicyAdministrationPoint"/> records
/// audit entries via <see cref="IAuditStore"/> for all mutation operations.
/// </summary>
public sealed class PersistentPolicyAdministrationPointAuditTests
{
    private readonly IPolicyStore _store;
    private readonly IAuditStore _auditStore;
    private readonly IRequestContext _requestContext;
    private readonly PersistentPolicyAdministrationPoint _sut;

    public PersistentPolicyAdministrationPointAuditTests()
    {
        _store = Substitute.For<IPolicyStore>();
        _auditStore = Substitute.For<IAuditStore>();
        _requestContext = Substitute.For<IRequestContext>();

        _requestContext.UserId.Returns("test-user");
        _requestContext.CorrelationId.Returns("corr-123");
        _requestContext.TenantId.Returns("tenant-abc");

        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, LanguageExt.Unit>>(
                Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Prelude.unit)));

        var logger = NullLoggerFactory.Instance.CreateLogger<PersistentPolicyAdministrationPoint>();
        _sut = new PersistentPolicyAdministrationPoint(_store, logger, _auditStore, _requestContext);
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

    /// <summary>
    /// Waits briefly for fire-and-forget audit tasks to complete.
    /// </summary>
    private static async Task WaitForAuditAsync()
    {
        await Task.Delay(50);
    }

    // ── AddPolicySetAsync ───────────────────────────────────────────

    [Fact]
    public async Task AddPolicySetAsync_Success_RecordsAuditEntry()
    {
        // Arrange
        var ps = CreatePolicySet("ps-audit");
        SetupStoreExistsPolicySet("ps-audit", false);
        SetupStoreSavePolicySetSuccess();

        // Act
        await _sut.AddPolicySetAsync(ps);
        await WaitForAuditAsync();

        // Assert
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "PolicySetCreated" &&
                e.EntityType == "PolicySet" &&
                e.EntityId == "ps-audit" &&
                e.UserId == "test-user" &&
                e.CorrelationId == "corr-123" &&
                e.TenantId == "tenant-abc" &&
                e.Outcome == AuditOutcome.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddPolicySetAsync_Failure_DoesNotRecordAudit()
    {
        // Arrange — duplicate
        var ps = CreatePolicySet("ps-dup");
        SetupStoreExistsPolicySet("ps-dup", true);

        // Act
        await _sut.AddPolicySetAsync(ps);
        await WaitForAuditAsync();

        // Assert
        await _auditStore.DidNotReceive().RecordAsync(
            Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    // ── UpdatePolicySetAsync ────────────────────────────────────────

    [Fact]
    public async Task UpdatePolicySetAsync_Success_RecordsAuditWithBeforeState()
    {
        // Arrange
        var existingPs = CreatePolicySet("ps-update") with { Description = "old" };
        var updatedPs = CreatePolicySet("ps-update") with { Description = "new" };
        SetupStoreExistsPolicySet("ps-update", true);
        SetupStoreGetPolicySet("ps-update", existingPs);
        SetupStoreSavePolicySetSuccess();

        // Act
        await _sut.UpdatePolicySetAsync(updatedPs);
        await WaitForAuditAsync();

        // Assert
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "PolicySetUpdated" &&
                e.EntityType == "PolicySet" &&
                e.EntityId == "ps-update" &&
                e.Metadata.ContainsKey("beforeState") &&
                e.Metadata.ContainsKey("afterState")),
            Arg.Any<CancellationToken>());
    }

    // ── RemovePolicySetAsync ────────────────────────────────────────

    [Fact]
    public async Task RemovePolicySetAsync_Success_RecordsAuditWithBeforeState()
    {
        // Arrange
        var ps = CreatePolicySet("ps-del");
        SetupStoreExistsPolicySet("ps-del", true);
        SetupStoreGetPolicySet("ps-del", ps);
        SetupStoreDeletePolicySetSuccess();

        // Act
        await _sut.RemovePolicySetAsync("ps-del");
        await WaitForAuditAsync();

        // Assert
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "PolicySetRemoved" &&
                e.EntityType == "PolicySet" &&
                e.EntityId == "ps-del" &&
                e.Metadata.ContainsKey("beforeState") &&
                !e.Metadata.ContainsKey("afterState")),
            Arg.Any<CancellationToken>());
    }

    // ── AddPolicyAsync — Standalone ─────────────────────────────────

    [Fact]
    public async Task AddPolicyAsync_Standalone_RecordsAuditEntry()
    {
        // Arrange
        var policy = CreatePolicy("p-audit");
        SetupStoreExistsPolicy("p-audit", false);
        SetupStoreGetAllPolicySets();
        SetupStoreSavePolicySuccess();

        // Act
        await _sut.AddPolicyAsync(policy, parentPolicySetId: null);
        await WaitForAuditAsync();

        // Assert
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "PolicyCreated" &&
                e.EntityType == "Policy" &&
                e.EntityId == "p-audit" &&
                e.UserId == "test-user"),
            Arg.Any<CancellationToken>());
    }

    // ── AddPolicyAsync — Nested ─────────────────────────────────────

    [Fact]
    public async Task AddPolicyAsync_Nested_RecordsAuditWithParentId()
    {
        // Arrange
        var policy = CreatePolicy("p-child");
        SetupStoreExistsPolicy("p-child", false);
        SetupStoreGetAllPolicySets();
        var parentPs = CreatePolicySet("ps-parent");
        SetupStoreGetPolicySet("ps-parent", parentPs);
        SetupStoreSavePolicySetSuccess();

        // Act
        await _sut.AddPolicyAsync(policy, parentPolicySetId: "ps-parent");
        await WaitForAuditAsync();

        // Assert
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "PolicyCreated" &&
                e.EntityType == "Policy" &&
                e.EntityId == "p-child" &&
                e.Metadata.ContainsKey("parentPolicySetId")),
            Arg.Any<CancellationToken>());
    }

    // ── UpdatePolicyAsync ───────────────────────────────────────────

    [Fact]
    public async Task UpdatePolicyAsync_Standalone_RecordsAuditWithBeforeState()
    {
        // Arrange
        var existingPolicy = CreatePolicy("p-update") with { Version = "1.0" };
        var updatedPolicy = CreatePolicy("p-update") with { Version = "2.0" };
        SetupStoreExistsPolicy("p-update", true);
        SetupStoreGetPolicy("p-update", existingPolicy);
        SetupStoreSavePolicySuccess();

        // Act
        await _sut.UpdatePolicyAsync(updatedPolicy);
        await WaitForAuditAsync();

        // Assert
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "PolicyUpdated" &&
                e.EntityType == "Policy" &&
                e.EntityId == "p-update" &&
                e.Metadata.ContainsKey("beforeState") &&
                e.Metadata.ContainsKey("afterState")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePolicyAsync_Nested_RecordsAuditWithParentId()
    {
        // Arrange
        var policy = CreatePolicy("p-nested") with { Version = "2.0" };
        SetupStoreExistsPolicy("p-nested", false);

        var originalPolicy = CreatePolicy("p-nested") with { Version = "1.0" };
        var ps = CreatePolicySet("ps-parent") with { Policies = [originalPolicy] };
        SetupStoreGetAllPolicySets(ps);
        SetupStoreSavePolicySetSuccess();

        // Act
        await _sut.UpdatePolicyAsync(policy);
        await WaitForAuditAsync();

        // Assert
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "PolicyUpdated" &&
                e.EntityType == "Policy" &&
                e.EntityId == "p-nested" &&
                e.Metadata.ContainsKey("parentPolicySetId") &&
                e.Metadata.ContainsKey("beforeState") &&
                e.Metadata.ContainsKey("afterState")),
            Arg.Any<CancellationToken>());
    }

    // ── RemovePolicyAsync ───────────────────────────────────────────

    [Fact]
    public async Task RemovePolicyAsync_Standalone_RecordsAuditWithBeforeState()
    {
        // Arrange
        var policy = CreatePolicy("p-del");
        SetupStoreExistsPolicy("p-del", true);
        SetupStoreGetPolicy("p-del", policy);
        SetupStoreDeletePolicySuccess();

        // Act
        await _sut.RemovePolicyAsync("p-del");
        await WaitForAuditAsync();

        // Assert
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "PolicyRemoved" &&
                e.EntityType == "Policy" &&
                e.EntityId == "p-del" &&
                e.Metadata.ContainsKey("beforeState") &&
                !e.Metadata.ContainsKey("afterState")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemovePolicyAsync_Nested_RecordsAuditWithParentId()
    {
        // Arrange
        SetupStoreExistsPolicy("p-nested-del", false);

        var nestedPolicy = CreatePolicy("p-nested-del");
        var ps = CreatePolicySet("ps-parent") with { Policies = [nestedPolicy] };
        SetupStoreGetAllPolicySets(ps);
        SetupStoreSavePolicySetSuccess();

        // Act
        await _sut.RemovePolicyAsync("p-nested-del");
        await WaitForAuditAsync();

        // Assert
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "PolicyRemoved" &&
                e.EntityType == "Policy" &&
                e.EntityId == "p-nested-del" &&
                e.Metadata.ContainsKey("parentPolicySetId") &&
                e.Metadata.ContainsKey("beforeState")),
            Arg.Any<CancellationToken>());
    }

    // ── Fire-and-Forget Behavior ────────────────────────────────────

    [Fact]
    public async Task AuditFailure_DoesNotBlockPolicyOperation()
    {
        // Arrange
        var ps = CreatePolicySet("ps-audit-fail");
        SetupStoreExistsPolicySet("ps-audit-fail", false);
        SetupStoreSavePolicySetSuccess();

        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, LanguageExt.Unit>>(
                Either<EncinaError, LanguageExt.Unit>.Left(EncinaError.New("audit store down"))));

        // Act
        var result = await _sut.AddPolicySetAsync(ps);

        // Assert — the policy operation still succeeds
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task AuditException_DoesNotBlockPolicyOperation()
    {
        // Arrange
        var ps = CreatePolicySet("ps-audit-ex");
        SetupStoreExistsPolicySet("ps-audit-ex", false);
        SetupStoreSavePolicySetSuccess();

        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, LanguageExt.Unit>>>(_ =>
                throw new InvalidOperationException("audit store crashed"));

        // Act
        var result = await _sut.AddPolicySetAsync(ps);

        // Assert — the policy operation still succeeds
        result.IsRight.ShouldBeTrue();
    }

    // ── No Audit Store ──────────────────────────────────────────────

    [Fact]
    public async Task NoAuditStore_PolicyOperationStillSucceeds()
    {
        // Arrange — SUT without audit store
        var store = Substitute.For<IPolicyStore>();
        var logger = NullLoggerFactory.Instance.CreateLogger<PersistentPolicyAdministrationPoint>();
        var sutNoAudit = new PersistentPolicyAdministrationPoint(store, logger);

        var ps = CreatePolicySet("ps-no-audit");
        store.ExistsPolicySetAsync("ps-no-audit", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(
                Either<EncinaError, bool>.Right(false)));
        store.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, LanguageExt.Unit>>(
                Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Prelude.unit)));

        // Act
        var result = await sutNoAudit.AddPolicySetAsync(ps);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // ── Actor Resolution ────────────────────────────────────────────

    [Fact]
    public async Task NoRequestContext_UsesSystemAsActor()
    {
        // Arrange — SUT with audit but no request context
        var store = Substitute.For<IPolicyStore>();
        var auditStore = Substitute.For<IAuditStore>();
        var logger = NullLoggerFactory.Instance.CreateLogger<PersistentPolicyAdministrationPoint>();
        var sutNoContext = new PersistentPolicyAdministrationPoint(store, logger, auditStore);

        auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, LanguageExt.Unit>>(
                Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Prelude.unit)));

        var ps = CreatePolicySet("ps-system");
        store.ExistsPolicySetAsync("ps-system", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(
                Either<EncinaError, bool>.Right(false)));
        store.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, LanguageExt.Unit>>(
                Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Prelude.unit)));

        // Act
        await sutNoContext.AddPolicySetAsync(ps);
        await WaitForAuditAsync();

        // Assert
        await auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e => e.UserId == "system"),
            Arg.Any<CancellationToken>());
    }
}
