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

        using var activity = EncinaDiagnostics.ActivitySource.HasListeners()
            ? EncinaDiagnostics.ActivitySource.StartActivity(string.Concat("Encina.Command.", typeof(TCommand).Name), ActivityKind.Internal)
            : null;

        if (activity is not null)
        {
            activity.SetTag("Encina.request_kind", "command");
            activity.SetTag("Encina.request_type", typeof(TCommand).FullName);
            activity.SetTag("Encina.request_name", typeof(TCommand).Name);
            activity.SetTag("Encina.response_type", typeof(TResponse).FullName);
        }

        Either<EncinaError, TResponse> outcome;

        try
        {
            outcome = await nextStep().ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
            activity?.SetTag("Encina.cancelled", true);
            return Left<EncinaError, TResponse>(EncinaErrors.Create(EncinaErrorCodes.BehaviorCancelled, $"Behavior {GetType().Name} cancelled the {typeof(TCommand).Name} request.", ex));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);
            var error = EncinaErrors.FromException(EncinaErrorCodes.BehaviorException, ex, $"Error running {GetType().Name} for {typeof(TCommand).Name}.");
            return Left<EncinaError, TResponse>(error);
        }
        _ = outcome.Match(
            Right: response =>
            {
                if (_failureDetector.TryExtractFailure(response, out var failureReason, out var capturedFailure))
                {
                    activity?.SetStatus(ActivityStatusCode.Error, failureReason);
                    activity?.SetTag("Encina.functional_failure", true);
                    if (!string.IsNullOrWhiteSpace(failureReason))
                    {
                        activity?.SetTag("Encina.failure_reason", failureReason);
                    }

                    var errorCode = _failureDetector.TryGetErrorCode(capturedFailure);
                    if (!string.IsNullOrWhiteSpace(errorCode))
                    {
                        activity?.SetTag("Encina.failure_code", errorCode);
                    }

                    var errorMessage = _failureDetector.TryGetErrorMessage(capturedFailure);
                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        activity?.SetTag("Encina.failure_message", errorMessage);
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
                activity?.SetTag("Encina.pipeline_failure", true);
                activity?.SetTag("Encina.failure_reason", effectiveError.GetEncinaCode());
                return Unit.Default;
            });

        return outcome;
    }
}
