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

    public static async IAsyncEnumerable<Either<EncinaError, TItem>> ExecuteAsync<TItem>(
        Encina Encina,
        IStreamRequest<TItem> request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Type requestType = request.GetType();
        Type itemType = typeof(TItem);

        using Activity? activity = EncinaDiagnostics.StartStreamActivity(requestType, itemType);

        StreamRequestHandlerBase handlerWrapper = StreamHandlerCache.GetOrAdd(
            (requestType, itemType),
            static key => CreateStreamHandlerWrapper(key.Request, key.Item));

        using IServiceScope scope = Encina._scopeFactory.CreateScope();
        object? handler = handlerWrapper.ResolveHandler(scope.ServiceProvider);

        if (handler is null)
        {
            Encina.Log.StreamHandlerMissing(Encina._logger, requestType.Name, itemType.Name);
            EncinaError error = EncinaErrors.Create(
                EncinaErrorCodes.HandlerMissing,
                $"No handler registered for {requestType.Name} -> IAsyncEnumerable<{itemType.Name}>.",
                details: new Dictionary<string, object?>
                {
                    ["requestType"] = requestType.FullName,
                    ["itemType"] = itemType.FullName
                });
            yield return Left<EncinaError, TItem>(error);
            yield break;
        }

        Encina.Log.ProcessingStreamRequest(Encina._logger, requestType.Name, handler.GetType().Name);

        int itemCount = 0;
        await foreach (Either<EncinaError, object?> item in handlerWrapper.Handle(Encina, request, handler, scope.ServiceProvider, cancellationToken).ConfigureAwait(false))
        {
            itemCount++;
            // Unbox the item from object? back to TItem
            yield return item.Map(obj => (TItem)obj!);
        }

        Encina.Log.StreamCompleted(Encina._logger, requestType.Name, handler.GetType().Name, itemCount);
        EncinaDiagnostics.RecordStreamItemCount(activity, itemCount);
    }

    private static StreamRequestHandlerBase CreateStreamHandlerWrapper(Type requestType, Type itemType)
    {
        Type wrapperType = typeof(StreamRequestHandlerWrapper<,>).MakeGenericType(requestType, itemType);
        return (StreamRequestHandlerBase)Activator.CreateInstance(wrapperType)!;
    }

    private abstract class StreamRequestHandlerBase
    {
        public abstract Type HandlerServiceType { get; }
        public abstract object? ResolveHandler(IServiceProvider provider);
        public abstract IAsyncEnumerable<Either<EncinaError, object?>> Handle(
            Encina Encina,
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

        public override async IAsyncEnumerable<Either<EncinaError, object?>> Handle(
            Encina Encina,
            object request,
            object handler,
            IServiceProvider provider,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;
            var typedHandler = (IStreamRequestHandler<TRequest, TItem>)handler;
            IRequestContext context = RequestContext.Create();
            var pipelineBuilder = new StreamPipelineBuilder<TRequest, TItem>(typedRequest, typedHandler, context, cancellationToken);
            StreamHandlerCallback<TItem> pipeline = pipelineBuilder.Build(provider);

            await foreach (Either<EncinaError, TItem> item in pipeline().ConfigureAwait(false))
            {
                // Box the item to return as object? (needed for abstract base class)
                yield return item.Map(i => (object?)i);
            }
        }
    }
}
