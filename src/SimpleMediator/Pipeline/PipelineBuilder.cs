using System;
using System.Collections.Generic;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace SimpleMediator;

/// <summary>
/// Contract for composing the mediator pipeline (behaviors + handler) into a reusable delegate.
/// Intended for future refactor to minimize reflection and allocations in the hot path.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
internal interface IPipelineBuilder<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Builds the pipeline delegate that executes behaviors and the handler.
    /// Implementations should keep the rail funcional (ValueTask&lt;Either&lt;MediatorError,TResponse&gt;&gt;).
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
/// <para><b>Error Strategy:</b> Railway Oriented Programming - errors short-circuit via Either&lt;MediatorError, TResponse&gt;.</para>
/// </remarks>
internal sealed class PipelineBuilder<TRequest, TResponse> : IPipelineBuilder<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly TRequest _request;
    private readonly IRequestHandler<TRequest, TResponse> _handler;
    private readonly CancellationToken _cancellationToken;

    public PipelineBuilder(TRequest request, IRequestHandler<TRequest, TResponse> handler, CancellationToken cancellationToken)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
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
                current = () => ExecuteBehaviorAsync(behavior, _request, nextStep, _cancellationToken);
            }
        }

        // Wrap everything with pre/post processors (outermost layer)
        return () => ExecutePipelineAsync(preProcessors, postProcessors, current, _request, _cancellationToken);
    }

    private static async ValueTask<Either<MediatorError, TResponse>> ExecutePipelineAsync(
        IReadOnlyList<IRequestPreProcessor<TRequest>> preProcessors,
        IReadOnlyList<IRequestPostProcessor<TRequest, TResponse>> postProcessors,
        RequestHandlerCallback<TResponse> terminal,
        TRequest request,
        CancellationToken cancellationToken)
    {
        foreach (var preProcessor in preProcessors)
        {
            var failure = await ExecutePreProcessorAsync(preProcessor, request, cancellationToken).ConfigureAwait(false);
            if (failure.IsSome)
            {
                var error = failure.Match(err => err, () => MediatorErrors.Unknown);
                return Left<MediatorError, TResponse>(error);
            }
        }

        var response = await terminal().ConfigureAwait(false);

        foreach (var postProcessor in postProcessors)
        {
            var failure = await ExecutePostProcessorAsync(postProcessor, request, response, cancellationToken).ConfigureAwait(false);
            var hasFailure = false;
            MediatorError capturedError = default;

            failure.IfSome(err =>
            {
                hasFailure = true;
                capturedError = err;
            });

            if (hasFailure)
            {
                return Left<MediatorError, TResponse>(capturedError);
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
    private static async ValueTask<Either<MediatorError, TResponse>> ExecuteHandlerAsync(
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
            return Left<MediatorError, TResponse>(MediatorErrors.Create(MediatorErrorCodes.HandlerCancelled, message, ex, metadata));
        }
        // Any other exception (e.g., NullReferenceException, InvalidOperationException) indicates
        // a bug in the handler and will propagate to let the application crash (fail-fast)
    }

    private static async ValueTask<Either<MediatorError, TResponse>> ExecuteBehaviorAsync(
        IPipelineBehavior<TRequest, TResponse> behavior,
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        try
        {
            return await behavior.Handle(request, nextStep, cancellationToken).ConfigureAwait(false);
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
            return Left<MediatorError, TResponse>(MediatorErrors.Create(MediatorErrorCodes.BehaviorCancelled, message, ex, metadata));
        }
        // Pure ROP: Any other exception indicates a bug in the behavior and will propagate (fail-fast)
    }

    private static async Task<Option<MediatorError>> ExecutePreProcessorAsync(
        IRequestPreProcessor<TRequest> preProcessor,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await preProcessor.Process(request, cancellationToken).ConfigureAwait(false);
            return Option<MediatorError>.None;
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
            return Some(MediatorErrors.Create(MediatorErrorCodes.PreProcessorCancelled, message, ex, preCancellationMetadata));
        }
        // Pure ROP: Any other exception indicates a bug in the preprocessor and will propagate (fail-fast)
    }

    private static async Task<Option<MediatorError>> ExecutePostProcessorAsync(
        IRequestPostProcessor<TRequest, TResponse> postProcessor,
        TRequest request,
        Either<MediatorError, TResponse> response,
        CancellationToken cancellationToken)
    {
        try
        {
            await postProcessor.Process(request, response, cancellationToken).ConfigureAwait(false);
            return Option<MediatorError>.None;
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
            return Some(MediatorErrors.Create(MediatorErrorCodes.PostProcessorCancelled, message, ex, postCancellationMetadata));
        }
        // Pure ROP: Any other exception indicates a bug in the postprocessor and will propagate (fail-fast)
    }
}
