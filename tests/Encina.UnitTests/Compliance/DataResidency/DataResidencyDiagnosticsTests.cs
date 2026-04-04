using System.Diagnostics;

using Encina.Compliance.DataResidency.Diagnostics;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataResidency;

/// <summary>
/// Unit tests for <see cref="DataResidencyDiagnostics"/> covering activity creation,
/// counters, histograms, and outcome recorders.
/// </summary>
public class DataResidencyDiagnosticsTests
{
    [Fact]
    public void SourceName_ShouldBeCorrect()
    {
        DataResidencyDiagnostics.SourceName.ShouldBe("Encina.Compliance.DataResidency");
    }

    [Fact]
    public void SourceVersion_ShouldBeCorrect()
    {
        DataResidencyDiagnostics.SourceVersion.ShouldBe("1.0");
    }

    [Fact]
    public void ActivitySource_ShouldNotBeNull()
    {
        DataResidencyDiagnostics.ActivitySource.ShouldNotBeNull();
        DataResidencyDiagnostics.ActivitySource.Name.ShouldBe(DataResidencyDiagnostics.SourceName);
    }

    [Fact]
    public void Meter_ShouldNotBeNull()
    {
        DataResidencyDiagnostics.Meter.ShouldNotBeNull();
        DataResidencyDiagnostics.Meter.Name.ShouldBe(DataResidencyDiagnostics.SourceName);
    }

    // ---- Counters ----

    [Fact]
    public void PipelineExecutionsTotal_ShouldExist()
    {
        DataResidencyDiagnostics.PipelineExecutionsTotal.ShouldNotBeNull();
    }

    [Fact]
    public void PolicyChecksTotal_ShouldExist()
    {
        DataResidencyDiagnostics.PolicyChecksTotal.ShouldNotBeNull();
    }

    [Fact]
    public void CrossBorderTransfersTotal_ShouldExist()
    {
        DataResidencyDiagnostics.CrossBorderTransfersTotal.ShouldNotBeNull();
    }

    [Fact]
    public void TransfersBlockedTotal_ShouldExist()
    {
        DataResidencyDiagnostics.TransfersBlockedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void LocationRecordsTotal_ShouldExist()
    {
        DataResidencyDiagnostics.LocationRecordsTotal.ShouldNotBeNull();
    }

    [Fact]
    public void ViolationsTotal_ShouldExist()
    {
        DataResidencyDiagnostics.ViolationsTotal.ShouldNotBeNull();
    }

    [Fact]
    public void AuditEntriesTotal_ShouldExist()
    {
        DataResidencyDiagnostics.AuditEntriesTotal.ShouldNotBeNull();
    }

    [Fact]
    public void PoliciesCreatedTotal_ShouldExist()
    {
        DataResidencyDiagnostics.PoliciesCreatedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void PoliciesUpdatedTotal_ShouldExist()
    {
        DataResidencyDiagnostics.PoliciesUpdatedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void PoliciesDeletedTotal_ShouldExist()
    {
        DataResidencyDiagnostics.PoliciesDeletedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void LocationsRegisteredTotal_ShouldExist()
    {
        DataResidencyDiagnostics.LocationsRegisteredTotal.ShouldNotBeNull();
    }

    [Fact]
    public void LocationsMigratedTotal_ShouldExist()
    {
        DataResidencyDiagnostics.LocationsMigratedTotal.ShouldNotBeNull();
    }

    [Fact]
    public void LocationsRemovedTotal_ShouldExist()
    {
        DataResidencyDiagnostics.LocationsRemovedTotal.ShouldNotBeNull();
    }

    // ---- Histograms ----

    [Fact]
    public void PipelineDuration_ShouldExist()
    {
        DataResidencyDiagnostics.PipelineDuration.ShouldNotBeNull();
    }

    [Fact]
    public void TransferValidationDuration_ShouldExist()
    {
        DataResidencyDiagnostics.TransferValidationDuration.ShouldNotBeNull();
    }

    // ---- Activity helpers ----

    [Fact]
    public void StartPipelineExecution_WithoutListeners_ShouldReturnNull()
    {
        // Without any listener, activity should be null
        var activity = DataResidencyDiagnostics.StartPipelineExecution("TestRequest", "TestResponse");
        activity.ShouldBeNull();
    }

    [Fact]
    public void StartTransferValidation_WithoutListeners_ShouldReturnNull()
    {
        var activity = DataResidencyDiagnostics.StartTransferValidation("DE", "US");
        activity.ShouldBeNull();
    }

    [Fact]
    public void StartLocationRecord_WithoutListeners_ShouldReturnNull()
    {
        var activity = DataResidencyDiagnostics.StartLocationRecord("entity-1", "DE");
        activity.ShouldBeNull();
    }

    // ---- Outcome recorders (should not throw on null) ----

    [Fact]
    public void RecordCompleted_NullActivity_ShouldNotThrow()
    {
        DataResidencyDiagnostics.RecordCompleted(null);
    }

    [Fact]
    public void RecordBlocked_NullActivity_ShouldNotThrow()
    {
        DataResidencyDiagnostics.RecordBlocked(null, "test reason");
    }

    [Fact]
    public void RecordSkipped_NullActivity_ShouldNotThrow()
    {
        DataResidencyDiagnostics.RecordSkipped(null);
    }

    [Fact]
    public void RecordWarning_NullActivity_ShouldNotThrow()
    {
        DataResidencyDiagnostics.RecordWarning(null, "test warning");
    }

    [Fact]
    public void RecordFailed_NullActivity_ShouldNotThrow()
    {
        DataResidencyDiagnostics.RecordFailed(null, "test failure");
    }

    // ---- Tag constants ----

    [Fact]
    public void TagConstants_ShouldHaveCorrectValues()
    {
        DataResidencyDiagnostics.TagOutcome.ShouldBe("residency.outcome");
        DataResidencyDiagnostics.TagRequestType.ShouldBe("residency.request_type");
        DataResidencyDiagnostics.TagResponseType.ShouldBe("residency.response_type");
        DataResidencyDiagnostics.TagDataCategory.ShouldBe("residency.data_category");
        DataResidencyDiagnostics.TagSourceRegion.ShouldBe("residency.source_region");
        DataResidencyDiagnostics.TagTargetRegion.ShouldBe("residency.target_region");
        DataResidencyDiagnostics.TagEnforcementMode.ShouldBe("residency.enforcement_mode");
        DataResidencyDiagnostics.TagLegalBasis.ShouldBe("residency.legal_basis");
        DataResidencyDiagnostics.TagAction.ShouldBe("residency.action");
        DataResidencyDiagnostics.TagFailureReason.ShouldBe("residency.failure_reason");
    }

    // ---- Activity source with listener ----

    [Fact]
    public void StartPipelineExecution_WithListener_ShouldCreateActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DataResidencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var activity = DataResidencyDiagnostics.StartPipelineExecution("TestReq", "TestResp");
        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Residency.Pipeline");
        activity.Dispose();
    }

    [Fact]
    public void StartTransferValidation_WithListener_ShouldCreateActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DataResidencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var activity = DataResidencyDiagnostics.StartTransferValidation("DE", "US");
        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Residency.TransferValidation");
        activity.Dispose();
    }

    [Fact]
    public void StartLocationRecord_WithListener_ShouldCreateActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DataResidencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var activity = DataResidencyDiagnostics.StartLocationRecord("entity-1", "DE");
        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Residency.LocationRecord");
        activity.Dispose();
    }
}
