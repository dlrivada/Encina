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

    public RequestHandlerCallback<TResponse> Build(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>()?.ToArray()
                        ?? System.Array.Empty<IPipelineBehavior<TRequest, TResponse>>();
        var preProcessors = serviceProvider.GetServices<IRequestPreProcessor<TRequest>>()?.ToArray()
                         ?? System.Array.Empty<IRequestPreProcessor<TRequest>>();
        var postProcessors = serviceProvider.GetServices<IRequestPostProcessor<TRequest, TResponse>>()?.ToArray()
                          ?? System.Array.Empty<IRequestPostProcessor<TRequest, TResponse>>();

        RequestHandlerCallback<TResponse> current = () => ExecuteHandlerAsync(_handler, _request, _cancellationToken);

        if (behaviors.Length > 0)
        {
            for (var index = behaviors.Length - 1; index >= 0; index--)
            {
                var behavior = behaviors[index];
                var nextStep = current;
                current = () => ExecuteBehaviorAsync(behavior, _request, nextStep, _cancellationToken);
            }
        }

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

    private static async ValueTask<Either<MediatorError, TResponse>> ExecuteHandlerAsync(
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var task = handler.Handle(request, cancellationToken);
            if (task is null)
            {
                var message = $"Handler {handler.GetType().Name} returned a null task while processing {typeof(TRequest).Name}.";
                var exception = new InvalidOperationException(message);
                var metadata = new Dictionary<string, object?>
                {
                    ["handler"] = handler.GetType().FullName,
                    ["request"] = typeof(TRequest).FullName,
                    ["stage"] = "handler_null_task"
                };
                var error = MediatorErrors.FromException(MediatorErrorCodes.HandlerException, exception, message, metadata);
                return Left<MediatorError, TResponse>(error);
            }

            var result = await task.ConfigureAwait(false);
            return Right<MediatorError, TResponse>(result);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Handler {handler.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            var metadata = new Dictionary<string, object?>
            {
                ["handler"] = handler.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "handler"
            };
            return Left<MediatorError, TResponse>(MediatorErrors.Create(MediatorErrorCodes.HandlerCancelled, message, ex, metadata));
        }
        catch (Exception ex)
        {
            var metadata = new Dictionary<string, object?>
            {
                ["handler"] = handler.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "handler"
            };
            var error = MediatorErrors.FromException(MediatorErrorCodes.HandlerException, ex, $"Error running {handler.GetType().Name} for {typeof(TRequest).Name}.", metadata);
            return Left<MediatorError, TResponse>(error);
        }
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
        catch (Exception ex)
        {
            var metadata = new Dictionary<string, object?>
            {
                ["behavior"] = behavior.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "behavior"
            };
            var error = MediatorErrors.FromException(MediatorErrorCodes.BehaviorException, ex, $"Error running {behavior.GetType().Name} for {typeof(TRequest).Name}.", metadata);
            return Left<MediatorError, TResponse>(error);
        }
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
        catch (Exception ex)
        {
            var preExceptionMetadata = new Dictionary<string, object?>
            {
                ["preProcessor"] = preProcessor.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "preprocessor"
            };
            var error = MediatorErrors.FromException(MediatorErrorCodes.PreProcessorException, ex, $"Error running {preProcessor.GetType().Name} for {typeof(TRequest).Name}.", preExceptionMetadata);
            return Some(error);
        }
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
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
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

            var postExceptionMetadata = new Dictionary<string, object?>
            {
                ["postProcessor"] = postProcessor.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "postprocessor"
            };
            var error = MediatorErrors.FromException(MediatorErrorCodes.PostProcessorException, ex, $"Error running {postProcessor.GetType().Name} for {typeof(TRequest).Name}.", postExceptionMetadata);
            return Some(error);
        }
        catch (Exception ex)
        {
            var postFailureMetadata = new Dictionary<string, object?>
            {
                ["postProcessor"] = postProcessor.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "postprocessor"
            };
            var error = MediatorErrors.FromException(MediatorErrorCodes.PostProcessorException, ex, $"Error running {postProcessor.GetType().Name} for {typeof(TRequest).Name}.", postFailureMetadata);
            return Some(error);
        }
    }
}
