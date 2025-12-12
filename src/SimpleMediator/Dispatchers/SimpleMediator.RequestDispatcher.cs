using System.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace SimpleMediator;

public sealed partial class SimpleMediator
{
    /// <summary>
    /// Internal dispatcher responsible for orchestrating the execution of a single request through the mediator pipeline.
    /// </summary>
    /// <remarks>
    /// <para><b>Responsibilities:</b></para>
    /// <list type="bullet">
    /// <item>Creates a scoped service provider for the request lifetime</item>
    /// <item>Resolves the appropriate handler from DI using cached reflection-generated wrappers</item>
    /// <item>Validates handler presence and type correctness</item>
    /// <item>Orchestrates the execution through behaviors, pre/post processors via PipelineBuilder</item>
    /// <item>Tracks metrics (duration, success/failure) and emits diagnostic activities</item>
    /// <item>Handles cancellation and unexpected exceptions with proper error codes</item>
    /// </list>
    /// <para><b>Flow:</b></para>
    /// <list type="number">
    /// <item>Setup: Create scope, resolve metrics, start stopwatch and activity</item>
    /// <item>Handler Resolution: Get cached dispatcher wrapper and resolve handler from DI</item>
    /// <item>Validation: Ensure handler exists and is of correct type</item>
    /// <item>Execution: Invoke handler through pipeline (behaviors → pre-processors → handler → post-processors)</item>
    /// <item>Observability: Log outcome, track metrics, complete activity</item>
    /// <item>Error Handling: Catch cancellations and exceptions, convert to Either</item>
    /// </list>
    /// <para><b>Error Strategy:</b></para>
    /// <para>All errors are returned via Either&lt;MediatorError, TResponse&gt; (Railway Oriented Programming).
    /// Exceptions are only caught at the dispatcher level and converted to Either for consistent error handling.</para>
    /// </remarks>
    private static class RequestDispatcher
    {
        public static async Task<Either<MediatorError, TResponse>> ExecuteAsync<TResponse>(SimpleMediator mediator, IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            // --- SETUP PHASE ---
            // Create a fresh DI scope for this request to ensure proper lifetime management
            // and isolation from other concurrent requests
            using var scope = mediator._scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var metrics = serviceProvider.GetService<IMediatorMetrics>();

            var requestType = request.GetType();
            var requestKind = GetRequestKind(requestType); // Determine if Command, Query, or generic Request
            var stopwatch = Stopwatch.StartNew();
            using var activity = MediatorDiagnostics.SendStarted(requestType, typeof(TResponse), requestKind);

            // --- HANDLER RESOLUTION PHASE ---
            // Get or create a cached dispatcher wrapper that knows how to invoke the handler
            // This avoids repeated reflection on every request - wrappers are generated once per request/response type pair
            var dispatcher = RequestHandlerCache.GetOrAdd(
                (requestType, typeof(TResponse)),
                static key => CreateRequestHandlerWrapper(key.Request, key.Response));

            // Resolve the actual handler instance from DI
            // May return null if no handler is registered
            var handler = dispatcher.ResolveHandler(serviceProvider);

            // --- VALIDATION PHASE ---
            // Validate that a handler was resolved from DI
            // Early return with error if no handler is registered for this request type
            if (!MediatorRequestGuards.TryValidateHandler<TResponse>(handler, requestType, typeof(TResponse), out var handlerError))
            {
                Log.HandlerMissing(mediator._logger, requestType.Name, typeof(TResponse).Name);
                stopwatch.Stop();
                metrics?.TrackFailure(requestKind, requestType.Name, stopwatch.Elapsed, MediatorErrorCodes.RequestHandlerMissing);
                var error = handlerError.Match(Left: err => err, Right: _ => MediatorErrors.Unknown);
                MediatorDiagnostics.SendCompleted(activity, isSuccess: false, errorCode: error.GetMediatorCode(), errorMessage: error.Message);
                return handlerError;
            }

            // Validate that the resolved handler is of the expected type
            // This guards against DI misconfiguration where the wrong service is registered
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
                // --- EXECUTION PHASE ---
                // Handler is valid - proceed with pipeline execution
                Log.ProcessingRequest(mediator._logger, requestType.Name, handler!.GetType().Name);
                activity?.SetTag("mediator.handler", handler.GetType().FullName);
                activity?.SetTag("mediator.handler_count", 1);

                // Invoke the handler through the full pipeline:
                // 1. Pipeline behaviors (in order of registration)
                // 2. Pre-processors (in order of registration)
                // 3. The actual request handler
                // 4. Post-processors (in order of registration)
                // The dispatcher.Handle method delegates to RequestHandlerWrapper which uses PipelineBuilder
                var outcomeObject = await dispatcher.Handle(mediator, request, handler, serviceProvider, cancellationToken).ConfigureAwait(false);
                var outcome = (Either<MediatorError, TResponse>)outcomeObject;

                mediator.LogSendOutcome(requestType, handler.GetType(), outcome);

                stopwatch.Stop();

                // --- OBSERVABILITY PHASE ---
                // Track metrics based on success or failure
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

                // Complete the diagnostic activity with appropriate error information
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
                // --- CANCELLATION HANDLING ---
                // Cancellation is expected behavior, not an error
                // Track it separately for observability purposes
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
                // --- UNEXPECTED EXCEPTION HANDLING ---
                // This is the safety net for any unhandled exceptions in the pipeline
                // All expected errors should flow through Either<MediatorError, TResponse>
                // If we reach here, it indicates a bug in a behavior, processor, or handler
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

        /// <summary>
        /// Determines the semantic kind of a request for observability and routing purposes.
        /// </summary>
        /// <param name="requestType">The runtime type of the request.</param>
        /// <returns>
        /// "command" if the request implements ICommand (write operation),
        /// "query" if it implements IQuery (read operation),
        /// or "request" for generic requests that don't follow CQRS.
        /// </returns>
        /// <remarks>
        /// This classification enables:
        /// <list type="bullet">
        /// <item>Selective behavior application (e.g., CommandActivityPipelineBehavior only runs for commands)</item>
        /// <item>Metrics segmentation (track command vs query performance separately)</item>
        /// <item>Tracing/logging categorization for better observability</item>
        /// </list>
        /// </remarks>
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
