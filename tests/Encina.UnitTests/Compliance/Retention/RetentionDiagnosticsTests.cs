using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Compliance.Retention.Diagnostics;

using FluentAssertions;

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
        RetentionDiagnostics.ActivitySource.Name.Should().Be("Encina.Compliance.Retention");
        RetentionDiagnostics.ActivitySource.Version.Should().Be("1.0");
    }

    [Fact]
    public void Meter_HasCorrectNameAndVersion()
    {
        RetentionDiagnostics.Meter.Name.Should().Be("Encina.Compliance.Retention");
        RetentionDiagnostics.Meter.Version.Should().Be("1.0");
    }

    #endregion

    #region StartPipelineExecution

    [Fact]
    public void StartPipelineExecution_SetsRequestAndResponseTypeTags()
    {
        using var activity = RetentionDiagnostics.StartPipelineExecution("CreateOrder", "OrderResponse");

        activity.Should().NotBeNull();
        activity!.GetTagItem(RetentionDiagnostics.TagRequestType).Should().Be("CreateOrder");
        activity.GetTagItem(RetentionDiagnostics.TagResponseType).Should().Be("OrderResponse");
    }

    [Fact]
    public void StartPipelineExecution_CreatesInternalKindActivity()
    {
        using var activity = RetentionDiagnostics.StartPipelineExecution("Req", "Resp");

        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Internal);
    }

    #endregion

    #region StartEnforcementCycle

    [Fact]
    public void StartEnforcementCycle_ReturnsInternalActivity()
    {
        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("Retention.Enforcement");
        activity.Kind.Should().Be(ActivityKind.Internal);
    }

    #endregion

    #region StartRecordDeletion

    [Fact]
    public void StartRecordDeletion_SetsEntityIdAndDataCategoryTags()
    {
        using var activity = RetentionDiagnostics.StartRecordDeletion("entity-1", "financial-records");

        activity.Should().NotBeNull();
        activity!.GetTagItem(RetentionDiagnostics.TagEntityId).Should().Be("entity-1");
        activity.GetTagItem(RetentionDiagnostics.TagDataCategory).Should().Be("financial-records");
        activity.OperationName.Should().Be("Retention.Deletion");
    }

    #endregion

    #region StartLegalHoldOperation

    [Fact]
    public void StartLegalHoldOperation_SetsHoldIdAndEntityIdTags()
    {
        using var activity = RetentionDiagnostics.StartLegalHoldOperation("hold-42", "entity-99");

        activity.Should().NotBeNull();
        activity!.GetTagItem(RetentionDiagnostics.TagHoldId).Should().Be("hold-42");
        activity.GetTagItem(RetentionDiagnostics.TagEntityId).Should().Be("entity-99");
        activity.OperationName.Should().Be("Retention.LegalHold");
    }

    #endregion

    #region StartPolicyResolution

    [Fact]
    public void StartPolicyResolution_SetsDataCategoryTag()
    {
        using var activity = RetentionDiagnostics.StartPolicyResolution("user-profiles");

        activity.Should().NotBeNull();
        activity!.GetTagItem(RetentionDiagnostics.TagDataCategory).Should().Be("user-profiles");
        activity.OperationName.Should().Be("Retention.PolicyResolution");
    }

    #endregion

    #region StartAuditRecording

    [Fact]
    public void StartAuditRecording_SetsActionTag()
    {
        using var activity = RetentionDiagnostics.StartAuditRecording("delete_entity");

        activity.Should().NotBeNull();
        activity!.GetTagItem(RetentionDiagnostics.TagAction).Should().Be("delete_entity");
        activity.OperationName.Should().Be("Retention.Audit");
    }

    #endregion

    #region StartServiceOperation

    [Fact]
    public void StartServiceOperation_SetsActionTag()
    {
        using var activity = RetentionDiagnostics.StartServiceOperation("TrackRecord");

        activity.Should().NotBeNull();
        activity!.GetTagItem(RetentionDiagnostics.TagAction).Should().Be("TrackRecord");
        activity.OperationName.Should().Be("Retention.Service.TrackRecord");
    }

    [Fact]
    public void StartServiceOperation_DifferentOperationNames_CreateDistinctActivities()
    {
        using var createActivity = RetentionDiagnostics.StartServiceOperation("CreatePolicy");
        using var updateActivity = RetentionDiagnostics.StartServiceOperation("UpdatePolicy");

        createActivity!.OperationName.Should().Be("Retention.Service.CreatePolicy");
        updateActivity!.OperationName.Should().Be("Retention.Service.UpdatePolicy");
    }

    #endregion

    #region RecordCompleted (no count)

    [Fact]
    public void RecordCompleted_SetsOutcomeTagToCompleted()
    {
        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        RetentionDiagnostics.RecordCompleted(activity);

        activity!.GetTagItem(RetentionDiagnostics.TagOutcome).Should().Be("completed");
        activity.Status.Should().Be(ActivityStatusCode.Ok);
    }

    #endregion

    #region RecordCompleted (with count)

    [Fact]
    public void RecordCompleted_WithRecordCount_SetsRecordsProcessedTag()
    {
        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        RetentionDiagnostics.RecordCompleted(activity, 15);

        activity!.GetTagItem(RetentionDiagnostics.TagOutcome).Should().Be("completed");
        activity.GetTagItem("retention.records_processed").Should().Be(15);
        activity.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordCompleted_WithZeroRecords_SetsZeroTag()
    {
        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        RetentionDiagnostics.RecordCompleted(activity, 0);

        activity!.GetTagItem("retention.records_processed").Should().Be(0);
    }

    #endregion

    #region RecordFailed

    [Fact]
    public void RecordFailed_SetsOutcomeAndFailureReasonTags()
    {
        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        RetentionDiagnostics.RecordFailed(activity, "database_timeout");

        activity!.GetTagItem(RetentionDiagnostics.TagOutcome).Should().Be("failed");
        activity.GetTagItem(RetentionDiagnostics.TagFailureReason).Should().Be("database_timeout");
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("database_timeout");
    }

    #endregion

    #region RecordSkipped

    [Fact]
    public void RecordSkipped_SetsOutcomeToSkipped()
    {
        using var activity = RetentionDiagnostics.StartPipelineExecution("Req", "Resp");

        RetentionDiagnostics.RecordSkipped(activity);

        activity!.GetTagItem(RetentionDiagnostics.TagOutcome).Should().Be("skipped");
        activity.Status.Should().Be(ActivityStatusCode.Ok);
    }

    #endregion

    #region RecordHeld

    [Fact]
    public void RecordHeld_SetsOutcomeAndEntityIdTags()
    {
        using var activity = RetentionDiagnostics.StartRecordDeletion("ent-1", "cat-1");

        RetentionDiagnostics.RecordHeld(activity, "ent-1");

        activity!.GetTagItem(RetentionDiagnostics.TagOutcome).Should().Be("held");
        // Note: EntityId is set both by StartRecordDeletion and RecordHeld
        activity.GetTagItem(RetentionDiagnostics.TagEntityId).Should().Be("ent-1");
        activity.Status.Should().Be(ActivityStatusCode.Ok);
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

        allTags.Should().Contain(expectedTag);
    }

    #endregion

    #region Counter Instruments

    [Fact]
    public void AllCounters_AreInitialized()
    {
        // Verify all counters are non-null and can be invoked without error
        RetentionDiagnostics.PipelineExecutionsTotal.Should().NotBeNull();
        RetentionDiagnostics.EnforcementCyclesTotal.Should().NotBeNull();
        RetentionDiagnostics.RecordsCreatedTotal.Should().NotBeNull();
        RetentionDiagnostics.RecordsDeletedTotal.Should().NotBeNull();
        RetentionDiagnostics.RecordsHeldTotal.Should().NotBeNull();
        RetentionDiagnostics.RecordsFailedTotal.Should().NotBeNull();
        RetentionDiagnostics.LegalHoldsAppliedTotal.Should().NotBeNull();
        RetentionDiagnostics.LegalHoldsReleasedTotal.Should().NotBeNull();
        RetentionDiagnostics.PolicyResolutionsTotal.Should().NotBeNull();
        RetentionDiagnostics.AuditEntriesTotal.Should().NotBeNull();
        RetentionDiagnostics.PoliciesCreatedTotal.Should().NotBeNull();
        RetentionDiagnostics.PoliciesUpdatedTotal.Should().NotBeNull();
        RetentionDiagnostics.PoliciesDeactivatedTotal.Should().NotBeNull();
        RetentionDiagnostics.RecordsTrackedTotal.Should().NotBeNull();
        RetentionDiagnostics.RecordsExpiredTotal.Should().NotBeNull();
        RetentionDiagnostics.RecordsAnonymizedTotal.Should().NotBeNull();
        RetentionDiagnostics.HoldsPlacedTotal.Should().NotBeNull();
        RetentionDiagnostics.HoldsLiftedTotal.Should().NotBeNull();
        RetentionDiagnostics.CacheHitsTotal.Should().NotBeNull();
        RetentionDiagnostics.CacheMissesTotal.Should().NotBeNull();
    }

    [Fact]
    public void AllHistograms_AreInitialized()
    {
        RetentionDiagnostics.EnforcementDuration.Should().NotBeNull();
        RetentionDiagnostics.PipelineDuration.Should().NotBeNull();
        RetentionDiagnostics.DeletionDuration.Should().NotBeNull();
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

        act.Should().NotThrow();
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

        act.Should().NotThrow();
    }

    #endregion
}
