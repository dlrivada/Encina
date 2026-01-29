using Encina.DomainModeling;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.DomainEvents;

/// <summary>
/// EF Core interceptor that automatically dispatches domain events after SaveChanges completes.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor collects domain events from all tracked entities that inherit from
/// <see cref="Entity{TId}"/> and dispatches them through <see cref="IEncina"/> after
/// the database transaction has been successfully committed.
/// </para>
/// <para>
/// <b>Event Dispatch Order</b>:
/// <list type="number">
/// <item><description>SaveChanges is called on the DbContext</description></item>
/// <item><description>Database changes are persisted</description></item>
/// <item><description>This interceptor is invoked in <c>SavedChangesAsync</c></description></item>
/// <item><description>Domain events are collected from all tracked entities</description></item>
/// <item><description>Each event is published via <see cref="IEncina.Publish{TNotification}"/></description></item>
/// <item><description>Events are cleared from entities (if <see cref="DomainEventDispatcherOptions.ClearEventsAfterDispatch"/> is true)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Why After SaveChanges?</b>: Events should only be dispatched after the aggregate state
/// has been successfully persisted. If dispatched before, and the save fails, subscribers
/// would act on changes that never actually happened.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register the interceptor
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseDomainEvents = true;
/// });
///
/// // In your DbContext
/// public class AppDbContext : DbContext
/// {
///     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
///     {
///         // Interceptor is added automatically via AddInterceptors
///     }
/// }
///
/// // In your aggregate
/// public class Order : AggregateRoot&lt;OrderId&gt;
/// {
///     public void Place()
///     {
///         Status = OrderStatus.Placed;
///         RaiseDomainEvent(new OrderPlacedEvent(Id));
///     }
/// }
///
/// // The event is published automatically after SaveChangesAsync
/// await dbContext.SaveChangesAsync();
/// </code>
/// </example>
public sealed class DomainEventDispatcherInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DomainEventDispatcherOptions _options;
    private readonly ILogger<DomainEventDispatcherInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatcherInterceptor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving IEncina.</param>
    /// <param name="options">The dispatcher options.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public DomainEventDispatcherInterceptor(
        IServiceProvider serviceProvider,
        DomainEventDispatcherOptions options,
        ILogger<DomainEventDispatcherInterceptor> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (_options.Enabled && eventData.Context is not null)
        {
            // Block on async to maintain sync signature
            DispatchDomainEventsAsync(eventData.Context, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_options.Enabled && eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken)
                .ConfigureAwait(false);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Dispatches all domain events from tracked entities.
    /// </summary>
    private async Task DispatchDomainEventsAsync(
        Microsoft.EntityFrameworkCore.DbContext context,
        CancellationToken cancellationToken)
    {
        // Get all entities that have domain events
        var entitiesWithEvents = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithEvents.Count == 0)
        {
            return;
        }

        // Resolve IEncina from the service provider
        var encina = _serviceProvider.GetRequiredService<IEncina>();

        Log.DispatchingDomainEvents(_logger, entitiesWithEvents.Sum(e => e.DomainEvents.Count));

        foreach (var entity in entitiesWithEvents)
        {
            // Take a snapshot of events to iterate (in case collection is modified)
            var events = entity.DomainEvents.ToList();

            foreach (var domainEvent in events)
            {
                // Check if event implements INotification
                if (domainEvent is not INotification notification)
                {
                    if (_options.RequireINotification)
                    {
                        Log.DomainEventNotNotification(
                            _logger,
                            domainEvent.GetType().FullName ?? domainEvent.GetType().Name);
                        continue;
                    }

                    // Skip non-INotification events if not required
                    Log.SkippingNonNotificationEvent(
                        _logger,
                        domainEvent.GetType().FullName ?? domainEvent.GetType().Name);
                    continue;
                }

                try
                {
                    var publishResult = await encina.Publish(notification, cancellationToken)
                        .ConfigureAwait(false);

                    publishResult.Match(
                        Right: _ => Log.DomainEventPublished(
                            _logger,
                            domainEvent.GetType().Name,
                            domainEvent.EventId),
                        Left: error =>
                        {
                            Log.DomainEventPublishFailed(
                                _logger,
                                domainEvent.GetType().Name,
                                domainEvent.EventId,
                                error.Message);

                            if (_options.StopOnFirstError)
                            {
                                throw new DomainEventDispatchException(
                                    $"Failed to dispatch domain event {domainEvent.GetType().Name}: {error.Message}",
                                    domainEvent,
                                    error);
                            }
                        });
                }
                catch (DomainEventDispatchException)
                {
                    // Re-throw our own exception
                    throw;
                }
                catch (Exception ex)
                {
                    Log.DomainEventPublishException(
                        _logger,
                        ex,
                        domainEvent.GetType().Name,
                        domainEvent.EventId);

                    if (_options.StopOnFirstError)
                    {
                        throw new DomainEventDispatchException(
                            $"Exception while dispatching domain event {domainEvent.GetType().Name}",
                            domainEvent,
                            ex);
                    }
                }
            }

            // Clear events from entity after dispatching
            if (_options.ClearEventsAfterDispatch)
            {
                entity.ClearDomainEvents();
            }
        }

        Log.DomainEventsDispatchCompleted(
            _logger,
            entitiesWithEvents.Sum(e => e.DomainEvents.Count));
    }
}

/// <summary>
/// Exception thrown when a domain event fails to dispatch.
/// </summary>
/// <remarks>
/// This exception is only thrown when <see cref="DomainEventDispatcherOptions.StopOnFirstError"/>
/// is set to <see langword="true"/> and an error occurs during event dispatch.
/// </remarks>
public sealed class DomainEventDispatchException : Exception
{
    /// <summary>
    /// Gets the domain event that failed to dispatch.
    /// </summary>
    public IDomainEvent DomainEvent { get; }

    /// <summary>
    /// Gets the Encina error that caused the failure, if any.
    /// </summary>
    public EncinaError? EncinaError { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatchException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="domainEvent">The domain event that failed.</param>
    /// <param name="encinaError">The Encina error.</param>
    public DomainEventDispatchException(string message, IDomainEvent domainEvent, EncinaError encinaError)
        : base(message)
    {
        DomainEvent = domainEvent;
        EncinaError = encinaError;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatchException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="domainEvent">The domain event that failed.</param>
    /// <param name="innerException">The inner exception.</param>
    public DomainEventDispatchException(string message, IDomainEvent domainEvent, Exception innerException)
        : base(message, innerException)
    {
        DomainEvent = domainEvent;
    }
}
