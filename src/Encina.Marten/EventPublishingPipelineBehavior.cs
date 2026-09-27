using LanguageExt;
using Marten;
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
        // Execute the command
        var result = await nextStep().ConfigureAwait(false);

        // If the command failed or auto-publish is disabled, return early
        if (result.IsLeft || !_options.AutoPublishDomainEvents)
        {
            return result;
        }

        var pendingEvents = GetPendingNotifications();
        if (pendingEvents.Count == 0)
        {
            return result;
        }

        Log.PublishingDomainEvents(_logger, pendingEvents.Count, typeof(TRequest).Name);

        // Publish each domain event
        foreach (var domainEvent in pendingEvents)
        {
            var publishError = await PublishEventAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            if (publishError is { } error)
            {
                return Left<EncinaError, TResponse>(error); // NOSONAR S6966: LanguageExt Left is a pure function
            }
        }

        Log.PublishedDomainEvents(_logger, pendingEvents.Count, typeof(TRequest).Name);

        return result;
    }

    /// <summary>
    /// Gets the pending domain-event notifications recorded on the session since the last save.
    /// </summary>
    private List<INotification> GetPendingNotifications() =>
        _session.PendingChanges.Streams()
            .SelectMany(s => s.Events)
            .Select(e => e.Data)
            .OfType<INotification>()
            .ToList();

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
            $"Failed to publish domain event {domainEvent.GetType().Name}: {error.Message}");
    }
}
