#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Encina.ContractTests.Security.ABAC;

/// <summary>
/// Contract of the registered <see cref="IABACDecisionRecorder"/> over the in-memory operation audit
/// store (#751 Phase 3): one record gives exactly one entry with the decision id, a missing store is
/// a failure (never a silent success), and what the recorder writes the reader reads back.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "ABAC")]
public sealed class AuditStoreABACDecisionRecorderContractTests
{
    private static ABACDecisionRecord Record(string correlationId) => new()
    {
        DecisionId = Guid.CreateVersion7(),
        UserId = "user-1",
        IdentityKind = IdentityKind.User,
        CorrelationId = correlationId,
        RequestType = "GetOrderQuery",
        ResourceId = "order-1",
        EnforcedOutcome = ABACEnforcedOutcome.Denied,
        ReasonCode = ABACErrors.AccessDeniedCode,
        EnforcementMode = ABACEnforcementMode.Block,
        StartedAtUtc = DateTimeOffset.UnixEpoch,
        CompletedAtUtc = DateTimeOffset.UnixEpoch.AddMilliseconds(5)
    };

    private static ServiceProvider Provider(InMemoryOperationAuditStore? store)
    {
        var services = new ServiceCollection().AddLogging();
        if (store is not null)
        {
            services.AddScoped<IOperationAuditStore>(_ => store);
        }

        return services.AddEncinaABAC().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public async Task EachRecord_IsWrittenAsExactlyOneEntryWithTheDecisionId()
    {
        var store = new InMemoryOperationAuditStore();
        await using var provider = Provider(store);
        var recorder = provider.GetRequiredService<IABACDecisionRecorder>();
        var records = Enumerable.Range(0, 3).Select(i => Record($"corr-{i}")).ToList();

        foreach (var record in records)
        {
            (await recorder.RecordAsync(record)).IsRight.ShouldBeTrue();
        }

        store.GetAllEntries().Select(entry => entry.Id).Order().ShouldBe(records.Select(record => record.DecisionId).Order());
        store.GetAllEntries().ShouldAllBe(entry => entry.Action == ABACDecisionAuditSchema.Action);
    }

    [Fact]
    public async Task WithoutAStore_TheWriteFails_SoTheEnforcementPointCanFailClosed()
    {
        await using var provider = Provider(store: null);

        var result = await provider.GetRequiredService<IABACDecisionRecorder>().RecordAsync(Record("corr"));

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.DecisionAuditStoreUnavailableCode));
    }

    private static ServiceProvider ProviderWithStore(IOperationAuditStore store) =>
        new ServiceCollection().AddLogging().AddScoped(_ => store).AddEncinaABAC()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    private static IOperationAuditStore FailingStore(
        Func<OperationAuditEntry, Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> lookup, string secret)
    {
        OperationAuditEntry? written = null;
        var store = Substitute.For<IOperationAuditStore>();
        store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                written = call.Arg<OperationAuditEntry>();
                return ValueTask.FromResult(Prelude.Left<EncinaError, Unit>(EncinaErrors.Create("store.down", secret)));
            });
        store.GetByCorrelationIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult(lookup(written!)));
        return store;
    }

    [Fact]
    public async Task AStoreFailureAfterTheEntryWasCommitted_CountsAsWritten()
    {
        var store = FailingStore(
            entry => Prelude.Right<EncinaError, IReadOnlyList<OperationAuditEntry>>([entry]), "secret detail");
        await using var provider = ProviderWithStore(store);

        var result = await provider.GetRequiredService<IABACDecisionRecorder>().RecordAsync(Record("corr-committed"));

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task AStoreFailureWithoutTheEntry_FailsWithTheStoreCodeAndNeverItsMessage()
    {
        var store = FailingStore(_ => Prelude.Right<EncinaError, IReadOnlyList<OperationAuditEntry>>([]), "secret detail");
        await using var provider = ProviderWithStore(store);

        var result = await provider.GetRequiredService<IABACDecisionRecorder>().RecordAsync(Record("corr-absent"));

        var error = result.Match(Right: _ => throw new ShouldAssertException("Right"), Left: e => e);
        error.GetCode().IfNone(string.Empty).ShouldBe("store.down");
        error.Message.ShouldNotContain("secret");
    }

    [Fact]
    public async Task AStoreFailureWhoseLookupAlsoFails_FailsWithTheOriginalStoreCode()
    {
        var store = FailingStore(
            _ => Prelude.Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(EncinaErrors.Create("lookup.down", "lookup secret")),
            "secret detail");
        await using var provider = ProviderWithStore(store);

        var result = await provider.GetRequiredService<IABACDecisionRecorder>().RecordAsync(Record("corr-lookup"));

        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty)).ShouldBe("store.down");
    }

    [Fact]
    public async Task AStoreThatThrowsWithoutTheEntry_PropagatesTheException()
    {
        var store = Substitute.For<IOperationAuditStore>();
        store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException("boom"));
        store.GetByCorrelationIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Prelude.Right<EncinaError, IReadOnlyList<OperationAuditEntry>>([])));
        await using var provider = ProviderWithStore(store);

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await provider.GetRequiredService<IABACDecisionRecorder>().RecordAsync(Record("corr-throw")));
    }

    [Fact]
    public void TheRecordAndTheQuery_PrintNoPersonalData()
    {
        var record = Record("corr-print") with { TenantId = "tenant-secret", IpAddress = "198.51.100.7" };
        var query = new ABACDecisionAuditQuery { UserId = "user-secret", TenantId = "tenant-secret", PageNumber = 2, PageSize = 7 };

        var printed = record + "|" + query;

        printed.ShouldNotContain("secret");
        printed.ShouldNotContain("user-1");
        printed.ShouldNotContain("198.51.100.7");
        query.ToString().ShouldContain("PageNumber = 2");
    }

    [Fact]
    public async Task ATracedRecordWithIdsAndAttributes_IsReadBackWithEachOfThem()
    {
        var store = new InMemoryOperationAuditStore();
        await using var provider = Provider(store);
        var record = Record("corr-full") with
        {
            Effect = Effect.Deny,
            PolicyId = "policy-a",
            RuleId = "rule-1",
            ModuleId = "module-1",
            TraceTruncated = true,
            EvaluatedPolicies =
            [
                new PolicyEvaluationTrace
                {
                    PolicyId = "policy-a",
                    IsPolicySet = false,
                    Effect = Effect.Deny,
                    Reason = PolicyTraceReason.Evaluated,
                    DecisiveRuleIds = ["rule-1"]
                }
            ],
            ObligationIds = ["obligation-1"],
            AdviceIds = ["advice-1"],
            AttributeNames = new Dictionary<AttributeCategory, IReadOnlyList<string>>
            {
                [AttributeCategory.Subject] = ["department"],
                [AttributeCategory.Resource] = ["owner"]
            },
            RecordedValues = new Dictionary<string, string> { ["department"] = "HR" }
        };
        (await provider.GetRequiredService<IABACDecisionRecorder>().RecordAsync(record)).IsRight.ShouldBeTrue();

        await using var scope = provider.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IABACDecisionAuditReader>()
            .QueryAsync(new ABACDecisionAuditQuery { CorrelationId = "corr-full" });

        var read = result.Match(Right: page => page.Items.ShouldHaveSingleItem(), Left: _ => throw new ShouldAssertException("Left"));
        read.EvaluatedPolicies.ShouldHaveSingleItem().PolicyId.ShouldBe("policy-a");
        read.TraceTruncated.ShouldBeTrue();
        read.PolicyId.ShouldBe("policy-a");
        read.RuleId.ShouldBe("rule-1");
        read.ModuleId.ShouldBe("module-1");
        read.ObligationIds.ShouldBe(["obligation-1"]);
        read.AdviceIds.ShouldBe(["advice-1"]);
        read.AttributeNames["Subject"].ShouldBe(["department"]);
        read.AttributeNames["Resource"].ShouldBe(["owner"]);
        read.RecordedValues["department"].ShouldBe("HR");
        read.ToString().ShouldNotContain("user-1");
    }

    [Fact]
    public async Task TheExport_WritesOneJsonLineForEachRecordedDecision()
    {
        var store = new InMemoryOperationAuditStore();
        await using var provider = Provider(store);
        var records = Enumerable.Range(0, 3).Select(i => Record("corr-export")).ToList();
        foreach (var record in records)
        {
            await provider.GetRequiredService<IABACDecisionRecorder>().RecordAsync(record);
        }

        await using var scope = provider.CreateAsyncScope();
        using var destination = new MemoryStream();
        var result = await scope.ServiceProvider.GetRequiredService<IABACDecisionAuditReader>()
            .ExportAsync(new ABACDecisionAuditQuery { CorrelationId = "corr-export" }, destination);

        result.Match(Right: count => count, Left: _ => -1).ShouldBe(3);
        var lines = System.Text.Encoding.UTF8.GetString(destination.ToArray()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(3);
        foreach (var record in records)
        {
            lines.ShouldContain(line => line.Contains(record.DecisionId.ToString(), StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task WhatTheRecorderWrites_TheReaderReadsBack()
    {
        var store = new InMemoryOperationAuditStore();
        await using var provider = Provider(store);
        var record = Record("corr-read");
        await provider.GetRequiredService<IABACDecisionRecorder>().RecordAsync(record);

        await using var scope = provider.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IABACDecisionAuditReader>()
            .QueryAsync(new ABACDecisionAuditQuery { CorrelationId = "corr-read" });

        var read = result.Match(Right: page => page.Items.ShouldHaveSingleItem(), Left: _ => throw new ShouldAssertException("Left"));
        read.DecisionId.ShouldBe(record.DecisionId);
        read.Outcome.ShouldBe(AuditOutcome.Denied);
        read.ReasonCode.ShouldBe(ABACErrors.AccessDeniedCode);
        read.ResourceId.ShouldBe("order-1");
        read.UserId.ShouldBe("user-1");
    }
}

#pragma warning restore CA2012
