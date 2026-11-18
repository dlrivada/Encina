using System.Security.Claims;
using Encina.AzureFunctions;
using Encina.AzureFunctions.Durable;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Testing;
using EncinaExtensions = Encina.AzureFunctions.FunctionContextExtensions;

namespace Encina.UnitTests.AzureFunctions;

/// <summary>
/// The categories of the errors this package produces (ADR-062) and the round trip of a category through
/// <see cref="ActivityResult{T}"/>.
/// </summary>
public class AzureFunctionsErrorCategoriesTests
{
    private static EncinaError LeftOf<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => throw new InvalidOperationException("Expected a Left."), Left: e => e);

    [Fact]
    public void ResolveTenantId_MismatchingHeader_IsForbidden()
    {
        // Arrange
        var context = Substitute.For<FunctionContext>();
        var request = Substitute.For<HttpRequestData>(context);
        var headers = new HttpHeadersCollection { { "X-Tenant-ID", "tenant-b" } };
        request.Headers.Returns(headers);
        request.Identities.Returns([new ClaimsIdentity([new Claim("tenant_id", "tenant-a")])]);

        // Act
        var error = LeftOf(context.ResolveTenantId(request, new EncinaAzureFunctionsOptions(), new FakeLogger()));

        // Assert
        error.GetCode().IfNone(string.Empty).ShouldBe(EncinaExtensions.TenantMismatchErrorCode);
        error.GetCategory().ShouldBe(ErrorCategory.Forbidden);
        error.GetStatusCode().ShouldBe(403);
    }

    [Fact]
    public void ResolveTenantId_EmptyClaim_IsForbidden()
    {
        // Arrange
        var context = Substitute.For<FunctionContext>();
        var request = Substitute.For<HttpRequestData>(context);
        request.Headers.Returns(new HttpHeadersCollection());
        request.Identities.Returns([new ClaimsIdentity([new Claim("tenant_id", string.Empty)])]);

        // Act
        var error = LeftOf(context.ResolveTenantId(request, new EncinaAzureFunctionsOptions(), new FakeLogger()));

        // Assert
        error.GetCode().IfNone(string.Empty).ShouldBe(EncinaExtensions.InvalidTenantClaimErrorCode);
        error.GetCategory().ShouldBe(ErrorCategory.Forbidden);
    }

    [Fact]
    public void SagaBuilder_BuildWithoutSteps_IsInternal()
    {
        // Act
        var error = LeftOf(new DurableSagaBuilder<object>().Build());

        // Assert
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
        error.GetStatusCode().ShouldBe(500);
    }

    [Fact]
    public void SagaBuilder_StepWithoutExecute_IsInternal()
    {
        // Act
        var error = LeftOf(new DurableSagaBuilder<object>().Step("pay").Build());

        // Assert
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    private static void Activity(TaskOrchestrationContext context, string name, ActivityResult<string> result) =>
        context.CallActivityAsync<ActivityResult<string>>(
                Arg.Is<TaskName>(n => n.Name == name), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.FromResult(result));

    private static void ActivityThrows(TaskOrchestrationContext context, string name, Exception exception) =>
        context.CallActivityAsync<ActivityResult<string>>(
                Arg.Is<TaskName>(n => n.Name == name), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.FromException<ActivityResult<string>>(exception));

    private static DurableSaga<string> TwoStepSaga() =>
        new DurableSagaBuilder<string>()
            .Step("first").Execute("First").Compensate("UndoFirst")
            .Step("second").Execute("Second").Compensate("UndoSecond")
            .Build()
            .Match(Right: s => s, Left: e => throw new InvalidOperationException(e.GetCode().IfNone(string.Empty)));

    [Fact]
    public async Task SagaExecute_AllStepsSucceed_ReturnsTheData()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        Activity(context, "First", ActivityResult<string>.Success("one"));
        Activity(context, "Second", ActivityResult<string>.Success("two"));

        // Act
        var result = await TwoStepSaga().ExecuteAsync(context, "start");

        // Assert
        result.Match(Right: d => d, Left: _ => "<left>").ShouldBe("two");
    }

    [Fact]
    public async Task SagaExecute_StepFailsWithACategory_KeepsItAndCompensatesThePreviousStep()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        Activity(context, "First", ActivityResult<string>.Success("one"));
        Activity(
            context,
            "Second",
            ActivityResult<string>.Failure(EncinaErrors.Create("order.not_found", ErrorCategory.NotFound, "m")));
        Activity(context, "UndoFirst", ActivityResult<string>.Success("undone"));

        // Act
        var result = await TwoStepSaga().ExecuteAsync(context, "start");

        // Assert
        var failure = result.Match(Right: _ => throw new InvalidOperationException(), Left: e => e);
        failure.FailedStep.ShouldBe("second");
        failure.OriginalError.GetStatusCode().ShouldBe(404);
        failure.WasCompensated.ShouldBeTrue();
        failure.CompensationErrors.Keys.ShouldBe(["first"]);
    }

    [Fact]
    public async Task SagaExecute_CompensationReturnsAnError_IsNotCompensated()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        Activity(context, "First", ActivityResult<string>.Success("one"));
        Activity(context, "Second", ActivityResult<string>.Failure("order.rejected", "m"));
        Activity(context, "UndoFirst", ActivityResult<string>.Failure("undo.failed", "m"));

        // Act
        var result = await TwoStepSaga().ExecuteAsync(context, "start");

        // Assert
        var failure = result.Match(Right: _ => throw new InvalidOperationException(), Left: e => e);
        failure.WasCompensated.ShouldBeFalse();
        failure.CompensationErrors["first"].ShouldNotBeNull();
    }

    [Fact]
    public async Task SagaExecute_ActivityThrows_IsAnInternalSagaExceptionAndCompensates()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        Activity(context, "First", ActivityResult<string>.Success("one"));
        ActivityThrows(context, "Second", new InvalidOperationException("boom"));
        Activity(context, "UndoFirst", ActivityResult<string>.Success("undone"));

        // Act
        var result = await TwoStepSaga().ExecuteAsync(context, "start");

        // Assert
        var failure = result.Match(Right: _ => throw new InvalidOperationException(), Left: e => e);
        failure.FailedStep.ShouldBe("Unknown (exception)");
        failure.OriginalError.GetCode().IfNone(string.Empty).ShouldBe("durable.saga_exception");
        failure.OriginalError.GetCategory().ShouldBe(ErrorCategory.Internal);
        failure.CompensationErrors.Keys.ShouldBe(["first"]);
    }

    [Fact]
    public async Task SagaExecute_CompensationThrows_IsAnInternalCompensationException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        Activity(context, "First", ActivityResult<string>.Success("one"));
        Activity(context, "Second", ActivityResult<string>.Failure("order.rejected", "m"));
        ActivityThrows(context, "UndoFirst", new InvalidOperationException("boom"));

        // Act
        var result = await TwoStepSaga().ExecuteAsync(context, "start");

        // Assert
        var failure = result.Match(Right: _ => throw new InvalidOperationException(), Left: e => e);
        var compensation = failure.CompensationErrors["first"]!.Value;
        compensation.GetCode().IfNone(string.Empty).ShouldBe("durable.compensation_exception");
        compensation.GetCategory().ShouldBe(ErrorCategory.Internal);
        failure.WasCompensated.ShouldBeFalse();
    }

    [Fact]
    public async Task SagaExecute_StepWithoutCompensation_IsNotCompensated()
    {
        // Arrange
        var saga = new DurableSagaBuilder<string>()
            .Step("only").Execute("Only")
            .Build()
            .Match(Right: s => s, Left: e => throw new InvalidOperationException(e.GetCode().IfNone(string.Empty)));
        var context = Substitute.For<TaskOrchestrationContext>();
        Activity(context, "Only", ActivityResult<string>.Failure("order.rejected", "m"));

        // Act
        var result = await saga.ExecuteAsync(context, "start");

        // Assert
        var failure = result.Match(Right: _ => throw new InvalidOperationException(), Left: e => e);
        failure.CompensationErrors.ShouldBeEmpty();
        failure.WasCompensated.ShouldBeTrue();
    }

    [Fact]
    public async Task WaitForEncinaExternalEventAsync_TimerWins_IsTimeout()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        context.CurrentUtcDateTime.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        context.WaitForExternalEvent<string>("approved", Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<string>().Task);
        context.CreateTimer(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        var result = await context.WaitForEncinaExternalEventAsync<string>("approved", TimeSpan.FromMinutes(1));

        // Assert
        var error = LeftOf(result);
        error.GetCode().IfNone(string.Empty).ShouldBe("durable.event_timeout");
        error.GetCategory().ShouldBe(ErrorCategory.Timeout);
        error.GetStatusCode().ShouldBe(504);
    }

    [Fact]
    public async Task WaitForEncinaExternalEventAsync_Cancelled_IsCancelled()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        context.WaitForExternalEvent<string>("approved", Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled<string>(new CancellationToken(canceled: true)));

        // Act
        var result = await context.WaitForEncinaExternalEventAsync<string>("approved");

        // Assert
        var error = LeftOf(result);
        error.GetCode().IfNone(string.Empty).ShouldBe("durable.event_cancelled");
        error.GetCategory().ShouldBe(ErrorCategory.Cancelled);
        error.GetStatusCode().ShouldBe(499);
    }

    [Theory]
    [InlineData(ErrorCategory.NotFound, 404)]
    [InlineData(ErrorCategory.Conflict, 409)]
    [InlineData(ErrorCategory.AlreadyExists, 409)]
    [InlineData(ErrorCategory.Forbidden, 403)]
    [InlineData(ErrorCategory.RateLimited, 429)]
    [InlineData(ErrorCategory.Cancelled, 499)]
    public void ActivityResult_RoundTrip_KeepsTheCategoryAndTheStatus(ErrorCategory category, int expectedStatus)
    {
        // Arrange
        Either<EncinaError, string> original = EncinaErrors.Create("order.some_code", category, "message");

        // Act
        var restored = LeftOf(original.ToActivityResult().ToEither());

        // Assert
        restored.GetCategory().ShouldBe(category);
        restored.GetStatusCode().ShouldBe(expectedStatus);
        restored.GetCode().IfNone(string.Empty).ShouldBe("order.some_code");
    }

    [Fact]
    public void ActivityResult_JsonRoundTrip_KeepsTheCategory()
    {
        // Arrange
        var error = EncinaErrors.Create("order.not_found", ErrorCategory.NotFound, "message");
        var activityResult = ActivityResult<string>.Failure(error);

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(activityResult);
        var restored = System.Text.Json.JsonSerializer.Deserialize<ActivityResult<string>>(json)!;

        // Assert
        restored.ErrorCategory.ShouldBe(ErrorCategory.NotFound);
        LeftOf(restored.ToEither()).GetStatusCode().ShouldBe(404);
    }

    [Fact]
    public void ActivityResult_FailureFromCodeAndMessage_IsInternal()
    {
        // Act
        var error = LeftOf(ActivityResult<string>.Failure("order.not_found", "message").ToEither());

        // Assert
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
        error.GetStatusCode().ShouldBe(500);
    }

    [Fact]
    public void ActivityResult_FailureWithNullCodeAndMessage_FallsBackToInternalActivityFailed()
    {
        // Arrange
        var activityResult = new ActivityResult<string> { IsSuccess = false };

        // Act
        var error = LeftOf(activityResult.ToEither());

        // Assert
        error.GetCode().IfNone(string.Empty).ShouldBe("durable.activity_failed");
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public void ActivityResult_Success_RestoresTheValue()
    {
        // Act
        var result = ActivityResult<string>.Success("ok").ToEither();

        // Assert
        result.IsRight.ShouldBeTrue();
    }
}
