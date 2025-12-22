using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// Contract for composing the Encina pipeline (behaviors + handler) into a reusable delegate.
/// Intended for future refactor to minimize reflection and allocations in the hot path.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
internal interface IPipelineBuilder<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Builds the pipeline delegate that executes behaviors and the handler.
    /// Implementations should keep the rail funcional (ValueTask&lt;Either&lt;EncinaError,TResponse&gt;&gt;).
    /// </summary>
    /// <param name="serviceProvider">Resolver for behaviors, pre/post processors y handler.</param>
    /// <returns>Delegate ready to execute the request.</returns>
    RequestHandlerCallback<TResponse> Build(IServiceProvider serviceProvider);
}

/// <summary>
/// Default pipeline builder that composes pre/post processors, behaviors and the handler
/// into a single delegate per request execution.
/// </summary>
/// <remarks>
/// <para><b>Pipeline Order:</b> PreProcessors → Behaviors → Handler → PostProcessors</para>
/// <para><b>Behavior Composition:</b> Uses nested delegates (Russian doll pattern) to chain behaviors in registration order.</para>
/// <para><b>Error Strategy:</b> Railway Oriented Programming - errors short-circuit via Either&lt;EncinaError, TResponse&gt;.</para>
/// </remarks>
internal sealed class PipelineBuilder<TRequest, TResponse> : IPipelineBuilder<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly TRequest _request;
    private readonly IRequestHandler<TRequest, TResponse> _handler;
    private readonly IRequestContext _context;
    private readonly CancellationToken _cancellationToken;

    public PipelineBuilder(TRequest request, IRequestHandler<TRequest, TResponse> handler, IRequestContext context, CancellationToken cancellationToken)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Builds the execution pipeline by wrapping the handler with behaviors and processors.
    /// </summary>
    /// <remarks>
    /// Behaviors are applied in reverse (right-to-left) to ensure left-to-right execution order.
    /// Example: Register A, B → Execute PreProc → A → B → Handler → PostProc
    /// </remarks>
    public RequestHandlerCallback<TResponse> Build(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        // Resolve all pipeline components from DI (fall back to empty arrays)
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>()?.ToArray()
                        ?? System.Array.Empty<IPipelineBehavior<TRequest, TResponse>>();
        var preProcessors = serviceProvider.GetServices<IRequestPreProcessor<TRequest>>()?.ToArray()
                         ?? System.Array.Empty<IRequestPreProcessor<TRequest>>();
        var postProcessors = serviceProvider.GetServices<IRequestPostProcessor<TRequest, TResponse>>()?.ToArray()
                          ?? System.Array.Empty<IRequestPostProcessor<TRequest, TResponse>>();

        // Start with the innermost delegate: handler invocation
        RequestHandlerCallback<TResponse> current = () => ExecuteHandlerAsync(_handler, _request, _cancellationToken);

        // Wrap handler with behaviors in reverse order (creates nested delegates)
        // This ensures behaviors execute in registration order when the pipeline runs
        if (behaviors.Length > 0)
        {
            for (var index = behaviors.Length - 1; index >= 0; index--)
            {
                var behavior = behaviors[index];
                var nextStep = current; // Capture in closure
                current = () => ExecuteBehaviorAsync(behavior, _request, _context, nextStep, _cancellationToken);
            }
        }

        // Wrap everything with pre/post processors (outermost layer)
        return () => ExecutePipelineAsync(preProcessors, postProcessors, current, _request, _context, _cancellationToken);
    }

    /// <summary>
    /// Executes the pipeline using optimized span-based iteration to minimize allocations.
    /// </summary>
    /// <remarks>
    /// Uses index-based iteration over arrays to avoid enumerator allocation overhead.
    /// Pre/post processors are passed as ReadOnlySpan for zero-allocation iteration.
    /// </remarks>
    private static async ValueTask<Either<EncinaError, TResponse>> ExecutePipelineAsync(
        IRequestPreProcessor<TRequest>[] preProcessors,
        IRequestPostProcessor<TRequest, TResponse>[] postProcessors,
        RequestHandlerCallback<TResponse> terminal,
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        // Pre-processors: index-based iteration avoids enumerator allocation
        for (var i = 0; i < preProcessors.Length; i++)
        {
            var failure = await ExecutePreProcessorAsync(preProcessors[i], request, context, cancellationToken).ConfigureAwait(false);
            if (failure.IsSome)
            {
                var error = failure.Match(err => err, () => EncinaErrors.Unknown);
                return Left<EncinaError, TResponse>(error);
            }
        }

        var response = await terminal().ConfigureAwait(false);

        // Post-processors: index-based iteration avoids enumerator allocation
        for (var i = 0; i < postProcessors.Length; i++)
        {
            var failure = await ExecutePostProcessorAsync(postProcessors[i], request, context, response, cancellationToken).ConfigureAwait(false);
            if (failure.IsSome)
            {
                return Left<EncinaError, TResponse>(failure.Match(err => err, () => EncinaErrors.Unknown));
            }
        }

        return response;
    }

    /// <summary>
    /// Executes the handler and returns its result directly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pure Railway Oriented Programming: Handlers return <see cref="Either{L,R}"/> and are responsible
    /// for converting all expected errors to Left values. Any exception that escapes indicates a bug
    /// in the handler and will propagate (fail-fast).
    /// </para>
    /// <para>
    /// Cancellation is the only exception we handle, as it represents expected cooperative cancellation
    /// rather than a programming error.
    /// </para>
    /// </remarks>
    private static async ValueTask<Either<EncinaError, TResponse>> ExecuteHandlerAsync(
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await handler.Handle(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation is expected behavior, convert to Left
            var message = $"Handler {handler.GetType().Name} was cancelled while processing {typeof(TRequest).Name}.";
            var metadata = new Dictionary<string, object?>
            {
                ["handler"] = handler.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "handler"
            };
            return Left<EncinaError, TResponse>(EncinaErrors.Create(EncinaErrorCodes.HandlerCancelled, message, ex, metadata));
        }
        // Any other exception (e.g., NullReferenceException, InvalidOperationException) indicates
        // a bug in the handler and will propagate to let the application crash (fail-fast)
    }

    private static async ValueTask<Either<EncinaError, TResponse>> ExecuteBehaviorAsync(
        IPipelineBehavior<TRequest, TResponse> behavior,
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        try
        {
            return await behavior.Handle(request, context, nextStep, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Behavior {behavior.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            var metadata = new Dictionary<string, object?>
            {
                ["behavior"] = behavior.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "behavior"
            };
            return Left<EncinaError, TResponse>(EncinaErrors.Create(EncinaErrorCodes.BehaviorCancelled, message, ex, metadata));
        }
        // Pure ROP: Any other exception indicates a bug in the behavior and will propagate (fail-fast)
    }

    private static async Task<Option<EncinaError>> ExecutePreProcessorAsync(
        IRequestPreProcessor<TRequest> preProcessor,
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await preProcessor.Process(request, context, cancellationToken).ConfigureAwait(false);
            return Option<EncinaError>.None;
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Pre-processor {preProcessor.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            var preCancellationMetadata = new Dictionary<string, object?>
            {
                ["preProcessor"] = preProcessor.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "preprocessor"
            };
            return Some(EncinaErrors.Create(EncinaErrorCodes.PreProcessorCancelled, message, ex, preCancellationMetadata));
        }
        // Pure ROP: Any other exception indicates a bug in the preprocessor and will propagate (fail-fast)
    }

    private static async Task<Option<EncinaError>> ExecutePostProcessorAsync(
        IRequestPostProcessor<TRequest, TResponse> postProcessor,
        TRequest request,
        IRequestContext context,
        Either<EncinaError, TResponse> response,
        CancellationToken cancellationToken)
    {
        try
        {
            await postProcessor.Process(request, context, response, cancellationToken).ConfigureAwait(false);
            return Option<EncinaError>.None;
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Post-processor {postProcessor.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            var postCancellationMetadata = new Dictionary<string, object?>
            {
                ["postProcessor"] = postProcessor.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "postprocessor"
            };
            return Some(EncinaErrors.Create(EncinaErrorCodes.PostProcessorCancelled, message, ex, postCancellationMetadata));
        }
        // Pure ROP: Any other exception indicates a bug in the postprocessor and will propagate (fail-fast)
    }
}
