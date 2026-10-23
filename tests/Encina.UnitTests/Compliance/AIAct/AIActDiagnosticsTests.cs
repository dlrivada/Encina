using System.Diagnostics;

using Encina.Compliance.AIAct.Diagnostics;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="AIActDiagnostics"/>: the activity helpers set the documented tags and status on a
/// started activity and are no-ops on a null activity. A listener is attached to the AI Act activity source so
/// activities are really started; each test asserts only on the activity it created.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "AIAct")]
public sealed class AIActDiagnosticsTests : IDisposable
{
    private readonly ActivityListener _listener = new()
    {
        ShouldListenTo = source => source.Name == AIActDiagnostics.SourceName,
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
    };

    public AIActDiagnosticsTests() => ActivitySource.AddActivityListener(_listener);

    public void Dispose() => _listener.Dispose();

    private static string UniqueRequestName() => $"Request-{Guid.NewGuid():N}";

    private static object? Tag(Activity activity, string key) => activity.GetTagItem(key);

    [Fact]
    public void StartComplianceCheck_WithListener_StartsActivityWithRequestAndModeTags()
    {
        var requestName = UniqueRequestName();

        using var activity = AIActDiagnostics.StartComplianceCheck(requestName, "Block");

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("AIAct.ComplianceCheck");
        activity.Kind.ShouldBe(ActivityKind.Internal);
        Tag(activity, AIActDiagnostics.TagRequestType).ShouldBe(requestName);
        Tag(activity, AIActDiagnostics.TagEnforcementMode).ShouldBe("Block");
    }

    [Fact]
    public void RecordPassed_StartedActivity_SetsOutcomePassedAndOkStatus()
    {
        using var activity = AIActDiagnostics.StartComplianceCheck(UniqueRequestName(), "Warn");
        activity.ShouldNotBeNull();

        AIActDiagnostics.RecordPassed(activity);

        Tag(activity, AIActDiagnostics.TagOutcome).ShouldBe("passed");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordFailed_StartedActivity_SetsOutcomeReasonAndErrorStatus()
    {
        using var activity = AIActDiagnostics.StartComplianceCheck(UniqueRequestName(), "Block");
        activity.ShouldNotBeNull();

        AIActDiagnostics.RecordFailed(activity, "aiact.prohibited_use");

        Tag(activity, AIActDiagnostics.TagOutcome).ShouldBe("failed");
        Tag(activity, AIActDiagnostics.TagFailureReason).ShouldBe("aiact.prohibited_use");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("aiact.prohibited_use");
    }

    [Fact]
    public void RecordSkipped_StartedActivity_SetsOutcomeSkippedAndOkStatus()
    {
        using var activity = AIActDiagnostics.StartComplianceCheck(UniqueRequestName(), "Block");
        activity.ShouldNotBeNull();

        AIActDiagnostics.RecordSkipped(activity);

        Tag(activity, AIActDiagnostics.TagOutcome).ShouldBe("skipped");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void SetRiskLevelAndSetSystemId_StartedActivity_SetTags()
    {
        using var activity = AIActDiagnostics.StartComplianceCheck(UniqueRequestName(), "Block");
        activity.ShouldNotBeNull();

        AIActDiagnostics.SetRiskLevel(activity, "HighRisk");
        AIActDiagnostics.SetSystemId(activity, "cv-screener");

        Tag(activity, AIActDiagnostics.TagRiskLevel).ShouldBe("HighRisk");
        Tag(activity, AIActDiagnostics.TagSystemId).ShouldBe("cv-screener");
    }

    [Fact]
    public void Helpers_NullActivity_DoNotThrow()
    {
        Should.NotThrow(() =>
        {
            AIActDiagnostics.RecordPassed(null);
            AIActDiagnostics.RecordFailed(null, "reason");
            AIActDiagnostics.RecordSkipped(null);
            AIActDiagnostics.SetRiskLevel(null, "HighRisk");
            AIActDiagnostics.SetSystemId(null, "system");
        });
    }

    [Fact]
    public void Instruments_AreNamedUnderTheAIActMeter()
    {
        AIActDiagnostics.Meter.Name.ShouldBe(AIActDiagnostics.SourceName);
        AIActDiagnostics.ComplianceCheckTotal.Name.ShouldBe("aiact.compliance_check.total");
        AIActDiagnostics.ComplianceCheckDuration.Unit.ShouldBe("ms");
    }
}
