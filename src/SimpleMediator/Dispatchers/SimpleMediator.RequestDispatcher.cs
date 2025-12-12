using System.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace SimpleMediator;

public sealed partial class SimpleMediator
{
    private static class RequestDispatcher
    {
        public static async Task<Either<MediatorError, TResponse>> ExecuteAsync<TResponse>(SimpleMediator mediator, IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            using var scope = mediator._scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var metrics = serviceProvider.GetService<IMediatorMetrics>();

            var requestType = request.GetType();
            var requestKind = GetRequestKind(requestType);
            var stopwatch = Stopwatch.StartNew();
            using var activity = MediatorDiagnostics.SendStarted(requestType, typeof(TResponse), requestKind);

            var dispatcher = RequestHandlerCache.GetOrAdd(
                (requestType, typeof(TResponse)),
                static key => CreateRequestHandlerWrapper(key.Request, key.Response));

            var handler = dispatcher.ResolveHandler(serviceProvider);

            if (!MediatorRequestGuards.TryValidateHandler<TResponse>(handler, requestType, typeof(TResponse), out var handlerError))
            {
                Log.HandlerMissing(mediator._logger, requestType.Name, typeof(TResponse).Name);
                stopwatch.Stop();
                metrics?.TrackFailure(requestKind, requestType.Name, stopwatch.Elapsed, MediatorErrorCodes.RequestHandlerMissing);
                var error = handlerError.Match(Left: err => err, Right: _ => MediatorErrors.Unknown);
                MediatorDiagnostics.SendCompleted(activity, isSuccess: false, errorCode: error.GetMediatorCode(), errorMessage: error.Message);
                return handlerError;
            }

            if (!MediatorRequestGuards.TryValidateHandlerType<TResponse>(handler!, dispatcher.HandlerServiceType, requestType, out var typeError))
            {
                Log.HandlerMissing(mediator._logger, requestType.Name, typeof(TResponse).Name);
                stopwatch.Stop();
                metrics?.TrackFailure(requestKind, requestType.Name, stopwatch.Elapsed, MediatorErrorCodes.RequestHandlerTypeMismatch);
                var error = typeError.Match(Left: err => err, Right: _ => MediatorErrors.Unknown);
                MediatorDiagnostics.SendCompleted(activity, isSuccess: false, errorCode: error.GetMediatorCode(), errorMessage: error.Message);
                return typeError;
            }

            try
            {
                Log.ProcessingRequest(mediator._logger, requestType.Name, handler!.GetType().Name);
                activity?.SetTag("mediator.handler", handler.GetType().FullName);
                activity?.SetTag("mediator.handler_count", 1);

                var outcomeObject = await dispatcher.Handle(mediator, request, handler, serviceProvider, cancellationToken).ConfigureAwait(false);
                var outcome = (Either<MediatorError, TResponse>)outcomeObject;

                mediator.LogSendOutcome(requestType, handler.GetType(), outcome);

                stopwatch.Stop();

                var resultInfo = ExtractOutcome(outcome);
                var reason = resultInfo.Error?.GetMediatorCode() ?? string.Empty;
                if (resultInfo.IsSuccess)
                {
                    metrics?.TrackSuccess(requestKind, requestType.Name, stopwatch.Elapsed);
                }
                else
                {
                    metrics?.TrackFailure(requestKind, requestType.Name, stopwatch.Elapsed, reason);
                }

                var outcomeError = outcome.IsRight
                    ? null
                    : outcome.Match(_ => (MediatorError?)null, err => err);

                MediatorDiagnostics.SendCompleted(
                    activity,
                    outcome.IsRight,
                    errorCode: outcomeError?.GetMediatorCode(),
                    errorMessage: outcomeError?.Message);
                return outcome;
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                var message = $"The {requestType.Name} request was cancelled.";
                var metadata = new Dictionary<string, object?>
                {
                    ["request"] = requestType.FullName,
                    ["handler"] = handler!.GetType().FullName,
                    ["stage"] = "request"
                };
                Log.RequestCancelledDuringSend(mediator._logger, requestType.Name);
                stopwatch.Stop();
                metrics?.TrackFailure(requestKind, requestType.Name, stopwatch.Elapsed, MediatorErrorCodes.RequestCancelled);
                MediatorDiagnostics.SendCompleted(activity, isSuccess: false, errorCode: MediatorErrorCodes.RequestCancelled, errorMessage: message);
                return Left<MediatorError, TResponse>(MediatorErrors.Create(MediatorErrorCodes.RequestCancelled, message, ex, metadata));
            }
            catch (Exception ex)
            {
                var metadata = new Dictionary<string, object?>
                {
                    ["request"] = requestType.FullName,
                    ["handler"] = handler!.GetType().FullName,
                    ["stage"] = "pipeline"
                };
                var error = MediatorErrors.FromException(MediatorErrorCodes.PipelineException, ex, $"Unexpected error while processing {requestType.Name}.", metadata);
                Log.RequestProcessingError(mediator._logger, requestType.Name, ex);
                stopwatch.Stop();
                metrics?.TrackFailure(requestKind, requestType.Name, stopwatch.Elapsed, MediatorErrorCodes.PipelineException);
                MediatorDiagnostics.SendCompleted(activity, isSuccess: false, errorCode: error.GetMediatorCode(), errorMessage: error.Message);
                return Left<MediatorError, TResponse>(error);
            }
        }

        private static string GetRequestKind(Type requestType)
        {
            if (typeof(ICommand).IsAssignableFrom(requestType))
            {
                return "command";
            }

            if (typeof(IQuery).IsAssignableFrom(requestType))
            {
                return "query";
            }

            return "request";
        }
    }
}
