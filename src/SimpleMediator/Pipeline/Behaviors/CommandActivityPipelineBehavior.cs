using System.Diagnostics;
using LanguageExt;
using static LanguageExt.Prelude;

namespace SimpleMediator;

/// <summary>
/// Creates diagnostic activities for commands and annotates functional failures.
/// </summary>
/// <typeparam name="TCommand">Command type being observed.</typeparam>
/// <typeparam name="TResponse">Response type returned by the handler.</typeparam>
/// <remarks>
/// Leverages <see cref="IFunctionalFailureDetector"/> to tag errors without depending on concrete
/// domain types. Activities are emitted with the <c>SimpleMediator</c> source ready for
/// OpenTelemetry.
/// </remarks>
/// <example>
/// <code>
/// services.AddSingleton&lt;IFunctionalFailureDetector, ApplicationFunctionalFailureDetector&gt;();
/// services.AddSimpleMediator(cfg =>
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
    public async ValueTask<Either<MediatorError, TResponse>> Handle(TCommand request, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
    {
        if (!MediatorBehaviorGuards.TryValidateRequest(GetType(), request, out var failure))
        {
            return Left<MediatorError, TResponse>(failure);
        }

        if (!MediatorBehaviorGuards.TryValidateNextStep(GetType(), nextStep, out failure))
        {
            return Left<MediatorError, TResponse>(failure);
        }

        using var activity = MediatorDiagnostics.ActivitySource.HasListeners()
            ? MediatorDiagnostics.ActivitySource.StartActivity(string.Concat("Mediator.Command.", typeof(TCommand).Name), ActivityKind.Internal)
            : null;

        if (activity is not null)
        {
            activity.SetTag("mediator.request_kind", "command");
            activity.SetTag("mediator.request_type", typeof(TCommand).FullName);
            activity.SetTag("mediator.request_name", typeof(TCommand).Name);
            activity.SetTag("mediator.response_type", typeof(TResponse).FullName);
        }

        Either<MediatorError, TResponse> outcome;

        try
        {
            outcome = await nextStep().ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
            activity?.SetTag("mediator.cancelled", true);
            return Left<MediatorError, TResponse>(MediatorErrors.Create(MediatorErrorCodes.BehaviorCancelled, $"Behavior {GetType().Name} cancelled the {typeof(TCommand).Name} request.", ex));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);
            var error = MediatorErrors.FromException(MediatorErrorCodes.BehaviorException, ex, $"Error running {GetType().Name} for {typeof(TCommand).Name}.");
            return Left<MediatorError, TResponse>(error);
        }
        _ = outcome.Match(
            Right: response =>
            {
                if (_failureDetector.TryExtractFailure(response, out var failureReason, out var capturedFailure))
                {
                    activity?.SetStatus(ActivityStatusCode.Error, failureReason);
                    activity?.SetTag("mediator.functional_failure", true);
                    if (!string.IsNullOrWhiteSpace(failureReason))
                    {
                        activity?.SetTag("mediator.failure_reason", failureReason);
                    }

                    var errorCode = _failureDetector.TryGetErrorCode(capturedFailure);
                    if (!string.IsNullOrWhiteSpace(errorCode))
                    {
                        activity?.SetTag("mediator.failure_code", errorCode);
                    }

                    var errorMessage = _failureDetector.TryGetErrorMessage(capturedFailure);
                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        activity?.SetTag("mediator.failure_message", errorMessage);
                    }
                }
                else
                {
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }

                return Unit.Default;
            },
            Left: error =>
            {
                var effectiveError = error;
                activity?.SetStatus(ActivityStatusCode.Error, effectiveError.Message);
                activity?.SetTag("mediator.pipeline_failure", true);
                activity?.SetTag("mediator.failure_reason", effectiveError.GetMediatorCode());
                return Unit.Default;
            });

        return outcome;
    }
}
