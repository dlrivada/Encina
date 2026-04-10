using System.Diagnostics;
using System.Reflection;

using Encina.Compliance.LawfulBasis;

using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.UnitTests.Compliance.LawfulBasisModule.Diagnostics;

/// <summary>
/// Unit tests for the internal <c>LawfulBasisDiagnostics</c> class.
/// </summary>
/// <remarks>
/// Uses reflection to access the internal static type since the diagnostics are
/// intentionally internal. The goal of these tests is to execute the real code
/// paths — creating activities, setting tags, completing them — not to assert
/// against reflection results.
/// </remarks>
public class LawfulBasisDiagnosticsTests
{
    private static readonly Type DiagnosticsType =
        typeof(LawfulBasisEnforcementMode).Assembly
            .GetType("Encina.Compliance.LawfulBasis.Diagnostics.LawfulBasisDiagnostics")!;

    private static T InvokeStatic<T>(string methodName, params object?[] args)
    {
        var method = DiagnosticsType.GetMethod(methodName,
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        method.ShouldNotBeNull($"Method {methodName} not found");
        return (T)method.Invoke(null, args)!;
    }

    private static void InvokeStaticVoid(string methodName, params object?[] args)
    {
        var method = DiagnosticsType.GetMethod(methodName,
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        method.ShouldNotBeNull($"Method {methodName} not found");
        method.Invoke(null, args);
    }

    public sealed class SampleRequest { }

    [Fact]
    public void StartValidation_WithoutListener_ReturnsNull()
    {
        // No listener attached → should return null fast path
        var activity = InvokeStatic<Activity?>("StartValidation", typeof(SampleRequest));
        activity.ShouldBeNull();
    }

    [Fact]
    public void StartValidation_WithListener_ReturnsActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Compliance.LawfulBasis",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = _ => { },
            ActivityStopped = _ => { }
        };
        ActivitySource.AddActivityListener(listener);

        var activity = InvokeStatic<Activity?>("StartValidation", typeof(SampleRequest));

        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("LawfulBasisValidation");
        activity.Stop();
    }

    [Fact]
    public void CompleteValidation_NullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => InvokeStaticVoid("CompleteValidation", null, true, null));
        Should.NotThrow(() => InvokeStaticVoid("CompleteValidation", null, false, "reason"));
    }

    [Fact]
    public void CompleteValidation_SuccessfulActivity_SetsOkStatus()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Compliance.LawfulBasis",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        var activity = InvokeStatic<Activity?>("StartValidation", typeof(SampleRequest));
        activity.ShouldNotBeNull();

        InvokeStaticVoid("CompleteValidation", activity, true, null);

        activity!.Status.ShouldBe(ActivityStatusCode.Ok);
        activity.Stop();
    }

    [Fact]
    public void CompleteValidation_FailedActivity_SetsErrorStatus()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Compliance.LawfulBasis",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        var activity = InvokeStatic<Activity?>("StartValidation", typeof(SampleRequest));
        activity.ShouldNotBeNull();

        InvokeStaticVoid("CompleteValidation", activity, false, "custom_reason");

        activity!.Status.ShouldBe(ActivityStatusCode.Error);
        activity.Stop();
    }

    [Fact]
    public void CompleteValidation_FailedWithNullReason_UsesUnknown()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Compliance.LawfulBasis",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        var activity = InvokeStatic<Activity?>("StartValidation", typeof(SampleRequest));
        InvokeStaticVoid("CompleteValidation", activity, false, null);
        activity?.Stop();
    }

    [Fact]
    public void SetBasis_NullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => InvokeStaticVoid("SetBasis", null, GDPRLawfulBasis.Contract));
    }

    [Fact]
    public void SetBasis_WithActivity_SetsTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Compliance.LawfulBasis",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        var activity = InvokeStatic<Activity?>("StartValidation", typeof(SampleRequest));
        activity.ShouldNotBeNull();

        foreach (var basis in Enum.GetValues<GDPRLawfulBasis>())
        {
            InvokeStaticVoid("SetBasis", activity, basis);
        }

        activity!.Stop();
    }
}
