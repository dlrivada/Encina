using System.Diagnostics;
using Encina.Audit.Marten.Diagnostics;
using Encina.DomainModeling.Diagnostics;
using Encina.EntityFrameworkCore.Diagnostics;
using Encina.Messaging.Diagnostics;
using Encina.Modules.Diagnostics;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry;

/// <summary>
/// Drives the span start and complete methods of the activity sources that #2134 trimmed down to a
/// success path (their <c>Failed</c> overloads had no caller), so each remaining method runs and sets
/// the status a trace backend reads.
/// </summary>
public sealed class ActivitySourceLifecycleTests
{
    private static Activity Started(Activity? activity)
    {
        activity.ShouldNotBeNull("the activity source must create a span while a listener is attached");
        return activity;
    }

    private static void ShouldBeCompletedOk(Activity activity)
    {
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
        activity.IsStopped.ShouldBeTrue();
    }

    [Fact]
    public void MartenAudit_EveryStartMethodCreatesASpanThatCompletesOk()
    {
        using var capture = new ActivityStatusCapture();
        Activity[] spans =
        [
            Started(MartenAuditActivitySource.StartRecord("Order", "Created", "stream-1")),
            Started(MartenAuditActivitySource.StartQuery("by_entity")),
            Started(MartenAuditActivitySource.StartPurge("monthly")),
            Started(MartenAuditActivitySource.StartEncrypt("2026-10")),
            Started(MartenAuditActivitySource.StartDecrypt("2026-10"))
        ];

        foreach (var span in spans)
        {
            MartenAuditActivitySource.Complete(span);
            ShouldBeCompletedOk(span);
        }
    }

    [Fact]
    public void QueryCache_LookupHitMissPopulateAndEvictionComplete()
    {
        using var capture = new ActivityStatusCapture();

        var hit = Started(QueryCacheActivitySource.StartLookup("Order", "hash-1"));
        QueryCacheActivitySource.CompleteHit(hit);
        var miss = Started(QueryCacheActivitySource.StartLookup("Order", "hash-2"));
        QueryCacheActivitySource.CompleteMiss(miss);
        var populate = Started(QueryCacheActivitySource.StartPopulate("Order", "hash-2"));
        QueryCacheActivitySource.Complete(populate);
        var eviction = Started(QueryCacheActivitySource.StartEviction("Order", "table_changed"));
        QueryCacheActivitySource.Complete(eviction);

        hit.GetTagItem("querycache.outcome").ShouldBe("hit");
        miss.GetTagItem("querycache.outcome").ShouldBe("miss");
        foreach (var span in new[] { hit, miss, populate, eviction })
        {
            ShouldBeCompletedOk(span);
        }
    }

    [Fact]
    public void SoftDelete_SoftDeleteRestoreAndHardDeleteComplete()
    {
        using var capture = new ActivityStatusCapture();
        Activity[] spans =
        [
            Started(SoftDeleteActivitySource.StartSoftDelete("Order")),
            Started(SoftDeleteActivitySource.StartRestore("Order")),
            Started(SoftDeleteActivitySource.StartHardDelete("Order"))
        ];

        foreach (var span in spans)
        {
            SoftDeleteActivitySource.Complete(span);
            ShouldBeCompletedOk(span);
        }
    }

    [Fact]
    public void DomainAudit_RecordAndQuerySpansComplete()
    {
        using var capture = new ActivityStatusCapture();
        var record = Started(AuditActivitySource.StartRecordAudit("Order", "Created"));
        var query = Started(AuditActivitySource.StartQueryAudit("by_entity"));

        AuditActivitySource.Complete(record);
        AuditActivitySource.Complete(query);

        ShouldBeCompletedOk(record);
        ShouldBeCompletedOk(query);
    }

    [Fact]
    public void BulkOperations_CompleteRecordsTheRowCount()
    {
        using var capture = new ActivityStatusCapture();
        var span = Started(BulkOperationsActivitySource.StartBulkOperation("insert", "Order", "efcore"));

        BulkOperationsActivitySource.Complete(span, 12);

        span.GetTagItem("bulk.rows_affected").ShouldBe(12);
        ShouldBeCompletedOk(span);
    }

    [Fact]
    public void Repository_CompleteWithAndWithoutAResultCount()
    {
        using var capture = new ActivityStatusCapture();
        var counted = Started(RepositoryActivitySource.StartOperation("get_all", "Order", "efcore"));
        var uncounted = Started(RepositoryActivitySource.StartOperation("add", "Order", "efcore"));

        RepositoryActivitySource.Complete(counted, 3);
        RepositoryActivitySource.Complete(uncounted);

        ShouldBeCompletedOk(counted);
        ShouldBeCompletedOk(uncounted);
    }

