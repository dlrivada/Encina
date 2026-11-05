using System.Collections.Concurrent;
using Encina.DomainModeling;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.DomainEvents;

/// <summary>
/// Aggregate of the <c>DomainEventDispatcherInterceptor</c> integration scenarios. It derives from
/// <see cref="AggregateRoot{TId}"/> because the interceptor only collects <see cref="IAggregateRoot"/> entries.
/// </summary>
public sealed class DomainEventTestAggregate : AggregateRoot<Guid>
{
    /// <summary>Initializes a new instance; the parameterless constructor is for EF Core.</summary>
    private DomainEventTestAggregate()
        : base(Guid.Empty)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="id">The aggregate id.</param>
    /// <param name="name">The aggregate name, unique in the table.</param>
    public DomainEventTestAggregate(Guid id, string name)
        : base(id)
    {
        Name = name;
    }

    /// <summary>Gets the unique name (a duplicate makes the save fail).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Raises <paramref name="domainEvent"/> through the protected API of the aggregate root.</summary>
    /// <param name="domainEvent">The event to raise.</param>
    public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
}

/// <summary>A domain event that is also a notification, so the interceptor publishes it.</summary>
/// <param name="AggregateId">The aggregate that raised it.</param>
/// <param name="Sequence">The position within the aggregate's raise order.</param>
public sealed record NoteRaised(Guid AggregateId, int Sequence) : DomainEvent, INotification;

/// <summary>A domain event whose handler fails according to the configured <see cref="HandlerBehavior"/>.</summary>
/// <param name="AggregateId">The aggregate that raised it.</param>
public sealed record PoisonRaised(Guid AggregateId) : DomainEvent, INotification;

/// <summary>What a handler built by <see cref="RecordingHandlers"/> does after recording.</summary>
public enum HandlerBehavior
{
    /// <summary>Returns Right.</summary>
    Succeed,

    /// <summary>Returns a Left.</summary>
    ReturnLeft,

    /// <summary>Throws an exception.</summary>
    Throw
}

/// <summary>Records every event a handler received, in arrival order.</summary>
public sealed class DomainEventRecorder
{
    private readonly ConcurrentQueue<IDomainEvent> _events = new();

    /// <summary>Gets the recorded events in arrival order.</summary>
    public IReadOnlyList<IDomainEvent> Events => [.. _events];

    /// <summary>Records <paramref name="domainEvent"/>.</summary>
    /// <param name="domainEvent">The event.</param>
    public void Record(IDomainEvent domainEvent) => _events.Enqueue(domainEvent);
}

/// <summary>
/// Builds notification handlers that record the event and then behave as configured. The handlers are
/// NSubstitute proxies created at run time, not classes of this assembly, so the assembly scanning that other
/// tests of this assembly run (<c>AddEncina</c> with this assembly) never discovers them.
/// </summary>
public static class RecordingHandlers
{
    /// <summary>Creates a handler for <typeparamref name="TEvent"/>.</summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="recorder">The recorder.</param>
    /// <param name="behavior">What to do after recording.</param>
    /// <returns>The handler.</returns>
    public static INotificationHandler<TEvent> Create<TEvent>(DomainEventRecorder recorder, HandlerBehavior behavior)
        where TEvent : class, IDomainEvent, INotification
    {
        var handler = Substitute.For<INotificationHandler<TEvent>>();
        handler.Handle(Arg.Any<TEvent>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                recorder.Record(call.Arg<TEvent>());

                return behavior switch
                {
                    HandlerBehavior.ReturnLeft => Task.FromResult(
                        Left<EncinaError, Unit>(EncinaErrors.Create("test.domain-event-handler", "handler failed"))),
                    HandlerBehavior.Throw => throw new InvalidOperationException("handler threw"),
                    _ => Task.FromResult(Right<EncinaError, Unit>(unit))
                };
            });
        return handler;
    }
}

/// <summary>Test context with the single aggregate table of the scenarios.</summary>
public sealed class DomainEventTestDbContext : DbContext
{
    /// <summary>The table, named so no other test class of the shared database collides with it.</summary>
    public const string TableName = "DomainEventDispatchAggregates";

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options.</param>
    public DomainEventTestDbContext(DbContextOptions<DomainEventTestDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the aggregates.</summary>
    public DbSet<DomainEventTestAggregate> Aggregates => Set<DomainEventTestAggregate>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DomainEventTestAggregate>(entity =>
        {
            entity.ToTable(TableName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.RowVersion);
        });
    }
}
