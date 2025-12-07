using LanguageExt;

namespace SimpleMediator;

/// <summary>
/// Representa un comando que fluye por el mediador.
/// </summary>
/// <typeparam name="TResponse">Tipo devuelto por el manejador cuando el comando concluye.</typeparam>
/// <remarks>
/// Los comandos suelen mutar estado o provocar efectos secundarios. Mantenga respuestas explícitas
/// (por ejemplo, <c>Unit</c> o DTOs de dominio) para que el mediador pueda encapsular los fallos
/// como <c>Either&lt;MediatorError, TResponse&gt;</c> dentro de la política Zero Exceptions.
/// </remarks>
/// <example>
/// <code>
/// public sealed record CreateReservation(Guid Id, ReservationDraft Draft) : ICommand&lt;Unit&gt;;
///
/// public sealed class CreateReservationHandler : ICommandHandler&lt;CreateReservation, Unit&gt;
/// {
///     public async Task&lt;Unit&gt; Handle(CreateReservation command, CancellationToken cancellationToken)
///     {
///         await reservations.SaveAsync(command.Id, command.Draft, cancellationToken);
///         await outbox.EnqueueAsync(command.Id, cancellationToken);
///         return Unit.Default;
///     }
/// }
/// </code>
/// </example>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Variante de comando que no devuelve un contenido específico.
/// </summary>
public interface ICommand : ICommand<Unit>
{
}

/// <summary>
/// Representa una consulta inmutable que produce un resultado.
/// </summary>
/// <typeparam name="TResponse">Tipo producido por el manejador al resolver la consulta.</typeparam>
/// <remarks>
/// Las consultas no deberían mutar estado. El patrón fomenta respuestas puras y caché fácil.
/// </remarks>
/// <example>
/// <code>
/// public sealed record GetAgendaById(Guid AgendaId) : IQuery&lt;Option&lt;AgendaReadModel&gt;&gt;;
///
/// public sealed class GetAgendaHandler : IQueryHandler&lt;GetAgendaById, Option&lt;AgendaReadModel&gt;&gt;
/// {
///     public Task&lt;Option&lt;AgendaReadModel&gt;&gt; Handle(GetAgendaById request, CancellationToken cancellationToken)
///     {
///         // Recuperar datos y proyectar el modelo de lectura.
///     }
/// }
/// </code>
/// </example>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Variante de consulta que no devuelve un valor concreto.
/// </summary>
public interface IQuery : IQuery<Unit>
{
}

/// <summary>
/// Maneja la ejecución de un comando concreto devolviendo una respuesta funcional.
/// </summary>
/// <typeparam name="TCommand">Tipo de comando atendido.</typeparam>
/// <typeparam name="TResponse">Tipo devuelto al completar el flujo.</typeparam>
/// <remarks>
/// Los manejadores deben ser puros siempre que sea posible, delegando efectos secundarios en
/// otras colaboraciones inyectadas.
/// </remarks>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Conveniencia para comandos que no retornan un valor adicional.
/// </summary>
public interface ICommandHandler<TCommand> : ICommandHandler<TCommand, Unit>
    where TCommand : ICommand<Unit>
{
}

/// <summary>
/// Maneja la ejecución de una consulta produciendo la respuesta solicitada.
/// </summary>
/// <typeparam name="TQuery">Tipo de consulta atendida.</typeparam>
/// <typeparam name="TResponse">Tipo devuelto al resolver la consulta.</typeparam>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}

/// <summary>
/// Conveniencia para consultas que no devuelven datos adicionales.
/// </summary>
public interface IQueryHandler<TQuery> : IQueryHandler<TQuery, Unit>
    where TQuery : IQuery<Unit>
{
}

/// <summary>
/// Especialización de <see cref="IPipelineBehavior{TRequest,TResponse}"/> para comandos.
/// </summary>
public interface ICommandPipelineBehavior<TCommand, TResponse> : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Variante para comandos sin respuesta específica.
/// </summary>
public interface ICommandPipelineBehavior<TCommand> : ICommandPipelineBehavior<TCommand, Unit>
    where TCommand : ICommand<Unit>
{
}

/// <summary>
/// Especialización de <see cref="IPipelineBehavior{TRequest,TResponse}"/> para consultas.
/// </summary>
public interface IQueryPipelineBehavior<TQuery, TResponse> : IPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}

/// <summary>
/// Variante para consultas sin respuesta concreta.
/// </summary>
public interface IQueryPipelineBehavior<TQuery> : IQueryPipelineBehavior<TQuery, Unit>
    where TQuery : IQuery<Unit>
{
}
