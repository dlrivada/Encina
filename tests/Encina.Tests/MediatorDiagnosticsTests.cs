using System.Diagnostics;
using Shouldly;

namespace Encina.Tests;

public sealed class EncinaDiagnosticsTests
{
    [Fact]
    public void ActivitySource_ExposesStableMetadata()
    {
        EncinaDiagnostics.ActivitySource.Name.ShouldBe("Encina");
        EncinaDiagnostics.ActivitySource.Version.ShouldBe("1.0");
    }

    [Fact]
    public void SendStarted_EmitsActivityWithRequestTag()
    {
        using var listener = CreateListener();

        var activity = EncinaDiagnostics.SendStarted(typeof(EncinaDiagnosticsTests), typeof(string), "request");

        activity.ShouldNotBeNull();
        activity!.DisplayName.ShouldBe("Encina.Send");
        activity.GetTagItem("Encina.request_type").ShouldBe(typeof(EncinaDiagnosticsTests).FullName);
        activity.GetTagItem("Encina.request_name").ShouldBe(nameof(EncinaDiagnosticsTests));
        activity.GetTagItem("Encina.response_type").ShouldBe(typeof(string).FullName);
        activity.GetTagItem("Encina.request_kind").ShouldBe("request");

        EncinaDiagnostics.SendCompleted(activity, isSuccess: true);
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void SendCompleted_SetsErrorStatus()
    {
        using var listener = CreateListener();

        var activity = EncinaDiagnostics.SendStarted(typeof(EncinaDiagnosticsTests), typeof(string), "request");
        activity.ShouldNotBeNull();

        EncinaDiagnostics.SendCompleted(activity, isSuccess: false, errorCode: "handler_missing", errorMessage: "failure");

        activity!.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("failure");
        activity.GetTagItem("Encina.failure_reason").ShouldBe("handler_missing");
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
