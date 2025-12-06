using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleMediator;

/// <summary>
/// Emite actividades de trazado para consultas y etiqueta fallos funcionales.
/// </summary>
/// <typeparam name="TQuery">Tipo de consulta observada.</typeparam>
/// <typeparam name="TResponse">Tipo devuelto por el handler.</typeparam>
/// <remarks>
/// Igual que <see cref="CommandActivityPipelineBehavior{TCommand,TResponse}"/> pero orientado a
/// consultas, lo que facilita correlacionar lecturas dentro de distribuciones OpenTelemetry.
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
    /// Inicializa el behavior con el detector de fallos funcionales.
    /// </summary>
    public QueryActivityPipelineBehavior(IFunctionalFailureDetector failureDetector)
    {
        _failureDetector = failureDetector ?? NullFunctionalFailureDetector.Instance;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TQuery request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        var activityName = $"Mediator.Query.{typeof(TQuery).Name}";
        using var activity = MediatorDiagnostics.ActivitySource.StartActivity(activityName, ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag("mediator.request_kind", "query");
            activity.SetTag("mediator.request_type", typeof(TQuery).FullName);
            activity.SetTag("mediator.request_name", typeof(TQuery).Name);
            activity.SetTag("mediator.response_type", typeof(TResponse).FullName);
        }

        try
        {
            var response = await next().ConfigureAwait(false);
            if (_failureDetector.TryExtractFailure(response, out var failureReason, out var error))
            {
                activity?.SetStatus(ActivityStatusCode.Error, failureReason);
                activity?.SetTag("mediator.functional_failure", true);
                if (!string.IsNullOrWhiteSpace(failureReason))
                {
                    activity?.SetTag("mediator.failure_reason", failureReason);
                }

                var errorCode = _failureDetector.TryGetErrorCode(error);
                if (!string.IsNullOrWhiteSpace(errorCode))
                {
                    activity?.SetTag("mediator.failure_code", errorCode);
                }

                var errorMessage = _failureDetector.TryGetErrorMessage(error);
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    activity?.SetTag("mediator.failure_message", errorMessage);
                }
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
            activity?.SetTag("mediator.cancelled", true);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);
            throw;
        }
    }
}
