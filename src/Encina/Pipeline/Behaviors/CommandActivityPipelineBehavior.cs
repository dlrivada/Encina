using System.Diagnostics;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// Creates diagnostic activities for commands and annotates functional failures.
/// </summary>
/// <typeparam name="TCommand">Command type being observed.</typeparam>
/// <typeparam name="TResponse">Response type returned by the handler.</typeparam>
/// <remarks>
/// Leverages <see cref="IFunctionalFailureDetector"/> to tag errors without depending on concrete
/// domain types. Activities are emitted with the <c>Encina</c> source ready for
/// OpenTelemetry.
/// </remarks>
/// <example>
/// <code>
/// services.AddSingleton&lt;IFunctionalFailureDetector, ApplicationFunctionalFailureDetector&gt;();
/// services.AddEncina(cfg =>
/// {
///     cfg.AddPipelineBehavior(typeof(CommandActivityPipelineBehavior&lt;,&gt;));
/// }, typeof(CreateReservation).Assembly);
/// </code>
/// </example>
/// <remarks>
/// Initializes the behavior with the functional failure detector to use.
/// </remarks>
/// <param name="failureDetector">Detector that interprets handler responses.</param>
public sealed class CommandActivityPipelineBehavior<TCommand, TResponse>(IFunctionalFailureDetector failureDetector) : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly IFunctionalFailureDetector _failureDetector = failureDetector ?? NullFunctionalFailureDetector.Instance;

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(TCommand request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
    {
        if (!EncinaBehaviorGuards.TryValidateRequest(GetType(), request, out var failure))
        {
            return Left<EncinaError, TResponse>(failure);
        }

        if (!EncinaBehaviorGuards.TryValidateNextStep(GetType(), nextStep, out failure))
        {
            return Left<EncinaError, TResponse>(failure);
        }

        using var activity = StartActivity();
        SetActivityTags(activity);

        var outcome = await ExecuteWithActivityTracking(activity, nextStep, cancellationToken).ConfigureAwait(false);

        if (outcome.IsLeft)
        {
            return outcome;
        }

        RecordOutcome(activity, outcome);
        return outcome;
    }

    private static Activity? StartActivity()
    {
        return EncinaDiagnostics.ActivitySource.HasListeners()
            ? EncinaDiagnostics.ActivitySource.StartActivity(string.Concat("Encina.Command.", typeof(TCommand).Name), ActivityKind.Internal)
            : null;
    }

    private static void SetActivityTags(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.RequestKind, "command");
        activity.SetTag(ActivityTagNames.RequestType, typeof(TCommand).FullName);
        activity.SetTag(ActivityTagNames.RequestName, typeof(TCommand).Name);
        activity.SetTag(ActivityTagNames.ResponseType, typeof(TResponse).FullName);
    }

    private async ValueTask<Either<EncinaError, TResponse>> ExecuteWithActivityTracking(
        Activity? activity,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        try
        {
            return await nextStep().ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            RecordCancellation(activity);
            return Left<EncinaError, TResponse>( // NOSONAR S6966: Left is a pure function, not an async operation
                EncinaErrors.Create(EncinaErrorCodes.BehaviorCancelled, $"Behavior {GetType().Name} cancelled the {typeof(TCommand).Name} request.", ex));
        }
        catch (Exception ex)
        {
            RecordException(activity, ex);
            return Left<EncinaError, TResponse>( // NOSONAR S6966: Left is a pure function, not an async operation
                EncinaErrors.FromException(EncinaErrorCodes.BehaviorException, ex, $"Error running {GetType().Name} for {typeof(TCommand).Name}."));
        }
    }

    private static void RecordCancellation(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
        activity?.SetTag(ActivityTagNames.Cancelled, true);
    }

    private static void RecordException(Activity? activity, Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.SetTag(ActivityTagNames.ExceptionType, ex.GetType().FullName);
        activity?.SetTag(ActivityTagNames.ExceptionMessage, ex.Message);
    }

    private void RecordOutcome(Activity? activity, Either<EncinaError, TResponse> outcome)
    {
        _ = outcome.Match(
            Right: response => RecordSuccessOutcome(activity, response),
            Left: error => RecordErrorOutcome(activity, error));
    }

    private Unit RecordSuccessOutcome(Activity? activity, TResponse response)
    {
        if (_failureDetector.TryExtractFailure(response, out var failureReason, out var capturedFailure))
        {
            RecordFunctionalFailure(activity, failureReason, capturedFailure);
        }
        else
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return Unit.Default;
    }

    private void RecordFunctionalFailure(Activity? activity, string? failureReason, object? capturedFailure)
    {
        activity?.SetStatus(ActivityStatusCode.Error, failureReason);
        activity?.SetTag(ActivityTagNames.FunctionalFailure, true);

        if (!string.IsNullOrWhiteSpace(failureReason))
        {
            activity?.SetTag(ActivityTagNames.FailureReason, failureReason);
        }

        var errorCode = _failureDetector.TryGetErrorCode(capturedFailure);
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            activity?.SetTag(ActivityTagNames.FailureCode, errorCode);
        }

        var errorMessage = _failureDetector.TryGetErrorMessage(capturedFailure);
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            activity?.SetTag(ActivityTagNames.FailureMessage, errorMessage);
        }
    }

    private static Unit RecordErrorOutcome(Activity? activity, EncinaError error)
    {
        activity?.SetStatus(ActivityStatusCode.Error, error.Message);
        activity?.SetTag(ActivityTagNames.PipelineFailure, true);
        activity?.SetTag(ActivityTagNames.FailureReason, error.GetEncinaCode());
        return Unit.Default;
    }
}
