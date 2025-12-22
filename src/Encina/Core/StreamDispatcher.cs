using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// Handles stream request dispatching by resolving handlers and building streaming pipelines.
/// </summary>
internal static class StreamDispatcher
{
    private static readonly ConcurrentDictionary<(Type Request, Type Item), StreamRequestHandlerBase> StreamHandlerCache = new();

    public static async IAsyncEnumerable<Either<MediatorError, TItem>> ExecuteAsync<TItem>(
        Encina mediator,
        IStreamRequest<TItem> request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var itemType = typeof(TItem);

        using var activity = MediatorDiagnostics.StartStreamActivity(requestType, itemType);

        var handlerWrapper = StreamHandlerCache.GetOrAdd(
            (requestType, itemType),
            static key => CreateStreamHandlerWrapper(key.Request, key.Item));

        using var scope = mediator._scopeFactory.CreateScope();
        var handler = handlerWrapper.ResolveHandler(scope.ServiceProvider);

        if (handler is null)
        {
            Encina.Log.StreamHandlerMissing(mediator._logger, requestType.Name, itemType.Name);
            var error = MediatorErrors.Create(
                MediatorErrorCodes.HandlerMissing,
                $"No handler registered for {requestType.Name} -> IAsyncEnumerable<{itemType.Name}>.",
                details: new Dictionary<string, object?>
                {
                    ["requestType"] = requestType.FullName,
                    ["itemType"] = itemType.FullName
                });
            yield return Left<MediatorError, TItem>(error);
            yield break;
        }

        Encina.Log.ProcessingStreamRequest(mediator._logger, requestType.Name, handler.GetType().Name);

        var itemCount = 0;
        await foreach (var item in handlerWrapper.Handle(mediator, request, handler, scope.ServiceProvider, cancellationToken).ConfigureAwait(false))
        {
            itemCount++;
            // Unbox the item from object? back to TItem
            yield return item.Map(obj => (TItem)obj!);
        }

        Encina.Log.StreamCompleted(mediator._logger, requestType.Name, handler.GetType().Name, itemCount);
        MediatorDiagnostics.RecordStreamItemCount(activity, itemCount);
    }

    private static StreamRequestHandlerBase CreateStreamHandlerWrapper(Type requestType, Type itemType)
    {
        var wrapperType = typeof(StreamRequestHandlerWrapper<,>).MakeGenericType(requestType, itemType);
        return (StreamRequestHandlerBase)Activator.CreateInstance(wrapperType)!;
    }

    private abstract class StreamRequestHandlerBase
    {
        public abstract Type HandlerServiceType { get; }
        public abstract object? ResolveHandler(IServiceProvider provider);
        public abstract IAsyncEnumerable<Either<MediatorError, object?>> Handle(
            Encina mediator,
            object request,
            object handler,
            IServiceProvider provider,
            CancellationToken cancellationToken);
    }

    private sealed class StreamRequestHandlerWrapper<TRequest, TItem> : StreamRequestHandlerBase
        where TRequest : IStreamRequest<TItem>
    {
        private static readonly Type HandlerType = typeof(IStreamRequestHandler<TRequest, TItem>);

        public override Type HandlerServiceType => HandlerType;

        public override object? ResolveHandler(IServiceProvider provider)
            => provider.GetService(HandlerType);

        public override async IAsyncEnumerable<Either<MediatorError, object?>> Handle(
            Encina mediator,
            object request,
            object handler,
            IServiceProvider provider,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;
            var typedHandler = (IStreamRequestHandler<TRequest, TItem>)handler;
            var context = RequestContext.Create();
            var pipelineBuilder = new StreamPipelineBuilder<TRequest, TItem>(typedRequest, typedHandler, context, cancellationToken);
            var pipeline = pipelineBuilder.Build(provider);

            await foreach (var item in pipeline().ConfigureAwait(false))
            {
                // Box the item to return as object? (needed for abstract base class)
                yield return item.Map(i => (object?)i);
            }
        }
    }
}
