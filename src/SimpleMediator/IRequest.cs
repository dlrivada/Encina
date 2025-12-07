namespace SimpleMediator;

/// <summary>
/// Representa una solicitud direccionada a un único handler que devuelve una respuesta.
/// </summary>
/// <typeparam name="TResponse">Tipo que producirá el manejador al completar el flujo.</typeparam>
/// <remarks>
/// Las implementaciones habituales son <see cref="ICommand{TResponse}"/> y
/// <see cref="IQuery{TResponse}"/>. Use clases o records inmutables para facilitar pruebas.
/// </remarks>
public interface IRequest<out TResponse>
{
}
