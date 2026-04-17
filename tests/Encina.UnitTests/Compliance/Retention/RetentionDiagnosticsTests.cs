using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Compliance.Retention.Diagnostics;

using Shouldly;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionDiagnostics"/> activity source, meters, and helper methods.
/// </summary>
public sealed class RetentionDiagnosticsTests : IDisposable
{
    private readonly ActivityListener _listener;

    public RetentionDiagnosticsTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == RetentionDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    #region Activity Source Configuration

    [Fact]
    public void ActivitySource_HasCorrectNameAndVersion()
    {
        RetentionDiagnostics.ActivitySource.Name.ShouldBe("Encina.Compliance.Retention");
        RetentionDiagnostics.ActivitySource.Version.ShouldBe("1.0");
    }

    [Fact]
    public void Meter_HasCorrectNameAndVersion()
    {
        RetentionDiagnostics.Meter.Name.ShouldBe("Encina.Compliance.Retention");
        RetentionDiagnostics.Meter.Version.ShouldBe("1.0");
    }

    #endregion

    #region StartPipelineExecution

    [Fact]
    public void StartPipelineExecution_SetsRequestAndResponseTypeTags()
    {
        using var activity = RetentionDiagnostics.StartPipelineExecution("CreateOrder", "OrderResponse");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(RetentionDiagnostics.TagRequestType).ShouldBe("CreateOrder");
        activity.GetTagItem(RetentionDiagnostics.TagResponseType).ShouldBe("OrderResponse");
    }

    [Fact]
    public void StartPipelineExecution_CreatesInternalKindActivity()
    {
        using var activity = RetentionDiagnostics.StartPipelineExecution("Req", "Resp");

        activity.ShouldNotBeNull();
        activity!.Kind.ShouldBe(ActivityKind.Internal);
    }

    #endregion

    #region StartEnforcementCycle

    [Fact]
    public void StartEnforcementCycle_ReturnsInternalActivity()
    {
        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("Retention.Enforcement");
        activity.Kind.ShouldBe(ActivityKind.Internal);
    }

    #endregion

    #region StartRecordDeletion

    [Fact]
    public void StartRecordDeletion_SetsEntityIdAndDataCategoryTags()
    {
        using var activity = RetentionDiagnostics.StartRecordDeletion("entity-1", "financial-records");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(RetentionDiagnostics.TagEntityId).ShouldBe("entity-1");
        activity.GetTagItem(RetentionDiagnostics.TagDataCategory).ShouldBe("financial-records");
        activity.OperationName.ShouldBe("Retention.Deletion");
    }

    #endregion

    #region StartLegalHoldOperation

    [Fact]
    public void StartLegalHoldOperation_SetsHoldIdAndEntityIdTags()
    {
        using var activity = RetentionDiagnostics.StartLegalHoldOperation("hold-42", "entity-99");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(RetentionDiagnostics.TagHoldId).ShouldBe("hold-42");
        activity.GetTagItem(RetentionDiagnostics.TagEntityId).ShouldBe("entity-99");
        activity.OperationName.ShouldBe("Retention.LegalHold");
    }

    #endregion

    #region StartPolicyResolution

    [Fact]
    public void StartPolicyResolution_SetsDataCategoryTag()
    {
        using var activity = RetentionDiagnostics.StartPolicyResolution("user-profiles");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(RetentionDiagnostics.TagDataCategory).ShouldBe("user-profiles");
        activity.OperationName.ShouldBe("Retention.PolicyResolution");
    }

    #endregion

    #region StartAuditRecording

    [Fact]
    public void StartAuditRecording_SetsActionTag()
    {
        using var activity = RetentionDiagnostics.StartAuditRecording("delete_entity");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(RetentionDiagnostics.TagAction).ShouldBe("delete_entity");
        activity.OperationName.ShouldBe("Retention.Audit");
    }

    #endregion

    #region StartServiceOperation

