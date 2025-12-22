using System.Collections.Concurrent;
using System.Threading.Channels;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.InMemory;

/// <summary>
/// In-memory implementation of the message bus using System.Threading.Channels.
/// </summary>
public sealed class InMemoryMessageBus : IInMemoryMessageBus, IDisposable
{
    private readonly Channel<object> _channel;
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ILogger<InMemoryMessageBus> _logger;
    private readonly EncinaInMemoryOptions _options;
    private readonly CancellationTokenSource _cts = new();
    private int _pendingCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryMessageBus"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public InMemoryMessageBus(
        ILogger<InMemoryMessageBus> logger,
        IOptions<EncinaInMemoryOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _options = options.Value;

        if (_options.UseUnboundedChannel)
        {
            _channel = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = _options.AllowSynchronousContinuations,
                SingleReader = false,
                SingleWriter = false
            });
        }
        else
        {
            var fullMode = _options.FullMode switch
            {
                InMemoryFullMode.Wait => BoundedChannelFullMode.Wait,
                InMemoryFullMode.DropOldest => BoundedChannelFullMode.DropOldest,
                InMemoryFullMode.DropNewest => BoundedChannelFullMode.DropNewest,
                _ => BoundedChannelFullMode.Wait
            };

            _channel = Channel.CreateBounded<object>(new BoundedChannelOptions(_options.BoundedCapacity)
            {
                AllowSynchronousContinuations = _options.AllowSynchronousContinuations,
                FullMode = fullMode,
                SingleReader = false,
                SingleWriter = false
            });
        }

        // Start background workers
        for (var i = 0; i < _options.WorkerCount; i++)
        {
            _ = ProcessMessagesAsync(_cts.Token);
        }

        Log.MessageBusStarted(_logger, _options.WorkerCount);
    }

    /// <inheritdoc />
    public int PendingCount => _pendingCount;

    /// <inheritdoc />
    public int SubscriberCount => _subscribers.Values.Sum(list => list.Count);

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            Log.PublishingMessage(_logger, typeof(TMessage).Name);

            if (_subscribers.TryGetValue(typeof(TMessage), out var handlers))
            {
                foreach (var handler in handlers.ToArray())
                {
                    if (handler is Func<TMessage, ValueTask> typedHandler)
                    {
                        await typedHandler(message).ConfigureAwait(false);
                    }
                }
            }

            Log.SuccessfullyPublishedMessage(_logger, typeof(TMessage).Name);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToPublishMessage(_logger, ex, typeof(TMessage).Name);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "INMEMORY_PUBLISH_FAILED",
                    ex,
                    $"Failed to publish message of type {typeof(TMessage).Name}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> EnqueueAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            Log.EnqueuingMessage(_logger, typeof(TMessage).Name);

            await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _pendingCount);

            Log.SuccessfullyEnqueuedMessage(_logger, typeof(TMessage).Name);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToEnqueueMessage(_logger, ex, typeof(TMessage).Name);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "INMEMORY_ENQUEUE_FAILED",
                    ex,
                    $"Failed to enqueue message of type {typeof(TMessage).Name}."));
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TMessage>(Func<TMessage, ValueTask> handler)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var handlers = _subscribers.GetOrAdd(typeof(TMessage), _ => []);
        lock (handlers)
        {
            handlers.Add(handler);
        }

        Log.SubscribedToMessages(_logger, typeof(TMessage).Name);

        return new Subscription<TMessage>(this, handler);
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                Interlocked.Decrement(ref _pendingCount);
                var messageType = message.GetType();

                if (_subscribers.TryGetValue(messageType, out var handlers))
                {
                    foreach (var handler in handlers.ToArray())
                    {
                        try
                        {
                            // Use reflection to invoke the typed handler
                            var invokeMethod = handler.GetType().GetMethod("Invoke");
                            if (invokeMethod?.Invoke(handler, [message]) is ValueTask task)
                            {
                                await task.ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorProcessingMessage(_logger, ex, messageType.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorProcessingQueuedMessage(_logger, ex);
            }
        }
    }

    private void Unsubscribe<TMessage>(Func<TMessage, ValueTask> handler)
        where TMessage : class
    {
        if (_subscribers.TryGetValue(typeof(TMessage), out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cts.Cancel();
        _channel.Writer.Complete();
        _cts.Dispose();
    }

    private sealed class Subscription<TMessage> : IDisposable
        where TMessage : class
    {
        private readonly InMemoryMessageBus _bus;
        private readonly Func<TMessage, ValueTask> _handler;

        public Subscription(InMemoryMessageBus bus, Func<TMessage, ValueTask> handler)
        {
            _bus = bus;
            _handler = handler;
        }

        public void Dispose()
        {
            _bus.Unsubscribe(_handler);
        }
    }
}
