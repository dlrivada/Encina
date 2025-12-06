using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleMediator;

/// <summary>
/// Crea actividades de diagnóstico para comandos y anota fallos funcionales.
/// </summary>
/// <typeparam name="TCommand">Tipo de comando observado.</typeparam>
/// <typeparam name="TResponse">Tipo devuelto por el handler.</typeparam>
/// <remarks>
/// Utiliza <see cref="IFunctionalFailureDetector"/> para etiquetar errores sin depender de tipos
/// concretos. Las actividades se emiten con la fuente <c>SimpleMediator</c>, lista para ser consumida
/// por OpenTelemetry.
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
    /// Inicializa el behavior con el detector de fallos funcionales a emplear.
    /// </summary>
    /// <param name="failureDetector">Detector que interpreta las respuestas del handler.</param>
    public CommandActivityPipelineBehavior(IFunctionalFailureDetector failureDetector)
    {
        _failureDetector = failureDetector ?? NullFunctionalFailureDetector.Instance;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TCommand request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        var activityName = $"Mediator.Command.{typeof(TCommand).Name}";
        using var activity = MediatorDiagnostics.ActivitySource.StartActivity(activityName, ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag("mediator.request_kind", "command");
            activity.SetTag("mediator.request_type", typeof(TCommand).FullName);
            activity.SetTag("mediator.request_name", typeof(TCommand).Name);
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
