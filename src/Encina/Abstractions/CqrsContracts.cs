using LanguageExt;

namespace Encina;

/// <summary>
/// Represents a command flowing through the Encina.
/// </summary>
/// <typeparam name="TResponse">Type returned by the handler when the command finishes.</typeparam>
/// <remarks>
/// Commands typically mutate state or trigger side effects. Keep responses explicit (for example
/// <c>Unit</c> or domain DTOs) so the Encina can wrap failures in
/// <c>Either&lt;EncinaError, TResponse&gt;</c> while honoring the Zero Exceptions policy.
/// </remarks>
/// <example>
/// <code>
/// public sealed record CreateReservation(Guid Id, ReservationDraft Draft) : ICommand&lt;Unit&gt;;
///
/// public sealed class CreateReservationHandler : ICommandHandler&lt;CreateReservation, Unit&gt;
/// {
///     public async Task&lt;Unit&gt; Handle(CreateReservation command, CancellationToken cancellationToken)
///     {
///         await reservations.SaveAsync(command.Id, command.Draft, cancellationToken).ConfigureAwait(false);
///         await outbox.EnqueueAsync(command.Id, cancellationToken).ConfigureAwait(false);
///         return Unit.Default;
///     }
/// }
/// </code>
/// </example>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Convenience command variant that does not return an explicit payload.
/// </summary>
public interface ICommand : ICommand<Unit>
{
}

/// <summary>
/// Represents an immutable query that produces a result.
/// </summary>
/// <typeparam name="TResponse">Type produced by the handler when the query is resolved.</typeparam>
/// <remarks>
/// Queries should avoid mutating state. This pattern encourages pure responses that are easy to cache.
/// </remarks>
/// <example>
/// <code>
/// public sealed record GetAgendaById(Guid AgendaId) : IQuery&lt;Option&lt;AgendaReadModel&gt;&gt;;
///
/// public sealed class GetAgendaHandler : IQueryHandler&lt;GetAgendaById, Option&lt;AgendaReadModel&gt;&gt;
/// {
///     public Task&lt;Option&lt;AgendaReadModel&gt;&gt; Handle(GetAgendaById request, CancellationToken cancellationToken)
///     {
///         // Look up the record and project it to the read model.
///     }
/// }
/// </code>
/// </example>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Convenience query variant that does not return a concrete value.
/// </summary>
public interface IQuery : IQuery<Unit>
{
}

/// <summary>
/// Handles a concrete command and returns a functional response.
/// </summary>
/// <typeparam name="TCommand">Type of command being handled.</typeparam>
/// <typeparam name="TResponse">Type returned once the flow completes.</typeparam>
/// <remarks>
/// Handlers should be pure where possible, delegating side effects to injected collaborators.
/// </remarks>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Convenience interface for commands that do not return an additional value.
/// </summary>
public interface ICommandHandler<TCommand> : ICommandHandler<TCommand, Unit>
    where TCommand : ICommand<Unit>
{
}

/// <summary>
/// Handles a query and produces the expected response.
/// </summary>
/// <typeparam name="TQuery">Type of query being handled.</typeparam>
/// <typeparam name="TResponse">Type returned once the query completes.</typeparam>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}

/// <summary>
/// Convenience interface for queries that do not return additional data.
/// </summary>
public interface IQueryHandler<TQuery> : IQueryHandler<TQuery, Unit>
    where TQuery : IQuery<Unit>
{
}

/// <summary>
/// Specialises <see cref="IPipelineBehavior{TRequest,TResponse}"/> for commands.
/// </summary>
public interface ICommandPipelineBehavior<TCommand, TResponse> : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Variant for commands without a specific response payload.
/// </summary>
public interface ICommandPipelineBehavior<TCommand> : ICommandPipelineBehavior<TCommand, Unit>
    where TCommand : ICommand<Unit>
{
}

/// <summary>
/// Specialises <see cref="IPipelineBehavior{TRequest,TResponse}"/> for queries.
/// </summary>
public interface IQueryPipelineBehavior<TQuery, TResponse> : IPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}

/// <summary>
/// Variant for queries that do not return a concrete value.
/// </summary>
public interface IQueryPipelineBehavior<TQuery> : IQueryPipelineBehavior<TQuery, Unit>
    where TQuery : IQuery<Unit>
{
}
