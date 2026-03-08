using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Security.ABAC.Diagnostics;

using FluentAssertions;

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
        ABACDiagnostics.SourceName.Should().Be("Encina.Security.ABAC");
    }

    [Fact]
    public void SourceVersion_Is1_0()
    {
        ABACDiagnostics.SourceVersion.Should().Be("1.0");
    }

    #endregion

    #region ActivitySource

    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        ABACDiagnostics.ActivitySource.Name.Should().Be("Encina.Security.ABAC");
    }

    [Fact]
    public void ActivitySource_HasCorrectVersion()
    {
        ABACDiagnostics.ActivitySource.Version.Should().Be("1.0");
    }

    #endregion

    #region Meter

    [Fact]
    public void Meter_HasCorrectName()
    {
        ABACDiagnostics.Meter.Name.Should().Be("Encina.Security.ABAC");
    }

    #endregion

    #region Counters Exist

    [Fact]
    public void EvaluationTotal_IsNotNull()
    {
        ABACDiagnostics.EvaluationTotal.Should().NotBeNull();
    }

    [Fact]
    public void EvaluationPermitted_IsNotNull()
    {
        ABACDiagnostics.EvaluationPermitted.Should().NotBeNull();
    }

    [Fact]
    public void EvaluationDenied_IsNotNull()
    {
        ABACDiagnostics.EvaluationDenied.Should().NotBeNull();
    }

    [Fact]
    public void EvaluationNotApplicable_IsNotNull()
    {
        ABACDiagnostics.EvaluationNotApplicable.Should().NotBeNull();
    }

    [Fact]
    public void EvaluationIndeterminate_IsNotNull()
    {
        ABACDiagnostics.EvaluationIndeterminate.Should().NotBeNull();
    }

    [Fact]
    public void ObligationExecuted_IsNotNull()
    {
        ABACDiagnostics.ObligationExecuted.Should().NotBeNull();
    }

    [Fact]
    public void ObligationFailed_IsNotNull()
    {
        ABACDiagnostics.ObligationFailed.Should().NotBeNull();
    }

    [Fact]
    public void ObligationNoHandler_IsNotNull()
    {
        ABACDiagnostics.ObligationNoHandler.Should().NotBeNull();
    }

    [Fact]
    public void AdviceExecuted_IsNotNull()
    {
        ABACDiagnostics.AdviceExecuted.Should().NotBeNull();
    }

    #endregion

    #region Histograms Exist

    [Fact]
    public void EvaluationDuration_IsNotNull()
    {
        ABACDiagnostics.EvaluationDuration.Should().NotBeNull();
    }

    [Fact]
    public void ObligationDuration_IsNotNull()
    {
        ABACDiagnostics.ObligationDuration.Should().NotBeNull();
    }

    #endregion

    #region Tag Constants

    [Fact]
    public void TagRequestType_HasCorrectValue()
    {
        ABACDiagnostics.TagRequestType.Should().Be("abac.request_type");
    }

    [Fact]
    public void TagEffect_HasCorrectValue()
    {
        ABACDiagnostics.TagEffect.Should().Be("abac.effect");
    }

    [Fact]
    public void TagPolicyId_HasCorrectValue()
    {
        ABACDiagnostics.TagPolicyId.Should().Be("abac.policy_id");
    }

    [Fact]
    public void TagEnforcementMode_HasCorrectValue()
    {
        ABACDiagnostics.TagEnforcementMode.Should().Be("abac.enforcement_mode");
    }

    [Fact]
    public void TagObligationId_HasCorrectValue()
    {
        ABACDiagnostics.TagObligationId.Should().Be("abac.obligation_id");
    }

    [Fact]
    public void TagAdviceId_HasCorrectValue()
    {
        ABACDiagnostics.TagAdviceId.Should().Be("abac.advice_id");
    }

    #endregion

    #region StartEvaluation

    [Fact]
    public void StartEvaluation_NoListeners_ReturnsNull()
    {
        // Without an ActivityListener, no activity is created
        var activity = ABACDiagnostics.StartEvaluation("TestRequest");

        activity.Should().BeNull("no listeners are registered");
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

        activity.Should().NotBeNull();
        activity!.GetTagItem(ABACDiagnostics.TagRequestType).Should().Be("MyRequest");
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

        activity.Should().NotBeNull();
        activity!.GetTagItem(ABACDiagnostics.TagEffect).Should().Be("permit");
        activity.GetTagItem(ABACDiagnostics.TagPolicyId).Should().Be("policy-1");
        activity.Status.Should().Be(ActivityStatusCode.Ok);
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

        activity.Should().NotBeNull();
        activity!.GetTagItem(ABACDiagnostics.TagEffect).Should().Be("deny");
        activity.GetTagItem(ABACDiagnostics.TagPolicyId).Should().Be("policy-deny");
        activity.Status.Should().Be(ActivityStatusCode.Error);
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

        activity.Should().NotBeNull();
        activity!.GetTagItem(ABACDiagnostics.TagEffect).Should().Be("indeterminate");
        activity.Status.Should().Be(ActivityStatusCode.Error);
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

        activity.Should().NotBeNull();
        activity!.GetTagItem(ABACDiagnostics.TagEffect).Should().Be("not_applicable");
        activity.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordPermitted_NullActivity_DoesNotThrow()
    {
        var act = () => ABACDiagnostics.RecordPermitted(null, "policy-1");

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordDenied_NullActivity_DoesNotThrow()
    {
        var act = () => ABACDiagnostics.RecordDenied(null, "policy-1", "denied");

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordIndeterminate_NullActivity_DoesNotThrow()
    {
        var act = () => ABACDiagnostics.RecordIndeterminate(null, "error");

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordNotApplicable_NullActivity_DoesNotThrow()
    {
        var act = () => ABACDiagnostics.RecordNotApplicable(null);

        act.Should().NotThrow();
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

        act.Should().NotThrow();
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

        act.Should().NotThrow();
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

        recordedValue.Should().Be(1);
    }

    #endregion
}
