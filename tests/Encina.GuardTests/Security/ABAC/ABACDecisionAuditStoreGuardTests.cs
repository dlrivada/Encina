using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard tests of the decision audit recorder, mapper and reader (#751 Phase 3): every public
/// constructor and method rejects <c>null</c> with the parameter's name, and a valid call runs.
/// </summary>
public sealed class ABACDecisionAuditStoreGuardTests
{
    private static readonly IServiceScopeFactory ScopeFactory =
        new ServiceCollection().AddSingleton<IOperationAuditStore, InMemoryOperationAuditStore>().BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();

    private static readonly IOptions<ABACOptions> AbacOptions = Options.Create(new ABACOptions());

    private static ABACDecisionRecord ValidRecord() => new()
    {
        DecisionId = Guid.CreateVersion7(),
        IdentityKind = IdentityKind.User,
        CorrelationId = "guard",
        RequestType = "GuardRequest",
        EnforcedOutcome = ABACEnforcedOutcome.Granted,
        ReasonCode = ABACDecisionAuditSchema.PermitReasonCode,
        EnforcementMode = ABACEnforcementMode.Block,
        StartedAtUtc = DateTimeOffset.UnixEpoch,
        CompletedAtUtc = DateTimeOffset.UnixEpoch
    };

    private static AuditStoreABACDecisionRecorder Recorder() =>
        new(ScopeFactory, AbacOptions, TimeProvider.System, NullLogger<AuditStoreABACDecisionRecorder>.Instance);

    private static IABACDecisionAuditReader Reader() =>
        new ServiceCollection()
            .AddLogging()
            .AddSingleton<IOperationAuditStore, InMemoryOperationAuditStore>()
            .AddEncinaABAC()
            .BuildServiceProvider()
            .CreateScope().ServiceProvider
            .GetRequiredService<IABACDecisionAuditReader>();

    // ── AuditStoreABACDecisionRecorder ───────────────────────────────

    [Fact]
    public void Recorder_NullScopeFactory_Throws() =>
        Should.Throw<ArgumentNullException>(() => new AuditStoreABACDecisionRecorder(null!, AbacOptions, TimeProvider.System, NullLogger<AuditStoreABACDecisionRecorder>.Instance))
            .ParamName.ShouldBe("scopeFactory");

    [Fact]
    public void Recorder_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new AuditStoreABACDecisionRecorder(ScopeFactory, null!, TimeProvider.System, NullLogger<AuditStoreABACDecisionRecorder>.Instance))
            .ParamName.ShouldBe("options");

    [Fact]
    public void Recorder_NullTimeProvider_Throws() =>
        Should.Throw<ArgumentNullException>(() => new AuditStoreABACDecisionRecorder(ScopeFactory, AbacOptions, null!, NullLogger<AuditStoreABACDecisionRecorder>.Instance))
            .ParamName.ShouldBe("timeProvider");

    [Fact]
    public void Recorder_NullLogger_Throws() =>
        Should.Throw<ArgumentNullException>(() => new AuditStoreABACDecisionRecorder(ScopeFactory, AbacOptions, TimeProvider.System, null!))
            .ParamName.ShouldBe("logger");

    [Fact]
    public async Task Recorder_RecordAsync_NullRecord_Throws()
    {
        var thrown = await Should.ThrowAsync<ArgumentNullException>(async () => await Recorder().RecordAsync(null!));

        thrown.ParamName.ShouldBe("record");
    }

    [Fact]
    public async Task Recorder_RecordAsync_ValidRecord_Writes()
    {
        (await Recorder().RecordAsync(ValidRecord())).IsRight.ShouldBeTrue();
    }

    // ── ABACDecisionAuditOptions ─────────────────────────────────────

    [Fact]
    public void Options_WriteTimeoutAndCrossTenantOptOut_AcceptAnyValueAndValidateAtStart()
    {
        var options = new ABACOptions();

        options.DecisionAudit.WriteTimeout = TimeSpan.FromSeconds(2);
        options.DecisionAudit.AllowCrossTenantQueries = true;

        options.DecisionAudit.WriteTimeout.ShouldBe(TimeSpan.FromSeconds(2));
        options.DecisionAudit.AllowCrossTenantQueries.ShouldBeTrue();
        new ABACOptionsValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void OptionsValidator_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACOptionsValidator().Validate(null, null!))
            .ParamName.ShouldBe("options");

    // ── ABACDecisionAuditEntryMapper ─────────────────────────────────

    [Fact]
    public void Mapper_ToOperationAuditEntry_NullRecord_Throws() =>
        Should.Throw<ArgumentNullException>(() => ABACDecisionAuditEntryMapper.ToOperationAuditEntry(null!))
            .ParamName.ShouldBe("record");

    [Fact]
    public void Mapper_ToOperationAuditEntry_ValidRecord_Maps() =>
        ABACDecisionAuditEntryMapper.ToOperationAuditEntry(ValidRecord()).Action.ShouldBe(ABACDecisionAuditSchema.Action);

    // ── IABACDecisionAuditReader ─────────────────────────────────────

    [Fact]
    public async Task Reader_QueryAsync_NullQuery_Throws()
    {
        var thrown = await Should.ThrowAsync<ArgumentNullException>(async () => await Reader().QueryAsync(null!));

        thrown.ParamName.ShouldBe("query");
    }

    [Fact]
    public async Task Reader_ExportAsync_NullQuery_Throws()
    {
        var thrown = await Should.ThrowAsync<ArgumentNullException>(async () => await Reader().ExportAsync(null!, Stream.Null));

        thrown.ParamName.ShouldBe("query");
    }

    [Fact]
    public async Task Reader_ExportAsync_NullDestination_Throws()
    {
        var thrown = await Should.ThrowAsync<ArgumentNullException>(async () => await Reader().ExportAsync(new ABACDecisionAuditQuery(), null!));

        thrown.ParamName.ShouldBe("destination");
    }

    [Fact]
    public async Task Reader_QueryAsync_ValidQuery_Runs()
    {
        (await Reader().QueryAsync(new ABACDecisionAuditQuery())).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Reader_ExportAsync_ValidArguments_Runs()
    {
        using var stream = new MemoryStream();

        (await Reader().ExportAsync(new ABACDecisionAuditQuery(), stream)).IsRight.ShouldBeTrue();
    }
}
