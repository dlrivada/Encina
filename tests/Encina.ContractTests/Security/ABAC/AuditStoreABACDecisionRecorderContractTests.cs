using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;

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
