namespace SimpleMediator;

/// <summary>
/// Represents a request routed to a single handler that produces a response.
/// </summary>
/// <typeparam name="TResponse">Type produced by the handler when the flow completes.</typeparam>
/// <remarks>
/// Typical implementations are <see cref="ICommand{TResponse}"/> and <see cref="IQuery{TResponse}"/>.
/// Prefer immutable classes or records so requests remain deterministic and easy to test.
/// </remarks>
/// <example>
/// <code>
/// public sealed record GetInvoiceById(Guid InvoiceId) : IRequest&lt;Option&lt;InvoiceReadModel&gt;&gt;;
/// </code>
/// </example>
public interface IRequest<out TResponse>
{
}
