using LanguageExt;
using Marten;
using Marten.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Marten;

/// <summary>
/// Pipeline behavior that publishes domain events from aggregates after successful command execution.
/// </summary>
/// <typeparam name="TRequest">The request type (must be a command).</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class EventPublishingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IDocumentSession _session;
    private readonly IEncina _encina;
    private readonly ILogger<EventPublishingPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly EncinaMartenOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventPublishingPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="session">The Marten document session.</param>
    /// <param name="encina">The Encina for publishing events.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public EventPublishingPipelineBehavior(
        IDocumentSession session,
        IEncina encina,
        ILogger<EventPublishingPipelineBehavior<TRequest, TResponse>> logger,
        IOptions<EncinaMartenOptions> options)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _session = session;
        _encina = encina;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // When auto-publish is disabled the behavior is a pass-through.
        if (!_options.AutoPublishDomainEvents)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // The aggregate repository commits the session inside the handler, so by the time
        // nextStep returns the events are no longer pending: capture them as they are committed.
        var collector = TryAttachCollector();
        if (collector is null)
        {
            // Another instance of this behavior already owns the session's collector (the same
            // session reached this behavior again); only the owner publishes, once, when it finishes.
            return await nextStep().ConfigureAwait(false);
        }

        try
        {
            // Execute the command
            var result = await nextStep().ConfigureAwait(false);

            return await PublishAfterSuccessAsync(result, collector, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _session.Listeners.Remove(collector);
        }
    }

    /// <summary>
    /// Publishes the domain events committed during a successful command; a failed
    /// command publishes nothing and a failed publication fails the command.
    /// </summary>
    private async ValueTask<Either<EncinaError, TResponse>> PublishAfterSuccessAsync(
        Either<EncinaError, TResponse> result,
        CommittedEventCollector collector,
        CancellationToken cancellationToken)
    {
        if (result.IsLeft)
        {
            return result;
        }

        // Only events that a commit made durable are published; events appended to the session but
        // never saved are not in the stream and are not published.
        var pendingEvents = collector.Drain();
        if (pendingEvents.Count == 0)
        {
            return result;
        }

        var publishError = await PublishPendingEventsAsync(pendingEvents, cancellationToken)
            .ConfigureAwait(false);

        // NOSONAR S6966: LanguageExt Left is a pure function
        return publishError is { } error ? Left<EncinaError, TResponse>(error) : result;
    }

    /// <summary>
    /// Attaches a collector to the session, or returns <see langword="null"/> when one is already
    /// attached by another instance of this behavior.
    /// </summary>
    private CommittedEventCollector? TryAttachCollector()
    {
        // CommittedEventCollector is not generic, so one collector is shared by every command type
        // that reaches the same session.
        if (_session.Listeners.OfType<CommittedEventCollector>().Any())
        {
            return null;
        }

        var created = new CommittedEventCollector();
        _session.Listeners.Add(created);
        return created;
    }

    /// <summary>
    /// Publishes every pending domain event in order, stopping at the first failure.
    /// </summary>
    /// <returns><see langword="null"/> when every event published; otherwise the first failure.</returns>
    private async ValueTask<EncinaError?> PublishPendingEventsAsync(
        List<INotification> pendingEvents, CancellationToken cancellationToken)
    {
        Log.PublishingDomainEvents(_logger, pendingEvents.Count, typeof(TRequest).Name);

        foreach (var domainEvent in pendingEvents)
        {
            var publishError = await PublishEventAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            if (publishError is not null)
            {
                return publishError;
            }
        }

        Log.PublishedDomainEvents(_logger, pendingEvents.Count, typeof(TRequest).Name);

        return null;
    }

    /// <summary>
    /// Publishes a single domain event and, on failure, logs only the error code (never
    /// <see cref="EncinaError.Message"/>) and returns the wrapped error to report upstream.
    /// </summary>
    /// <returns><see langword="null"/> when the publish succeeded; otherwise the error to return.</returns>
    private async ValueTask<EncinaError?> PublishEventAsync(INotification domainEvent, CancellationToken cancellationToken)
    {
        var publishResult = await _encina.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
        if (publishResult.IsRight)
        {
            return null;
        }

        var error = publishResult.Match(
            Left: err => err,
            Right: _ => EncinaErrors.Unknown);

        Log.FailedToPublishDomainEvent(_logger, domainEvent.GetType().Name, error.GetEncinaCode());

        return EncinaErrors.Create(
            MartenErrorCodes.PublishEventsFailed,
            $"Failed to publish domain event {domainEvent.GetType().Name} (error code {error.GetEncinaCode()}).");
    }
}
