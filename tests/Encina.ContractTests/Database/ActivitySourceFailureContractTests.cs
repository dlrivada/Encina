using System.Diagnostics;
using Encina.DomainModeling.Diagnostics;
using Shouldly;

namespace Encina.ContractTests.Database;

/// <summary>
/// Contract of the DomainModeling activity sources: <c>Failed</c> records only the error code it is
/// given, as status description and tag, and tolerates a missing span (AGENTS.md section 3, #2134).
/// </summary>
public sealed class ActivitySourceFailureContractTests : IDisposable
{
    private readonly ActivityListener _listener = new()
    {
        ShouldListenTo = source => source.Name is "Encina.BulkOperations" or "Encina.Repository" or "Encina.UnitOfWork" or "Encina.Audit",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
    };

    public ActivitySourceFailureContractTests() => ActivitySource.AddActivityListener(_listener);

    public void Dispose() => _listener.Dispose();

    private static void ShouldCarryOnlyTheCode(Activity? activity)
    {
        activity.ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("test.code");
        activity.GetTagItem("error.code").ShouldBe("test.code");
    }

    [Fact]
    public void BulkOperations_Failed_RecordsOnlyTheCode()
    {
        var span = BulkOperationsActivitySource.StartBulkOperation("insert", "Order", "efcore");
        BulkOperationsActivitySource.Failed(span, "test.code");
        ShouldCarryOnlyTheCode(span);
        BulkOperationsActivitySource.Failed(null, "test.code");
    }

    [Fact]
    public void Repository_Failed_RecordsOnlyTheCode()
    {
        var span = RepositoryActivitySource.StartOperation("add", "Order", "efcore");
        RepositoryActivitySource.Failed(span, "test.code");
        ShouldCarryOnlyTheCode(span);
        RepositoryActivitySource.Failed(null, "test.code");
    }

    [Fact]
    public void UnitOfWork_Failed_RecordsOnlyTheCode()
    {
        var span = UnitOfWorkActivitySource.StartSaveChanges();
        UnitOfWorkActivitySource.Failed(span, "test.code");
        ShouldCarryOnlyTheCode(span);
        UnitOfWorkActivitySource.Failed(null, "test.code");
    }

    [Fact]
    public void Failed_WithBlankCode_SetsNoCodeTag()
    {
        var span = RepositoryActivitySource.StartOperation("add", "Order", "efcore");
        RepositoryActivitySource.Failed(span, " ");
        span.ShouldNotBeNull();
        span.GetTagItem("error.code").ShouldBeNull();
    }

    [Fact]
    public void Complete_WithAndWithoutSpan_SetsOkOrIgnores()
    {
        var bulk = BulkOperationsActivitySource.StartBulkOperation("insert", "Order", "efcore");
        BulkOperationsActivitySource.Complete(bulk, 3);
        var repo = RepositoryActivitySource.StartOperation("get", "Order", "efcore");
        RepositoryActivitySource.Complete(repo, 2);
        var uow = UnitOfWorkActivitySource.StartTransaction();
        UnitOfWorkActivitySource.CompleteTransaction(uow, "committed");
        var save = UnitOfWorkActivitySource.StartSaveChanges();
        UnitOfWorkActivitySource.CompleteSaveChanges(save, 1);
        var audit = AuditActivitySource.StartRecordAudit("Order", "Created");
        AuditActivitySource.Complete(audit);

        new[] { bulk, repo, uow, save, audit }.ShouldAllBe(a => a != null && a.Status == ActivityStatusCode.Ok);
        AuditActivitySource.Complete(null);
        AuditActivitySource.StartQueryAudit("by_entity").ShouldNotBeNull().Dispose();
    }
}
