using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LanguageExt;
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
        var requestType = request.GetType();
        var itemType = typeof(TItem);

        using var activity = EncinaDiagnostics.StartStreamActivity(requestType, itemType);

        var handlerWrapper = StreamHandlerCache.GetOrAdd(
            (requestType, itemType),
            static key => CreateStreamHandlerWrapper(key.Request, key.Item));

        using var scope = Encina._scopeFactory.CreateScope();
        var handler = handlerWrapper.ResolveHandler(scope.ServiceProvider);

        if (handler is null)
        {
            Encina.Log.StreamHandlerMissing(Encina._logger, requestType.Name, itemType.Name);
            var error = EncinaErrors.Create(
                EncinaErrorCodes.HandlerMissing,
                $"No handler registered for {requestType.Name} -> IAsyncEnumerable<{itemType.Name}>.",
                details: new Dictionary<string, object?>
                {
                    ["requestType"] = requestType.FullName,
                    ["itemType"] = itemType.FullName
                });
            yield return Left<EncinaError, TItem>(error); // NOSONAR S6966: LanguageExt Left is a pure function
            yield break;
        }

        Encina.Log.ProcessingStreamRequest(Encina._logger, requestType.Name, handler.GetType().Name);

        var itemCount = 0;
        await foreach (var item in handlerWrapper.Handle(Encina, request, handler, scope.ServiceProvider, cancellationToken).ConfigureAwait(false))
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
        var wrapperType = typeof(StreamRequestHandlerWrapper<,>).MakeGenericType(requestType, itemType);
        return (StreamRequestHandlerBase)Activator.CreateInstance(wrapperType)!;
    }

    private abstract class StreamRequestHandlerBase
    {
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
