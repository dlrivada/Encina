#pragma warning disable CA2012 // Use ValueTasks correctly — NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.ABAC.Health;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Health;

/// <summary>
/// The decision audit step of <see cref="ABACHealthCheck"/> (#751 Phase 5): no write probe, state read from
/// <see cref="ABACDecisionAuditHealthState"/>, the worse status wins, and no identifiers in the result.
/// </summary>
public sealed class ABACHealthCheckDecisionAuditTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private sealed class Rig
    {
        public required ABACHealthCheck Check { get; init; }
        public required ABACDecisionAuditHealthState State { get; init; }
        public required FakeTimeProvider Clock { get; init; }
        public required IOperationAuditStore? Store { get; init; }
    }

    private static Rig NewRig(
        bool auditEnabled = true,
        ABACDecisionAuditFailureMode mode = ABACDecisionAuditFailureMode.FailClosed,
        bool store = true,
        bool inMemoryStore = false,
        bool papLoaded = true)
    {
        var clock = new FakeTimeProvider(Now);
        var state = new ABACDecisionAuditHealthState(clock);
        var options = new ABACOptions();
        options.DecisionAudit.Enabled = auditEnabled;
        options.DecisionAudit.FailureMode = mode;

        IOperationAuditStore? auditStore = !store
            ? null
            : inMemoryStore ? new InMemoryOperationAuditStore() : Substitute.For<IOperationAuditStore>();

        var services = new ServiceCollection();
        services.AddSingleton<IOptions<ABACOptions>>(Microsoft.Extensions.Options.Options.Create(options));
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(state);
        if (auditStore is not null)
        {
            services.AddSingleton(auditStore);
        }

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>()).Returns(
            Either<EncinaError, IReadOnlyList<PolicySet>>.Right(
                papLoaded ? [new PolicySet
                {
                    Id = "ps-1",
                    Target = null,
                    Algorithm = CombiningAlgorithmId.DenyOverrides,
                    Policies = [],
                    PolicySets = [],
                    Obligations = [],
                    Advice = []
                }] : []));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>()).Returns(
            Either<EncinaError, IReadOnlyList<Policy>>.Right([]));

        return new Rig
        {
            Check = new ABACHealthCheck(pap, services.BuildServiceProvider()),
            State = state,
            Clock = clock,
            Store = auditStore
        };
    }

    private static Task<HealthCheckResult> RunAsync(Rig rig) =>
        rig.Check.CheckHealthAsync(new HealthCheckContext());

    [Fact]
    public async Task AuditDisabled_TheResultIsTheEngineResultAndNoStoreIsTouched()
    {
        var rig = NewRig(auditEnabled: false);
        rig.State.RecordWriteFailed();

        var result = await RunAsync(rig);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldBeEmpty();
        rig.Store!.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task AuditEnabledAndNoFailure_IsHealthyWithoutAWriteProbe()
    {
        var rig = NewRig();

        var result = await RunAsync(rig);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["decision_audit"].ShouldBe("ok");
        rig.Store!.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task FailClosedAfterAFailedWrite_IsUnhealthyUntilASuccessfulWriteClearsIt()
    {
        var rig = NewRig();
        rig.State.RecordWriteFailed();

        (await RunAsync(rig)).Status.ShouldBe(HealthStatus.Unhealthy);

        rig.State.RecordWriteSucceeded();

        (await RunAsync(rig)).Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task FailClosedWithAFailureOlderThanTheWindow_IsDegraded()
    {
        var rig = NewRig();
        rig.State.RecordWriteFailed();
        rig.Clock.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

        (await RunAsync(rig)).Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task FailClosedWithAFailureExactlyAtTheWindow_IsStillUnhealthy()
    {
        var rig = NewRig();
        rig.State.RecordWriteFailed();
        rig.Clock.Advance(TimeSpan.FromMinutes(5));

        (await RunAsync(rig)).Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task BestEffortAfterAFailedWrite_IsDegraded()
    {
        var rig = NewRig(mode: ABACDecisionAuditFailureMode.BestEffort);
        rig.State.RecordWriteFailed();

        (await RunAsync(rig)).Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task InMemoryStore_IsDegraded()
    {
        var rig = NewRig(inMemoryStore: true);

        var result = await RunAsync(rig);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Data["decision_audit"].ShouldBe("in_memory_store");
    }

    [Fact]
    public async Task NoStore_IsUnhealthy()
    {
        var rig = NewRig(store: false);

        var result = await RunAsync(rig);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Data["decision_audit"].ShouldBe("no_store");
    }

    [Fact]
    public async Task TheWorseStatusWins_AnEmptyEngineStaysDegradedWhenTheAuditIsHealthy()
    {
        var rig = NewRig(papLoaded: false);

        var result = await RunAsync(rig);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("No policies");
        result.Description!.ShouldContain("Decision audit is recording");
    }

    [Fact]
    public async Task TheResult_CarriesNoIdentifierOrErrorText()
    {
        var rig = NewRig();
        rig.State.RecordWriteFailed();

        var result = await RunAsync(rig);

        result.Exception.ShouldBeNull();
        result.Data.Keys.ShouldBe(["decision_audit"]);
        result.Description!.ShouldNotContain("tenant");
    }

    [Fact]
    public void State_StartsWithoutAFailureAndStampsTheFailureFromItsClock()
    {
        var clock = new FakeTimeProvider(Now);
        var state = new ABACDecisionAuditHealthState(clock);

        state.LastFailureAtUtc.ShouldBeNull();

        state.RecordWriteFailed();
        state.LastFailureAtUtc.ShouldBe(Now);

        clock.Advance(TimeSpan.FromMinutes(1));
        state.RecordWriteFailed();
        state.LastFailureAtUtc.ShouldBe(Now.AddMinutes(1));

        state.RecordWriteSucceeded();
        state.LastFailureAtUtc.ShouldBeNull();
    }

    [Fact]
    public void State_RejectsANullClock() =>
        Should.Throw<ArgumentNullException>(() => new ABACDecisionAuditHealthState(null!));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_RejectsANonPositiveHealthFailureWindow(int seconds)
    {
        var options = new ABACOptions();
        options.DecisionAudit.HealthFailureWindow = TimeSpan.FromSeconds(seconds);

        var result = new ABACOptionsValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("HealthFailureWindow");
    }

    [Fact]
    public void Options_HealthFailureWindowDefaultsToFiveMinutes() =>
        new ABACOptions().DecisionAudit.HealthFailureWindow.ShouldBe(TimeSpan.FromMinutes(5));
}
