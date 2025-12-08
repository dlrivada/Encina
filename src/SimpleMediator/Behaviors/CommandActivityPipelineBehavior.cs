using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
public sealed class CommandActivityPipelineBehavior<TCommand, TResponse> : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly IFunctionalFailureDetector _failureDetector;

    /// <summary>
    /// Initializes the behavior with the functional failure detector to use.
    /// </summary>
    /// <param name="failureDetector">Detector that interprets handler responses.</param>
    public CommandActivityPipelineBehavior(IFunctionalFailureDetector failureDetector)
    {
        _failureDetector = failureDetector ?? NullFunctionalFailureDetector.Instance;
    }

    /// <inheritdoc />
    public async Task<Either<Error, TResponse>> Handle(TCommand request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        if (request is null)
        {
            var message = $"{GetType().Name} received a null request.";
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.behavior.null_request", message));
        }

        if (next is null)
        {
            var message = $"{GetType().Name} received a null delegate.";
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.behavior.null_next", message));
        }

        var activityName = $"Mediator.Command.{typeof(TCommand).Name}";
        using var activity = MediatorDiagnostics.ActivitySource.StartActivity(activityName, ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag("mediator.request_kind", "command");
            activity.SetTag("mediator.request_type", typeof(TCommand).FullName);
            activity.SetTag("mediator.request_name", typeof(TCommand).Name);
            activity.SetTag("mediator.response_type", typeof(TResponse).FullName);
        }

        Either<Error, TResponse> outcome;

        try
        {
            outcome = await next().ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
            activity?.SetTag("mediator.cancelled", true);
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.behavior.cancelled", $"Behavior {GetType().Name} cancelled the {typeof(TCommand).Name} request.", ex));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);
            var error = MediatorErrors.FromException("mediator.behavior.exception", ex, $"Error running {GetType().Name} for {typeof(TCommand).Name}.");
            return Left<Error, TResponse>(error);
        }
        outcome.Match(
            Right: response =>
            {
                if (_failureDetector.TryExtractFailure(response, out var failureReason, out var functionalError))
                {
                    activity?.SetStatus(ActivityStatusCode.Error, failureReason);
                    activity?.SetTag("mediator.functional_failure", true);
                    if (!string.IsNullOrWhiteSpace(failureReason))
                    {
                        activity?.SetTag("mediator.failure_reason", failureReason);
                    }

                    var errorCode = _failureDetector.TryGetErrorCode(functionalError);
                    if (!string.IsNullOrWhiteSpace(errorCode))
                    {
                        activity?.SetTag("mediator.failure_code", errorCode);
                    }

                    var errorMessage = _failureDetector.TryGetErrorMessage(functionalError);
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
