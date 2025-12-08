using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;

namespace SimpleMediator;

/// <summary>
/// Emits tracing activities for queries and labels functional failures.
/// </summary>
/// <typeparam name="TQuery">Query type being observed.</typeparam>
/// <typeparam name="TResponse">Response type returned by the handler.</typeparam>
/// <remarks>
/// Similar to <see cref="CommandActivityPipelineBehavior{TCommand,TResponse}"/> but focused on
/// queries, which helps correlate reads across OpenTelemetry traces.
/// </remarks>
/// <example>
/// <code>
/// services.AddSingleton&lt;IFunctionalFailureDetector, ApplicationFunctionalFailureDetector&gt;();
/// services.AddSimpleMediator(cfg => cfg.AddPipelineBehavior(typeof(QueryActivityPipelineBehavior&lt;,&gt;)), assemblies);
/// </code>
/// </example>
public sealed class QueryActivityPipelineBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly IFunctionalFailureDetector _failureDetector;

    /// <summary>
    /// Initializes the behavior with the functional failure detector.
    /// </summary>
    public QueryActivityPipelineBehavior(IFunctionalFailureDetector failureDetector)
    {
        _failureDetector = failureDetector ?? NullFunctionalFailureDetector.Instance;
    }

    /// <inheritdoc />
    public async Task<Either<Error, TResponse>> Handle(TQuery request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
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

        var activityName = $"Mediator.Query.{typeof(TQuery).Name}";
        using var activity = MediatorDiagnostics.ActivitySource.StartActivity(activityName, ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag("mediator.request_kind", "query");
            activity.SetTag("mediator.request_type", typeof(TQuery).FullName);
            activity.SetTag("mediator.request_name", typeof(TQuery).Name);
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
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.behavior.cancelled", $"Behavior {GetType().Name} cancelled the {typeof(TQuery).Name} request.", ex));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);
            var error = MediatorErrors.FromException("mediator.behavior.exception", ex, $"Error running {GetType().Name} for {typeof(TQuery).Name}.");
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