    [Fact]
    public void StartServiceOperation_SetsActionTag()
    {
        using var activity = RetentionDiagnostics.StartServiceOperation("TrackRecord");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(RetentionDiagnostics.TagAction).ShouldBe("TrackRecord");
        activity.OperationName.ShouldBe("Retention.Service.TrackRecord");
    }

    [Fact]
    public void StartServiceOperation_DifferentOperationNames_CreateDistinctActivities()
    {
        using var createActivity = RetentionDiagnostics.StartServiceOperation("CreatePolicy");
        using var updateActivity = RetentionDiagnostics.StartServiceOperation("UpdatePolicy");

        createActivity!.OperationName.ShouldBe("Retention.Service.CreatePolicy");
        updateActivity!.OperationName.ShouldBe("Retention.Service.UpdatePolicy");
    }

    #endregion

    #region RecordCompleted (no count)

    [Fact]
    public void RecordCompleted_SetsOutcomeTagToCompleted()
    {
        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        RetentionDiagnostics.RecordCompleted(activity);

        activity!.GetTagItem(RetentionDiagnostics.TagOutcome).ShouldBe("completed");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    #endregion

    #region RecordCompleted (with count)

    [Fact]
    public void RecordCompleted_WithRecordCount_SetsRecordsProcessedTag()
    {
        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        RetentionDiagnostics.RecordCompleted(activity, 15);

        activity!.GetTagItem(RetentionDiagnostics.TagOutcome).ShouldBe("completed");
        activity.GetTagItem("retention.records_processed").ShouldBe(15);
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordCompleted_WithZeroRecords_SetsZeroTag()
    {
        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        RetentionDiagnostics.RecordCompleted(activity, 0);

        activity!.GetTagItem("retention.records_processed").ShouldBe(0);
    }

    #endregion

    #region RecordFailed

    [Fact]
    public void RecordFailed_SetsOutcomeAndFailureReasonTags()
    {
        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        RetentionDiagnostics.RecordFailed(activity, "database_timeout");

        activity!.GetTagItem(RetentionDiagnostics.TagOutcome).ShouldBe("failed");
        activity.GetTagItem(RetentionDiagnostics.TagFailureReason).ShouldBe("database_timeout");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("database_timeout");
    }

    #endregion

    #region RecordSkipped

    [Fact]
    public void RecordSkipped_SetsOutcomeToSkipped()
    {
        using var activity = RetentionDiagnostics.StartPipelineExecution("Req", "Resp");

        RetentionDiagnostics.RecordSkipped(activity);

        activity!.GetTagItem(RetentionDiagnostics.TagOutcome).ShouldBe("skipped");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    #endregion

    #region RecordHeld

    [Fact]
    public void RecordHeld_SetsOutcomeAndEntityIdTags()
    {
        using var activity = RetentionDiagnostics.StartRecordDeletion("ent-1", "cat-1");

        RetentionDiagnostics.RecordHeld(activity, "ent-1");

        activity!.GetTagItem(RetentionDiagnostics.TagOutcome).ShouldBe("held");
        // Note: EntityId is set both by StartRecordDeletion and RecordHeld
        activity.GetTagItem(RetentionDiagnostics.TagEntityId).ShouldBe("ent-1");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    #endregion

    #region Tag Constants

    [Theory]
    [InlineData("retention.outcome")]
    [InlineData("retention.entity_id")]
    [InlineData("retention.data_category")]
    [InlineData("retention.request_type")]
    [InlineData("retention.response_type")]
    [InlineData("retention.enforcement_mode")]
    [InlineData("retention.failure_reason")]
    [InlineData("retention.action")]
    [InlineData("retention.hold_id")]
    [InlineData("retention.policy_id")]
    [InlineData("retention.deletion_outcome")]
    public void TagConstants_AllStartWithRetentionPrefix(string expectedTag)
    {
        var allTags = new[]
        {
            RetentionDiagnostics.TagOutcome,
            RetentionDiagnostics.TagEntityId,
            RetentionDiagnostics.TagDataCategory,
            RetentionDiagnostics.TagRequestType,
            RetentionDiagnostics.TagResponseType,
            RetentionDiagnostics.TagEnforcementMode,
            RetentionDiagnostics.TagFailureReason,
            RetentionDiagnostics.TagAction,
            RetentionDiagnostics.TagHoldId,
            RetentionDiagnostics.TagPolicyId,
            RetentionDiagnostics.TagDeletionOutcome
        };

        allTags.ShouldContain(expectedTag);
    }

    #endregion

    #region Counter Instruments

    [Fact]
    public void AllCounters_AreInitialized()
    {
        // Verify all counters are non-null and can be invoked without error
        RetentionDiagnostics.PipelineExecutionsTotal.ShouldNotBeNull();
        RetentionDiagnostics.EnforcementCyclesTotal.ShouldNotBeNull();
        RetentionDiagnostics.RecordsCreatedTotal.ShouldNotBeNull();
        RetentionDiagnostics.RecordsDeletedTotal.ShouldNotBeNull();
        RetentionDiagnostics.RecordsHeldTotal.ShouldNotBeNull();
        RetentionDiagnostics.RecordsFailedTotal.ShouldNotBeNull();
        RetentionDiagnostics.LegalHoldsAppliedTotal.ShouldNotBeNull();
        RetentionDiagnostics.LegalHoldsReleasedTotal.ShouldNotBeNull();
        RetentionDiagnostics.PolicyResolutionsTotal.ShouldNotBeNull();
        RetentionDiagnostics.AuditEntriesTotal.ShouldNotBeNull();
        RetentionDiagnostics.PoliciesCreatedTotal.ShouldNotBeNull();
        RetentionDiagnostics.PoliciesUpdatedTotal.ShouldNotBeNull();
        RetentionDiagnostics.PoliciesDeactivatedTotal.ShouldNotBeNull();
        RetentionDiagnostics.RecordsTrackedTotal.ShouldNotBeNull();
        RetentionDiagnostics.RecordsExpiredTotal.ShouldNotBeNull();
        RetentionDiagnostics.RecordsAnonymizedTotal.ShouldNotBeNull();
        RetentionDiagnostics.HoldsPlacedTotal.ShouldNotBeNull();
        RetentionDiagnostics.HoldsLiftedTotal.ShouldNotBeNull();
        RetentionDiagnostics.CacheHitsTotal.ShouldNotBeNull();
        RetentionDiagnostics.CacheMissesTotal.ShouldNotBeNull();
    }

    [Fact]
    public void AllHistograms_AreInitialized()
    {
        RetentionDiagnostics.EnforcementDuration.ShouldNotBeNull();
        RetentionDiagnostics.PipelineDuration.ShouldNotBeNull();
        RetentionDiagnostics.DeletionDuration.ShouldNotBeNull();
    }

    [Fact]
    public void Counters_CanBeIncrementedWithoutError()
    {
        // Verify counters can be incremented without throwing
        var act = () =>
        {
            RetentionDiagnostics.PipelineExecutionsTotal.Add(1,
                new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "completed"));
            RetentionDiagnostics.RecordsCreatedTotal.Add(1,
                new KeyValuePair<string, object?>(RetentionDiagnostics.TagDataCategory, "test"));
            RetentionDiagnostics.LegalHoldsAppliedTotal.Add(1);
            RetentionDiagnostics.CacheHitsTotal.Add(1);
        };

        Should.NotThrow(act);
    }

    [Fact]
    public void Histograms_CanRecordWithoutError()
    {
        var act = () =>
        {
            RetentionDiagnostics.EnforcementDuration.Record(42.5);
            RetentionDiagnostics.PipelineDuration.Record(1.2);
            RetentionDiagnostics.DeletionDuration.Record(0.5);
        };

        Should.NotThrow(act);
    }

    #endregion
}
