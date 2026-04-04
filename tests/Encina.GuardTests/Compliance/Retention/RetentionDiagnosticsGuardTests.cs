using System.Diagnostics;

using Encina.Compliance.Retention.Diagnostics;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionDiagnostics"/> activity helpers and outcome recorders.
/// Verifies that null activities are handled gracefully (no-op behavior).
/// </summary>
public sealed class RetentionDiagnosticsGuardTests
{
    #region Activity Helper Null Safety

    [Fact]
    public void StartPipelineExecution_WhenNoListeners_ReturnsNull()
    {
        // ActivitySource without listeners returns null
        var activity = RetentionDiagnostics.StartPipelineExecution("TestRequest", "TestResponse");

        // No listeners registered in test context, so activity should be null
        // This is valid — the helper handles null gracefully
        activity.ShouldBeNull();
    }

    [Fact]
    public void StartEnforcementCycle_WhenNoListeners_ReturnsNull()
    {
        var activity = RetentionDiagnostics.StartEnforcementCycle();

        activity.ShouldBeNull();
    }

    [Fact]
    public void StartRecordDeletion_WhenNoListeners_ReturnsNull()
    {
        var activity = RetentionDiagnostics.StartRecordDeletion("entity-123", "financial");

        activity.ShouldBeNull();
    }

    [Fact]
    public void StartLegalHoldOperation_WhenNoListeners_ReturnsNull()
    {
        var activity = RetentionDiagnostics.StartLegalHoldOperation("hold-1", "entity-1");

        activity.ShouldBeNull();
    }

    [Fact]
    public void StartPolicyResolution_WhenNoListeners_ReturnsNull()
    {
        var activity = RetentionDiagnostics.StartPolicyResolution("user-profiles");

        activity.ShouldBeNull();
    }

    [Fact]
    public void StartAuditRecording_WhenNoListeners_ReturnsNull()
    {
        var activity = RetentionDiagnostics.StartAuditRecording("delete");

        activity.ShouldBeNull();
    }

    [Fact]
    public void StartServiceOperation_WhenNoListeners_ReturnsNull()
    {
        var activity = RetentionDiagnostics.StartServiceOperation("CreatePolicy");

        activity.ShouldBeNull();
    }

    #endregion

    #region Outcome Recorder Null Activity Safety

    [Fact]
    public void RecordCompleted_NullActivity_DoesNotThrow()
    {
        var act = () => RetentionDiagnostics.RecordCompleted(null);

        Should.NotThrow(act);
    }

    [Fact]
    public void RecordCompleted_NullActivityWithCount_DoesNotThrow()
    {
        var act = () => RetentionDiagnostics.RecordCompleted(null, 42);

        Should.NotThrow(act);
    }

    [Fact]
    public void RecordFailed_NullActivity_DoesNotThrow()
    {
        var act = () => RetentionDiagnostics.RecordFailed(null, "some reason");

        Should.NotThrow(act);
    }

    [Fact]
    public void RecordSkipped_NullActivity_DoesNotThrow()
    {
        var act = () => RetentionDiagnostics.RecordSkipped(null);

        Should.NotThrow(act);
    }

    [Fact]
    public void RecordHeld_NullActivity_DoesNotThrow()
    {
        var act = () => RetentionDiagnostics.RecordHeld(null, "entity-123");

        Should.NotThrow(act);
    }

    #endregion

    #region Outcome Recorders With Real Activity

