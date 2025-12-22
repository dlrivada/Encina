using System.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina;

public sealed partial class Encina
{
    /// <summary>
    /// Internal dispatcher responsible for orchestrating the execution of a single request through the Encina pipeline.
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
    /// <para>All errors are returned via Either&lt;EncinaError, TResponse&gt; (Railway Oriented Programming).
    /// Exceptions are only caught at the dispatcher level and converted to Either for consistent error handling.</para>
    /// </remarks>
    private static class RequestDispatcher
    {
        public static async Task<Either<EncinaError, TResponse>> ExecuteAsync<TResponse>(Encina Encina, IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            // --- SETUP PHASE ---
            // Create a fresh DI scope for this request to ensure proper lifetime management
            // and isolation from other concurrent requests
            using IServiceScope scope = Encina._scopeFactory.CreateScope();
            IServiceProvider serviceProvider = scope.ServiceProvider;
            IEncinaMetrics? metrics = serviceProvider.GetService<IEncinaMetrics>();

            Type requestType = request.GetType();
            string requestKind = GetRequestKind(requestType); // Determine if Command, Query, or generic Request
            var stopwatch = Stopwatch.StartNew();
            using Activity? activity = EncinaDiagnostics.SendStarted(requestType, typeof(TResponse), requestKind);

            // --- HANDLER RESOLUTION PHASE ---
            // Get or create a cached dispatcher wrapper that knows how to invoke the handler
            // This avoids repeated reflection on every request - wrappers are generated once per request/response type pair
            RequestHandlerBase dispatcher = RequestHandlerCache.GetOrAdd(
                (requestType, typeof(TResponse)),
                static key => CreateRequestHandlerWrapper(key.Request, key.Response));

            // Resolve the actual handler instance from DI
            // May return null if no handler is registered
            object? handler = dispatcher.ResolveHandler(serviceProvider);

            // --- VALIDATION PHASE ---
            // Validate that a handler was resolved from DI
            // Early return with error if no handler is registered for this request type
            if (!EncinaRequestGuards.TryValidateHandler<TResponse>(handler, requestType, typeof(TResponse), out Either<EncinaError, TResponse> handlerError))
            {
                Log.HandlerMissing(Encina._logger, requestType.Name, typeof(TResponse).Name);
                stopwatch.Stop();
                metrics?.TrackFailure(requestKind, requestType.Name, stopwatch.Elapsed, EncinaErrorCodes.RequestHandlerMissing);
                EncinaError error = handlerError.Match(Left: err => err, Right: _ => EncinaErrors.Unknown);
                EncinaDiagnostics.SendCompleted(activity, isSuccess: false, errorCode: error.GetEncinaCode(), errorMessage: error.Message);
                return handlerError;
            }

            // Validate that the resolved handler is of the expected type
            // This guards against DI misconfiguration where the wrong service is registered
            if (!EncinaRequestGuards.TryValidateHandlerType<TResponse>(handler!, dispatcher.HandlerServiceType, requestType, out Either<EncinaError, TResponse> typeError))
            {
                Log.HandlerMissing(Encina._logger, requestType.Name, typeof(TResponse).Name);
                stopwatch.Stop();
                metrics?.TrackFailure(requestKind, requestType.Name, stopwatch.Elapsed, EncinaErrorCodes.RequestHandlerTypeMismatch);
                EncinaError error = typeError.Match(Left: err => err, Right: _ => EncinaErrors.Unknown);
                EncinaDiagnostics.SendCompleted(activity, isSuccess: false, errorCode: error.GetEncinaCode(), errorMessage: error.Message);
                return typeError;
            }

            try
            {
                // --- EXECUTION PHASE ---
                // Handler is valid - proceed with pipeline execution
                Log.ProcessingRequest(Encina._logger, requestType.Name, handler!.GetType().Name);
                activity?.SetTag("Encina.handler", handler.GetType().FullName);
                activity?.SetTag("Encina.handler_count", 1);

                // Invoke the handler through the full pipeline:
                // 1. Pipeline behaviors (in order of registration)
                // 2. Pre-processors (in order of registration)
                // 3. The actual request handler
                // 4. Post-processors (in order of registration)
                // The dispatcher.Handle method delegates to RequestHandlerWrapper which uses PipelineBuilder
                object outcomeObject = await dispatcher.Handle(Encina, request, handler, serviceProvider, cancellationToken).ConfigureAwait(false);
                var outcome = (Either<EncinaError, TResponse>)outcomeObject;

                Encina.LogSendOutcome(requestType, handler.GetType(), outcome);

                stopwatch.Stop();

                // --- OBSERVABILITY PHASE ---
                // Track metrics based on success or failure
                (bool IsSuccess, EncinaError? Error) = ExtractOutcome(outcome);
                string reason = Error?.GetEncinaCode() ?? string.Empty;
                if (IsSuccess)
                {
                    metrics?.TrackSuccess(requestKind, requestType.Name, stopwatch.Elapsed);
                }
                else
                {
                    metrics?.TrackFailure(requestKind, requestType.Name, stopwatch.Elapsed, reason);
                }

                // Complete the diagnostic activity with appropriate error information
                EncinaError? outcomeError = outcome.IsRight
                    ? null
                    : outcome.Match(_ => (EncinaError?)null, err => err);

                EncinaDiagnostics.SendCompleted(
                    activity,
                    outcome.IsRight,
                    errorCode: outcomeError?.GetEncinaCode(),
                    errorMessage: outcomeError?.Message);
                return outcome;
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                // --- CANCELLATION HANDLING ---
                // Cancellation is expected cooperative behavior, not an error
                string message = $"The {requestType.Name} request was cancelled.";
                var metadata = new Dictionary<string, object?>
                {
                    ["request"] = requestType.FullName,
                    ["handler"] = handler!.GetType().FullName,
                    ["stage"] = "request"
                };
                Log.RequestCancelledDuringSend(Encina._logger, requestType.Name);
                stopwatch.Stop();
                metrics?.TrackFailure(requestKind, requestType.Name, stopwatch.Elapsed, EncinaErrorCodes.RequestCancelled);
                EncinaDiagnostics.SendCompleted(activity, isSuccess: false, errorCode: EncinaErrorCodes.RequestCancelled, errorMessage: message);
                return Left<EncinaError, TResponse>(EncinaErrors.Create(EncinaErrorCodes.RequestCancelled, message, ex, metadata));
            }
            // Pure ROP: Any other exception (e.g., NullReferenceException, InvalidOperationException)
            // indicates a bug in a handler, behavior, or processor and will propagate to crash the process (fail-fast).
            // Well-written handlers/behaviors/processors return Either<EncinaError, T> for all expected errors.
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
