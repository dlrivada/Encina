using System.Threading.Tasks;

namespace SimpleMediator;

/// <summary>
/// Representa la continuación del pipeline en un behavior.
/// </summary>
/// <typeparam name="TResponse">Tipo devuelto por el handler final.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
