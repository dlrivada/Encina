using System.Diagnostics;
using Encina.DomainModeling.Diagnostics;
using Shouldly;

namespace Encina.UnitTests.DomainModeling.Diagnostics;

/// <summary>
/// The repository, bulk-operation and unit-of-work activity sources record the error code as the
/// status description and as the <c>error.code</c> tag (#2134).
/// </summary>
public sealed class FailedActivityStatusTests
{
    private static ActivityListener Listen(string sourceName)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Fact]
    public void Repository_Failed_RecordsCodeAsStatusAndTag()
    {
        using var listener = Listen(RepositoryActivitySource.SourceName);
        using var activity = RepositoryActivitySource.StartOperation("GetById", "Order", "EF");
        activity.ShouldNotBeNull();

        RepositoryActivitySource.Failed(activity, "repo.not_found");

        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("repo.not_found");
        activity.GetTagItem("error.code").ShouldBe("repo.not_found");
    }

    [Fact]
    public void BulkOperations_Failed_RecordsCodeAsStatusAndTag()
    {
        using var listener = Listen(BulkOperationsActivitySource.SourceName);
        using var activity = BulkOperationsActivitySource.StartBulkOperation("Insert", "Order", "EF");
        activity.ShouldNotBeNull();

        BulkOperationsActivitySource.Failed(activity, "bulk.failed");

        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("bulk.failed");
        activity.GetTagItem("error.code").ShouldBe("bulk.failed");
    }

    [Fact]
    public void UnitOfWork_Failed_RecordsCodeAsStatusAndTag()
    {
        using var listener = Listen(UnitOfWorkActivitySource.SourceName);
        using var activity = UnitOfWorkActivitySource.StartSaveChanges();
        activity.ShouldNotBeNull();

        UnitOfWorkActivitySource.Failed(activity, "uow.failed");

        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("uow.failed");
        activity.GetTagItem("error.code").ShouldBe("uow.failed");
    }

    [Fact]
    public void Failed_WithNullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => RepositoryActivitySource.Failed(null, "code"));
        Should.NotThrow(() => BulkOperationsActivitySource.Failed(null, "code"));
        Should.NotThrow(() => UnitOfWorkActivitySource.Failed(null, "code"));
    }
}
