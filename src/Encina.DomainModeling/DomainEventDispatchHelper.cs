using LanguageExt;

namespace Encina.DomainModeling;

/// <summary>
/// Helper class for dispatching collected domain events through <see cref="IEncina"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a convenient way to dispatch domain events collected by
/// <see cref="IDomainEventCollector"/>. It handles the iteration, type checking,
/// publishing, and cleanup of events in a single method call.
/// </para>
/// <para>
/// <b>Usage Pattern</b>:
/// <list type="number">
/// <item><description>Repository tracks aggregates via <see cref="IDomainEventCollector.TrackAggregate"/></description></item>
/// <item><description>After transaction commits, call <see cref="DispatchCollectedEventsAsync"/></description></item>
/// <item><description>Helper collects, publishes, and clears events automatically</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Error Handling</b>: If any event fails to publish, the helper accumulates errors
/// and returns them via <see cref="DomainEventDispatchErrors.CreateAggregateError"/>. Events are only cleared
/// if ALL events publish successfully.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DapperUnitOfWork
/// {
///     private readonly DomainEventDispatchHelper _dispatchHelper;
///
///     public async Task&lt;Either&lt;EncinaError, Unit&gt;&gt; CommitAsync(CancellationToken ct)
///     {
///         await _transaction.CommitAsync(ct);
///
///         // Dispatch all collected events
///         return await _dispatchHelper.DispatchCollectedEventsAsync(ct);
///     }
/// }
/// </code>
/// </example>
public sealed class DomainEventDispatchHelper
{
    private readonly IEncina _encina;
    private readonly IDomainEventCollector _collector;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatchHelper"/> class.
    /// </summary>
    /// <param name="encina">The Encina mediator for publishing notifications.</param>
    /// <param name="collector">The domain event collector.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public DomainEventDispatchHelper(IEncina encina, IDomainEventCollector collector)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(collector);

        _encina = encina;
        _collector = collector;
    }

    /// <summary>
    /// Dispatches all collected domain events through <see cref="IEncina.Publish{TNotification}"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see cref="Either{L,R}"/> with <see cref="Unit"/> on success,
    /// or <see cref="EncinaError"/> if any event fails to publish.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs the following steps:
    /// <list type="number">
    /// <item><description>Collects all events from tracked aggregates</description></item>
    /// <item><description>Filters to events implementing <see cref="INotification"/></description></item>
    /// <item><description>Publishes each event via <see cref="IEncina.Publish{TNotification}"/></description></item>
    /// <item><description>If ALL succeed, clears events from aggregates</description></item>
    /// <item><description>If ANY fail, returns aggregated error without clearing</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Non-INotification Events</b>: Domain events that don't implement
    /// <see cref="INotification"/> are silently skipped. This allows mixed event types
    /// where some are for internal use only.
    /// </para>
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> DispatchCollectedEventsAsync(
        CancellationToken cancellationToken = default)
    {
        var events = _collector.CollectEvents();

        if (events.Count == 0)
        {
            return Unit.Default;
        }

        var errors = new List<EncinaError>();

        foreach (var domainEvent in events)
        {
            // Only dispatch events that implement INotification
            if (domainEvent is not INotification notification)
            {
                continue;
            }

            var result = await _encina.Publish(notification, cancellationToken)
                .ConfigureAwait(false);

            result.IfLeft(error => errors.Add(error));
        }

        if (errors.Count > 0)
        {
            // Return aggregated error without clearing events (for retry)
            return DomainEventDispatchErrors.CreateAggregateError(errors);
        }

        // All events published successfully, clear them
        _collector.ClearCollectedEvents();

        return Unit.Default;
    }

    /// <summary>
    /// Dispatches all collected domain events, continuing on errors.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A result containing the count of successfully dispatched events and any errors.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="DispatchCollectedEventsAsync"/>, this method continues
    /// dispatching remaining events even if some fail. Events are always cleared
    /// after dispatch, regardless of success or failure.
    /// </para>
    /// <para>
    /// Use this method when you want best-effort dispatch and don't need to retry
    /// failed events.
    /// </para>
    /// </remarks>
    public async ValueTask<DomainEventDispatchResult> DispatchCollectedEventsWithContinuationAsync(
        CancellationToken cancellationToken = default)
    {
        var events = _collector.CollectEvents();

        if (events.Count == 0)
        {
            return new DomainEventDispatchResult(0, 0, []);
        }

        var errors = new List<DomainEventDispatchError>();
        var successCount = 0;
        var skippedCount = 0;

        foreach (var domainEvent in events)
        {
            if (domainEvent is not INotification notification)
            {
                skippedCount++;
                continue;
            }

            var result = await _encina.Publish(notification, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Right: _ => successCount++,
                Left: error => errors.Add(new DomainEventDispatchError(domainEvent, error)));
        }

        // Always clear events (best-effort dispatch)
        _collector.ClearCollectedEvents();

        return new DomainEventDispatchResult(successCount, skippedCount, errors);
    }
}

