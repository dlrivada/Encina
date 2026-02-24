using System.Diagnostics;
using Encina.Compliance.GDPR.Diagnostics;
using FluentAssertions;

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
        ProcessingActivityDiagnostics.SourceName.Should().Be("Encina.Compliance.GDPR.ProcessingActivity");
    }

    [Fact]
    public void SourceVersion_ShouldBe1_0()
    {
        ProcessingActivityDiagnostics.SourceVersion.Should().Be("1.0");
    }

    // -- StartRegistration --

    [Fact]
    public void StartRegistration_WithListener_ShouldCreateActivity()
    {
        // Act
        using var activity = ProcessingActivityDiagnostics.StartRegistration(typeof(string));

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("ProcessingActivity.Register");
    }

    [Fact]
    public void StartRegistration_ShouldSetRequestTypeTag()
    {
        // Act
        using var activity = ProcessingActivityDiagnostics.StartRegistration(typeof(string));

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(ProcessingActivityDiagnostics.TagRequestType).Should().Be("String");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagOperation).Should().Be("register");
    }

    // -- StartUpdate --

    [Fact]
    public void StartUpdate_WithListener_ShouldCreateActivity()
    {
        // Act
        using var activity = ProcessingActivityDiagnostics.StartUpdate(typeof(int));

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("ProcessingActivity.Update");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagOperation).Should().Be("update");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagRequestType).Should().Be("Int32");
    }

    // -- StartGetByRequestType --

    [Fact]
    public void StartGetByRequestType_WithListener_ShouldCreateActivity()
    {
        // Act
        using var activity = ProcessingActivityDiagnostics.StartGetByRequestType(typeof(double));

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("ProcessingActivity.GetByRequestType");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagOperation).Should().Be("get_by_request_type");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagRequestType).Should().Be("Double");
    }

    // -- StartGetAll --

    [Fact]
    public void StartGetAll_WithListener_ShouldCreateActivity()
    {
        // Act
        using var activity = ProcessingActivityDiagnostics.StartGetAll();

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("ProcessingActivity.GetAll");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagOperation).Should().Be("get_all");
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
        activity.Should().NotBeNull();
        activity!.GetTagItem(ProcessingActivityDiagnostics.TagOutcome).Should().Be("success");
        activity.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordSuccess_WithCount_ShouldSetCountTag()
    {
        // Arrange
        using var activity = ProcessingActivityDiagnostics.StartGetAll();

        // Act
        ProcessingActivityDiagnostics.RecordSuccess(activity, 5, "get_all");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(ProcessingActivityDiagnostics.TagOutcome).Should().Be("success");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagActivityCount).Should().Be(5);
        activity.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordSuccess_NullActivity_ShouldNotThrow()
    {
        // Act & Assert — counters still fire but no activity tags set
        var act = () => ProcessingActivityDiagnostics.RecordSuccess(null, "register");
        act.Should().NotThrow();
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
        activity.Should().NotBeNull();
        activity!.GetTagItem(ProcessingActivityDiagnostics.TagOutcome).Should().Be("failed");
        activity.GetTagItem(ProcessingActivityDiagnostics.TagFailureReason).Should().Be("Duplicate request type");
        activity.Status.Should().Be(ActivityStatusCode.Error);
    }

    [Fact]
    public void RecordFailure_NullReason_ShouldSetUnknown()
    {
        // Arrange
        using var activity = ProcessingActivityDiagnostics.StartRegistration(typeof(string));

        // Act
        ProcessingActivityDiagnostics.RecordFailure(activity, "register");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(ProcessingActivityDiagnostics.TagFailureReason).Should().Be("unknown");
    }

    [Fact]
    public void RecordFailure_NullActivity_ShouldNotThrow()
    {
        // Act & Assert — counters still fire but no activity tags set
        var act = () => ProcessingActivityDiagnostics.RecordFailure(null, "register", "reason");
        act.Should().NotThrow();
    }

    // -- Counter existence --

    [Fact]
    public void OperationsTotal_ShouldExist()
    {
        ProcessingActivityDiagnostics.OperationsTotal.Should().NotBeNull();
    }

    [Fact]
    public void OperationsFailedTotal_ShouldExist()
    {
        ProcessingActivityDiagnostics.OperationsFailedTotal.Should().NotBeNull();
    }

    // -- Tag constants --

    [Fact]
    public void TagConstants_ShouldHaveExpectedValues()
    {
        ProcessingActivityDiagnostics.TagOperation.Should().Be("operation");
        ProcessingActivityDiagnostics.TagOutcome.Should().Be("outcome");
        ProcessingActivityDiagnostics.TagRequestType.Should().Be("request.type");
        ProcessingActivityDiagnostics.TagActivityCount.Should().Be("activity.count");
        ProcessingActivityDiagnostics.TagFailureReason.Should().Be("failure_reason");
    }

    [Fact]
    public void TagProvider_ShouldExist()
    {
        ProcessingActivityDiagnostics.TagProvider.Should().Be("provider");
    }
}
