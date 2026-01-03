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

        // Get pending events from the session
        var pendingEvents = _session.PendingChanges.Streams()
            .SelectMany(s => s.Events)
            .Select(e => e.Data)
            .OfType<INotification>()
            .ToList();

        if (pendingEvents.Count == 0)
        {
            return result;
        }

        Log.PublishingDomainEvents(_logger, pendingEvents.Count, typeof(TRequest).Name);

        // Publish each domain event
        foreach (var domainEvent in pendingEvents)
        {
            var publishResult = await _encina.Publish(domainEvent, cancellationToken).ConfigureAwait(false);

            if (publishResult.IsLeft)
            {
                var error = publishResult.Match(
                    Left: err => err,
                    Right: _ => EncinaErrors.Unknown);

                Log.FailedToPublishDomainEvent(_logger, domainEvent.GetType().Name, error.Message);

                return Left<EncinaError, TResponse>(
                    EncinaErrors.Create(
                        MartenErrorCodes.PublishEventsFailed,
                        $"Failed to publish domain event {domainEvent.GetType().Name}: {error.Message}"));
            }
        }

        Log.PublishedDomainEvents(_logger, pendingEvents.Count, typeof(TRequest).Name);

        return result;
    }
}
