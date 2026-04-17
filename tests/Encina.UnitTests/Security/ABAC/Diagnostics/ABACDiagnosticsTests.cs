using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Security.ABAC.Diagnostics;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Diagnostics;

/// <summary>
/// Unit tests for <see cref="ABACDiagnostics"/>: verifies the ABAC observability
/// infrastructure including activity sources, meters, counters, and histograms.
/// </summary>
public sealed class ABACDiagnosticsTests
{
    #region Source Name and Version

    [Fact]
    public void SourceName_IsEncinaSecurityABAC()
    {
        ABACDiagnostics.SourceName.ShouldBe("Encina.Security.ABAC");
    }

    [Fact]
    public void SourceVersion_Is1_0()
    {
        ABACDiagnostics.SourceVersion.ShouldBe("1.0");
    }

    #endregion

    #region ActivitySource

    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        ABACDiagnostics.ActivitySource.Name.ShouldBe("Encina.Security.ABAC");
    }

    [Fact]
    public void ActivitySource_HasCorrectVersion()
    {
        ABACDiagnostics.ActivitySource.Version.ShouldBe("1.0");
    }

    #endregion

    #region Meter

    [Fact]
    public void Meter_HasCorrectName()
    {
        ABACDiagnostics.Meter.Name.ShouldBe("Encina.Security.ABAC");
    }

    #endregion

    #region Counters Exist

    [Fact]
    public void EvaluationTotal_IsNotNull()
    {
        ABACDiagnostics.EvaluationTotal.ShouldNotBeNull();
    }

    [Fact]
    public void EvaluationPermitted_IsNotNull()
    {
        ABACDiagnostics.EvaluationPermitted.ShouldNotBeNull();
    }

    [Fact]
    public void EvaluationDenied_IsNotNull()
    {
        ABACDiagnostics.EvaluationDenied.ShouldNotBeNull();
    }

    [Fact]
    public void EvaluationNotApplicable_IsNotNull()
    {
        ABACDiagnostics.EvaluationNotApplicable.ShouldNotBeNull();
    }

    [Fact]
    public void EvaluationIndeterminate_IsNotNull()
    {
        ABACDiagnostics.EvaluationIndeterminate.ShouldNotBeNull();
    }

    [Fact]
    public void ObligationExecuted_IsNotNull()
    {
        ABACDiagnostics.ObligationExecuted.ShouldNotBeNull();
    }

    [Fact]
    public void ObligationFailed_IsNotNull()
    {
        ABACDiagnostics.ObligationFailed.ShouldNotBeNull();
    }

    [Fact]
    public void ObligationNoHandler_IsNotNull()
    {
        ABACDiagnostics.ObligationNoHandler.ShouldNotBeNull();
    }

    [Fact]
    public void AdviceExecuted_IsNotNull()
    {
        ABACDiagnostics.AdviceExecuted.ShouldNotBeNull();
    }

    #endregion

    #region Histograms Exist

    [Fact]
    public void EvaluationDuration_IsNotNull()
    {
        ABACDiagnostics.EvaluationDuration.ShouldNotBeNull();
    }

    [Fact]
    public void ObligationDuration_IsNotNull()
    {
        ABACDiagnostics.ObligationDuration.ShouldNotBeNull();
    }

    #endregion

    #region Tag Constants

    [Fact]
    public void TagRequestType_HasCorrectValue()
    {
        ABACDiagnostics.TagRequestType.ShouldBe("abac.request_type");
    }

    [Fact]
    public void TagEffect_HasCorrectValue()
    {
        ABACDiagnostics.TagEffect.ShouldBe("abac.effect");
    }

    [Fact]
    public void TagPolicyId_HasCorrectValue()
    {
        ABACDiagnostics.TagPolicyId.ShouldBe("abac.policy_id");
    }

    [Fact]
    public void TagEnforcementMode_HasCorrectValue()
    {
        ABACDiagnostics.TagEnforcementMode.ShouldBe("abac.enforcement_mode");
    }

    [Fact]
    public void TagObligationId_HasCorrectValue()
    {
        ABACDiagnostics.TagObligationId.ShouldBe("abac.obligation_id");
    }

    [Fact]
    public void TagAdviceId_HasCorrectValue()
    {
        ABACDiagnostics.TagAdviceId.ShouldBe("abac.advice_id");
    }

    #endregion

    #region StartEvaluation

    [Fact]
    public void StartEvaluation_NoListeners_ReturnsNull()
    {
        // Without an ActivityListener, no activity is created
        var activity = ABACDiagnostics.StartEvaluation("TestRequest");

        activity.ShouldBeNull("no listeners are registered");
    }

    [Fact]
    public void StartEvaluation_WithListener_ReturnsActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ABACDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var activity = ABACDiagnostics.StartEvaluation("MyRequest");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(ABACDiagnostics.TagRequestType).ShouldBe("MyRequest");
        activity.Dispose();
    }

    #endregion

    #region Activity Recording Helpers

    [Fact]
    public void RecordPermitted_SetsCorrectTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ABACDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = ABACDiagnostics.StartEvaluation("TestRequest");
        ABACDiagnostics.RecordPermitted(activity, "policy-1");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(ABACDiagnostics.TagEffect).ShouldBe("permit");
        activity.GetTagItem(ABACDiagnostics.TagPolicyId).ShouldBe("policy-1");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordDenied_SetsCorrectTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ABACDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = ABACDiagnostics.StartEvaluation("TestRequest");
        ABACDiagnostics.RecordDenied(activity, "policy-deny", "access denied");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(ABACDiagnostics.TagEffect).ShouldBe("deny");
        activity.GetTagItem(ABACDiagnostics.TagPolicyId).ShouldBe("policy-deny");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public void RecordIndeterminate_SetsCorrectTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ABACDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = ABACDiagnostics.StartEvaluation("TestRequest");
        ABACDiagnostics.RecordIndeterminate(activity, "evaluation error");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(ABACDiagnostics.TagEffect).ShouldBe("indeterminate");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public void RecordNotApplicable_SetsCorrectTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ABACDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = ABACDiagnostics.StartEvaluation("TestRequest");
        ABACDiagnostics.RecordNotApplicable(activity);

        activity.ShouldNotBeNull();
        activity!.GetTagItem(ABACDiagnostics.TagEffect).ShouldBe("not_applicable");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordPermitted_NullActivity_DoesNotThrow()
    {
        var act = () => ABACDiagnostics.RecordPermitted(null, "policy-1");

        Should.NotThrow(act);
    }

    [Fact]
    public void RecordDenied_NullActivity_DoesNotThrow()
    {
        var act = () => ABACDiagnostics.RecordDenied(null, "policy-1", "denied");

        Should.NotThrow(act);
    }

    [Fact]
    public void RecordIndeterminate_NullActivity_DoesNotThrow()
    {
        var act = () => ABACDiagnostics.RecordIndeterminate(null, "error");

        Should.NotThrow(act);
    }

    [Fact]
    public void RecordNotApplicable_NullActivity_DoesNotThrow()
    {
        var act = () => ABACDiagnostics.RecordNotApplicable(null);

        Should.NotThrow(act);
    }

    #endregion

    #region Counter Metrics Collection

    [Fact]
    public void Counters_CanIncrementWithoutError()
    {
        // Verify that incrementing counters does not throw
        var tag = new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, "TestRequest");

        var act = () =>
        {
            ABACDiagnostics.EvaluationTotal.Add(1, tag);
            ABACDiagnostics.EvaluationPermitted.Add(1, tag);
            ABACDiagnostics.EvaluationDenied.Add(1, tag);
            ABACDiagnostics.EvaluationNotApplicable.Add(1, tag);
            ABACDiagnostics.EvaluationIndeterminate.Add(1, tag);
            ABACDiagnostics.ObligationExecuted.Add(1,
                new KeyValuePair<string, object?>(ABACDiagnostics.TagObligationId, "test-obligation"));
            ABACDiagnostics.ObligationFailed.Add(1,
                new KeyValuePair<string, object?>(ABACDiagnostics.TagObligationId, "test-obligation"));
            ABACDiagnostics.ObligationNoHandler.Add(1,
                new KeyValuePair<string, object?>(ABACDiagnostics.TagObligationId, "test-obligation"));
            ABACDiagnostics.AdviceExecuted.Add(1,
                new KeyValuePair<string, object?>(ABACDiagnostics.TagAdviceId, "test-advice"));
        };

        Should.NotThrow(act);
    }

    [Fact]
    public void Histograms_CanRecordWithoutError()
    {
        var act = () =>
        {
            ABACDiagnostics.EvaluationDuration.Record(1.5);
            ABACDiagnostics.ObligationDuration.Record(0.5,
                new KeyValuePair<string, object?>(ABACDiagnostics.TagObligationId, "test"));
        };

        Should.NotThrow(act);
    }

    #endregion

    #region MeterListener Verification

    [Fact]
    public void EvaluationTotal_MeterListener_RecordsIncrement()
    {
        long recordedValue = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == "abac.evaluation.total")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "abac.evaluation.total")
            {
                recordedValue += measurement;
            }
        });
        listener.Start();

        ABACDiagnostics.EvaluationTotal.Add(1,
            new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, "TestRequest"));

        listener.RecordObservableInstruments();

        recordedValue.ShouldBe(1);
    }

    #endregion
}
