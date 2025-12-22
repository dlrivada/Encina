using System.Diagnostics;
using Shouldly;

namespace Encina.Tests;

public sealed class MediatorDiagnosticsTests
{
    [Fact]
    public void ActivitySource_ExposesStableMetadata()
    {
        MediatorDiagnostics.ActivitySource.Name.ShouldBe("Encina");
        MediatorDiagnostics.ActivitySource.Version.ShouldBe("1.0");
    }

    [Fact]
    public void SendStarted_EmitsActivityWithRequestTag()
    {
        using var listener = CreateListener();

        var activity = MediatorDiagnostics.SendStarted(typeof(MediatorDiagnosticsTests), typeof(string), "request");

        activity.ShouldNotBeNull();
        activity!.DisplayName.ShouldBe("Encina.Send");
        activity.GetTagItem("mediator.request_type").ShouldBe(typeof(MediatorDiagnosticsTests).FullName);
        activity.GetTagItem("mediator.request_name").ShouldBe(nameof(MediatorDiagnosticsTests));
        activity.GetTagItem("mediator.response_type").ShouldBe(typeof(string).FullName);
        activity.GetTagItem("mediator.request_kind").ShouldBe("request");

        MediatorDiagnostics.SendCompleted(activity, isSuccess: true);
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void SendCompleted_SetsErrorStatus()
    {
        using var listener = CreateListener();

        var activity = MediatorDiagnostics.SendStarted(typeof(MediatorDiagnosticsTests), typeof(string), "request");
        activity.ShouldNotBeNull();

        MediatorDiagnostics.SendCompleted(activity, isSuccess: false, errorCode: "handler_missing", errorMessage: "failure");

        activity!.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("failure");
        activity.GetTagItem("mediator.failure_reason").ShouldBe("handler_missing");
    }

    private static ActivityListener CreateListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