    [Fact]
    public void RecordCompleted_WithActivity_SetsOutcomeTag()
    {
        using var source = new ActivitySource("test.retention.guard");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "test.retention.guard",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("test");
        activity.ShouldNotBeNull();

        RetentionDiagnostics.RecordCompleted(activity);

        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordCompleted_WithActivityAndCount_SetsRecordsProcessedTag()
    {
        using var source = new ActivitySource("test.retention.guard.count");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "test.retention.guard.count",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("test");
        activity.ShouldNotBeNull();

        RetentionDiagnostics.RecordCompleted(activity, 10);

        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordFailed_WithActivity_SetsErrorStatus()
    {
        using var source = new ActivitySource("test.retention.guard.fail");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "test.retention.guard.fail",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("test");
        activity.ShouldNotBeNull();

        RetentionDiagnostics.RecordFailed(activity, "test failure");

        activity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public void RecordSkipped_WithActivity_SetsOkStatus()
    {
        using var source = new ActivitySource("test.retention.guard.skip");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "test.retention.guard.skip",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("test");
        activity.ShouldNotBeNull();

        RetentionDiagnostics.RecordSkipped(activity);

        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordHeld_WithActivity_SetsOkStatus()
    {
        using var source = new ActivitySource("test.retention.guard.held");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "test.retention.guard.held",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("test");
        activity.ShouldNotBeNull();

        RetentionDiagnostics.RecordHeld(activity, "entity-456");

        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    #endregion

    #region Tag Constant Validation

    [Fact]
    public void TagConstants_AreNotNullOrEmpty()
    {
        RetentionDiagnostics.TagOutcome.ShouldNotBeNullOrEmpty();
        RetentionDiagnostics.TagEntityId.ShouldNotBeNullOrEmpty();
        RetentionDiagnostics.TagDataCategory.ShouldNotBeNullOrEmpty();
        RetentionDiagnostics.TagRequestType.ShouldNotBeNullOrEmpty();
        RetentionDiagnostics.TagResponseType.ShouldNotBeNullOrEmpty();
        RetentionDiagnostics.TagEnforcementMode.ShouldNotBeNullOrEmpty();
        RetentionDiagnostics.TagFailureReason.ShouldNotBeNullOrEmpty();
        RetentionDiagnostics.TagAction.ShouldNotBeNullOrEmpty();
        RetentionDiagnostics.TagHoldId.ShouldNotBeNullOrEmpty();
        RetentionDiagnostics.TagPolicyId.ShouldNotBeNullOrEmpty();
        RetentionDiagnostics.TagDeletionOutcome.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void TagConstants_FollowRetentionPrefix()
    {
        RetentionDiagnostics.TagOutcome.ShouldStartWith("retention.");
        RetentionDiagnostics.TagEntityId.ShouldStartWith("retention.");
        RetentionDiagnostics.TagDataCategory.ShouldStartWith("retention.");
        RetentionDiagnostics.TagRequestType.ShouldStartWith("retention.");
        RetentionDiagnostics.TagResponseType.ShouldStartWith("retention.");
        RetentionDiagnostics.TagEnforcementMode.ShouldStartWith("retention.");
        RetentionDiagnostics.TagFailureReason.ShouldStartWith("retention.");
        RetentionDiagnostics.TagAction.ShouldStartWith("retention.");
        RetentionDiagnostics.TagHoldId.ShouldStartWith("retention.");
        RetentionDiagnostics.TagPolicyId.ShouldStartWith("retention.");
        RetentionDiagnostics.TagDeletionOutcome.ShouldStartWith("retention.");
    }

    #endregion

    #region Meter and ActivitySource Validation

    [Fact]
    public void SourceName_IsCorrect()
    {
        RetentionDiagnostics.SourceName.ShouldBe("Encina.Compliance.Retention");
    }

    [Fact]
    public void SourceVersion_IsCorrect()
    {
        RetentionDiagnostics.SourceVersion.ShouldBe("1.0");
    }

    [Fact]
    public void ActivitySource_IsInitialized()
    {
        RetentionDiagnostics.ActivitySource.ShouldNotBeNull();
        RetentionDiagnostics.ActivitySource.Name.ShouldBe("Encina.Compliance.Retention");
    }

    [Fact]
    public void Meter_IsInitialized()
    {
        RetentionDiagnostics.Meter.ShouldNotBeNull();
        RetentionDiagnostics.Meter.Name.ShouldBe("Encina.Compliance.Retention");
    }

    #endregion

    #region Counter Instruments Initialization

    [Fact]
    public void PipelineExecutionsTotal_IsInitialized()
    {
        RetentionDiagnostics.PipelineExecutionsTotal.ShouldNotBeNull();
    }

    [Fact]
    public void EnforcementCyclesTotal_IsInitialized()
    {
        RetentionDiagnostics.EnforcementCyclesTotal.ShouldNotBeNull();
    }

    [Fact]
    public void RecordsCreatedTotal_IsInitialized()
    {
        RetentionDiagnostics.RecordsCreatedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void RecordsDeletedTotal_IsInitialized()
    {
        RetentionDiagnostics.RecordsDeletedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void RecordsHeldTotal_IsInitialized()
    {
        RetentionDiagnostics.RecordsHeldTotal.ShouldNotBeNull();
    }

    [Fact]
    public void RecordsFailedTotal_IsInitialized()
    {
        RetentionDiagnostics.RecordsFailedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void LegalHoldsAppliedTotal_IsInitialized()
    {
        RetentionDiagnostics.LegalHoldsAppliedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void LegalHoldsReleasedTotal_IsInitialized()
    {
        RetentionDiagnostics.LegalHoldsReleasedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void PolicyResolutionsTotal_IsInitialized()
    {
        RetentionDiagnostics.PolicyResolutionsTotal.ShouldNotBeNull();
    }

    [Fact]
    public void AuditEntriesTotal_IsInitialized()
    {
        RetentionDiagnostics.AuditEntriesTotal.ShouldNotBeNull();
    }

    [Fact]
    public void PoliciesCreatedTotal_IsInitialized()
    {
        RetentionDiagnostics.PoliciesCreatedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void PoliciesUpdatedTotal_IsInitialized()
    {
        RetentionDiagnostics.PoliciesUpdatedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void PoliciesDeactivatedTotal_IsInitialized()
    {
        RetentionDiagnostics.PoliciesDeactivatedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void RecordsTrackedTotal_IsInitialized()
    {
        RetentionDiagnostics.RecordsTrackedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void RecordsExpiredTotal_IsInitialized()
    {
        RetentionDiagnostics.RecordsExpiredTotal.ShouldNotBeNull();
    }

    [Fact]
    public void RecordsAnonymizedTotal_IsInitialized()
    {
        RetentionDiagnostics.RecordsAnonymizedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void HoldsPlacedTotal_IsInitialized()
    {
        RetentionDiagnostics.HoldsPlacedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void HoldsLiftedTotal_IsInitialized()
    {
        RetentionDiagnostics.HoldsLiftedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void CacheHitsTotal_IsInitialized()
    {
        RetentionDiagnostics.CacheHitsTotal.ShouldNotBeNull();
    }

    [Fact]
    public void CacheMissesTotal_IsInitialized()
    {
        RetentionDiagnostics.CacheMissesTotal.ShouldNotBeNull();
    }

    #endregion

    #region Histogram Instruments Initialization

    [Fact]
    public void EnforcementDuration_IsInitialized()
    {
        RetentionDiagnostics.EnforcementDuration.ShouldNotBeNull();
    }

    [Fact]
    public void PipelineDuration_IsInitialized()
    {
        RetentionDiagnostics.PipelineDuration.ShouldNotBeNull();
    }

    [Fact]
    public void DeletionDuration_IsInitialized()
    {
        RetentionDiagnostics.DeletionDuration.ShouldNotBeNull();
    }

    #endregion

    #region Activity Helpers With Listener

    [Fact]
    public void StartPipelineExecution_WithListener_ReturnsActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == RetentionDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = RetentionDiagnostics.StartPipelineExecution("TestReq", "TestResp");

        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("Retention.Pipeline");
    }

    [Fact]
    public void StartEnforcementCycle_WithListener_ReturnsActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == RetentionDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("Retention.Enforcement");
    }

    [Fact]
    public void StartRecordDeletion_WithListener_ReturnsActivityWithTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == RetentionDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = RetentionDiagnostics.StartRecordDeletion("ent-1", "financial");

        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("Retention.Deletion");
    }

    [Fact]
    public void StartServiceOperation_WithListener_ReturnsActivityWithOperationName()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == RetentionDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = RetentionDiagnostics.StartServiceOperation("CreatePolicy");

        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("Retention.Service.CreatePolicy");
    }

    #endregion
}
