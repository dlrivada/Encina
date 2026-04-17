using System.Diagnostics;
using Encina.Compliance.GDPR.Diagnostics;
using Shouldly;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="ProcessingActivityDiagnostics"/>.
/// </summary>
public class ProcessingActivityDiagnosticsTests : IDisposable
{
    private readonly ActivityListener _listener;

    public ProcessingActivityDiagnosticsTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ProcessingActivityDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    // -- Source configuration --

    [Fact]
    public void SourceName_ShouldBeDedicatedProcessingActivitySource()
    {
        ProcessingActivityDiagnostics.SourceName.ShouldBe("Encina.Compliance.GDPR.ProcessingActivity");
    }

    [Fact]
    public void SourceVersion_ShouldBe1_0()
    {
        ProcessingActivityDiagnostics.SourceVersion.ShouldBe("1.0");
    }

    // -- StartRegistration --

    [Fact]
    public void StartRegistration_WithListener_ShouldCreateActivity()
    {
        // Act
        using var activity = ProcessingActivityDiagnostics.StartRegistration(typeof(string));

        // Assert
        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("ProcessingActivity.Register");
    }

    [Fact]
    public void StartRegistration_ShouldSetRequestTypeTag()
    {
        // Act
        using var activity = ProcessingActivityDiagnostics.StartRegistration(typeof(string));

        // Assert
        activity.ShouldNotBeNull();
        activity!.GetTagItem(ProcessingActivityDiagnostics.TagRequestType).ShouldBe("String");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagOperation).ShouldBe("register");
    }

    // -- StartUpdate --

    [Fact]
    public void StartUpdate_WithListener_ShouldCreateActivity()
    {
        // Act
        using var activity = ProcessingActivityDiagnostics.StartUpdate(typeof(int));

        // Assert
        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("ProcessingActivity.Update");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagOperation).ShouldBe("update");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagRequestType).ShouldBe("Int32");
    }

    // -- StartGetByRequestType --

    [Fact]
    public void StartGetByRequestType_WithListener_ShouldCreateActivity()
    {
        // Act
        using var activity = ProcessingActivityDiagnostics.StartGetByRequestType(typeof(double));

        // Assert
        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("ProcessingActivity.GetByRequestType");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagOperation).ShouldBe("get_by_request_type");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagRequestType).ShouldBe("Double");
    }

    // -- StartGetAll --

    [Fact]
    public void StartGetAll_WithListener_ShouldCreateActivity()
    {
        // Act
        using var activity = ProcessingActivityDiagnostics.StartGetAll();

        // Assert
        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("ProcessingActivity.GetAll");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagOperation).ShouldBe("get_all");
    }

    // -- RecordSuccess --

    [Fact]
    public void RecordSuccess_ShouldSetOutcomeTagAndStatusOk()
    {
        // Arrange
        using var activity = ProcessingActivityDiagnostics.StartRegistration(typeof(string));

        // Act
        ProcessingActivityDiagnostics.RecordSuccess(activity, "register");

        // Assert
        activity.ShouldNotBeNull();
        activity!.GetTagItem(ProcessingActivityDiagnostics.TagOutcome).ShouldBe("success");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordSuccess_WithCount_ShouldSetCountTag()
    {
        // Arrange
        using var activity = ProcessingActivityDiagnostics.StartGetAll();

        // Act
        ProcessingActivityDiagnostics.RecordSuccess(activity, 5, "get_all");

        // Assert
        activity.ShouldNotBeNull();
        activity!.GetTagItem(ProcessingActivityDiagnostics.TagOutcome).ShouldBe("success");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagActivityCount).ShouldBe(5);
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordSuccess_NullActivity_ShouldNotThrow()
    {
        // Act & Assert — counters still fire but no activity tags set
        var act = () => ProcessingActivityDiagnostics.RecordSuccess(null, "register");
        Should.NotThrow(act);
    }

    // -- RecordFailure --

    [Fact]
    public void RecordFailure_ShouldSetOutcomeTagAndStatusError()
    {
        // Arrange
        using var activity = ProcessingActivityDiagnostics.StartRegistration(typeof(string));

        // Act
        ProcessingActivityDiagnostics.RecordFailure(activity, "register", "Duplicate request type");

        // Assert
        activity.ShouldNotBeNull();
        activity!.GetTagItem(ProcessingActivityDiagnostics.TagOutcome).ShouldBe("failed");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagFailureReason).ShouldBe("Duplicate request type");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public void RecordFailure_NullReason_ShouldSetUnknown()
    {
        // Arrange
        using var activity = ProcessingActivityDiagnostics.StartRegistration(typeof(string));

        // Act
        ProcessingActivityDiagnostics.RecordFailure(activity, "register");

        // Assert
        activity.ShouldNotBeNull();
        activity!.GetTagItem(ProcessingActivityDiagnostics.TagFailureReason).ShouldBe("unknown");
    }

    [Fact]
    public void RecordFailure_NullActivity_ShouldNotThrow()
    {
        // Act & Assert — counters still fire but no activity tags set
        var act = () => ProcessingActivityDiagnostics.RecordFailure(null, "register", "reason");
        Should.NotThrow(act);
    }

    // -- Counter existence --

    [Fact]
    public void OperationsTotal_ShouldExist()
    {
        ProcessingActivityDiagnostics.OperationsTotal.ShouldNotBeNull();
    }

    [Fact]
    public void OperationsFailedTotal_ShouldExist()
    {
        ProcessingActivityDiagnostics.OperationsFailedTotal.ShouldNotBeNull();
    }

    // -- Tag constants --

    [Fact]
    public void TagConstants_ShouldHaveExpectedValues()
    {
        ProcessingActivityDiagnostics.TagOperation.ShouldBe("operation");
        ProcessingActivityDiagnostics.TagOutcome.ShouldBe("outcome");
        ProcessingActivityDiagnostics.TagRequestType.ShouldBe("request.type");
        ProcessingActivityDiagnostics.TagActivityCount.ShouldBe("activity.count");
        ProcessingActivityDiagnostics.TagFailureReason.ShouldBe("failure_reason");
    }

    [Fact]
    public void TagProvider_ShouldExist()
    {
        ProcessingActivityDiagnostics.TagProvider.ShouldBe("provider");
    }
}
