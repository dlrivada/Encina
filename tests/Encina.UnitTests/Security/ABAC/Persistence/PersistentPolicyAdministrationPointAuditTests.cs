#pragma warning disable CA2012 // Use ValueTasks correctly — NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.Persistence;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

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
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly FakeTimeProvider _time;
    private readonly PersistentPolicyAdministrationPoint _sut;

    public PersistentPolicyAdministrationPointAuditTests()
    {
        _store = Substitute.For<IPolicyStore>();
        _auditStore = Substitute.For<IAuditStore>();
        _requestContext = Substitute.For<IRequestContext>();
        _requestContextAccessor = Substitute.For<IRequestContextAccessor>();

        _requestContext.UserId.Returns("test-user");
        _requestContext.CorrelationId.Returns("corr-123");
        _requestContext.TenantId.Returns("tenant-abc");
        _requestContextAccessor.RequestContext.Returns(_requestContext);

        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, LanguageExt.Unit>>(
                Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Prelude.unit)));

        _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
        _sut = CreateSut(_store, _auditStore, _requestContextAccessor, _time);
    }

    private static PersistentPolicyAdministrationPoint CreateSut(
        IPolicyStore store,
        IAuditStore? auditStore,
        IRequestContextAccessor? accessor,
        TimeProvider? time = null)
    {
        var services = new ServiceCollection();
        if (auditStore is not null)
        {
            services.AddScoped(_ => auditStore);
        }

        var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var logger = NullLoggerFactory.Instance.CreateLogger<PersistentPolicyAdministrationPoint>();
        return new PersistentPolicyAdministrationPoint(
            store, logger, provider.GetRequiredService<IServiceScopeFactory>(), accessor, time);
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

    // ── Fail-closed behavior ────────────────────────────────────────

    [Fact]
    public async Task AuditFailure_FailsPolicyOperationAndPersistsNothing()
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

        // Assert — fail closed: nothing was persisted
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.PolicyChangeAuditFailedCode));
        await _store.DidNotReceive().SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuditException_FailsPolicyOperationAndPersistsNothing()
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

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.PolicyChangeAuditFailedCode));
        await _store.DidNotReceive().SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuditWriteTimeout_FailsPolicyOperationAndPersistsNothing()
    {
        // Arrange — an audit store that never completes until its token is cancelled
        var ps = CreatePolicySet("ps-audit-slow");
        SetupStoreExistsPolicySet("ps-audit-slow", false);
        SetupStoreSavePolicySetSuccess();
        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(call => HangUntilCancelledAsync(call.Arg<CancellationToken>()));

        // Act
        var pending = _sut.AddPolicySetAsync(ps).AsTask();
        await Task.Yield();
        _time.Advance(TimeSpan.FromSeconds(31));
        var result = await pending;

        // Assert
        result.IsLeft.ShouldBeTrue();
        await _store.DidNotReceive().SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CallerCancellation_PropagatesAsCancellationNotAuditFailure()
    {
        // Arrange
        var ps = CreatePolicySet("ps-cancel");
        SetupStoreExistsPolicySet("ps-cancel", false);
        using var cts = new CancellationTokenSource();
        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(call => HangUntilCancelledAsync(call.Arg<CancellationToken>()));

        // Act
        var pending = _sut.AddPolicySetAsync(ps, cts.Token).AsTask();
        await Task.Yield();
        await cts.CancelAsync();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(async () => await pending);
    }

    [Fact]
    public async Task StoreRejectsChangeAfterAudit_RecordsErrorEntry()
    {
        // Arrange
        var ps = CreatePolicySet("ps-store-fail");
        SetupStoreExistsPolicySet("ps-store-fail", false);
        _store.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, LanguageExt.Unit>>(
                Either<EncinaError, LanguageExt.Unit>.Left(EncinaErrors.Create("store.failed", "db down"))));

        // Act
        var result = await _sut.AddPolicySetAsync(ps);

        // Assert
        result.IsLeft.ShouldBeTrue();
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e => e.Outcome == AuditOutcome.Success), Arg.Any<CancellationToken>());
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e => e.Outcome == AuditOutcome.Error && e.ErrorMessage == "store.failed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Timestamps_ComeFromTheInjectedTimeProvider()
    {
        // Arrange
        var ps = CreatePolicySet("ps-time");
        SetupStoreExistsPolicySet("ps-time", false);
        SetupStoreSavePolicySetSuccess();

        // Act
        await _sut.AddPolicySetAsync(ps);

        // Assert
        var expected = _time.GetUtcNow();
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.StartedAtUtc == expected && e.CompletedAtUtc == expected && e.TimestampUtc == expected.UtcDateTime),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoreThrowsAfterAudit_RecordsErrorEntryAndRethrows()
    {
        // Arrange
        var ps = CreatePolicySet("ps-store-throws");
        SetupStoreExistsPolicySet("ps-store-throws", false);
        _store.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, LanguageExt.Unit>>>(_ => throw new InvalidOperationException("db crashed"));

        // Act and assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await _sut.AddPolicySetAsync(ps));
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e => e.Outcome == AuditOutcome.Error && e.ErrorMessage == nameof(InvalidOperationException)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuditStoreCannotBeResolved_FailsPolicyOperationClosed()
    {
        // Arrange — a scoped audit store whose construction throws
        var services = new ServiceCollection();
        services.AddScoped<IAuditStore>(_ => throw new InvalidOperationException("no connection"));
        var provider = services.BuildServiceProvider();
        var sut = new PersistentPolicyAdministrationPoint(
            _store,
            NullLoggerFactory.Instance.CreateLogger<PersistentPolicyAdministrationPoint>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            _requestContextAccessor);
        var ps = CreatePolicySet("ps-unresolvable");
        SetupStoreExistsPolicySet("ps-unresolvable", false);

        // Act
        var result = await sut.AddPolicySetAsync(ps);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.PolicyChangeAuditFailedCode));
        await _store.DidNotReceive().SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>());
    }

    private static async ValueTask<Either<EncinaError, LanguageExt.Unit>> HangUntilCancelledAsync(
        CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return LanguageExt.Prelude.unit;
    }

    // ── No Audit Store ──────────────────────────────────────────────

    [Fact]
    public async Task NoAuditStore_PolicyOperationStillSucceeds()
    {
        // Arrange — SUT without audit store
        var store = Substitute.For<IPolicyStore>();
        var sutNoAudit = CreateSut(store, auditStore: null, _requestContextAccessor);

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
    public async Task NoRequestContext_RefusesChangeWithPrincipalRequiredError()
    {
        // Arrange — SUT with audit but no request context
        var store = Substitute.For<IPolicyStore>();
        var auditStore = Substitute.For<IAuditStore>();
        var sutNoContext = CreateSut(store, auditStore, accessor: null);

        // Act
        var result = await sutNoContext.AddPolicySetAsync(CreatePolicySet("ps-refused"));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.PolicyChangePrincipalRequiredCode));
        await store.DidNotReceive().ExistsPolicySetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await auditStore.DidNotReceive().RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SystemActorScope_AllowsChangeAndRecordsSystemActor()
    {
        // Arrange — SUT with audit but no request context, inside the explicit system-actor scope
        var store = Substitute.For<IPolicyStore>();
        var auditStore = Substitute.For<IAuditStore>();
        var sutNoContext = CreateSut(store, auditStore, accessor: null);

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
        using (PolicyChangeActorScope.BeginSystemActor())
        {
            (await sutNoContext.AddPolicySetAsync(ps)).IsRight.ShouldBeTrue();
        }

        // Assert
        PolicyChangeActorScope.IsSystemActorActive.ShouldBeFalse();
        await auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e => e.UserId == "system" && (string?)e.Metadata["actor"] == "system"),
            Arg.Any<CancellationToken>());
    }
}