/// <summary>
/// Result of a domain event dispatch operation with continuation.
/// </summary>
/// <param name="SuccessCount">Number of events successfully dispatched.</param>
/// <param name="SkippedCount">Number of events skipped (not implementing INotification).</param>
/// <param name="Errors">List of errors for events that failed to dispatch.</param>
public sealed record DomainEventDispatchResult(
    int SuccessCount,
    int SkippedCount,
    IReadOnlyList<DomainEventDispatchError> Errors)
{
    /// <summary>
    /// Gets a value indicating whether all dispatchable events were successful.
    /// </summary>
    public bool IsSuccess => Errors.Count == 0;

    /// <summary>
    /// Gets the total number of events that were attempted to dispatch.
    /// </summary>
    public int TotalAttempted => SuccessCount + Errors.Count;

    /// <summary>
    /// Gets the total number of events processed (including skipped).
    /// </summary>
    public int TotalProcessed => SuccessCount + SkippedCount + Errors.Count;
}

/// <summary>
/// Represents a domain event that failed to dispatch.
/// </summary>
/// <param name="DomainEvent">The domain event that failed.</param>
/// <param name="Error">The error that occurred during dispatch.</param>
public sealed record DomainEventDispatchError(
    IDomainEvent DomainEvent,
    EncinaError Error);

/// <summary>
/// Factory methods for creating domain event dispatch errors.
/// </summary>
public static class DomainEventDispatchErrors
{
    /// <summary>
    /// Error code for aggregated domain event dispatch failures.
    /// </summary>
    public const string AggregateDispatchFailedCode = "DOMAIN_EVENT_DISPATCH_FAILED";

    /// <summary>
    /// Creates an aggregated error from multiple dispatch failures.
    /// </summary>
    /// <param name="errors">The individual errors.</param>
    /// <returns>An <see cref="EncinaError"/> containing all failure information.</returns>
    /// <remarks>
    /// The error includes metadata with:
    /// <list type="bullet">
    /// <item><description><c>ErrorCount</c>: Number of errors</description></item>
    /// <item><description><c>ErrorMessages</c>: Array of individual error messages</description></item>
    /// </list>
    /// </remarks>
    public static EncinaError CreateAggregateError(IReadOnlyList<EncinaError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            return EncinaErrors.Create(
                AggregateDispatchFailedCode,
                "No errors to aggregate");
        }

        if (errors.Count == 1)
        {
            return errors[0];
        }

        var errorMessages = errors.Select(e => e.Message).ToArray();
        var details = new Dictionary<string, object?>
        {
            ["ErrorCount"] = errors.Count,
            ["ErrorMessages"] = errorMessages
        };

        return EncinaErrors.Create(
            AggregateDispatchFailedCode,
            $"Failed to dispatch {errors.Count} domain event(s)",
            details: details);
    }
}