    [Fact]
    public void UnitOfWork_SaveChangesAndTransactionSpansComplete()
    {
        using var capture = new ActivityStatusCapture();
        var save = Started(UnitOfWorkActivitySource.StartSaveChanges());
        var transaction = Started(UnitOfWorkActivitySource.StartTransaction());

        UnitOfWorkActivitySource.CompleteSaveChanges(save, 5);
        UnitOfWorkActivitySource.CompleteTransaction(transaction, "committed");

        save.GetTagItem("uow.affected_rows").ShouldBe(5);
        transaction.GetTagItem("uow.outcome").ShouldBe("committed");
        ShouldBeCompletedOk(save);
        ShouldBeCompletedOk(transaction);
    }

    [Fact]
    public void Inbox_ReceiveDuplicateCheckAndMarkProcessedComplete()
    {
        using var capture = new ActivityStatusCapture();
        var receive = Started(InboxActivitySource.StartReceive("OrderPlaced", "m-1"));
        var duplicate = Started(InboxActivitySource.StartDuplicateCheck("m-1"));
        var processed = Started(InboxActivitySource.StartMarkProcessed("m-1"));

        InboxActivitySource.Complete(receive);
        InboxActivitySource.CompleteDuplicateFound(duplicate);
        InboxActivitySource.Complete(processed);

        duplicate.GetTagItem("inbox.duplicate").ShouldBe(true);
        foreach (var span in new[] { receive, duplicate, processed })
        {
            ShouldBeCompletedOk(span);
        }
    }

    [Fact]
    public void Outbox_AddBatchAndMarkProcessedComplete()
    {
        using var capture = new ActivityStatusCapture();
        var add = Started(OutboxActivitySource.StartAdd("OrderPlaced", "m-1"));
        var batch = Started(OutboxActivitySource.StartProcessBatch(20));
        var processed = Started(OutboxActivitySource.StartMarkProcessed("m-1"));

        OutboxActivitySource.Complete(add);
        OutboxActivitySource.CompleteBatch(batch, 18);
        OutboxActivitySource.Complete(processed);

        foreach (var span in new[] { add, batch, processed })
        {
            ShouldBeCompletedOk(span);
        }
    }

    [Fact]
    public void Saga_TransitionCreateCompleteAndCompensateComplete()
    {
        using var capture = new ActivityStatusCapture();
        var sagaId = Guid.NewGuid();
        Activity[] spans =
        [
            Started(SagaActivitySource.StartSagaTransition("OrderSaga", sagaId, "reserve", "charge")),
            Started(SagaActivitySource.StartSagaCreate("OrderSaga", sagaId)),
            Started(SagaActivitySource.StartSagaComplete("OrderSaga", sagaId)),
            Started(SagaActivitySource.StartSagaCompensate("OrderSaga", sagaId))
        ];

        foreach (var span in spans)
        {
            SagaActivitySource.Complete(span);
            ShouldBeCompletedOk(span);
        }
    }

    [Fact]
    public void Modules_DispatchAndLifecycleSpansComplete()
    {
        using var capture = new ActivityStatusCapture();
        var dispatch = Started(ModuleActivitySource.StartDispatch("Orders", "PlaceOrder"));
        var lifecycle = Started(ModuleActivitySource.StartLifecycle("Orders", "initialize"));

        ModuleActivitySource.Complete(dispatch);
        ModuleActivitySource.Complete(lifecycle);

        ShouldBeCompletedOk(dispatch);
        ShouldBeCompletedOk(lifecycle);
    }

    [Fact]
    public void EverySource_CompletingANullSpan_IsASafeNoOp()
    {
        // The Start methods return null when no listener is attached, and callers pass that null on.
        MartenAuditActivitySource.Complete(null);
        QueryCacheActivitySource.CompleteHit(null);
        QueryCacheActivitySource.CompleteMiss(null);
        QueryCacheActivitySource.Complete(null);
        SoftDeleteActivitySource.Complete(null);
        AuditActivitySource.Complete(null);
        BulkOperationsActivitySource.Complete(null, 1);
        RepositoryActivitySource.Complete(null);
        UnitOfWorkActivitySource.CompleteSaveChanges(null, 1);
        UnitOfWorkActivitySource.CompleteTransaction(null, "committed");
        InboxActivitySource.Complete(null);
        InboxActivitySource.CompleteDuplicateFound(null);
        OutboxActivitySource.Complete(null);
        OutboxActivitySource.CompleteBatch(null, 1);
        SagaActivitySource.Complete(null);
        ModuleActivitySource.Complete(null);
    }
}
